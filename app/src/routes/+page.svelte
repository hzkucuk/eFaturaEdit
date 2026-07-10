<script lang="ts">
  import { onMount, onDestroy } from 'svelte';
  import { goto } from '$app/navigation';
  import { invoke } from '@tauri-apps/api/core';
  import { getCurrentWindow } from '@tauri-apps/api/window';
  import { snippets, samples, completion, manifest, groupSnippetsByCategory } from '$lib/data';
  import type { Snippet } from '$lib/data/types';
  import { transformXml, validateXml, XsltError } from '$lib/xslt';
  import { settings, updatePanelSize, updateSetting, themeKind } from '$lib/settings.svelte';
  import { editorState } from '$lib/editor-state.svelte';
  import { openFile, saveFile, saveFileAs, reopenFile } from '$lib/fileio';
  import { recentFiles, pushRecent, clearRecent, basename } from '$lib/recent-files.svelte';
  import { dragState, beginPossibleDrag } from '$lib/drag.svelte';
  import {
    listUserSamples,
    loadUserSample,
    addSamplePair,
    removeUserSample,
    openSamplesFolder,
    type UserSample,
  } from '$lib/user-samples';
  import { listUserSnippets, saveUserSnippet, removeUserSnippet } from '$lib/user-snippets';
  import { ask } from '@tauri-apps/plugin-dialog';
  import SnippetEditor from '$lib/SnippetEditor.svelte';
  import CodeEditor from '$lib/CodeEditor.svelte';
  import Splitter from '$lib/Splitter.svelte';
  import ContextMenu from '$lib/ContextMenu.svelte';
  import HelpModal from '$lib/HelpModal.svelte';
  import type { Completion } from '@codemirror/autocomplete';

  // ─── UI state ────────────────────────────────────────────────────────
  let statusMsg = $state('Hazır.');
  let statusIsError = $state(false);
  let activeCategory = $state<string>('HTML Öğeleri');
  let snippetFilter = $state('');
  let sampleMenuOpen = $state(false);
  let userSamples = $state<UserSample[]>([]);
  let userSnippets = $state<Snippet[]>([]);
  let snippetEditorOpen = $state(false);
  let editingSnippet = $state<Snippet | undefined>(undefined);
  const userSnippetKeys = $derived(new Set(userSnippets.map((s) => s.key)));
  let recentMenuOpen = $state(false);
  let helpOpen = $state(false);
  let exitConfirmOpen = $state(false);
  let exitInProgress = $state(false);
  let styleApplyOpen = $state(false);
  let capturedCss = $state('');

  // Editor referansları (bind:this)
  let xsltEditor = $state<CodeEditor>();
  let xmlEditor = $state<CodeEditor>();
  let previewFrame = $state<HTMLIFrameElement>();

  // Preview sağ tık menüsü
  let previewMenu = $state<{ x: number; y: number } | null>(null);

  // Panel boyutları (localStorage persist)
  let snippetsWidth = $state(settings.panelSizes.snippetsWidth);
  let editorsWidth = $state(settings.panelSizes.editorsWidth);
  let xsltHeight = $state(settings.panelSizes.xsltHeight);
  $effect(() => updatePanelSize('snippetsWidth', snippetsWidth));
  $effect(() => updatePanelSize('editorsWidth', editorsWidth));
  $effect(() => updatePanelSize('xsltHeight', xsltHeight));

  // ─── Snippet grupları ───────────────────────────────────────────────
  const allSnippets = $derived([...snippets, ...userSnippets]);
  const groupedSnippets = $derived(groupSnippetsByCategory(allSnippets));
  const categories = $derived(Array.from(groupedSnippets.keys()));
  const visibleSnippets = $derived.by<Snippet[]>(() => {
    const subMap = groupedSnippets.get(activeCategory);
    if (!subMap) return [];
    const all: Snippet[] = [];
    for (const [, list] of subMap) all.push(...list);
    const term = snippetFilter.trim().toLowerCase();
    if (!term) return all;
    return all.filter(
      (s) =>
        s.displayName.toLowerCase().includes(term) ||
        s.key.toLowerCase().includes(term) ||
        (s.description ?? '').toLowerCase().includes(term),
    );
  });

  // Autocomplete kataloğu
  const xsltCompletions: Completion[] = $derived.by(() => {
    const items: Completion[] = [];
    for (const t of completion.xsltTags) {
      items.push({ label: t.text, detail: t.description, type: 'keyword' });
    }
    for (const p of completion.xPathPaths) {
      items.push({ label: p.text, detail: p.description, type: 'variable' });
    }
    for (const s of allSnippets) {
      items.push({
        label: s.key,
        detail: s.displayName,
        info: s.description,
        type: 'snippet',
        apply: s.xsltCode,
      });
    }
    return items;
  });

  // İlk açılış / editörler boşsa hoşgeldin göster
  const showWelcome = $derived(!editorState.xsltText && !editorState.xmlText);

  // ─── Dirty tracker ───────────────────────────────────────────────────
  let ignoreNextChange = { xslt: false, xml: false };

  $effect(() => {
    const _t = editorState.xsltText;
    void _t;
    if (ignoreNextChange.xslt) {
      ignoreNextChange.xslt = false;
      return;
    }
    if (editorState.xsltText || editorState.xsltPath) editorState.xsltDirty = true;
  });

  $effect(() => {
    const _t = editorState.xmlText;
    void _t;
    if (ignoreNextChange.xml) {
      ignoreNextChange.xml = false;
      return;
    }
    if (editorState.xmlText || editorState.xmlPath) editorState.xmlDirty = true;
  });

  // ─── Auto-transform debounce (yazarken) ─────────────────────────────
  let debounceTimer: ReturnType<typeof setTimeout> | null = null;

  $effect(() => {
    const _x = editorState.xsltText;
    const _y = editorState.xmlText;
    void _x;
    void _y;
    if (settings.autoTransformDebounceMs <= 0) return;
    if (!editorState.xsltText || !editorState.xmlText) return;
    if (debounceTimer) clearTimeout(debounceTimer);
    debounceTimer = setTimeout(() => runTransform(true), settings.autoTransformDebounceMs);
  });

  // ─── Auto-save timer ────────────────────────────────────────────────
  let autoSaveTimer: ReturnType<typeof setTimeout> | null = null;

  $effect(() => {
    const _x = editorState.xsltDirty;
    const _y = editorState.xmlDirty;
    void _x;
    void _y;
    if (!settings.autoSave) return;
    // Sadece path'i olan dosyalar için (yeni dosyalar dialog açmayı istemeyiz)
    if (!editorState.xsltPath && !editorState.xmlPath) return;
    if (!editorState.xsltDirty && !editorState.xmlDirty) return;
    if (autoSaveTimer) clearTimeout(autoSaveTimer);
    autoSaveTimer = setTimeout(() => {
      if (settings.autoSave) saveAll(true);
    }, settings.autoSaveDelayMs);
  });

  // ─── Actions: load ──────────────────────────────────────────────────
  async function loadDefaultSample() {
    await loadSampleByPath('/samples/default.xslt', '/samples/default.xml', 'Varsayılan örnek');
  }

  async function loadSample(fileName: string, displayName: string) {
    sampleMenuOpen = false;
    await loadSampleByPath('/samples/default.xslt', `/samples/${fileName}`, `default + ${displayName}`);
  }

  async function loadSampleByPath(xsltUrl: string, xmlUrl: string, label = 'örnek') {
    try {
      const [xsl, xml] = await Promise.all([
        fetch(xsltUrl).then((r) => (r.ok ? r.text() : Promise.reject(new Error(`${xsltUrl}: ${r.status}`)))),
        fetch(xmlUrl).then((r) => (r.ok ? r.text() : Promise.reject(new Error(`${xmlUrl}: ${r.status}`)))),
      ]);
      ignoreNextChange.xslt = true;
      ignoreNextChange.xml = true;
      editorState.xsltText = xsl;
      editorState.xmlText = xml;
      editorState.xsltPath = null;
      editorState.xmlPath = null;
      editorState.xsltDirty = false;
      editorState.xmlDirty = false;
      xsltEditor?.setValue(xsl);
      xmlEditor?.setValue(xml);
      status(`${label} yüklendi (${(xsl.length / 1024).toFixed(0)} KB XSLT + ${(xml.length / 1024).toFixed(0)} KB XML)`);
      if (settings.autoTransformOnLoad) await runTransform();
    } catch (err) {
      status(`Yüklenemedi: ${(err as Error).message}`, true);
    }
  }

  // ─── Kullanıcı örnekleri ────────────────────────────────────────────
  async function refreshUserSamples() {
    try {
      userSamples = await listUserSamples();
    } catch (err) {
      status(`Kullanıcı örnekleri okunamadı: ${(err as Error).message}`, true);
    }
  }

  async function loadUserSampleEntry(sample: UserSample) {
    sampleMenuOpen = false;
    try {
      const { xslt, xml } = await loadUserSample(sample);
      ignoreNextChange.xslt = true;
      ignoreNextChange.xml = true;
      editorState.xsltText = xslt;
      editorState.xmlText = xml;
      editorState.xsltPath = sample.xsltPath;
      editorState.xmlPath = sample.xmlPath;
      editorState.xsltDirty = false;
      editorState.xmlDirty = false;
      xsltEditor?.setValue(xslt);
      xmlEditor?.setValue(xml);
      status(`Kullanıcı örneği yüklendi: ${sample.name}`);
      if (settings.autoTransformOnLoad) await runTransform();
    } catch (err) {
      status(`Yüklenemedi: ${(err as Error).message}`, true);
    }
  }

  async function addCurrentAsUserSample() {
    if (!editorState.xsltPath || !editorState.xmlPath) {
      status('Örnek olarak eklemek için hem XSLT hem XML diskte kayıtlı (kaydedilmiş) olmalı.', true);
      return;
    }
    try {
      const sample = await addSamplePair(editorState.xsltPath, editorState.xmlPath);
      status(`Örnek eklendi: ${sample.name}`);
      await refreshUserSamples();
    } catch (err) {
      status(`Örnek eklenemedi: ${(err as Error).message}`, true);
    }
  }

  async function removeUserSampleEntry(sample: UserSample, e: MouseEvent) {
    e.stopPropagation();
    try {
      await removeUserSample(sample);
      status(`Örnek silindi: ${sample.name}`);
      await refreshUserSamples();
    } catch (err) {
      status(`Silinemedi: ${(err as Error).message}`, true);
    }
  }

  async function openUserSamplesFolder() {
    try {
      await openSamplesFolder();
    } catch (err) {
      status(`Klasör açılamadı: ${(err as Error).message}`, true);
    }
  }

  // ─── Kullanıcı snippet'leri ─────────────────────────────────────────
  async function refreshUserSnippets() {
    try {
      userSnippets = await listUserSnippets();
    } catch (err) {
      status(`Kullanıcı snippet'leri okunamadı: ${(err as Error).message}`, true);
    }
  }

  function openEditSnippet(s: Snippet) {
    editingSnippet = s;
    snippetEditorOpen = true;
  }

  async function onSaveSnippet(s: Snippet, originalKey?: string) {
    try {
      userSnippets = await saveUserSnippet(s, originalKey);
      snippetEditorOpen = false;
      editingSnippet = undefined;
      status(`Snippet kaydedildi: ${s.key}`);
    } catch (err) {
      status(`Snippet kaydedilemedi: ${(err as Error).message}`, true);
    }
  }

  async function onDeleteSnippet(s: Snippet, e: MouseEvent) {
    e.stopPropagation();
    const confirmed = await ask(`"${s.displayName}" snippet'ini silmek istediğine emin misin?`, {
      title: 'Snippet Sil',
      kind: 'warning',
    });
    if (!confirmed) return;
    try {
      userSnippets = await removeUserSnippet(s.key);
      status(`Snippet silindi: ${s.key}`);
    } catch (err) {
      status(`Silinemedi: ${(err as Error).message}`, true);
    }
  }

  async function openXslt() {
    try {
      const result = await openFile('xslt');
      if (!result) return;
      ignoreNextChange.xslt = true;
      editorState.xsltText = result.content;
      editorState.xsltPath = result.path;
      editorState.xsltDirty = false;
      xsltEditor?.setValue(result.content);
      pushRecent(result.path, 'xslt');
      status(`XSLT açıldı: ${result.path}`);
      if (settings.autoTransformOnLoad && editorState.xmlText) await runTransform();
    } catch (err) {
      status(`XSLT açılamadı: ${(err as Error).message}`, true);
    }
  }

  async function openXml() {
    try {
      const result = await openFile('xml');
      if (!result) return;
      ignoreNextChange.xml = true;
      editorState.xmlText = result.content;
      editorState.xmlPath = result.path;
      editorState.xmlDirty = false;
      xmlEditor?.setValue(result.content);
      pushRecent(result.path, 'xml');
      status(`XML açıldı: ${result.path}`);
      if (settings.autoTransformOnLoad && editorState.xsltText) await runTransform();
    } catch (err) {
      status(`XML açılamadı: ${(err as Error).message}`, true);
    }
  }

  async function reopenRecent(path: string, kind: 'xslt' | 'xml') {
    recentMenuOpen = false;
    try {
      const result = await reopenFile(path);
      if (kind === 'xslt') {
        ignoreNextChange.xslt = true;
        editorState.xsltText = result.content;
        editorState.xsltPath = result.path;
        editorState.xsltDirty = false;
        xsltEditor?.setValue(result.content);
      } else {
        ignoreNextChange.xml = true;
        editorState.xmlText = result.content;
        editorState.xmlPath = result.path;
        editorState.xmlDirty = false;
        xmlEditor?.setValue(result.content);
      }
      pushRecent(path, kind);
      status(`Yeniden açıldı: ${path}`);
      if (settings.autoTransformOnLoad && editorState.xsltText && editorState.xmlText) await runTransform();
    } catch (err) {
      status(`Açılamadı: ${(err as Error).message}`, true);
    }
  }

  // ─── Actions: save ──────────────────────────────────────────────────
  async function saveAll(silent = false): Promise<boolean> {
    let anySaved = false;
    let anyError = false;

    if (editorState.xsltDirty) {
      const ok = await saveOne('xslt', silent);
      anySaved ||= ok;
      anyError ||= !ok;
    }
    if (editorState.xmlDirty) {
      const ok = await saveOne('xml', silent);
      anySaved ||= ok;
      anyError ||= !ok;
    }

    if (!anySaved && !anyError && !silent) {
      status('Kaydedilecek değişiklik yok.');
      return false;
    }
    if (anySaved && !anyError && settings.autoTransformOnSave) {
      await runTransform();
    }
    return anySaved && !anyError;
  }

  async function saveOne(kind: 'xslt' | 'xml', silent = false): Promise<boolean> {
    const text = kind === 'xslt' ? editorState.xsltText : editorState.xmlText;
    if (!text.trim()) return false;

    try {
      validateXml(text, kind);
    } catch (err) {
      if (err instanceof XsltError && err.line) {
        const editor = kind === 'xslt' ? xsltEditor : xmlEditor;
        editor?.goToLine(err.line, err.column ?? 1);
        if (!silent)
          status(`${kind.toUpperCase()} syntax hatası (satır ${err.line}): ${err.message}`, true);
      } else if (!silent) {
        status(`${kind.toUpperCase()} syntax hatası: ${(err as Error).message}`, true);
      }
      return false;
    }

    try {
      const currentPath = kind === 'xslt' ? editorState.xsltPath : editorState.xmlPath;
      if (currentPath) {
        await saveFile(currentPath, text);
        if (kind === 'xslt') editorState.xsltDirty = false;
        else editorState.xmlDirty = false;
        pushRecent(currentPath, kind);
        if (!silent) status(`${kind.toUpperCase()} kaydedildi: ${currentPath}`);
      } else {
        const path = await saveFileAs(text, kind, kind === 'xslt' ? 'yeni.xslt' : 'yeni.xml');
        if (!path) return false;
        if (kind === 'xslt') {
          editorState.xsltPath = path;
          editorState.xsltDirty = false;
        } else {
          editorState.xmlPath = path;
          editorState.xmlDirty = false;
        }
        pushRecent(path, kind);
        if (!silent) status(`${kind.toUpperCase()} farklı kaydedildi: ${path}`);
      }
      return true;
    } catch (err) {
      status(`Kaydedilemedi: ${(err as Error).message}`, true);
      return false;
    }
  }

  async function saveXsltAs() {
    const path = await saveFileAs(editorState.xsltText, 'xslt', 'yeni.xslt');
    if (!path) return;
    editorState.xsltPath = path;
    editorState.xsltDirty = false;
    pushRecent(path, 'xslt');
    status(`XSLT farklı kaydedildi: ${path}`);
    if (settings.autoTransformOnSave) await runTransform();
  }

  // ─── Actions: transform ─────────────────────────────────────────────
  async function runTransform(silent = false) {
    if (!editorState.xsltText || !editorState.xmlText) {
      if (!silent) status('Önce XSLT ve XML yükleyin.', true);
      return;
    }
    if (!silent) status('Dönüştürülüyor...');
    try {
      const html = await transformXml(editorState.xmlText, editorState.xsltText);
      editorState.previewHtml = html;
      status(`Dönüşüm tamam (${(html.length / 1024).toFixed(1)} KB HTML)`);
    } catch (err) {
      if (err instanceof XsltError && err.line) {
        const editor = err.source === 'xml' ? xmlEditor : xsltEditor;
        editor?.goToLine(err.line, err.column ?? 1);
        editorState.previewHtml = `<pre style="color:#c00;padding:1rem;font-family:monospace;">Hata (${err.source} satır ${err.line}):\n\n${escapeHtml(err.message)}</pre>`;
        status(`Hata (${err.source} satır ${err.line}): ${err.message}`, true);
      } else {
        const msg = (err as Error).message ?? String(err);
        editorState.previewHtml = `<pre style="color:#c00;padding:1rem;font-family:monospace;">${escapeHtml(msg)}</pre>`;
        status(`Dönüşüm hatası: ${msg}`, true);
      }
    }
  }

  // ─── Snippet insert (click) & drop (drag) ──────────────────────────
  function insertSnippet(snippet: Snippet) {
    if (xsltEditor) {
      xsltEditor.insertAtCursor(snippet.xsltCode);
      status(`Eklendi: ${snippet.key}`);
    }
  }

  function handleDrop(
    editorKind: 'xslt' | 'xml' | null,
    x: number,
    y: number,
    snippetKey: string,
    snippetText: string,
  ) {
    if (!editorKind) {
      status(`Sürükleme iptal: hedef editör değil (${snippetKey || '?'})`);
      return;
    }
    if (!snippetText) {
      status('Sürüklenen içerik boş.', true);
      return;
    }
    const editor = editorKind === 'xslt' ? xsltEditor : xmlEditor;
    if (!editor) {
      status(`Hedef editör hazır değil (${editorKind})`, true);
      return;
    }
    editor.insertAtCoords(x, y, snippetText);
    status(`✓ Eklendi (${editorKind.toUpperCase()}): ${snippetKey || 'snippet'}`);
  }

  function onSnippetMouseDown(e: MouseEvent, snippet: Snippet) {
    beginPossibleDrag(
      e,
      snippet.key,
      snippet.xsltCode,
      () => insertSnippet(snippet),
      handleDrop,
    );
  }

  // ─── Preview aksiyonları ────────────────────────────────────────────
  /**
   * Önizlemeyi yazdır / PDF olarak kaydet.
   *
   * NOT: Tauri'nin WKWebView'inde `window.print()` native print panelini
   * güvenilir şekilde açmıyor — sadece `@media print` stillerini anlık
   * tetikleyip (toolbar font boyutu kısa süre değişip geri dönüyor) hiçbir
   * dialog açmadan geri dönüyor. Bu yüzden yazdırma tamamen sistem
   * tarayıcısına (Safari) devredildi: HTML dosya olarak yazılıp açılıyor,
   * kullanıcı orada Cmd+P ile güvenilir şekilde yazdırır / PDF kaydeder.
   */
  async function printPreview() {
    if (!editorState.previewHtml) {
      status('Önizleme boş.', true);
      return;
    }
    try {
      const { writeTextFile, mkdir, exists } = await import('@tauri-apps/plugin-fs');
      const { appLocalDataDir, join } = await import('@tauri-apps/api/path');
      const { openPath } = await import('@tauri-apps/plugin-opener');

      const base = await appLocalDataDir();
      const previewDir = await join(base, 'preview');
      if (!(await exists(previewDir))) {
        await mkdir(previewDir, { recursive: true });
      }
      const filename = `preview-${Date.now()}.html`;
      const path = await join(previewDir, filename);
      await writeTextFile(path, editorState.previewHtml);
      await openPath(path);
      status('Tarayıcıda açıldı — Cmd+P ile yazdır veya "PDF olarak Kaydet" seç.');
    } catch (err) {
      status(`Yazdırma için tarayıcı açılamadı: ${(err as Error).message ?? err}`, true);
    }
  }

  function copyPreviewHtml() {
    if (!editorState.previewHtml) return;
    navigator.clipboard
      .writeText(editorState.previewHtml)
      .then(() => status(`HTML kopyalandı (${(editorState.previewHtml.length / 1024).toFixed(1)} KB)`))
      .catch((err) => status(`Kopyalanamadı: ${err.message}`, true));
  }

  async function openDevTools() {
    try {
      await invoke('open_devtools');
      status('DevTools açıldı.');
    } catch (err) {
      status(`DevTools açılamadı: ${(err as Error).message ?? err}`, true);
    }
  }

  // ─── DevTools'ta düzenlenen CSS'i XSLT'ye aktarma ───────────────────
  // Yalnızca CSS/stil: DevTools'ta Styles panelinden yapılan değişiklikler
  // canlı CSSOM'u (styleSheets[].cssRules) günceller, stil etiketinin
  // textContent'ini DEĞİL. Bu yüzden mevcut kuralları cssRules üzerinden
  // yeniden serileştirip yakalıyoruz. Veriden üretilen metin/yapı
  // değişiklikleri (xsl:value-of, xsl:for-each vb.) genel olarak XSLT
  // kaynağına güvenilir şekilde geri eşlenemez — bu yüzden kapsam dışı.
  //
  // NOT: Etiket adı ("style") aşağıda kasıtlı olarak bir değişkenden
  // interpolate ediliyor — kaynak kodda "<" + "style" bitişik geçerse
  // (yorumda, regex'te, string'de fark etmez) Svelte derleyicisinin
  // script/style blok sınırlarını bulmak için yaptığı ön-tarama yanlış
  // pozitif verip gerçek kapanış script etiketini yutabiliyor.
  const STYLE_TAG = 'style';
  const styleBlockRegex = new RegExp(`(<${STYLE_TAG}[^>]*>)([\\s\\S]*?)(<\\/${STYLE_TAG}>)`, 'i');
  let cssCaptureResolve: ((css: string) => void) | null = null;
  let cssCaptureTimer: ReturnType<typeof setTimeout> | null = null;

  function requestCssCapture(): Promise<string> {
    return new Promise((resolve, reject) => {
      cssCaptureResolve = resolve;
      previewFrame?.contentWindow?.postMessage({ type: 'capture-css' }, '*');
      cssCaptureTimer = setTimeout(() => {
        if (cssCaptureResolve) {
          cssCaptureResolve = null;
          reject(new Error('Önizleme yanıt vermedi (zaman aşımı).'));
        }
      }, 2000);
    });
  }

  async function captureStyleFromPreview() {
    if (!editorState.previewHtml) {
      status('Önce bir önizleme oluşturun.', true);
      return;
    }
    if (!styleBlockRegex.test(editorState.xsltText)) {
      status('XSLT içinde stil bloğu bulunamadı — bu özellik yalnızca gömülü CSS içeren şablonlarda çalışır.', true);
      return;
    }
    try {
      const css = await requestCssCapture();
      if (!css.trim()) {
        status('Önizlemede yakalanacak CSS kuralı bulunamadı.', true);
        return;
      }
      capturedCss = css;
      styleApplyOpen = true;
    } catch (err) {
      status(`Stil yakalanamadı: ${(err as Error).message}`, true);
    }
  }

  function applyCapturedCssToXslt() {
    const newXslt = editorState.xsltText.replace(styleBlockRegex, (_m, open, _old, close) => `${open}\n${capturedCss}\n${close}`);
    editorState.xsltText = newXslt;
    xsltEditor?.setValue(newXslt);
    styleApplyOpen = false;
    status('DevTools\'taki stil değişiklikleri XSLT\'ye uygulandı — kaydetmeyi unutmayın.');
    runTransform();
  }

  function cancelStyleApply() {
    styleApplyOpen = false;
  }

  function setPreviewWidth(w: number | null) {
    updateSetting('previewWidth', w);
    status(`Önizleme genişliği: ${w ? w + 'px' : 'Tam'}`);
  }

  function zoomPreview(delta: number) {
    const newZoom = Math.max(0.5, Math.min(2.0, +(settings.previewZoom + delta).toFixed(2)));
    updateSetting('previewZoom', newZoom);
  }

  function resetZoom() {
    updateSetting('previewZoom', 1.0);
  }

  /**
   * iframe içine sağ tık köprüsü enjekte eder.
   * `previewHtml` artık tam bir `<!DOCTYPE html>...</html>` dokümanı
   * olduğundan, script'i `</body>` etiketinden hemen önce ekleriz
   * (yoksa sona ekleriz — tarayıcı yine de doğru parse eder).
   */
  const previewHtmlWithBridge = $derived.by(() => {
    if (!editorState.previewHtml) return '';
    const bridge = `<script>
document.addEventListener('contextmenu', function(e) {
  e.preventDefault();
  window.parent.postMessage({ type: 'preview-contextmenu', x: e.clientX, y: e.clientY }, '*');
});
window.addEventListener('message', function(e) {
  if (!e.data || e.data.type !== 'capture-css') return;
  var parts = [];
  for (var i = 0; i < document.styleSheets.length; i++) {
    try {
      var rules = document.styleSheets[i].cssRules;
      for (var j = 0; j < rules.length; j++) parts.push(rules[j].cssText);
    } catch (err) { /* erişilemeyen (cross-origin) sheet — atla */ }
  }
  window.parent.postMessage({ type: 'css-captured', css: parts.join('\\n') }, '*');
});
<\/script>`;
    const html = editorState.previewHtml;
    const bodyCloseIdx = html.lastIndexOf('</body>');
    if (bodyCloseIdx !== -1) {
      return html.slice(0, bodyCloseIdx) + bridge + html.slice(bodyCloseIdx);
    }
    return html + bridge;
  });

  // ─── Undo/Redo (aktif editör: XSLT varsayılan) ──────────────────────
  function undoActive() {
    (xsltEditor ?? xmlEditor)?.undo();
  }
  function redoActive() {
    (xsltEditor ?? xmlEditor)?.redo();
  }

  // ─── Util ───────────────────────────────────────────────────────────
  function status(msg: string, isError = false) {
    statusMsg = msg;
    statusIsError = isError;
  }
  function escapeHtml(s: string): string {
    return s.replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;');
  }

  // ─── Global keyboard shortcuts ──────────────────────────────────────
  function onGlobalKeydown(e: KeyboardEvent) {
    const meta = e.metaKey || e.ctrlKey;
    if (meta && e.key === 's') {
      e.preventDefault();
      saveAll();
    } else if (meta && e.key === 'r' && !e.shiftKey) {
      e.preventDefault();
      runTransform();
    } else if (meta && e.key === 'p') {
      e.preventDefault();
      printPreview();
    } else if (meta && (e.key === '=' || e.key === '+')) {
      e.preventDefault();
      zoomPreview(0.1);
    } else if (meta && e.key === '-') {
      e.preventDefault();
      zoomPreview(-0.1);
    } else if (meta && e.key === '0') {
      e.preventDefault();
      resetZoom();
    } else if (e.key === 'F1') {
      e.preventDefault();
      helpOpen = true;
    }
  }

  function onPreviewMessage(e: MessageEvent) {
    if (!e.data || typeof e.data !== 'object') return;
    if (e.data.type === 'css-captured') {
      if (cssCaptureTimer) clearTimeout(cssCaptureTimer);
      cssCaptureResolve?.(e.data.css ?? '');
      cssCaptureResolve = null;
      return;
    }
    if (e.data.type !== 'preview-contextmenu') return;
    const rect = previewFrame?.getBoundingClientRect();
    if (!rect) return;
    previewMenu = {
      x: rect.left + (e.data.x ?? 0),
      y: rect.top + (e.data.y ?? 0),
    };
  }

  function onGlobalClick(e: MouseEvent) {
    const target = e.target as HTMLElement;
    if (sampleMenuOpen && !target.closest('.sample-menu-wrap')) sampleMenuOpen = false;
    if (recentMenuOpen && !target.closest('.recent-menu-wrap')) recentMenuOpen = false;
  }

  // ─── Çıkışta kaydetme kontrolü ───────────────────────────────────────
  // `forceClose` true iken onCloseRequested engellenmeden pencerenin
  // gerçekten kapanmasına izin verilir (sonsuz döngüyü engeller).
  let forceClose = false;
  let unlistenClose: (() => void) | null = null;

  async function setupCloseGuard() {
    try {
      const win = getCurrentWindow();
      unlistenClose = await win.onCloseRequested(async (event) => {
        if (forceClose) return; // izin verildi, kapanmaya devam et
        if (editorState.xsltDirty || editorState.xmlDirty) {
          event.preventDefault();
          exitConfirmOpen = true;
        }
      });
    } catch (err) {
      // Tauri dışı ortamda (örn. sadece tarayıcıda test) bu API yok — sessiz geç.
      console.warn('Close guard kurulamadı:', err);
    }
  }

  /** Kaydet ve çık. Syntax hatası varsa çıkışı iptal eder, kullanıcı düzeltmeli. */
  async function confirmSaveAndExit() {
    exitInProgress = true;
    try {
      const ok = await saveAll();
      if (!ok && (editorState.xsltDirty || editorState.xmlDirty)) {
        status('Kaydedilemedi — sözdizimi hatası olabilir. Lütfen düzeltip tekrar deneyin.', true);
        exitInProgress = false;
        return; // modal açık kalır, kullanıcı düzeltsin
      }
      forceClose = true;
      exitConfirmOpen = false;
      await getCurrentWindow().close();
    } catch (err) {
      status(`Çıkış sırasında hata: ${(err as Error).message ?? err}`, true);
      exitInProgress = false;
    }
  }

  /** Kaydetmeden çık — değişiklikler kaybolur. */
  async function confirmDiscardAndExit() {
    exitInProgress = true;
    forceClose = true;
    exitConfirmOpen = false;
    try {
      await getCurrentWindow().close();
    } catch (err) {
      status(`Çıkış sırasında hata: ${(err as Error).message ?? err}`, true);
      exitInProgress = false;
      forceClose = false;
    }
  }

  function cancelExit() {
    exitConfirmOpen = false;
  }

  onMount(() => {
    window.addEventListener('keydown', onGlobalKeydown);
    window.addEventListener('click', onGlobalClick);
    window.addEventListener('message', onPreviewMessage);
    setupCloseGuard();
    refreshUserSamples();
    refreshUserSnippets();
    if (showWelcome) {
      status(`e-Fatura Edit v${manifest.version} — ${snippets.length} snippet · ${xsltCompletions.length} tamamlama · hazır`);
    }
  });

  onDestroy(() => {
    if (typeof window !== 'undefined') {
      window.removeEventListener('keydown', onGlobalKeydown);
      window.removeEventListener('click', onGlobalClick);
      window.removeEventListener('message', onPreviewMessage);
    }
    if (debounceTimer) clearTimeout(debounceTimer);
    if (autoSaveTimer) clearTimeout(autoSaveTimer);
    unlistenClose?.();
  });
</script>

<div class="app" class:dark={themeKind(settings.theme) === 'dark'}>
  <!-- ─── Toolbar ────────────────────────────────────────────────── -->
  <header class="toolbar">
    <div class="brand">
      <strong>e-Fatura Edit</strong>
      <span class="ver">v{manifest.version}</span>
    </div>
    <div class="actions">
      <div class="btn-group" title="Düzenle">
        <button onclick={undoActive} title="Geri Al (Cmd/Ctrl+Z)">↩</button>
        <button onclick={redoActive} title="İleri Al (Cmd/Ctrl+Shift+Z)">↪</button>
      </div>

      <div class="btn-group" title="Dosya">
        <button onclick={openXslt} title="XSLT dosyası aç">📂 XSLT</button>
        <button onclick={openXml} title="XML dosyası aç">📄 XML</button>

        {#if recentFiles.length > 0}
          <div class="recent-menu-wrap">
            <button onclick={() => (recentMenuOpen = !recentMenuOpen)} title="Son dosyalar">
              🕒 Son ▼
            </button>
            {#if recentMenuOpen}
              <div class="dropdown recent">
                <div class="dd-header">
                  <span>Son Açılanlar</span>
                  <button class="dd-clear" onclick={clearRecent} title="Tümünü sil">🗑</button>
                </div>
                {#each recentFiles as f}
                  <button class="dd-item" onclick={() => reopenRecent(f.path, f.kind)} title={f.path}>
                    <span class="dd-badge {f.kind}">{f.kind.toUpperCase()}</span>
                    <span class="dd-name">{basename(f.path)}</span>
                    <span class="dd-path">{f.path}</span>
                  </button>
                {/each}
              </div>
            {/if}
          </div>
        {/if}

        <button
          onclick={() => saveAll()}
          class:dirty={editorState.xsltDirty || editorState.xmlDirty}
          disabled={!editorState.xsltDirty && !editorState.xmlDirty}
          title="Kaydet (Cmd/Ctrl+S)"
        >
          {#if editorState.xsltDirty || editorState.xmlDirty}
            💾* ({[editorState.xsltDirty && 'XSLT', editorState.xmlDirty && 'XML'].filter(Boolean).join('+')})
          {:else}
            💾 Kaydet
          {/if}
        </button>

        <button onclick={saveXsltAs} title="XSLT'yi farklı adla kaydet">💾 Farklı</button>
      </div>

      <div class="btn-group" title="İşlemler">
        <div class="sample-menu-wrap">
          <button onclick={() => (sampleMenuOpen = !sampleMenuOpen)} title="Örnek fatura yükle">
            🎲 Örnek ▼
          </button>
          {#if sampleMenuOpen}
            <div class="dropdown">
              <button class="dd-item primary" onclick={loadDefaultSample}>
                🌟 Varsayılan (default.xslt + default.xml)
              </button>
              <div class="dd-divider"></div>
              {#each samples.groups as group}
                <div class="dd-group-title">{group.categoryName}</div>
                {#each group.entries as entry}
                  <button class="dd-item" onclick={() => loadSample(entry.fileName, entry.displayName)}>
                    <span class="dd-name">{entry.displayName}</span>
                    <span class="dd-path">{entry.fileName}</span>
                  </button>
                {/each}
              {/each}

              <div class="dd-divider"></div>
              <div class="dd-header">
                <span>Kullanıcı Örnekleri</span>
                <button class="dd-clear" onclick={openUserSamplesFolder} title="Örnekler klasörünü Finder'da aç">📁</button>
              </div>
              {#if userSamples.length === 0}
                <div class="dd-empty">Henüz yok — aşağıdan geçerli dosyaları ekleyebilirsin.</div>
              {/if}
              {#each userSamples as us (us.name)}
                <div class="dd-item-row">
                  <button class="dd-item" onclick={() => loadUserSampleEntry(us)}>
                    <span class="dd-name">{us.name}</span>
                  </button>
                  <button class="dd-remove" onclick={(e) => removeUserSampleEntry(us, e)} title="Bu örneği sil">🗑</button>
                </div>
              {/each}
              <button
                class="dd-item primary"
                onclick={addCurrentAsUserSample}
                disabled={!editorState.xsltPath || !editorState.xmlPath}
                title={!editorState.xsltPath || !editorState.xmlPath ? 'Önce XSLT ve XML dosyalarını diske kaydet' : 'Açık olan XSLT + XML ikilisini örnek olarak kaydet'}
              >
                ➕ Geçerli ikiliyi örnek olarak kaydet
              </button>
            </div>
          {/if}
        </div>

        <button class="primary" onclick={() => runTransform()} title="Dönüştür (Cmd/Ctrl+R)">▶ Dönüştür</button>
      </div>

      <div class="btn-group" title="Yardım ve Ayarlar">
        <button onclick={() => (helpOpen = true)} title="Yardım ve Dokümantasyon (F1)">❓ Yardım</button>
        <button onclick={() => goto('/settings')} title="Ayarlar (tema, font, davranış)">⚙️ Ayarlar</button>
      </div>
    </div>
    <div class="status" class:error={statusIsError}>{statusMsg}</div>
  </header>

  <!-- ─── Main Grid ─────────────────────────────────────────────── -->
  <div
    class="main-grid"
    style="grid-template-columns: {snippetsWidth}px 4px {editorsWidth}px 4px 1fr;"
  >
    <!-- Snippet paneli -->
    <aside class="snippets">
      <div class="snippets-header">
        <h3>Snippet'ler ({allSnippets.length})</h3>
        <button class="snippet-add" onclick={() => { editingSnippet = undefined; snippetEditorOpen = true; }} title="Yeni snippet ekle">➕</button>
        <input type="text" placeholder="Ara..." bind:value={snippetFilter} class="search" />
        <div class="tabs">
          {#each categories as cat}
            <button
              class="tab"
              class:active={cat === activeCategory}
              onclick={() => (activeCategory = cat)}
            >
              {cat}
            </button>
          {/each}
        </div>
        <div class="hint">💡 Tıkla = imlece ekle · Sürükle = istediğin yere bırak</div>
      </div>
      <div class="snippet-list">
        {#each visibleSnippets as snippet (snippet.key)}
          <div
            class="snippet-item"
            title={snippet.description}
            role="button"
            tabindex="0"
            onmousedown={(e) => onSnippetMouseDown(e, snippet)}
            onkeydown={(e) => {
              if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                insertSnippet(snippet);
              }
            }}
          >
            <span class="icon">{snippet.iconText}</span>
            <span class="name">{snippet.displayName}</span>
            <span class="key">{snippet.key}</span>
            {#if userSnippetKeys.has(snippet.key)}
              <button
                class="snippet-edit"
                title="Düzenle"
                onmousedown={(e) => e.stopPropagation()}
                onclick={(e) => {
                  e.stopPropagation();
                  openEditSnippet(snippet);
                }}
              >
                ✏️
              </button>
              <button
                class="snippet-delete"
                title="Sil"
                onmousedown={(e) => e.stopPropagation()}
                onclick={(e) => onDeleteSnippet(snippet, e)}
              >
                🗑
              </button>
            {/if}
          </div>
        {:else}
          <p class="muted">Snippet bulunamadı.</p>
        {/each}
      </div>
    </aside>

    <Splitter direction="vertical" bind:position={snippetsWidth} min={200} max={500} />

    <!-- Editör paneli -->
    <section class="editors" style="grid-template-rows: 22px {xsltHeight}px 4px 22px 1fr;">
      <div class="panel-header">
        XSLT {editorState.xsltPath ? `— ${basename(editorState.xsltPath)}` : '(yeni)'} · {editorState.xsltText.length}
        {#if editorState.xsltDirty}<span class="dirty-mark">●</span>{/if}
      </div>
      <div class="editor-slot" data-editor-kind="xslt">
        {#if showWelcome}
          <div class="welcome">
            <h2>e-Fatura Dizayn Editörü</h2>
            <p>Başlamak için bir seçenek belirle:</p>
            <div class="welcome-actions">
              <button class="w-btn primary" onclick={loadDefaultSample} title="Hızlı başlangıç — örnek XSLT + XML birlikte yükler">🎲 Örnek Fatura Yükle</button>
              <button class="w-btn" onclick={openXslt} title="Bilgisayarından bir .xslt/.xsl dosyası aç">📂 XSLT Dosyası Aç</button>
              <button class="w-btn" onclick={openXml} title="Bilgisayarından bir .xml dosyası aç">📄 XML Dosyası Aç</button>
            </div>
            <p class="hint-lg">
              🎨 {snippets.length} snippet · 🔎 Ctrl+Space autocomplete · 💾 Cmd+S kaydet
            </p>
          </div>
        {:else}
          <CodeEditor
            bind:this={xsltEditor}
            bind:value={editorState.xsltText}
            language="xml"
            completions={xsltCompletions}
          />
        {/if}
      </div>

      <Splitter direction="horizontal" bind:position={xsltHeight} min={100} max={800} />

      <div class="panel-header">
        XML {editorState.xmlPath ? `— ${basename(editorState.xmlPath)}` : '(yeni)'} · {editorState.xmlText.length}
        {#if editorState.xmlDirty}<span class="dirty-mark">●</span>{/if}
      </div>
      <div class="editor-slot" data-editor-kind="xml">
        {#if showWelcome}
          <div class="welcome sub">
            <p class="hint-lg">Örnek yükledikten sonra sağdaki önizleme otomatik oluşur.</p>
          </div>
        {:else}
          <CodeEditor bind:this={xmlEditor} bind:value={editorState.xmlText} language="xml" />
        {/if}
      </div>
    </section>

    <Splitter direction="vertical" bind:position={editorsWidth} min={300} max={1400} />

    <!-- Preview paneli -->
    <section class="preview">
      <div class="panel-header preview-header">
        <span>Önizleme ({(editorState.previewHtml.length / 1024).toFixed(1)} KB)</span>
        <div class="preview-actions">
          <!-- Responsive boyut butonları -->
          <button
            class:active={settings.previewWidth === 320}
            onclick={() => setPreviewWidth(320)}
            title="Mobil (320px)"
          >📱 320</button>
          <button
            class:active={settings.previewWidth === 768}
            onclick={() => setPreviewWidth(768)}
            title="Tablet (768px)"
          >📱 768</button>
          <button
            class:active={settings.previewWidth === 1200}
            onclick={() => setPreviewWidth(1200)}
            title="Masaüstü (1200px)"
          >🖥️ 1200</button>
          <button
            class:active={settings.previewWidth === null}
            onclick={() => setPreviewWidth(null)}
            title="Tam genişlik"
          >⬜ Full</button>

          <span class="mini-sep"></span>

          <!-- Zoom -->
          <button onclick={() => zoomPreview(-0.1)} title="Yakınlaştır az (Cmd+-)">−</button>
          <button onclick={resetZoom} title="Zoom sıfırla (Cmd+0)">
            {Math.round(settings.previewZoom * 100)}%
          </button>
          <button onclick={() => zoomPreview(0.1)} title="Yakınlaştır (Cmd++)">+</button>

          <span class="mini-sep"></span>

          <button
            onclick={captureStyleFromPreview}
            disabled={!editorState.previewHtml}
            title="DevTools'ta (Styles panelinde) yaptığın CSS değişikliklerini XSLT'deki stil bloğuna aktar"
          >🎨 Stili XSLT'ye Al</button>

          <span class="mini-sep"></span>

          <button onclick={printPreview} disabled={!editorState.previewHtml} title="Yazdır / PDF (Cmd+P)">🖨</button>
        </div>
      </div>
      <div class="preview-frame-wrap">
        <div
          class="preview-frame-container"
          style:max-width={settings.previewWidth ? `${settings.previewWidth}px` : 'none'}
        >
          <iframe
            bind:this={previewFrame}
            srcdoc={previewHtmlWithBridge}
            title="Önizleme"
            sandbox="allow-same-origin allow-scripts allow-modals"
            style:transform="scale({settings.previewZoom})"
            style:transform-origin="top left"
            style:width="{100 / settings.previewZoom}%"
            style:height="{100 / settings.previewZoom}%"
          ></iframe>
        </div>
      </div>
    </section>
  </div>
</div>

<!-- ─── Custom drag ghost ─────────────────────────────────────────── -->
{#if dragState.active}
  <div
    class="drag-ghost"
    class:on-target={dragState.targetEditor !== null}
    style="left: {dragState.currentX + 14}px; top: {dragState.currentY + 14}px;"
  >
    <span class="ghost-icon">{dragState.targetEditor ? '📌' : '📋'}</span>
    <span class="ghost-key">{dragState.snippetKey}</span>
    {#if dragState.targetEditor}
      <span class="ghost-target">→ {dragState.targetEditor.toUpperCase()}</span>
    {/if}
  </div>
{/if}

<!-- ─── Preview sağ tık menü ──────────────────────────────────────── -->
{#if previewMenu}
  <ContextMenu x={previewMenu.x} y={previewMenu.y} onclose={() => (previewMenu = null)}>
    {#snippet children()}
      <button onclick={() => { printPreview(); previewMenu = null; }}>🖨 Yazdır / PDF Kaydet… (tarayıcıda)</button>
      <button onclick={() => { copyPreviewHtml(); previewMenu = null; }}>📋 HTML'i Kopyala</button>
      <div class="divider"></div>
      <button onclick={() => { runTransform(); previewMenu = null; }}>▶ Yeniden Dönüştür</button>
      <button onclick={() => { openDevTools(); previewMenu = null; }}>🔧 Geliştirici Araçları</button>
    {/snippet}
  </ContextMenu>
{/if}

<!-- ─── Yardım penceresi ──────────────────────────────────────────── -->
{#if helpOpen}
  <HelpModal onclose={() => (helpOpen = false)} />
{/if}

<!-- ─── Snippet ekle/düzenle ──────────────────────────────────────── -->
{#if snippetEditorOpen}
  <SnippetEditor
    snippet={editingSnippet}
    categories={categories}
    existingKeys={allSnippets.filter((s) => s.key !== editingSnippet?.key).map((s) => s.key)}
    onsave={onSaveSnippet}
    onclose={() => {
      snippetEditorOpen = false;
      editingSnippet = undefined;
    }}
  />
{/if}

<!-- ─── Çıkışta kaydetme onayı ────────────────────────────────────── -->
{#if exitConfirmOpen}
  <div class="exit-overlay" role="presentation">
    <div class="exit-modal" role="alertdialog" aria-label="Kaydedilmemiş değişiklikler">
      <h3>⚠️ Kaydedilmemiş Değişiklikler</h3>
      <p>
        {[editorState.xsltDirty && 'XSLT', editorState.xmlDirty && 'XML'].filter(Boolean).join(' ve ')}
        dosyasında kaydedilmemiş değişiklikler var. Çıkmadan önce kaydetmek ister misin?
      </p>
      <div class="exit-actions">
        <button class="exit-btn cancel" onclick={cancelExit} disabled={exitInProgress}>İptal</button>
        <button class="exit-btn discard" onclick={confirmDiscardAndExit} disabled={exitInProgress}>
          Kaydetmeden Çık
        </button>
        <button class="exit-btn save" onclick={confirmSaveAndExit} disabled={exitInProgress}>
          {exitInProgress ? 'Kaydediliyor…' : 'Kaydet ve Çık'}
        </button>
      </div>
    </div>
  </div>
{/if}

<!-- ─── DevTools stilini XSLT'ye uygula onayı ─────────────────────── -->
{#if styleApplyOpen}
  <div class="exit-overlay" role="presentation">
    <div class="style-modal" role="alertdialog" aria-label="Stil değişikliklerini XSLT'ye uygula">
      <h3>🎨 Stil Değişikliklerini XSLT'ye Uygula</h3>
      <p>
        DevTools'ta yaptığın CSS değişiklikleri yakalandı. Uygularsan XSLT'deki
        <code>&lt;style&gt;</code> bloğunun içeriği aşağıdakiyle <b>değiştirilecek</b>
        (metin/veri içeriği etkilenmez, yalnızca stil).
      </p>
      <pre class="style-preview">{capturedCss}</pre>
      <div class="exit-actions">
        <button class="exit-btn cancel" onclick={cancelStyleApply}>İptal</button>
        <button class="exit-btn save" onclick={applyCapturedCssToXslt}>Uygula</button>
      </div>
    </div>
  </div>
{/if}

<style>
  :global(:root) {
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
    font-size: 13px;
    color: #1a1a1a;
    background: #f5f6f8;
  }
  :global(*) { box-sizing: border-box; }
  :global(body) { margin: 0; }

  .app { display: grid; grid-template-rows: auto 1fr; height: 100vh; background: #f5f6f8; }
  .app.dark { color: #e6e6e6; background: #1e1e1e; }

  /* Toolbar */
  .toolbar {
    display: grid;
    grid-template-columns: auto 1fr auto;
    align-items: center;
    gap: 1rem;
    padding: 0.5rem 1rem;
    background: linear-gradient(180deg, #ffffff 0%, #eef0f3 100%);
    border-bottom: 1px solid #d5d8dc;
    box-shadow: 0 1px 2px rgba(0, 0, 0, 0.04);
  }
  .app.dark .toolbar {
    background: linear-gradient(180deg, #2d2d30 0%, #252526 100%);
    border-bottom-color: #3f3f46;
  }
  .brand { display: flex; align-items: baseline; gap: 0.5rem; }
  .brand strong { color: #0a5cff; }
  .ver { color: #6b7280; font-size: 11px; }

  .actions { display: flex; gap: 0.5rem; align-items: center; }

  /* Grup kutusu — butonları görsel olarak grupla */
  .btn-group {
    display: flex;
    gap: 1px;
    padding: 2px;
    background: rgba(15, 23, 42, 0.05);
    border: 1px solid rgba(15, 23, 42, 0.08);
    border-radius: 6px;
    align-items: center;
  }
  .app.dark .btn-group {
    background: rgba(255, 255, 255, 0.04);
    border-color: rgba(255, 255, 255, 0.08);
  }
  .btn-group > button,
  .btn-group > .sample-menu-wrap > button,
  .btn-group > .recent-menu-wrap > button {
    padding: 0.35rem 0.65rem;
    border: none;
    background: transparent;
    border-radius: 3px;
    cursor: pointer;
    font-size: 12px;
    color: inherit;
    transition: background 0.12s;
    white-space: nowrap;
  }
  .btn-group > button:hover:not(:disabled),
  .btn-group > .sample-menu-wrap > button:hover,
  .btn-group > .recent-menu-wrap > button:hover {
    background: #fff;
  }
  .app.dark .btn-group > button:hover:not(:disabled),
  .app.dark .btn-group > .sample-menu-wrap > button:hover,
  .app.dark .btn-group > .recent-menu-wrap > button:hover {
    background: #4a4a4a;
  }
  .btn-group > button:disabled {
    opacity: 0.4;
    cursor: not-allowed;
  }
  .btn-group > button.primary {
    background: #0a5cff;
    color: white;
    font-weight: 600;
    padding: 0.35rem 0.9rem;
  }
  .btn-group > button.primary:hover {
    background: #0847c9;
  }
  .btn-group > button.dirty {
    background: #f59e0b;
    color: white;
    box-shadow: 0 0 0 0 rgba(245, 158, 11, 0.6);
    animation: pulse 1.8s ease-in-out infinite;
    font-weight: 600;
  }
  .btn-group > button.dirty:hover {
    background: #d97706;
  }
  @keyframes pulse {
    0%, 100% { box-shadow: 0 0 0 0 rgba(245, 158, 11, 0.5); }
    50% { box-shadow: 0 0 0 6px rgba(245, 158, 11, 0); }
  }

  .status { color: #4b5563; font-size: 12px; justify-self: end; }
  .app.dark .status { color: #a0a0a0; }
  .status.error { color: #b91c1c; font-weight: 600; }

  /* Dropdown (sample + recent) */
  .sample-menu-wrap, .recent-menu-wrap { position: relative; }
  .dropdown {
    position: absolute;
    top: 100%;
    left: 0;
    margin-top: 4px;
    background: #fff;
    border: 1px solid #cbd0d6;
    border-radius: 4px;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.12);
    min-width: 320px;
    max-height: 500px;
    overflow-y: auto;
    z-index: 100;
    padding: 4px 0;
  }
  .dropdown.recent { min-width: 380px; }
  .app.dark .dropdown { background: #2d2d30; border-color: #555; }
  .dd-header {
    display: flex; align-items: center; justify-content: space-between;
    padding: 6px 12px; font-size: 11px; text-transform: uppercase;
    color: #6b7280; letter-spacing: 0.5px; font-weight: 600;
    border-bottom: 1px solid #e5e7eb;
  }
  .app.dark .dd-header { border-bottom-color: #3f3f46; }
  .dd-clear {
    background: none; border: none; cursor: pointer;
    font-size: 12px; padding: 2px 6px; border-radius: 3px;
  }
  .dd-clear:hover { background: #fee2e2; }
  .dd-item {
    display: grid; grid-template-columns: auto 1fr; align-items: center;
    gap: 0.5rem; width: 100%; text-align: left;
    padding: 6px 12px; background: none; border: none; cursor: pointer;
    font-size: 12px; color: inherit;
  }
  .dropdown.recent .dd-item { grid-template-columns: auto 1fr; }
  .dd-item:hover { background: #eef4ff; }
  .app.dark .dd-item:hover { background: #094771; }
  .dd-item.primary { font-weight: 600; color: #0a5cff; grid-template-columns: 1fr; }
  .dd-name { color: inherit; }
  .dd-path {
    display: block; grid-column: 1 / -1;
    font-family: ui-monospace, Menlo, monospace; font-size: 10px;
    color: #6b7280; margin-top: 2px;
  }
  .dd-badge {
    display: inline-block;
    padding: 2px 6px; border-radius: 3px;
    font-family: ui-monospace, Menlo, monospace; font-size: 9px;
    font-weight: 700; letter-spacing: 0.5px;
    background: #eef4ff; color: #0a5cff;
  }
  .dd-badge.xml { background: #f0fdf4; color: #16a34a; }
  .dd-divider { height: 1px; background: #e5e7eb; margin: 4px 0; }
  .app.dark .dd-divider { background: #3f3f46; }
  .dd-group-title {
    padding: 6px 12px 2px; font-size: 10px; text-transform: uppercase;
    color: #6b7280; letter-spacing: 0.5px; font-weight: 600;
  }
  .dd-empty { padding: 6px 12px; font-size: 11px; color: #9ca3af; font-style: italic; }
  .dd-item-row { display: flex; align-items: stretch; }
  .dd-item-row .dd-item { flex: 1; grid-template-columns: 1fr; }
  .dd-remove {
    background: none; border: none; cursor: pointer; padding: 0 10px;
    font-size: 12px; color: #9ca3af;
  }
  .dd-remove:hover { color: #b91c1c; background: #fee2e2; }

  /* Ana grid */
  .main-grid { display: grid; overflow: hidden; }

  /* Snippets */
  .snippets { display: flex; flex-direction: column; background: #fafbfc; overflow: hidden; }
  .app.dark .snippets { background: #252526; }
  .snippets-header { padding: 0.5rem; border-bottom: 1px solid #e5e7eb; background: #fff; }
  .app.dark .snippets-header { background: #2d2d30; border-bottom-color: #3f3f46; }
  .snippets-header h3 {
    margin: 0 0 0.4rem 0; font-size: 12px; color: #6b7280;
    text-transform: uppercase; letter-spacing: 0.5px;
    display: inline-block;
  }
  .snippet-add {
    float: right; margin-top: -2px;
    border: 1px solid #cbd0d6; background: #fff; border-radius: 3px;
    font-size: 11px; padding: 0.1rem 0.4rem; cursor: pointer;
  }
  .snippet-add:hover { background: #eef4ff; }
  .app.dark .snippet-add { background: #3c3c3c; border-color: #555; }
  .search {
    width: 100%; padding: 0.3rem 0.5rem;
    border: 1px solid #cbd0d6; border-radius: 3px; font-size: 12px;
  }
  .app.dark .search { background: #3c3c3c; color: #e6e6e6; border-color: #555; }
  .tabs { display: flex; flex-wrap: wrap; gap: 2px; margin-top: 0.5rem; }
  .tab {
    padding: 0.2rem 0.5rem;
    border: 1px solid #cbd0d6; background: #fff; border-radius: 3px;
    font-size: 11px; cursor: pointer;
  }
  .app.dark .tab { background: #3c3c3c; color: #e6e6e6; border-color: #555; }
  .tab:hover { background: #eef0f3; }
  .tab.active { background: #0a5cff; color: white; border-color: #0a5cff; }
  .hint {
    margin-top: 0.4rem;
    font-size: 10px; color: #6b7280; font-style: italic;
    padding: 4px 6px; background: #f0f2f5; border-radius: 3px;
  }
  .app.dark .hint { background: #3c3c3c; color: #a0a0a0; }
  .snippet-list { flex: 1; overflow-y: auto; padding: 0.3rem; }
  .snippet-item {
    display: grid; grid-template-columns: auto 1fr auto auto auto;
    align-items: center; gap: 0.5rem;
    width: 100%; padding: 0.4rem 0.5rem; margin-bottom: 2px;
    border: 1px solid transparent; background: transparent; border-radius: 3px;
    text-align: left; cursor: grab; font-size: 12px; color: inherit;
    user-select: none;
  }
  .snippet-item:active { cursor: grabbing; opacity: 0.7; }
  .snippet-item:hover { background: #eef4ff; border-color: #b3d4ff; }
  .app.dark .snippet-item:hover { background: #094771; border-color: #0a5cff; }
  .snippet-item .icon { font-size: 14px; }
  .snippet-item .key {
    font-family: ui-monospace, Menlo, monospace; font-size: 10px; color: #6b7280;
  }
  .snippet-edit, .snippet-delete {
    border: none; background: none; cursor: pointer; font-size: 11px;
    padding: 2px 4px; border-radius: 3px; opacity: 0.6;
  }
  .snippet-edit:hover, .snippet-delete:hover { opacity: 1; background: #e5e7eb; }
  .app.dark .snippet-edit:hover, .app.dark .snippet-delete:hover { background: #3c3c3c; }
  .muted { color: #9ca3af; padding: 1rem; text-align: center; }

  /* Editors */
  .editors { display: grid; background: #fff; overflow: hidden; }
  .app.dark .editors { background: #1e1e1e; }
  .editor-slot { overflow: hidden; position: relative; }

  /* Welcome */
  .welcome {
    display: flex; flex-direction: column; align-items: center; justify-content: center;
    height: 100%; padding: 2rem; text-align: center;
    background: linear-gradient(180deg, #fafbfc 0%, #f0f4ff 100%);
  }
  .app.dark .welcome {
    background: linear-gradient(180deg, #252526 0%, #1a2947 100%);
  }
  .welcome h2 { margin: 0 0 0.5rem; color: #0a5cff; font-size: 22px; }
  .welcome p { margin: 0 0 1rem; color: #4b5563; }
  .app.dark .welcome p { color: #a0a0a0; }
  .welcome.sub { padding: 1rem; background: none; }
  .app.dark .welcome.sub { background: none; }
  .welcome-actions { display: flex; gap: 0.5rem; flex-wrap: wrap; justify-content: center; }
  .w-btn {
    padding: 0.6rem 1.2rem; border: 1px solid #cbd0d6;
    background: #fff; border-radius: 6px; font-size: 13px; cursor: pointer;
  }
  .w-btn:hover { background: #f0f2f5; }
  .w-btn.primary { background: #0a5cff; color: white; border-color: #0a5cff; font-weight: 600; }
  .w-btn.primary:hover { background: #0847c9; }
  .app.dark .w-btn { background: #3c3c3c; color: #e6e6e6; border-color: #555; }
  .app.dark .w-btn:hover { background: #4a4a4a; }
  .hint-lg { margin-top: 1.5rem; font-size: 11px; color: #6b7280; }

  /* Preview */
  .preview { display: flex; flex-direction: column; background: #f0f2f5; overflow: hidden; }
  .app.dark .preview { background: #1a1a1a; }
  .preview-frame-wrap {
    flex: 1;
    overflow: auto;
    display: flex; justify-content: center;
    padding: 8px;
  }
  .preview-frame-container {
    width: 100%;
    background: white;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
    overflow: hidden;
  }
  .preview-frame-container iframe {
    display: block;
    border: none;
    background: white;
    width: 100%;
    height: 100%;
    min-height: 400px;
  }
  .preview-header {
    display: flex; align-items: center; justify-content: space-between; gap: 0.5rem;
  }
  .preview-actions { display: flex; gap: 3px; align-items: center; }
  .preview-actions button {
    padding: 2px 6px; font-size: 10px;
    text-transform: none; letter-spacing: 0; font-weight: 500;
    background: #fff; color: #1a1a1a;
    border: 1px solid #cbd0d6; border-radius: 3px; cursor: pointer;
    min-width: 24px;
  }
  .preview-actions button:hover:not(:disabled) { background: #eef4ff; border-color: #0a5cff; }
  .preview-actions button:disabled { opacity: 0.4; cursor: not-allowed; }
  .preview-actions button.active {
    background: #0a5cff; color: white; border-color: #0a5cff;
  }
  .app.dark .preview-actions button {
    background: #3c3c3c; color: #e6e6e6; border-color: #555;
  }
  .app.dark .preview-actions button:hover:not(:disabled) {
    background: #094771; border-color: #0a5cff;
  }
  .app.dark .preview-actions button.active { background: #0a5cff; color: white; }
  .mini-sep { width: 1px; height: 14px; background: #d5d8dc; margin: 0 2px; }

  /* Panel header */
  .panel-header {
    padding: 0.3rem 0.6rem; font-size: 11px; color: #6b7280;
    background: #f0f2f5; border-bottom: 1px solid #e5e7eb;
    text-transform: uppercase; letter-spacing: 0.5px;
    white-space: nowrap; overflow: hidden; text-overflow: ellipsis;
    display: flex; align-items: center; gap: 0.5rem;
  }
  .app.dark .panel-header {
    background: #2d2d30; color: #a0a0a0; border-bottom-color: #3f3f46;
  }
  .dirty-mark { color: #f59e0b; font-size: 14px; line-height: 1; }

  /* Drag ghost — mouse'un yanında hareket eden görsel */
  .drag-ghost {
    position: fixed;
    pointer-events: none;
    z-index: 10000;
    display: flex; align-items: center; gap: 6px;
    padding: 6px 12px;
    background: #fff;
    border: 2px solid #cbd0d6;
    border-radius: 6px;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.2);
    font-size: 12px; font-family: ui-monospace, Menlo, monospace;
    color: #1a1a1a;
    transition: border-color 0.1s;
  }
  .drag-ghost.on-target {
    border-color: #0a5cff;
    background: #eef4ff;
    color: #0a5cff;
  }
  .app.dark .drag-ghost { background: #2d2d30; color: #e6e6e6; border-color: #555; }
  .app.dark .drag-ghost.on-target { background: #094771; color: #fff; border-color: #0a5cff; }
  .ghost-icon { font-size: 14px; }
  .ghost-key { font-weight: 600; }
  .ghost-target { font-size: 10px; opacity: 0.8; }

  /* ─── Çıkış onay modalı ──────────────────────────────────────── */
  .exit-overlay {
    position: fixed;
    inset: 0;
    background: rgba(15, 23, 42, 0.55);
    z-index: 30000;
    display: flex;
    align-items: center;
    justify-content: center;
  }
  .exit-modal {
    width: min(420px, 90vw);
    background: #fff;
    border-radius: 10px;
    box-shadow: 0 20px 60px rgba(0, 0, 0, 0.35);
    padding: 1.5rem;
  }
  .app.dark .exit-modal {
    background: #2d2d30;
    color: #e6e6e6;
  }
  .exit-modal h3 {
    margin: 0 0 0.75rem;
    font-size: 16px;
    color: #b45309;
  }
  .exit-modal p {
    margin: 0 0 1.25rem;
    font-size: 13px;
    line-height: 1.5;
    color: #374151;
  }
  .style-modal {
    width: min(560px, 92vw);
    max-height: 80vh;
    background: #fff;
    border-radius: 10px;
    box-shadow: 0 20px 60px rgba(0, 0, 0, 0.35);
    padding: 1.5rem;
    display: flex;
    flex-direction: column;
  }
  .app.dark .style-modal { background: #2d2d30; color: #e6e6e6; }
  .style-modal h3 { margin: 0 0 0.75rem; font-size: 16px; color: #0a5cff; }
  .style-modal p { margin: 0 0 0.75rem; font-size: 13px; line-height: 1.5; color: #374151; }
  .style-preview {
    background: #f5f6f8;
    border: 1px solid #d5d8dc;
    border-radius: 6px;
    padding: 0.75rem;
    font-family: ui-monospace, Menlo, monospace;
    font-size: 11px;
    line-height: 1.5;
    overflow: auto;
    max-height: 40vh;
    margin: 0 0 1.25rem;
    white-space: pre-wrap;
  }
  .app.dark .style-preview { background: #1e1e1e; border-color: #3f3f46; }
  .exit-actions {
    display: flex;
    justify-content: flex-end;
    gap: 0.5rem;
    flex-wrap: wrap;
  }
  .exit-btn {
    padding: 0.5rem 1rem;
    border: 1px solid #cbd0d6;
    border-radius: 6px;
    font-size: 13px;
    cursor: pointer;
    background: #fff;
    color: #1a1a1a;
  }
  .exit-btn:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
  .exit-btn.cancel:hover:not(:disabled) {
    background: #f0f2f5;
  }
  .exit-btn.discard {
    color: #b91c1c;
    border-color: #fca5a5;
  }
  .exit-btn.discard:hover:not(:disabled) {
    background: #fee2e2;
  }
  .exit-btn.save {
    background: #0a5cff;
    color: white;
    border-color: #0a5cff;
    font-weight: 600;
  }
  .exit-btn.save:hover:not(:disabled) {
    background: #0847c9;
  }
</style>
