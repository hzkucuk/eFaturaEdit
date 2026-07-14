<script lang="ts">
  import { onMount, onDestroy, tick } from 'svelte';
  import { goto } from '$app/navigation';
  import { invoke } from '@tauri-apps/api/core';
  import { getCurrentWindow } from '@tauri-apps/api/window';
  import { getCurrentWebview } from '@tauri-apps/api/webview';
  import { listen } from '@tauri-apps/api/event';
  import { snippets, samples, completion, manifest, groupSnippetsByCategory } from '$lib/data';
  import { cssSnippets } from '$lib/data/css-snippets';
  import { xsltSnippets } from '$lib/data/xslt-snippets';
  import { xpathSnippets } from '$lib/data/xpath-snippets';
  import type { Snippet } from '$lib/data/types';
  import { transformXml, validateXml, isWellFormed, XsltError, engineStatus } from '$lib/xslt';
  import { settings, updatePanelSize, updateSetting, themeKind, loadApiKeys } from '$lib/settings.svelte';
  import {
    editorState,
    tabsState,
    newTab,
    closeTab,
    activateTab,
    tabTitle,
    dirtyTabs,
    isTabEmpty,
  } from '$lib/editor-state.svelte';
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
  import XPathConsole from '$lib/XPathConsole.svelte';
  import BatchRunner from '$lib/BatchRunner.svelte';
  import Splitter from '$lib/Splitter.svelte';
  import ContextMenu from '$lib/ContextMenu.svelte';
  import TabBar from '$lib/TabBar.svelte';
  import HelpModal from '$lib/HelpModal.svelte';
  import AIAssistant from '$lib/AIAssistant.svelte';
  import UpdateModal from '$lib/UpdateModal.svelte';
  import { checkForUpdate } from '$lib/updater.svelte';
  import { applyEdits, type AiSuggestion, type AiEdit, type AiTarget } from '$lib/ai-suggestion';
  import { instrumentXslt, type XsltElementRef } from '$lib/xslt-map';
  import { log, installGlobalErrorLogging } from '$lib/logger';
  import { m, f } from '$lib/i18n.svelte';
  import type { Completion } from '@codemirror/autocomplete';

  // ─── UI state ────────────────────────────────────────────────────────
  let statusMsg = $state('');
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
  let aiApplyOpen = $state(false);
  let aiApplyTarget = $state<AiTarget>('xslt');
  let aiApplySuggestion = $state<AiSuggestion | null>(null);
  let aiApplyNewText = $state(''); // onaylanınca editöre yazılacak TAM metin
  let aiApplyUnmatched = $state<AiEdit[]>([]);
  let aiApplyPreviewLoading = $state(false);
  let aiApplyPreviewHtml = $state(''); // popup içindeki sonuç önizlemesi
  let aiApplyPreviewError = $state(''); // sonuç geçersiz XML üretiyorsa hata metni
  let aiApplyShowFull = $state(false); // 'full' önerinin dev metnini isteğe bağlı göster
  let aiApplyZoom = $state(0.6); // popup içi önizleme yakınlaştırması (0.25–2.0)

  function zoomAiApply(delta: number) {
    aiApplyZoom = Math.max(0.25, Math.min(2.0, +(aiApplyZoom + delta).toFixed(2)));
  }

  // Uygulanabilecek gerçek bir değişiklik var mı? (Tüm düzenlemeler
  // eşleşmediyse newText mevcut metne eşittir, uygulanacak bir şey yok.)
  const aiApplyHasChange = $derived.by(() => {
    const current = aiApplyTarget === 'xslt' ? editorState.xsltText : editorState.xmlText;
    return aiApplyNewText !== current;
  });

  // Yalnızca "tam dosya" önerisi için: öneri tam bir belge gibi görünmüyorsa
  // (kök öğe yok ya da mevcut dosyadan çok kısa) tüm dosyayı bir parçayla ezip
  // bozabilir — uyar. Hedefli düzenlemelerde bu risk yok.
  const aiApplyLooksPartial = $derived.by(() => {
    if (aiApplySuggestion?.kind !== 'full') return false;
    const code = aiApplyNewText.trim();
    if (!code) return false;
    const hasRoot = aiApplyTarget === 'xslt'
      ? /<\?xml|<xsl:stylesheet|<xsl:transform/i.test(code)
      : /<\?xml|<\w[\w:-]*[\s>]/.test(code.slice(0, 200));
    const current = (aiApplyTarget === 'xslt' ? editorState.xsltText : editorState.xmlText).trim();
    const muchShorter = current.length > 200 && code.length < current.length * 0.5;
    return !hasRoot || muchShorter;
  });

  // Editor referansları (bind:this)
  let xsltEditor = $state<CodeEditor>();
  let xmlEditor = $state<CodeEditor>();
  /** XPath test konsolu açık mı (XML panelinin altında). */
  let xpathOpen = $state(false);
  /** Toplu regresyon penceresi açık mı. */
  let batchOpen = $state(false);
  let previewFrame = $state<HTMLIFrameElement>();

  // Preview sağ tık menüsü
  let previewMenu = $state<{ x: number; y: number } | null>(null);

  // Panel boyutları (localStorage persist)
  let snippetsWidth = $state(settings.panelSizes.snippetsWidth);
  let editorsWidth = $state(settings.panelSizes.editorsWidth);
  let xsltHeight = $state(settings.panelSizes.xsltHeight);
  let aiPanelHeight = $state(settings.panelSizes.aiPanelHeight);
  $effect(() => updatePanelSize('snippetsWidth', snippetsWidth));
  $effect(() => updatePanelSize('editorsWidth', editorsWidth));
  $effect(() => updatePanelSize('xsltHeight', xsltHeight));
  $effect(() => updatePanelSize('aiPanelHeight', aiPanelHeight));

  // ─── Snippet grupları ───────────────────────────────────────────────
  const allSnippets = $derived([
    ...snippets,
    ...xsltSnippets,
    ...xpathSnippets,
    ...cssSnippets,
    ...userSnippets,
  ]);
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
    // Sekme değişimi de "metin değişti" sayılır (proxy başka sekmeyi gösterir).
    // Ama o sekmenin önizlemesi zaten hazır — 600 KB'lık faturayı boşuna yeniden
    // dönüştürüp her sekme geçişini yavaşlatma.
    if (skipNextAutoTransform) {
      skipNextAutoTransform = false;
      return;
    }
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

  // ─── Sekmeler ────────────────────────────────────────────────────────
  // Bir sekme = bir XSLT+XML çifti + önizlemesi (bkz. editor-state.svelte.ts).
  //
  // CodeMirror örnekleri sekmeler arasında PAYLAŞILIR — sekme başına ayrı editör
  // yok. Bu yüzden sekme değişince aktif sekmenin metnini editörlere elle yazmak
  // ve dirty izleyiciyi susturmak gerekir; yoksa sekmeye geçer geçmez içerik
  // "değişmiş" sayılır ve kullanıcı hiç dokunmadığı dosyayı kaydetmeye çağrılır.

  /** Sekme değişiminde önizlemeyi yeniden derlemeyi bir kez atla (zaten önbellekte). */
  let skipNextAutoTransform = false;

  /** Aktif sekmenin içeriğini CodeMirror'a yazar. */
  function syncEditorsFromState() {
    ignoreNextChange.xslt = true;
    ignoreNextChange.xml = true;
    xsltEditor?.setValue(editorState.xsltText);
    xmlEditor?.setValue(editorState.xmlText);
  }

  function switchToTab(id: number) {
    if (id === tabsState.activeId) return;
    activateTab(id);
    // Önizleme sekmeyle birlikte taşınır; varsa yeniden dönüştürmeye gerek yok.
    skipNextAutoTransform = Boolean(editorState.previewHtml);
    syncEditorsFromState();
  }

  function addTab() {
    newTab();
    skipNextAutoTransform = true; // boş sekme — dönüştürecek bir şey yok
    syncEditorsFromState();
    status(m.tabs.opened);
  }

  /**
   * Sekmeyi kapat. Kaydedilmemiş değişiklik varsa <b>sor</b> — sekme kapatmak,
   * pencereyi kapatmak kadar kolay veri kaybettirir.
   */
  async function requestCloseTab(id: number) {
    const tab = tabsState.list.find((t) => t.id === id);
    if (!tab) return;

    if (tab.xsltDirty || tab.xmlDirty) {
      const proceed = await ask(f(m.tabs.closeDirtyBody, { name: tabTitle(tab, m.tabs.newTab) }), {
        title: m.tabs.closeDirtyTitle,
        kind: 'warning',
      });
      if (!proceed) return; // "İptal" → sekme açık kalır
    }

    closeTab(id);
    skipNextAutoTransform = Boolean(editorState.previewHtml);
    syncEditorsFromState();
  }

  /**
   * Bir dosya yüklenirken hedef sekmeyi seçer.
   *
   * Kural: <b>kaydedilmemiş içerik asla ezilmez.</b> Yüklenecek slot ('xslt' /
   * 'xml' / çift için 'both') aktif sekmede kirliyse yeni sekme açılır; değilse
   * (boş ya da diske yazılmış) aktif sekmeye yüklenir — böylece "şablonu aç,
   * sonra faturayı aç" akışı eskisi gibi tek sekmede çalışır.
   */
  function prepareTargetTab(slot: 'xslt' | 'xml' | 'both'): void {
    const t = tabsState.list.find((x) => x.id === tabsState.activeId);
    if (!t) return;
    const clash =
      (slot === 'xslt' && t.xsltDirty) ||
      (slot === 'xml' && t.xmlDirty) ||
      (slot === 'both' && (t.xsltDirty || t.xmlDirty));
    if (!clash || isTabEmpty(t)) return;

    // Yeni sekme, veri yüklenirken şablonu DEVRALIR: "aynı şablon, başka fatura"
    // bu uygulamanın en sık akışı — boş bir sekme açmak önizlemeyi kör bırakırdı.
    // Yalnızca TEMİZ şablon devralınır: diskteki hâliyle aynı olduğu için aynı
    // yolu gösteren iki sekme aynı içeriği taşır. Kirli metni kopyalasaydık tek
    // bir yol için iki farklı sürüm doğar, biri diğerini sessizce ezerdi.
    const inherit =
      slot === 'xml' && t.xsltText && !t.xsltDirty
        ? { xsltText: t.xsltText, xsltPath: t.xsltPath }
        : {};
    newTab(inherit);
  }

  /**
   * Aynı dosyayı gösteren DİĞER temiz sekmeleri diskteki yeni içerikle eşitler.
   *
   * Şablon devralma yüzünden bir yol birden çok sekmede açık olabilir. Kaydettikten
   * sonra o sekmeler eski metni tutmaya devam ederse, kullanıcı oraya geçip
   * kaydettiğinde <b>az önceki kaydını sessizce geri alır.</b> Kirli sekmelere
   * dokunulmaz — oradaki metin kullanıcının kendi düzenlemesidir.
   */
  function syncCleanTwins(kind: 'xslt' | 'xml', path: string, text: string) {
    for (const t of tabsState.list) {
      if (t.id === tabsState.activeId) continue;
      if (kind === 'xslt' && t.xsltPath === path && !t.xsltDirty) t.xsltText = text;
      if (kind === 'xml' && t.xmlPath === path && !t.xmlDirty) t.xmlText = text;
    }
  }

  // ─── Actions: load ──────────────────────────────────────────────────
  async function loadDefaultSample() {
    await loadSampleByPath('/samples/default.xslt', '/samples/default.xml', 'default');
  }

  async function loadCarryForwardSample() {
    await loadSampleByPath('/samples/default-nakli-yekun.xslt', '/samples/default.xml', 'nakli yekûn');
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
      prepareTargetTab('both');
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
      status(f(m.status.sampleLoaded, { label, xsltKb: (xsl.length / 1024).toFixed(0), xmlKb: (xml.length / 1024).toFixed(0) }));
      if (settings.autoTransformOnLoad) await runTransform();
    } catch (err) {
      status(f(m.errors.loadFailed, { msg: (err as Error).message }), true);
    }
  }

  // ─── Kullanıcı örnekleri ────────────────────────────────────────────
  async function refreshUserSamples() {
    try {
      userSamples = await listUserSamples();
    } catch (err) {
      status(f(m.samples.userReadFailed, { msg: (err as Error).message }), true);
    }
  }

  async function loadUserSampleEntry(sample: UserSample) {
    sampleMenuOpen = false;
    try {
      const { xslt, xml } = await loadUserSample(sample);
      prepareTargetTab('both');
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
      status(f(m.samples.loaded, { name: sample.name }));
      if (settings.autoTransformOnLoad) await runTransform();
    } catch (err) {
      status(f(m.errors.loadFailed, { msg: (err as Error).message }), true);
    }
  }

  async function addCurrentAsUserSample() {
    if (!editorState.xsltPath || !editorState.xmlPath) {
      status(m.samples.needSavedPair, true);
      return;
    }
    try {
      const sample = await addSamplePair(editorState.xsltPath, editorState.xmlPath);
      status(f(m.samples.added, { name: sample.name }));
      await refreshUserSamples();
    } catch (err) {
      status(f(m.samples.addFailed, { msg: (err as Error).message }), true);
    }
  }

  async function removeUserSampleEntry(sample: UserSample, e: MouseEvent) {
    e.stopPropagation();
    try {
      await removeUserSample(sample);
      status(f(m.samples.removed, { name: sample.name }));
      await refreshUserSamples();
    } catch (err) {
      status(f(m.errors.deleteFailed, { msg: (err as Error).message }), true);
    }
  }

  async function openUserSamplesFolder() {
    try {
      await openSamplesFolder();
    } catch (err) {
      status(f(m.samples.folderOpenFailed, { msg: (err as Error).message }), true);
    }
  }

  // ─── Kullanıcı snippet'leri ─────────────────────────────────────────
  async function refreshUserSnippets() {
    try {
      userSnippets = await listUserSnippets();
    } catch (err) {
      status(f(m.snip.userReadFailed, { msg: (err as Error).message }), true);
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
      status(f(m.snip.saved, { key: s.key }));
    } catch (err) {
      status(f(m.snip.saveFailed, { msg: (err as Error).message }), true);
    }
  }

  async function onDeleteSnippet(s: Snippet, e: MouseEvent) {
    e.stopPropagation();
    const confirmed = await ask(f(m.snip.deleteConfirm, { name: s.displayName }), {
      title: 'Snippet Sil',
      kind: 'warning',
    });
    if (!confirmed) return;
    try {
      userSnippets = await removeUserSnippet(s.key);
      status(f(m.snip.deleted, { key: s.key }));
    } catch (err) {
      status(f(m.errors.deleteFailed, { msg: (err as Error).message }), true);
    }
  }

  /**
   * XSLT tek başına açıldıysa (sürükle-bırak, "Birlikte Aç" veya Aç düğmesi)
   * önizleme boş kalmasın: paketli varsayılan UBL-TR verisiyle eşle.
   *
   * Yol atanmaz ve `dirty` işaretlenmez — bu veri diske ait değildir; kullanıcı
   * kaydetmek isterse "Farklı Kaydet" der. Zaten XML yüklüyse dokunulmaz.
   * @returns varsayılan veri yüklendiyse `true`
   */
  async function ensureXmlData(): Promise<boolean> {
    if (editorState.xmlText.trim()) return false;
    try {
      const res = await fetch('/samples/default.xml');
      if (!res.ok) throw new Error(`default.xml: ${res.status}`);
      const xml = await res.text();
      ignoreNextChange.xml = true;
      editorState.xmlText = xml;
      editorState.xmlPath = null;
      editorState.xmlDirty = false;
      xmlEditor?.setValue(xml);
      return true;
    } catch (err) {
      status(f(m.errors.defaultXmlFailed, { msg: (err as Error).message }), true);
      return false;
    }
  }

  async function openXslt() {
    try {
      const result = await openFile('xslt');
      if (!result) return;
      if (!isXsltDoc(result.content)) {
        status(f(m.errors.notAnXslt, { name: basename(result.path) }), true);
        return;
      }
      prepareTargetTab('xslt');
      ignoreNextChange.xslt = true;
      editorState.xsltText = result.content;
      editorState.xsltPath = result.path;
      editorState.xsltDirty = false;
      xsltEditor?.setValue(result.content);
      pushRecent(result.path, 'xslt');
      const paired = await ensureXmlData();
      status(
        paired
          ? f(m.status.xsltOpenedPaired, { path: result.path })
          : f(m.status.xsltOpened, { path: result.path })
      );
      if (settings.autoTransformOnLoad && editorState.xmlText) await runTransform();
    } catch (err) {
      status(f(m.errors.openFailed, { what: 'XSLT', msg: (err as Error).message }), true);
    }
  }

  async function openXml() {
    try {
      const result = await openFile('xml');
      if (!result) return;
      // Şablonu veri alanına almayı reddet — aksi halde dönüşümün girdisi
      // şablonun kendisi olur ve önizleme sessizce anlamsız çıkar.
      if (isXsltDoc(result.content)) {
        status(f(m.errors.notAnXml, { name: basename(result.path) }), true);
        return;
      }
      prepareTargetTab('xml');
      ignoreNextChange.xml = true;
      editorState.xmlText = result.content;
      editorState.xmlPath = result.path;
      editorState.xmlDirty = false;
      xmlEditor?.setValue(result.content);
      pushRecent(result.path, 'xml');
      status(f(m.status.xmlOpened, { path: result.path }));
      if (settings.autoTransformOnLoad && editorState.xsltText) await runTransform();
    } catch (err) {
      status(f(m.errors.openFailed, { what: 'XML', msg: (err as Error).message }), true);
    }
  }

  /** Toplu koşu listesinden bir faturayı editöre yükler (satıra çift tık). */
  async function loadXmlFromPath(path: string) {
    try {
      const content = await reopenFile(path);
      prepareTargetTab('xml');
      ignoreNextChange.xml = true;
      editorState.xmlText = content.content;
      editorState.xmlPath = content.path;
      editorState.xmlDirty = false;
      xmlEditor?.setValue(content.content);
      pushRecent(content.path, 'xml');
      batchOpen = false;
      status(f(m.status.xmlOpened, { path: content.path }));
      if (editorState.xsltText) await runTransform();
    } catch (err) {
      status(f(m.errors.openFailed, { what: 'XML', msg: (err as Error).message }), true);
    }
  }

  /** XSLT ad alanı — bir belgeyi şablon yapan tek kesin işaret. */
  const XSLT_NS = 'http://www.w3.org/1999/XSL/Transform';

  /**
   * Belge bir XSLT şablonu mu?
   *
   * Uzantıya güvenilmez: XSLT de geçerli bir XML'dir, `.xml` olarak kaydedilmiş
   * şablonlar vardır (ve tersi). Belirleyici olan, XSLT ad alanının bildirilmiş
   * olmasıdır — UBL-TR fatura verisi bunu asla içermez. Kök öğe genelde ilk
   * birkaç KB'dadır; tüm dosyayı taramaya gerek yok.
   */
  function isXsltDoc(text: string): boolean {
    return text.slice(0, 8192).includes(XSLT_NS);
  }

  /**
   * Dışarıdan gelen dosya yollarını (Finder/Explorer'dan sürükleme veya
   * "Birlikte Aç") doğru editöre yükler.
   *
   * Hedef editör **içeriğe** göre seçilir, uzantıya değil: şablon şablon
   * editörüne, veri veri editörüne. Böylece `.xml` uzantılı bir XSLT yanlışlıkla
   * veri alanına düşmez (ve tersi). Aynı türden birden çok dosya bırakılırsa
   * ilki alınır. XSLT tek başına geldiyse `ensureXmlData()` ile varsayılan
   * veriye eşlenir ki önizleme hemen derlensin.
   */
  async function openPaths(paths: string[]) {
    const ext = (p: string) => p.slice(p.lastIndexOf('.') + 1).toLowerCase();
    const candidates = paths.filter((p) => ['xslt', 'xsl', 'xml'].includes(ext(p)));

    if (candidates.length === 0) {
      status(m.errors.onlySupportedFiles, true);
      return;
    }

    try {
      // Önce hepsini oku ve İÇERİĞİNE göre sınıflandır.
      const files = await Promise.all(
        candidates.map(async (p) => {
          const r = await reopenFile(p);
          return { ...r, xslt: isXsltDoc(r.content) };
        })
      );

      const xsltFile = files.find((f) => f.xslt);
      const xmlFile = files.find((f) => !f.xslt);
      const loaded: string[] = [];
      const skipped: string[] = [];

      prepareTargetTab(xsltFile && xmlFile ? 'both' : xsltFile ? 'xslt' : 'xml');

      // Aynı türden fazlası varsa ilkini al, kalanını sessizce yutma — söyle.
      for (const f of files) {
        if (f !== xsltFile && f !== xmlFile) {
          skipped.push(basename(f.path));
        }
      }

      if (xsltFile) {
        ignoreNextChange.xslt = true;
        editorState.xsltText = xsltFile.content;
        editorState.xsltPath = xsltFile.path;
        editorState.xsltDirty = false;
        xsltEditor?.setValue(xsltFile.content);
        pushRecent(xsltFile.path, 'xslt');
        loaded.push(`şablon: ${basename(xsltFile.path)}`);
      }
      if (xmlFile) {
        ignoreNextChange.xml = true;
        editorState.xmlText = xmlFile.content;
        editorState.xmlPath = xmlFile.path;
        editorState.xmlDirty = false;
        xmlEditor?.setValue(xmlFile.content);
        pushRecent(xmlFile.path, 'xml');
        loaded.push(`veri: ${basename(xmlFile.path)}`);
      }

      // XSLT geldi ama veri yoksa → varsayılan UBL-TR verisiyle eşle.
      if (xsltFile && (await ensureXmlData())) loaded.push(m.misc.defaultXmlLabel);

      const note = skipped.length > 0 ? f(m.misc.skippedNote, { files: skipped.join(', ') }) : '';
      status(f(m.status.opened, { what: loaded.join(' · ') + note }));

      if (settings.autoTransformOnLoad && editorState.xsltText && editorState.xmlText) {
        await runTransform();
      }
    } catch (err) {
      status(`Dosya açılamadı: ${(err as Error).message}`, true);
    }
  }

  async function reopenRecent(path: string, kind: 'xslt' | 'xml') {
    recentMenuOpen = false;
    try {
      const result = await reopenFile(path);
      prepareTargetTab(kind);
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
      status(f(m.misc.reopened, { path }));
      if (settings.autoTransformOnLoad && editorState.xsltText && editorState.xmlText) await runTransform();
    } catch (err) {
      status(f(m.misc.openFailedShort, { msg: (err as Error).message }), true);
    }
  }

  // ─── Actions: save ──────────────────────────────────────────────────
  /**
   * Bir dosyanın kaydedilmesi gerekiyor mu?
   *
   * "Değişti mi" yetmez: örnek yükleyip yalnızca XSLT'yi düzenlersen XML
   * `dirty` olmaz ve **diske hiç yazılmazdı** — elinde diskte eşi olmayan bir
   * şablon kalırdı. XSLT+XML tek bir çalışma birimidir; diskte karşılığı
   * olmayan bir eş de kaydedilmelidir.
   */
  function needsSave(kind: 'xslt' | 'xml', silent: boolean): boolean {
    const text = kind === 'xslt' ? editorState.xsltText : editorState.xmlText;
    const path = kind === 'xslt' ? editorState.xsltPath : editorState.xmlPath;
    const dirty = kind === 'xslt' ? editorState.xsltDirty : editorState.xmlDirty;
    if (!text.trim()) return false;
    // Diskte hiç yoksa kaydedilmeli — ama otomatik (sessiz) kayıt kullanıcının
    // önüne dialog açamaz; onu elle kaydetmeye bırak.
    if (!path) return !silent;
    return dirty;
  }

  /** Kaydedilecek bir şey var mı? (değişmiş VEYA diskte hiç olmayan dosya) */
  const canSave = $derived(needsSave('xslt', false) || needsSave('xml', false));

  /**
   * Çıkış modalında "neyi kaydedeceğiz" listesi. Tek sekmede eskisi gibi
   * "XSLT ve XML" der; birden çok sekme açıkken sekme adlarını sayar — kullanıcı
   * hangi faturanın kaydedilmemiş olduğunu görmeli.
   */
  const dirtyLabel = $derived.by(() => {
    const dirty = dirtyTabs();
    if (tabsState.list.length === 1 && dirty.length === 1) {
      return [dirty[0].xsltDirty && 'XSLT', dirty[0].xmlDirty && 'XML'].filter(Boolean).join(m.exit.and);
    }
    return dirty.map((t) => tabTitle(t, m.tabs.newTab)).join(m.exit.and);
  });

  /** XSLT ve XML'i BİRLİKTE kaydet (Cmd+S, Kaydet düğmesi, otomatik kayıt). */
  async function saveAll(silent = false): Promise<boolean> {
    let anySaved = false;
    let anyError = false;

    for (const kind of ['xslt', 'xml'] as const) {
      if (!needsSave(kind, silent)) continue;
      const ok = await saveOne(kind, silent);
      anySaved ||= ok;
      anyError ||= !ok;
    }

    if (!anySaved && !anyError && !silent) {
      status(m.status.nothingToSave);
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
          status(f(m.misc.syntaxErrLine, { kind: kind.toUpperCase(), line: err.line, msg: err.message }), true);
      } else if (!silent) {
        status(f(m.misc.syntaxErr, { kind: kind.toUpperCase(), msg: (err as Error).message }), true);
      }
      return false;
    }

    try {
      const currentPath = kind === 'xslt' ? editorState.xsltPath : editorState.xmlPath;
      if (currentPath) {
        await saveFile(currentPath, text);
        if (kind === 'xslt') editorState.xsltDirty = false;
        else editorState.xmlDirty = false;
        syncCleanTwins(kind, currentPath, text);
        pushRecent(currentPath, kind);
        if (!silent) status(f(m.status.saved, { kind: kind.toUpperCase(), path: currentPath }));
      } else {
        const path = await saveFileAs(
          text,
          kind,
          saveTargetFor(kind, kind === 'xslt' ? 'yeni.xslt' : 'yeni.xml'),
        );
        if (!path) return false;
        if (kind === 'xslt') {
          editorState.xsltPath = path;
          editorState.xsltDirty = false;
        } else {
          editorState.xmlPath = path;
          editorState.xmlDirty = false;
        }
        pushRecent(path, kind);
        if (!silent) status(f(m.misc.savedAsOne, { kind: kind.toUpperCase(), path }));
      }
      return true;
    } catch (err) {
      status(f(m.errors.saveFailed, { msg: (err as Error).message }), true);
      return false;
    }
  }

  /** Yolun klasör kısmı ("/a/b/c.xslt" → "/a/b"). */
  function dirname(path: string): string {
    const i = Math.max(path.lastIndexOf('/'), path.lastIndexOf('\\'));
    return i > 0 ? path.slice(0, i) : '';
  }

  /**
   * Kaydetme penceresinin açılacağı yol.
   *
   * Yalnızca dosya adı verilirse (eski davranış) işletim sistemi EN SON
   * KULLANILAN klasörü açar — bu, alakasız bir yere (".../muhasebe/06/") düşmeye
   * yol açıyordu. Doğrusu: üzerinde çalışılan dosyanın yanı. O da yoksa
   * diğer dosyanın (XSLT↔XML çifti) klasörü; hiçbiri yoksa OS'a bırak.
   */
  function saveTargetFor(kind: 'xslt' | 'xml', fileName: string): string {
    const own = kind === 'xslt' ? editorState.xsltPath : editorState.xmlPath;
    const other = kind === 'xslt' ? editorState.xmlPath : editorState.xsltPath;
    const base = own ? dirname(own) : other ? dirname(other) : '';
    return base ? `${base}/${fileName}` : fileName;
  }

  /**
   * "Farklı Kaydet" — çifti birlikte kaydeder: önce XSLT, sonra XML.
   *
   * Eskiden yalnızca XSLT'yi kaydediyordu; XML kullanıcının seçtiği yeni klasöre
   * gitmediği için ortaya **eşleşmeyen bir çift** çıkıyordu. XML penceresi,
   * XSLT'nin kaydedildiği klasörde açılır ve varsa mevcut adı önerir.
   */
  async function saveAsPair() {
    const xsltPath = await saveFileAs(
      editorState.xsltText,
      'xslt',
      saveTargetFor('xslt', basename(editorState.xsltPath ?? 'yeni.xslt')),
    );
    if (!xsltPath) return; // kullanıcı vazgeçti → XML'e hiç dokunma
    editorState.xsltPath = xsltPath;
    editorState.xsltDirty = false;
    pushRecent(xsltPath, 'xslt');

    const saved = [`XSLT: ${xsltPath}`];

    if (editorState.xmlText.trim()) {
      const suggested = basename(editorState.xmlPath ?? 'yeni.xml');
      const xmlPath = await saveFileAs(
        editorState.xmlText,
        'xml',
        `${dirname(xsltPath)}/${suggested}`,
      );
      if (xmlPath) {
        editorState.xmlPath = xmlPath;
        editorState.xmlDirty = false;
        pushRecent(xmlPath, 'xml');
        saved.push(`XML: ${xmlPath}`);
      } else {
        // XSLT yazıldı ama kullanıcı XML'i atladı — sessiz geçme, söyle.
        status(f(m.status.savedXsltOnly, { path: xsltPath }), true);
        if (settings.autoTransformOnSave) await runTransform();
        return;
      }
    }

    status(f(m.status.savedAs, { what: saved.join(' · ') }));
    if (settings.autoTransformOnSave) await runTransform();
  }

  // ─── Actions: transform ─────────────────────────────────────────────
  async function runTransform(silent = false) {
    if (!editorState.xsltText || !editorState.xmlText) {
      if (!silent) status(m.status.needBoth, true);
      return;
    }
    if (!silent) status(m.status.transforming);
    try {
      // Görsel düzenleyici açıkken önizleme, `data-xsl-id` enjekte edilmiş
      // GEÇİCİ bir kopyayla üretilir; böylece önizlemedeki her öğenin şablonda
      // hangi satırdan geldiği bilinir. Bu kopya BELLEKTE kalır — kullanıcının
      // dosyasına (editorState.xsltText) asla yazılmaz, kaydedilmez, dışa
      // aktarılmaz. Doğrulandı: data-xsl-id'ler çıkarıldığında çıktı, temiz
      // dönüşümle birebir aynıdır (render etkilenmez).
      let xsltForPreview = editorState.xsltText;
      if (wzMode) {
        const { instrumented, refs } = instrumentXslt(editorState.xsltText);
        xsltForPreview = instrumented;
        wzRefs = refs;
      } else {
        wzRefs = null;
      }
      const t0 = performance.now();
      const html = await transformXml(editorState.xmlText, xsltForPreview);
      lastTransformMs = Math.round(performance.now() - t0);
      lastHtmlBytes = html.length;
      editorState.previewHtml = html;

      // Saxon çalışmıyorsa tarayıcının XSLT 1.0 işlemcisine düşülmüştür. Bunu
      // SESSİZ geçmek tehlikeli: 1.0 işlemcisi `format-dateTime`, `tokenize`,
      // `for-each-group` gibi 2.0 komutlarını hata vermeden yok sayar —
      // kullanıcı şablonunun çalıştığını sanır, oysa çıktı yanlıştır.
      lastTransformError = ''; // başarılı dönüşüm → AI'a taşınacak hata kalmadı

      if (!engineStatus.saxon) {
        if (!engineWarning) {
          log.error(`[motor] Saxon kullanilamiyor, XSLT 1.0'a dusuldu: ${engineStatus.reason}`);
        }
        engineWarning = engineStatus.reason;
        status(f(m.status.transformedFallback, { kb: (html.length / 1024).toFixed(1) }), true);
      } else {
        status(f(m.status.transformed, { kb: (html.length / 1024).toFixed(1) }));
      }
    } catch (err) {
      if (err instanceof XsltError && err.line) {
        const editor = err.source === 'xml' ? xmlEditor : xsltEditor;
        editor?.goToLine(err.line, err.column ?? 1);
        editorState.previewHtml = `<pre style="color:#c00;padding:1rem;font-family:monospace;">Hata (${err.source} satır ${err.line}):\n\n${escapeHtml(err.message)}</pre>`;
        status(f(m.misc.transformErrAt, { source: err.source ?? 'transform', line: err.line, msg: err.message }), true);
        // AI'a taşı: ajan modu KAPALIYKEN de model hatayı görebilsin (yoksa
        // kullanıcı hatayı elle kopyalamak zorunda kalıyordu).
        lastTransformError = `${err.source ?? 'transform'} — satır ${err.line}${err.column ? ', sütun ' + err.column : ''}: ${err.message}`;
      } else {
        const msg = (err as Error).message ?? String(err);
        editorState.previewHtml = `<pre style="color:#c00;padding:1rem;font-family:monospace;">${escapeHtml(msg)}</pre>`;
        status(f(m.misc.transformErr, { msg }), true);
        lastTransformError = msg;
      }
    }
  }

  // ─── Snippet insert (click) & drop (drag) ──────────────────────────
  function insertSnippet(snippet: Snippet) {
    if (xsltEditor) {
      xsltEditor.insertAtCursor(snippet.xsltCode);
      status(f(m.snip.inserted, { key: snippet.key }));
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
      status(f(m.snip.dragCancel, { key: snippetKey || '?' }));
      return;
    }
    if (!snippetText) {
      status(m.snip.dragEmpty, true);
      return;
    }
    const editor = editorKind === 'xslt' ? xsltEditor : xmlEditor;
    if (!editor) {
      status(f(m.snip.editorNotReady, { editor: editorKind }), true);
      return;
    }
    editor.insertAtCoords(x, y, snippetText);
    status(f(m.snip.insertedTo, { editor: editorKind.toUpperCase(), key: snippetKey || 'snippet' }));
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
      status(m.misc.previewEmpty, true);
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
      status(m.misc.printOpened);
    } catch (err) {
      status(f(m.misc.printFailed, { msg: (err as Error).message ?? String(err) }), true);
    }
  }

  function copyPreviewHtml() {
    if (!editorState.previewHtml) return;
    navigator.clipboard
      .writeText(editorState.previewHtml)
      .then(() => status(f(m.misc.htmlCopied, { kb: (editorState.previewHtml.length / 1024).toFixed(1) })))
      .catch((err) => status(f(m.misc.copyFailed, { msg: err.message }), true));
  }

  async function openDevTools() {
    try {
      await invoke('open_devtools');
      status(m.misc.devtoolsOpened);
    } catch (err) {
      status(f(m.misc.devtoolsFailed, { msg: (err as Error).message ?? String(err) }), true);
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
          reject(new Error(m.misc.previewTimeout));
        }
      }, 2000);
    });
  }

  async function captureStyleFromPreview() {
    if (!editorState.previewHtml) {
      status(m.misc.previewFirst, true);
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

  // ─── AI Asistan: önerilen değişikliği onaydan sonra uygula ──────────
  // Hedefli düzenlemeler (bul/değiştir) mevcut dosyaya doğru yerinden uygulanır;
  // "tam dosya" önerisi ise içeriği baştan yazar. Her iki durumda da sonuç
  // önce onay modalında gösterilir, editöre ancak onayla yazılır.
  function requestAiApply(suggestion: AiSuggestion) {
    const target = suggestion.target;
    const current = target === 'xslt' ? editorState.xsltText : editorState.xmlText;
    aiApplyTarget = target;
    aiApplySuggestion = suggestion;
    if (suggestion.kind === 'full') {
      aiApplyNewText = suggestion.code;
      aiApplyUnmatched = [];
    } else {
      const { result, unmatched } = applyEdits(current, suggestion.edits);
      aiApplyNewText = result;
      aiApplyUnmatched = unmatched;
    }

    // BOZUK ÖNERİ UYGULANMAZ. "Tam dosya" önerisi token sınırında kesilirse
    // (gerçek vaka: 172 KB fatura, 17 KB'lık yarım yanıtla ezildi) sonuç
    // iyi-biçimli olmaz. Uygula sonrası dosya OTOMATİK KAYDEDİLDİĞİ için bu,
    // doğrudan veri kaybıdır — modalı hiç açma, hatayı söyle.
    if (!isWellFormed(aiApplyNewText)) {
      aiApplyOpen = false;
      status(
        '⛔ AI önerisi geçerli bir XML/XSLT belgesi üretmiyor (yanıt kesilmiş olabilir) — ' +
          'uygulanmadı. Daha küçük bir değişiklik isteyin.',
        true,
      );
      return;
    }

    aiApplyShowFull = false;
    aiApplyOpen = true;
    void buildAiApplyPreview();
  }

  // Onay öncesi: uygulanacak metnin dönüştürülmüş halini popup içinde göster.
  async function buildAiApplyPreview() {
    aiApplyPreviewHtml = '';
    aiApplyPreviewError = '';
    aiApplyPreviewLoading = true;
    // transformXml gövdesi senkron ve ağır (600 KB'da ana thread'i bloklar).
    // Önce modalın DOM'a işlenip BOYANMASINA izin ver; aksi halde modal
    // görünmeden uygulama donmuş gibi olur.
    await tick();
    await new Promise((r) => requestAnimationFrame(() => requestAnimationFrame(() => r(null))));
    try {
      const xslt = aiApplyTarget === 'xslt' ? aiApplyNewText : editorState.xsltText;
      const xml = aiApplyTarget === 'xml' ? aiApplyNewText : editorState.xmlText;
      aiApplyPreviewHtml = await transformXml(xml, xslt);
    } catch (err) {
      const msg = (err as Error).message ?? String(err);
      aiApplyPreviewError = msg;
      aiApplyPreviewHtml = `<pre style="color:#c00;padding:1rem;font-family:monospace;white-space:pre-wrap;">Önizleme oluşturulamadı:\n\n${escapeHtml(msg)}</pre>`;
    } finally {
      aiApplyPreviewLoading = false;
    }
  }

  /**
   * AI önerisini editöre yaz ve DİSKE DE KAYDET.
   *
   * Otomatik kayıt olmadan dosya bayat kalıyordu: editör güncel ama diskteki
   * içerik eski oluyor, sonraki tur/dış araçlar eski dosyayla çalışıyordu.
   * `saveOne()` önce XML/XSLT syntax doğrulaması yapar — bozuk çıktı diske
   * yazılmaz, kullanıcı uyarılır.
   */
  async function confirmAiApply() {
    const target = aiApplyTarget;
    if (target === 'xslt') {
      editorState.xsltText = aiApplyNewText;
      xsltEditor?.setValue(aiApplyNewText);
    } else {
      editorState.xmlText = aiApplyNewText;
      xmlEditor?.setValue(aiApplyNewText);
    }
    aiApplyOpen = false;
    runTransform();

    const path = target === 'xslt' ? editorState.xsltPath : editorState.xmlPath;
    if (!path) {
      status(
        `AI önerisi ${target.toUpperCase()} editörüne uygulandı — dosya henüz diskte yok, "Farklı" ile kaydedin.`,
      );
      return;
    }
    const saved = await saveOne(target, true);
    status(
      saved
        ? `AI önerisi uygulandı ve ${target.toUpperCase()} kaydedildi: ${path}`
        : `AI önerisi uygulandı ama ${target.toUpperCase()} KAYDEDİLEMEDİ (syntax hatası) — düzeltip elle kaydedin.`,
      !saved,
    );
  }

  function cancelAiApply() {
    aiApplyOpen = false;
  }

  // AI paneline eklenen/yapıştırılan bir görseli base64 data URI olarak XSLT
  // editörüne, imleç konumuna <img> etiketiyle gömer (AI'a gerek yok — base64
  // uygulama tarafında üretilir).
  function embedImageInXslt(dataUrl: string, name: string) {
    if (!xsltEditor) {
      status('Önce XSLT editörüne tıklayıp imleci konumlandırın.', true);
      return;
    }
    const alt = name.replace(/"/g, '');
    xsltEditor.insertAtCursor(`<img src="${dataUrl}" alt="${alt}" style="width:150px; height:auto;" />`);
    status(f(m.misc.imageEmbedded, { name }));
    runTransform();
  }

  function setPreviewWidth(w: number | null) {
    updateSetting('previewWidth', w);
    status(f(m.misc.previewWidth, { w: w ? w + 'px' : m.misc.previewWidthFull }));
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
    // Kaynakta bitişik script açılış/kapanış metni YAZILMAZ (yorumda bile):
    // Svelte'in blok-sınırı ön taraması bunu gerçek bir etiket sanıp bileşenin
    // kendi script bloğunu erken kapatıyor (bkz. STYLE_TAG deseni). Bu yüzden
    // etiket adı değişkenden interpolasyonla üretilir.
    const SCRIPT_TAG = 'script';
    const bridgeJs = `
document.addEventListener('contextmenu', function(e) {
  e.preventDefault();
  window.parent.postMessage({ type: 'preview-contextmenu', x: e.clientX, y: e.clientY }, '*');
});
/* Iframe içindeki tıklamalar ana pencereye ULAŞMAZ; bu yüzden sağ tık menüsü
   önizlemeye tıklayarak kapanmıyordu (menü window'a gelen click ile kapanıyor).
   Kapatmayı ana pencereye biz haber veriyoruz. */
document.addEventListener('mousedown', function() {
  window.parent.postMessage({ type: 'preview-dismiss' }, '*');
}, true);
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

/* ─── Görsel düzenleyici (WYSIWYG Faz 1) ────────────────────────────────
   Seçim modu açıkken: fare üzerindeki öğeyi çerçevele, tıklanınca öğenin
   seçicisini + hesaplanmış stillerini uygulamaya bildir. Uygulama, stil
   panelindeki her değişiklikte buraya canlı CSS gönderir; XSLT'ye yalnızca
   "Uygula" denince yazılır.                                            */
var __wz = { on: false, hl: null, live: null };
var __WZ_PROPS = ['color','background-color','font-size','font-weight','font-style',
  'text-align','text-transform','padding','margin','border-width','border-style',
  'border-color','border-radius','width','height','display'];

function __wzHl() {
  if (__wz.hl) return __wz.hl;
  var d = document.createElement('div');
  d.setAttribute('data-wz', 'hl');
  d.style.cssText = 'position:fixed;pointer-events:none;z-index:2147483647;' +
    'border:2px solid #0a5cff;background:rgba(10,92,255,0.10);border-radius:2px;';
  document.body.appendChild(d);
  __wz.hl = d;
  return d;
}
function __wzFrame(el) {
  var r = el.getBoundingClientRect();
  var h = __wzHl();
  h.style.display = 'block';
  h.style.left = r.left + 'px';
  h.style.top = r.top + 'px';
  h.style.width = r.width + 'px';
  h.style.height = r.height + 'px';
}
/* Öğe için kararlı bir CSS seçicisi üret: id > class > en yakın id'li atadan
   nth-of-type yolu. Şablonlarda id yaygın olduğundan çoğu öğe #id'ye düşer. */
function __wzSelector(el) {
  if (el.id) return '#' + el.id;
  var parts = [];
  var n = el;
  while (n && n.nodeType === 1 && n !== document.documentElement) {
    if (n.id) { parts.unshift('#' + n.id); break; }
    var seg = n.tagName.toLowerCase();
    var cls = (n.getAttribute('class') || '').trim().split(/\\s+/).filter(Boolean);
    if (cls.length) {
      seg += '.' + cls.join('.');
    } else if (n.parentElement) {
      var i = 1, s = n;
      while ((s = s.previousElementSibling)) { if (s.tagName === n.tagName) i++; }
      seg += ':nth-of-type(' + i + ')';
    }
    parts.unshift(seg);
    n = n.parentElement;
    if (parts.length > 5) break;
  }
  return parts.join(' > ');
}
function __wzComputed(el) {
  var cs = getComputedStyle(el);
  var o = {};
  for (var i = 0; i < __WZ_PROPS.length; i++) {
    o[__WZ_PROPS[i]] = cs.getPropertyValue(__WZ_PROPS[i]);
  }
  return o;
}
document.addEventListener('mouseover', function(e) {
  if (!__wz.on) return;
  if (e.target && e.target.getAttribute && e.target.getAttribute('data-wz')) return;
  __wzFrame(e.target);
}, true);
/* Öğe DÜZ METİN mi içeriyor? (tek text düğümü → şablonda sabit metin olabilir)
   Böyle ise metni döndür; değilse null (karma içerik/alt öğe var). */
function __wzOwnText(el) {
  if (!el.childNodes || el.childNodes.length === 0) return null;
  var t = '';
  for (var i = 0; i < el.childNodes.length; i++) {
    var n = el.childNodes[i];
    if (n.nodeType === 3) { t += n.nodeValue; }
    else if (n.nodeType === 1) { return null; }  /* alt öğe var → sabit metin sayma */
  }
  t = t.replace(/\\s+/g, ' ').trim();
  return t.length > 0 ? t : null;
}
document.addEventListener('click', function(e) {
  if (!__wz.on) return;
  e.preventDefault();
  e.stopPropagation();
  var el = e.target;
  __wz.sel = el;
  __wzFrame(el);
  /* Öğenin kendisinde yoksa en yakın işaretli atayı ara: bir <td> literal
     olmasa bile onu üreten satır/blok işaretlidir. */
  var owner = el.closest('[data-xsl-id]');
  window.parent.postMessage({
    type: 'wysiwyg-select',
    selector: __wzSelector(el),
    tag: el.tagName.toLowerCase(),
    computed: __wzComputed(el),
    text: __wzOwnText(el),
    xslId: owner ? owner.getAttribute('data-xsl-id') : null,
    xslExact: owner === el
  }, '*');
}, true);
window.addEventListener('message', function(e) {
  if (!e.data) return;
  if (e.data.type === 'wysiwyg-mode') {
    __wz.on = !!e.data.on;
    document.body.style.cursor = __wz.on ? 'crosshair' : '';
    if (!__wz.on && __wz.hl) __wz.hl.style.display = 'none';
  }
  if (e.data.type === 'wysiwyg-text') {
    /* Seçili öğenin metnini canlı güncelle (yalnızca önizleme; XSLT'ye
       "Uygula" denince yazılır). */
    if (__wz.sel) { __wz.sel.textContent = e.data.text || ''; __wzFrame(__wz.sel); }
  }
  if (e.data.type === 'wysiwyg-live') {
    if (!__wz.live) {
      /* Not: kaynakta bitişik "<" + "style" yazmamak için createElement. */
      __wz.live = document.createElement('style');
      __wz.live.setAttribute('data-wz', 'live');
      document.head.appendChild(__wz.live);
    }
    __wz.live.textContent = e.data.css || '';
  }
});
`;
    const bridge = `<${SCRIPT_TAG}>${bridgeJs}</${SCRIPT_TAG}>`;
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
  // ─── Alt bilgi çubuğu (footer) ──────────────────────────────────────
  /** Son dönüşümün süresi (ms) ve üretilen HTML boyutu — footer'da gösterilir. */
  let lastTransformMs = $state(0);
  let lastHtmlBytes = $state(0);

  const fmtBytes = (n: number) =>
    n >= 1024 * 1024 ? `${(n / 1024 / 1024).toFixed(1)} MB` : `${(n / 1024).toFixed(1)} KB`;

  /** Hangi XSLT motoru gerçekten kullanılıyor? Footer'ın en kritik bilgisi. */
  const engineLabel = $derived(engineStatus.saxon ? m.engine.saxon : m.engine.fallback);

  /** Footer saati — saniyeli, her saniye ilerler. */
  let now = $state(new Date());
  $effect(() => {
    const id = setInterval(() => (now = new Date()), 1000);
    return () => clearInterval(id);
  });

  const clockText = $derived(
    now.toLocaleString('tr-TR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
    }),
  );

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
    if (meta && e.shiftKey && (e.key === 'x' || e.key === 'X')) {
      // XPath konsolu — şablon yüklü değilken anlamsız.
      e.preventDefault();
      if (!showWelcome) xpathOpen = !xpathOpen;
    } else if (meta && e.key === 's') {
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
    } else if (meta && (e.key === 't' || e.key === 'T')) {
      e.preventDefault();
      addTab();
    } else if (e.ctrlKey && e.key === 'Tab') {
      // Sonraki/önceki sekme (tarayıcı geleneği). Ctrl+Tab, macOS'ta da Ctrl'dür.
      e.preventDefault();
      const list = tabsState.list;
      if (list.length > 1) {
        const i = list.findIndex((t) => t.id === tabsState.activeId);
        const step = e.shiftKey ? -1 : 1;
        switchToTab(list[(i + step + list.length) % list.length].id);
      }
    } else if (meta && e.key >= '1' && e.key <= '9') {
      // Cmd/Ctrl+N → N'inci sekme. (Cmd+0 zaten "yakınlaştırmayı sıfırla".)
      e.preventDefault();
      const tab = tabsState.list[Number(e.key) - 1];
      if (tab) switchToTab(tab.id);
    } else if (e.key === 'F1') {
      e.preventDefault();
      helpOpen = true;
    }
  }

  // ─── Görsel düzenleyici (WYSIWYG Faz 1) ─────────────────────────────
  // Önizlemede tıkla-seç → stil panelinden düzenle → anlık önizleme (iframe'e
  // canlı CSS enjekte edilir, XSLT'ye dokunulmaz) → "Uygula" ile XSLT'nin
  // stil bloğuna CSS kuralı olarak yazılır (AI'sız, deterministik).
  // NOT: yorumlarda bile bitişik stil/script etiketi YAZILMAZ — Svelte'in
  // blok-sınırı ön taraması yanlış pozitif verip bileşenin script bloğunu
  // erken kapatıyor (bkz. STYLE_TAG / SCRIPT_TAG interpolasyon deseni).
  interface WzSelection {
    selector: string;
    tag: string;
    computed: Record<string, string>;
    /** Öğe yalnızca düz metin içeriyorsa o metin (şablonda sabit olabilir). */
    text: string | null;
    /** Bu öğeyi üreten XSLT literal öğesinin kimliği (Faz 2a eşleme). */
    xslId: string | null;
    /** Kimlik öğenin kendisine mi ait, yoksa bir atasına mı? */
    xslExact: boolean;
  }
  let wzMode = $state(false);
  let wzSel = $state<WzSelection | null>(null);
  let wzEdits = $state<Record<string, string>>({});
  let wzText = $state(''); // düzenlenen sabit metin
  /** Enstrümantasyondan gelen id → XSLT kaynak konumu eşlemesi. */
  let wzRefs = $state<Map<string, XsltElementRef> | null>(null);

  /** Seçili öğenin XSLT'deki kaynak konumu (yoksa null). */
  const wzSource = $derived(
    wzSel?.xslId && wzRefs ? (wzRefs.get(wzSel.xslId) ?? null) : null
  );

  /** Şablonda ilgili satıra atla ve editörü öne getir. */
  function wzGoToSource() {
    if (!wzSource) return;
    xsltEditor?.goToLine(wzSource.line, 1);
    status(f(m.status.goToLine, { line: wzSource.line, tag: wzSource.name }));
  }

  /** Düzenlenen özelliklerden CSS kuralı üret (boş değerler atlanır). */
  const wzRule = $derived.by(() => {
    if (!wzSel) return '';
    const decls = Object.entries(wzEdits)
      .filter(([, v]) => v !== '' && v != null)
      .map(([k, v]) => `  ${k}: ${v};`);
    if (decls.length === 0) return '';
    return `${wzSel.selector} {\n${decls.join('\n')}\n}`;
  });

  // Her değişiklikte iframe'e canlı CSS gönder — anlık görsel geri bildirim.
  $effect(() => {
    const css = wzRule;
    previewFrame?.contentWindow?.postMessage({ type: 'wysiwyg-live', css }, '*');
  });

  async function toggleWzMode() {
    wzMode = !wzMode;
    if (!wzMode) {
      wzSel = null;
      wzEdits = {};
      previewFrame?.contentWindow?.postMessage({ type: 'wysiwyg-live', css: '' }, '*');
    }
    // Önizlemeyi yeniden üret: açılırken kaynak eşlemesi (data-xsl-id) eklenir,
    // kapanırken temiz sürüme dönülür. Yeni HTML iframe'i yeniden yükleyeceği
    // için modu buradan DEĞİL, onPreviewLoad'dan gönderiyoruz (yarış durumu).
    if (editorState.xsltText && editorState.xmlText) await runTransform(true);
    else previewFrame?.contentWindow?.postMessage({ type: 'wysiwyg-mode', on: wzMode }, '*');
    status(wzMode ? m.wysiwyg.opened : m.wysiwyg.closed);
  }

  /** İframe her yeniden yüklendiğinde köprü sıfırlanır → seçim modunu geri ver. */
  function onPreviewLoad() {
    if (wzMode) {
      previewFrame?.contentWindow?.postMessage({ type: 'wysiwyg-mode', on: true }, '*');
    }
  }

  function wzSet(prop: string, value: string) {
    wzEdits = { ...wzEdits, [prop]: value };
  }

  /** Metin kutusu değişince önizlemedeki öğeyi canlı güncelle. */
  function wzSetText(value: string) {
    wzText = value;
    previewFrame?.contentWindow?.postMessage({ type: 'wysiwyg-text', text: value }, '*');
  }

  function countOccurrences(haystack: string, needle: string): number {
    if (!needle) return 0;
    let n = 0;
    let i = haystack.indexOf(needle);
    while (i !== -1) {
      n++;
      i = haystack.indexOf(needle, i + needle.length);
    }
    return n;
  }

  /**
   * Sabit metni XSLT'ye yaz.
   *
   * Metin şablonda birebir geçtiğinden hedefli bul/değiştir yeterli. ANCAK aynı
   * metin ("Toplam" gibi) birden çok yerde geçebilir — o zaman hangisinin
   * değişeceği belirsizdir. Bu yüzden önce etiket sınırlarıyla (`>metin<`)
   * daraltılır; yine benzersiz değilse İŞLEM YAPILMAZ ve kullanıcı uyarılır
   * (yanlış yeri değiştirmektense hiç değiştirmemek doğrudur).
   */
  function wzApplyText() {
    if (!wzSel?.text) return;
    const oldText = wzSel.text;
    const newText = wzText;
    if (!newText || newText === oldText) return;

    const xslt = editorState.xsltText;

    // 1) Etiket sınırlarıyla çapalı ara — en güvenli.
    const anchored = `>${oldText}<`;
    if (countOccurrences(xslt, anchored) === 1) {
      requestAiApply({
        kind: 'edits',
        target: 'xslt',
        edits: [{ search: anchored, replace: `>${newText}<` }],
      });
      return;
    }

    // 2) Düz metin olarak benzersiz mi?
    const plain = countOccurrences(xslt, oldText);
    if (plain === 1) {
      requestAiApply({
        kind: 'edits',
        target: 'xslt',
        edits: [{ search: oldText, replace: newText }],
      });
      return;
    }

    if (plain === 0) {
      status(
        f(m.wzPanel.textNotFound, { text: oldText }),
        true,
      );
      return;
    }
    status(
      f(m.wzPanel.textAmbiguous, { text: oldText, n: plain }),
      true,
    );
  }

  /** `getComputedStyle` rgb()/rgba() döndürür; <input type="color"> hex ister. */
  function rgbToHex(value: string | undefined): string {
    if (!value) return '#000000';
    if (value.startsWith('#')) return value;
    const match = value.match(/rgba?\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)/i);
    if (!match) return '#000000';
    const hex = (n: string) => Number(n).toString(16).padStart(2, '0');
    return `#${hex(match[1])}${hex(match[2])}${hex(match[3])}`;
  }

  function wzReset() {
    wzEdits = {};
  }

  /**
   * Görsel düzenlemeyi XSLT'ye yaz.
   *
   * Deterministik: AI'a gerek yok. Kural XSLT'nin stil bloğuna eklenir;
   * aynı seçici için kural zaten varsa o kural GÜNCELLENİR (yinelenmez).
   * Sonuç, mevcut onay modalına hedefli bir bul/değiştir düzenlemesi olarak
   * verilir → diff + canlı önizleme + onayda otomatik kaydetme çalışır.
   */
  function wzApply() {
    if (!wzSel || !wzRule) return;
    const xslt = editorState.xsltText;
    const match = xslt.match(styleBlockRegex);
    if (!match) {
      status(m.wzPanel.styleBlockMissing, true);
      return;
    }
    const inner = match[2];

    // Bu seçici için mevcut kural var mı? (satır başında, süslü parantezli)
    const escaped = wzSel.selector.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    const existing = new RegExp(`(^|\\n)[ \\t]*${escaped}[ \\t]*\\{[^}]*\\}`, 'm');
    const found = inner.match(existing);

    let search: string;
    let replace: string;
    if (found) {
      // Mevcut kuralı güncelle — SEARCH birebir o kural.
      search = found[0].replace(/^\n/, '');
      replace = wzRule;
    } else {
      // Kural yok → stil bloğunun SONUNA ekle. Kapanış etiketi benzersiz bir
      // çıpa; SEARCH kısa kalır, diff okunur olur.
      const closing = match[3];
      search = closing;
      replace = `\n${wzRule}\n${closing}`;
    }

    requestAiApply({ kind: 'edits', target: 'xslt', edits: [{ search, replace }] });
  }

  function onPreviewMessage(e: MessageEvent) {
    if (!e.data || typeof e.data !== 'object') return;
    if (e.data.type === 'wysiwyg-select') {
      wzSel = {
        selector: e.data.selector ?? '',
        tag: e.data.tag ?? '',
        computed: e.data.computed ?? {},
        text: e.data.text ?? null,
        xslId: e.data.xslId ?? null,
        xslExact: !!e.data.xslExact,
      };
      wzEdits = {};
      wzText = wzSel.text ?? '';
      const src = wzSel.xslId ? wzRefs?.get(wzSel.xslId) : null;
      status(
        src
          ? `Seçildi: ${wzSel.selector} → XSLT satır ${src.line}`
          : `Seçildi: ${wzSel.selector}`
      );
      return;
    }
    if (e.data.type === 'css-captured') {
      if (cssCaptureTimer) clearTimeout(cssCaptureTimer);
      cssCaptureResolve?.(e.data.css ?? '');
      cssCaptureResolve = null;
      return;
    }
    // Önizlemenin içine tıklandı → açık sağ tık menüsünü kapat. (Iframe'deki
    // tıklamalar ana pencereye ulaşmadığı için menü kendiliğinden kapanmıyordu.)
    if (e.data.type === 'preview-dismiss') {
      previewMenu = null;
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
  /**
   * Saxon (XSLT 2.0/3.0) motoru çalışmıyorsa sebebi — kalıcı uyarı bandında
   * gösterilir. Durum çubuğu mesajı bir sonraki işlemde silinir; bu uyarı ise
   * kalmalı, çünkü kullanıcı 2.0 komutlarının SESSİZCE yok sayıldığını bilmeli.
   */
  let engineWarning = $state('');
  /**
   * Son dönüşüm hatası (varsa). AI asistanına iletilir: ajan modu KAPALIYKEN de
   * model hatayı görsün — aksi halde kullanıcı hata metnini elle kopyalamak
   * zorunda kalıyor. Başarılı dönüşümde temizlenir.
   */
  let lastTransformError = $state('');

  let unlistenClose: (() => void) | null = null;
  let unlistenDrop: (() => void) | null = null;
  let unlistenOpened: (() => void) | null = null;
  /** Pencere üzerine dosya sürükleniyor mu (bırakma alanı göstergesi). */
  let dropActive = $state(false);

  async function setupCloseGuard() {
    try {
      const win = getCurrentWindow();
      unlistenClose = await win.onCloseRequested(async (event) => {
        if (forceClose) return; // izin verildi, kapanmaya devam et
        // Aktif sekmeye değil, TÜM sekmelere bak: arka sekmedeki kaydedilmemiş
        // fatura da kullanıcının emeği — sessizce gitmesin.
        if (dirtyTabs().length > 0) {
          event.preventDefault();
          exitConfirmOpen = true;
        }
      });
    } catch (err) {
      // Tauri dışı ortamda (örn. sadece tarayıcıda test) bu API yok — sessiz geç.
      console.warn('Close guard kurulamadı:', err);
    }
  }

  /**
   * Kirli sekmelerin hepsini kaydeder.
   *
   * Kaydetme yolu (`saveAll` → `saveOne`) baştan sona `editorState` üzerinden,
   * yani <b>aktif</b> sekme üzerinden okur. Sekme başına ikinci bir kaydetme yolu
   * yazmak yerine odağı sekmeler arasında gezdiriyoruz — böylece sözdizimi
   * hatasında imleç doğru sekmede doğru satıra gider (`saveOne` → `goToLine`).
   */
  async function saveAllTabs(): Promise<boolean> {
    for (const tab of dirtyTabs()) {
      switchToTab(tab.id);
      await tick(); // editörler yeni içeriğe otursun
      if (!(await saveAll())) return false;
    }
    return true;
  }

  /** Kaydet ve çık. Syntax hatası varsa çıkışı iptal eder, kullanıcı düzeltmeli. */
  async function confirmSaveAndExit() {
    exitInProgress = true;
    try {
      const ok = await saveAllTabs();
      if (!ok && dirtyTabs().length > 0) {
        status(m.exit.saveFailedSyntax, true);
        exitInProgress = false;
        return; // modal açık kalır, kullanıcı düzeltsin
      }
      forceClose = true;
      exitConfirmOpen = false;
      await getCurrentWindow().close();
    } catch (err) {
      status(f(m.exit.exitError, { msg: (err as Error).message ?? String(err) }), true);
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
      status(f(m.exit.exitError, { msg: (err as Error).message ?? String(err) }), true);
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
    void loadApiKeys(); // API anahtarlarını OS anahtar zincirinden belleğe yükle
    void setupFileEntry();
    installGlobalErrorLogging(); // yakalanmayan hatalar da diske düşsün
    // Güncelleme denetimi: açılışı bekletmesin diye ertelenir ve sessizdir
    // (internet yoksa veya dev modundaysak kullanıcıya hata gösterilmez).
    setTimeout(() => void checkForUpdate(), 3000);
    if (showWelcome) {
      status(f(m.status.ready, { version: manifest.version, snippets: allSnippets.length, completions: xsltCompletions.length }));
    }
  });

  /**
   * Dosyanın uygulamaya DIŞARIDAN girdiği iki yolu bağlar:
   *  1. Finder/Explorer'dan pencereye sürükle-bırak (Tauri'nin native olayı —
   *     HTML5 drag-drop WKWebView'de güvenilir değil, zaten webview'de kapalı).
   *  2. "Birlikte Aç": uygulama kapalıyken açıldıysa Rust tarafında kuyruğa
   *     alınmıştır (`take_opened_files`); açıkken gelirse `files-opened` olayı.
   */
  async function setupFileEntry() {
    try {
      unlistenDrop = await getCurrentWebview().onDragDropEvent((e) => {
        const p = e.payload;
        if (p.type === 'enter' || p.type === 'over') {
          dropActive = true;
        } else if (p.type === 'leave') {
          dropActive = false;
        } else if (p.type === 'drop') {
          dropActive = false;
          void openPaths(p.paths);
        }
      });

      unlistenOpened = await listen<string[]>('files-opened', (e) => {
        void openPaths(e.payload);
      });

      const pending = await invoke<string[]>('take_opened_files');
      if (pending.length > 0) await openPaths(pending);
    } catch (err) {
      console.error('Dosya giriş noktaları bağlanamadı:', err);
    }
  }

  onDestroy(() => {
    if (typeof window !== 'undefined') {
      window.removeEventListener('keydown', onGlobalKeydown);
      window.removeEventListener('click', onGlobalClick);
      window.removeEventListener('message', onPreviewMessage);
    }
    if (debounceTimer) clearTimeout(debounceTimer);
    if (autoSaveTimer) clearTimeout(autoSaveTimer);
    unlistenClose?.();
    unlistenDrop?.();
    unlistenOpened?.();
  });
</script>

<div class="app" class:dark={themeKind(settings.theme) === 'dark'}>
  {#if engineWarning}
    <!-- Sessizce XSLT 1.0'a düşmek, 2.0 şablonlarını hata vermeden bozar. Söyle. -->
    <div class="engine-warn">
      <strong>{m.engine.warnTitle}</strong>
      <span>{m.engine.warnBody}</span>
      <details>
        <summary>{m.common.details}</summary>
        <code class="engine-reason">{engineWarning}</code>
      </details>
      <button class="engine-close" onclick={() => (engineWarning = '')} title={m.common.hide}>✕</button>
    </div>
  {/if}

  {#if dropActive}
    <!-- Finder/Explorer'dan dosya sürükleniyor — Tauri'nin native olayıyla tetiklenir. -->
    <div class="drop-overlay">
      <div class="drop-card">
        <span class="drop-icon">📥</span>
        <strong>{m.drop.title}</strong>
        <span class="drop-hint">{m.drop.hint}</span>
      </div>
    </div>
  {/if}

  <!-- ─── Toolbar ────────────────────────────────────────────────── -->
  <header class="toolbar">
    <div class="brand">
      <strong>e-Fatura Edit</strong>
      <span class="ver">v{manifest.version}</span>
    </div>
    <div class="actions">
      <div class="btn-group" title={m.groups.edit}>
        <button onclick={undoActive} title={m.toolbar.undo}>↩</button>
        <button onclick={redoActive} title={m.toolbar.redo}>↪</button>
      </div>

      <div class="btn-group" title={m.groups.file}>
        <button onclick={openXslt} title={m.toolbar.openXsltTitle}>📂 XSLT</button>
        <button onclick={openXml} title={m.toolbar.openXmlTitle}>📄 XML</button>

        {#if recentFiles.length > 0}
          <div class="recent-menu-wrap">
            <button onclick={() => (recentMenuOpen = !recentMenuOpen)} title={m.toolbar.recentTitle}>
              🕒 {m.toolbar.recent} ▼
            </button>
            {#if recentMenuOpen}
              <div class="dropdown recent">
                <div class="dd-header">
                  <span>{m.toolbar.recentHeader}</span>
                  <button class="dd-clear" onclick={clearRecent} title={m.toolbar.clearAll}>🗑</button>
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
          disabled={!canSave}
          title={m.toolbar.saveTitle}
        >
          {#if editorState.xsltDirty || editorState.xmlDirty}
            💾* ({[editorState.xsltDirty && 'XSLT', editorState.xmlDirty && 'XML'].filter(Boolean).join('+')})
          {:else}
            💾 {m.common.save}
          {/if}
        </button>

        <button onclick={saveAsPair} title={m.toolbar.saveAsTitle}>💾 {m.common.saveAs}</button>
      </div>

      <div class="btn-group" title={m.groups.actions}>
        <div class="sample-menu-wrap">
          <button onclick={() => (sampleMenuOpen = !sampleMenuOpen)} title={m.samples.menuTitle}>
            🎲 {m.toolbar.samples} ▼
          </button>
          {#if sampleMenuOpen}
            <div class="dropdown">
              <button class="dd-item primary" onclick={loadDefaultSample}>
                {m.samples.defaultPair}
              </button>
              <button class="dd-item primary" onclick={loadCarryForwardSample}>
                {m.samples.carryForwardPair}
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
                <span>{m.samples.userHeader}</span>
                <button class="dd-clear" onclick={openUserSamplesFolder} title={m.samples.openFolderTitle}>📁</button>
              </div>
              {#if userSamples.length === 0}
                <div class="dd-empty">{m.samples.empty}</div>
              {/if}
              {#each userSamples as us (us.name)}
                <div class="dd-item-row">
                  <button class="dd-item" onclick={() => loadUserSampleEntry(us)}>
                    <span class="dd-name">{us.name}</span>
                  </button>
                  <button class="dd-remove" onclick={(e) => removeUserSampleEntry(us, e)} title={m.samples.removeTitle}>🗑</button>
                </div>
              {/each}
              <button
                class="dd-item primary"
                onclick={addCurrentAsUserSample}
                disabled={!editorState.xsltPath || !editorState.xmlPath}
                title={!editorState.xsltPath || !editorState.xmlPath ? m.samples.addCurrentTitleDisabled : m.samples.addCurrentTitle}
              >
                {m.samples.addCurrent}
              </button>
            </div>
          {/if}
        </div>

        <button class="primary" onclick={() => runTransform()} title={m.toolbar.transformTitle}>▶ {m.toolbar.transform}</button>
        <button onclick={() => (batchOpen = true)} title={m.batch.buttonTitle}>🧪 {m.batch.button}</button>
      </div>

      <div class="btn-group" title={m.groups.helpSettings}>
        <button onclick={() => (helpOpen = true)} title={m.toolbar.helpTitle}>❓ {m.toolbar.help}</button>
        <button onclick={() => goto('/settings')} title={m.toolbar.settingsTitle}>⚙️ {m.toolbar.settings}</button>
      </div>
    </div>
    <div class="status" class:error={statusIsError}>{statusMsg}</div>
  </header>

  <!-- ─── Main Grid ─────────────────────────────────────────────── -->
  <div
    class="main-grid"
    style="grid-template-columns: {snippetsWidth}px 4px {editorsWidth}px 4px 1fr;"
  >
    <!-- Snippet paneli + AI Asistan (sol sütun, dikey bölünmüş) -->
    <aside class="snippets" style="grid-template-rows: 1fr 4px {aiPanelHeight}px;">
      <div class="snippets-top">
        <div class="snippets-header">
          <h3>{m.panels.snippets} ({allSnippets.length})</h3>
          <button class="snippet-add" onclick={() => { editingSnippet = undefined; snippetEditorOpen = true; }} title={m.panels.addSnippet}>➕</button>
          <input type="text" placeholder={m.common.search} bind:value={snippetFilter} class="search" />
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
          <div class="hint">💡 {m.panels.dragHint}</div>
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
                  title={m.snip.edit}
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
                  title={m.snip.del}
                  onmousedown={(e) => e.stopPropagation()}
                  onclick={(e) => onDeleteSnippet(snippet, e)}
                >
                  🗑
                </button>
              {/if}
            </div>
          {:else}
            <p class="muted">{m.snip.none}</p>
          {/each}
        </div>
      </div>

      <Splitter direction="horizontal" bind:position={aiPanelHeight} min={40} />

      <div class="ai-dock">
        <AIAssistant
          xsltPath={editorState.xsltPath}
          xmlPath={editorState.xmlPath}
          xsltText={editorState.xsltText}
          xmlText={editorState.xmlText}
          transformError={lastTransformError}
          onApply={requestAiApply}
          onEmbedImage={embedImageInXslt}
        />
      </div>
    </aside>

    <Splitter direction="vertical" bind:position={snippetsWidth} min={40} />

    <!-- Editör paneli -->
    <section class="editors" style="grid-template-rows: 26px 22px {xsltHeight}px 4px 26px 22px 1fr;">
      <!-- Her panonun kendi sekme şeridi var, ama şeritler EVLİDİR: ikisi de aynı
           sekme listesini gösterir ve aynı çifti seçer. XML şeridinden sekme
           değiştirmek XSLT şeridini de taşır — şablon ile verisi ayrılmaz. -->
      <TabBar kind="xslt" onselect={switchToTab} onclose={requestCloseTab} onnew={addTab} />

      <!-- Tam yol gösterilir: aynı adlı şablonlar farklı klasörlerde durabilir,
           sadece dosya adı hangi dosyayla çalıştığını söylemeye yetmez. -->
      <div class="panel-header">
        <span class="ph-kind">XSLT</span>
        {#if editorState.xsltPath}
          <span class="ph-path" title={editorState.xsltPath}>{editorState.xsltPath}</span>
        {:else}
          <span class="ph-new">{m.common.unsaved}</span>
        {/if}
        <span class="ph-meta">{editorState.xsltText.length.toLocaleString()} {m.common.characters}</span>
        {#if editorState.xsltDirty}<span class="dirty-mark" title={m.common.unsavedChanges}>●</span>{/if}
        {#if !showWelcome}
          <button class="ph-fold" onclick={() => xsltEditor?.collapseAll()} title={m.panels.collapseAllTitle}>⊟</button>
          <button class="ph-fold" onclick={() => xsltEditor?.expandAll()} title={m.panels.expandAllTitle}>⊞</button>
        {/if}
      </div>
      <div class="editor-slot" data-editor-kind="xslt">
        {#if showWelcome}
          <div class="welcome">
            <h2>{m.welcome.title}</h2>
            <p>{m.welcome.subtitle}</p>
            <div class="welcome-actions">
              <button class="w-btn primary" onclick={loadDefaultSample} title={m.welcome.loadSampleTitle}>{m.welcome.loadSample}</button>
              <button class="w-btn" onclick={openXslt} title={m.welcome.openXsltTitle}>{m.welcome.openXslt}</button>
              <button class="w-btn" onclick={openXml} title={m.welcome.openXmlTitle}>{m.welcome.openXml}</button>
            </div>
            <p class="hint-lg">
              {f(m.welcome.hint, { snippets: allSnippets.length })}
            </p>
          </div>
        {:else}
          <CodeEditor
            bind:this={xsltEditor}
            bind:value={editorState.xsltText}
            language="xml"
            completions={xsltCompletions}
            snippets={allSnippets}
            onsnippet={insertSnippet}
            onerror={(msg) => status(msg, true)}
          />
        {/if}
      </div>

      <Splitter direction="horizontal" bind:position={xsltHeight} min={40} />

      <TabBar kind="xml" onselect={switchToTab} onclose={requestCloseTab} onnew={addTab} />

      <div class="panel-header">
        <span class="ph-kind">XML</span>
        {#if editorState.xmlPath}
          <span class="ph-path" title={editorState.xmlPath}>{editorState.xmlPath}</span>
        {:else}
          <span class="ph-new">{m.common.unsaved}</span>
        {/if}
        <span class="ph-meta">{editorState.xmlText.length.toLocaleString()} {m.common.characters}</span>
        {#if editorState.xmlDirty}<span class="dirty-mark" title={m.common.unsavedChanges}>●</span>{/if}
        {#if !showWelcome}
          <button
            class="ph-fold"
            class:active={xpathOpen}
            onclick={() => (xpathOpen = !xpathOpen)}
            title={m.xpath.title}>ƒx</button>
          <button class="ph-fold" onclick={() => xmlEditor?.collapseAll()} title={m.panels.collapseAllTitle}>⊟</button>
          <button class="ph-fold" onclick={() => xmlEditor?.expandAll()} title={m.panels.expandAllTitle}>⊞</button>
        {/if}
      </div>
      <div class="editor-slot xml-slot" data-editor-kind="xml">
        {#if showWelcome}
          <div class="welcome sub">
            <p class="hint-lg">{m.welcome.xmlHint}</p>
          </div>
        {:else}
          <!-- XML veri editörüne snippet verilmez: snippet'ler XSLT şablon kodudur. -->
          <div class="ed-fill">
            <CodeEditor
              bind:this={xmlEditor}
              bind:value={editorState.xmlText}
              language="xml"
              onerror={(msg) => status(msg, true)}
            />
          </div>
          {#if xpathOpen}
            <XPathConsole xmlText={editorState.xmlText} onclose={() => (xpathOpen = false)} />
          {/if}
        {/if}
      </div>
    </section>

    <Splitter direction="vertical" bind:position={editorsWidth} min={40} />

    <!-- Preview paneli -->
    <section class="preview">
      <div class="panel-header preview-header">
        <span>{f(m.misc.previewKb, { kb: (editorState.previewHtml.length / 1024).toFixed(1) })}</span>
        <div class="preview-actions">
          <!-- Responsive boyut butonları -->
          <button
            class:active={settings.previewWidth === 320}
            onclick={() => setPreviewWidth(320)}
            title={m.preview.mobile}
          >📱 320</button>
          <button
            class:active={settings.previewWidth === 768}
            onclick={() => setPreviewWidth(768)}
            title={m.preview.tablet}
          >📱 768</button>
          <button
            class:active={settings.previewWidth === 1200}
            onclick={() => setPreviewWidth(1200)}
            title={m.preview.desktop}
          >🖥️ 1200</button>
          <button
            class:active={settings.previewWidth === null}
            onclick={() => setPreviewWidth(null)}
            title={m.preview.full}
          >⬜ Full</button>

          <span class="mini-sep"></span>

          <!-- Zoom -->
          <button onclick={() => zoomPreview(-0.1)} title={m.preview.zoomOut}>−</button>
          <button onclick={resetZoom} title={m.misc.zoomResetCmd}>
            {Math.round(settings.previewZoom * 100)}%
          </button>
          <button onclick={() => zoomPreview(0.1)} title={m.preview.zoomIn}>+</button>

          <span class="mini-sep"></span>

          <button
            class:active={wzMode}
            onclick={toggleWzMode}
            disabled={!editorState.previewHtml}
            title={m.wysiwyg.toggleTitle}
          >{m.wysiwyg.toggle}</button>

          <button
            onclick={captureStyleFromPreview}
            disabled={!editorState.previewHtml}
            title={m.wysiwyg.captureStyleTitle}
          >{m.wysiwyg.captureStyle}</button>

          <span class="mini-sep"></span>

          <button onclick={printPreview} disabled={!editorState.previewHtml} title={m.preview.print}>🖨</button>
        </div>
      </div>

      {#if wzMode}
        <div class="wz-panel">
          {#if !wzSel}
            <p class="wz-hint">{m.wysiwyg.hint}</p>
          {:else}
            <div class="wz-head">
              <code class="wz-sel">{wzSel.selector}</code>
              <span class="wz-tag">&lt;{wzSel.tag}&gt;</span>
              <button class="wz-reset" onclick={wzReset} title={m.wysiwyg.resetTitle}>↺</button>
              <button class="wz-apply" onclick={wzApply} disabled={!wzRule}>{m.wysiwyg.applyToXslt}</button>
            </div>

            <!-- Kaynak eşlemesi (salt-okunur): bu öğeyi hangi XSLT satırı üretti? -->
            <div class="wz-src">
              {#if wzSource}
                <button
                  class="wz-src-btn"
                  onclick={wzGoToSource}
                  title={m.wysiwyg.sourceGoTitle}
                >
                  {f(m.wysiwyg.sourceLine, { line: wzSource.line, tag: wzSource.name })}
                  {#if !wzSel.xslExact}<span class="wz-src-approx">{m.wysiwyg.sourceApprox}</span>{/if}
                </button>
              {:else}
                <span class="wz-src-none" title={m.wysiwyg.sourceNoneTitle}>
                  {m.wysiwyg.sourceNone}
                </span>
              {/if}
            </div>

            {#if wzSel.text}
              <div class="wz-text-row">
                <label for="wz-text">{m.wysiwyg.text}</label>
                <input
                  id="wz-text"
                  type="text"
                  value={wzText}
                  oninput={(e) => wzSetText((e.currentTarget as HTMLInputElement).value)}
                />
                <button
                  class="wz-apply"
                  onclick={wzApplyText}
                  disabled={!wzText || wzText === wzSel.text}
                >{m.wysiwyg.applyText}</button>
              </div>
            {/if}

            <div class="wz-grid">
              <label>{m.wzPanel.color}
                <input type="color" value={wzEdits['color'] ?? rgbToHex(wzSel.computed['color'])}
                  oninput={(e) => wzSet('color', (e.currentTarget as HTMLInputElement).value)} />
              </label>
              <label>{m.wzPanel.background}
                <input type="color" value={wzEdits['background-color'] ?? rgbToHex(wzSel.computed['background-color'])}
                  oninput={(e) => wzSet('background-color', (e.currentTarget as HTMLInputElement).value)} />
              </label>
              <label>{m.wzPanel.fontSize}
                <input type="text" placeholder={wzSel.computed['font-size']} value={wzEdits['font-size'] ?? ''}
                  oninput={(e) => wzSet('font-size', (e.currentTarget as HTMLInputElement).value)} />
              </label>
              <label>{m.wzPanel.weight}
                <select value={wzEdits['font-weight'] ?? ''}
                  onchange={(e) => wzSet('font-weight', (e.currentTarget as HTMLSelectElement).value)}>
                  <option value="">{m.wzPanel.noChange}</option>
                  <option value="normal">normal</option>
                  <option value="bold">bold</option>
                  <option value="600">600</option>
                </select>
              </label>
              <label>{m.wzPanel.align}
                <select value={wzEdits['text-align'] ?? ''}
                  onchange={(e) => wzSet('text-align', (e.currentTarget as HTMLSelectElement).value)}>
                  <option value="">{m.wzPanel.noChange}</option>
                  <option value="left">{m.wzPanel.alignLeft}</option>
                  <option value="center">{m.wzPanel.alignCenter}</option>
                  <option value="right">{m.wzPanel.alignRight}</option>
                </select>
              </label>
              <label>{m.wzPanel.padding}
                <input type="text" placeholder={wzSel.computed['padding']} value={wzEdits['padding'] ?? ''}
                  oninput={(e) => wzSet('padding', (e.currentTarget as HTMLInputElement).value)} />
              </label>
              <label>{m.wzPanel.border}
                <input type="text" placeholder="1px solid #ccc" value={wzEdits['border'] ?? ''}
                  oninput={(e) => wzSet('border', (e.currentTarget as HTMLInputElement).value)} />
              </label>
              <label>{m.wzPanel.radius}
                <input type="text" placeholder={wzSel.computed['border-radius']} value={wzEdits['border-radius'] ?? ''}
                  oninput={(e) => wzSet('border-radius', (e.currentTarget as HTMLInputElement).value)} />
              </label>
              <label>{m.wzPanel.width}
                <input type="text" placeholder={wzSel.computed['width']} value={wzEdits['width'] ?? ''}
                  oninput={(e) => wzSet('width', (e.currentTarget as HTMLInputElement).value)} />
              </label>
            </div>

            {#if wzRule}
              <pre class="wz-rule">{wzRule}</pre>
            {/if}
            <p class="wz-warn">{m.wzPanel.ruleWarn}</p>
          {/if}
        </div>
      {/if}

      <div class="preview-frame-wrap">
        <div
          class="preview-frame-container"
          style:max-width={settings.previewWidth ? `${settings.previewWidth}px` : 'none'}
        >
          <iframe
            bind:this={previewFrame}
            srcdoc={previewHtmlWithBridge}
            onload={onPreviewLoad}
            title={m.panels.preview}
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

  <!-- ─── Alt bilgi çubuğu ───────────────────────────────────────────
       Bir bakışta "neyle çalışıyorum ve ne durumdayım" sorusuna cevap.
       En kritik alan MOTOR: Saxon mu, yoksa yalnızca XSLT 1.0 yapan yedek
       tarayıcı motoru mu? Bu ayrım sessiz kalırsa 2.0 komutları hata vermeden
       yok sayılır ve fatura yanlış basılır (bkz. engineStatus).  -->
  <footer class="footbar">
    <!-- Dosyalar -->
    <span class="fb-item" title={editorState.xsltPath ?? m.misc.unsavedXslt}>
      <b>XSLT</b>
      {editorState.xsltPath ? basename(editorState.xsltPath) : '(yeni)'}
      <span class="fb-dim">{fmtBytes(editorState.xsltText.length)}</span>
      {#if editorState.xsltDirty}<span class="fb-dirty" title={m.common.unsavedChanges}>●</span>{/if}
    </span>

    <span class="fb-sep"></span>

    <span class="fb-item" title={editorState.xmlPath ?? m.misc.unsavedXml}>
      <b>XML</b>
      {editorState.xmlPath ? basename(editorState.xmlPath) : '(yeni)'}
      <span class="fb-dim">{fmtBytes(editorState.xmlText.length)}</span>
      {#if editorState.xmlDirty}<span class="fb-dirty" title={m.common.unsavedChanges}>●</span>{/if}
    </span>

    <span class="fb-sep"></span>

    <!-- Son dönüşüm -->
    {#if lastHtmlBytes > 0}
      <span class="fb-item" title={m.footer.lastTransform}>
        ⚡ {fmtBytes(lastHtmlBytes)} · {lastTransformMs} ms
      </span>
      <span class="fb-sep"></span>
    {/if}

    <!-- Boşluk -->
    <span class="fb-spacer"></span>

    <!-- AI modeli -->
    <span class="fb-item fb-dim" title={m.footer.aiModelTitle}>
      🤖 {settings.aiProviders[settings.aiProvider].model || m.footer.noModel}
    </span>

    <span class="fb-sep"></span>

    <!-- XSLT motoru — footer'ın en önemli alanı -->
    <button
      class="fb-engine"
      class:degraded={!engineStatus.saxon}
      onclick={() => { if (!engineStatus.saxon) engineWarning = engineStatus.reason; }}
      title={engineStatus.saxon
        ? m.engine.saxonTitle
        : f(m.engine.fallbackTitle, { reason: engineStatus.reason })}
    >
      {engineStatus.saxon ? '✅' : '⚠️'} {engineLabel}
    </button>

    <span class="fb-sep"></span>

    <span class="fb-item fb-dim" title={m.footer.snippetCount}>✂️ {allSnippets.length}</span>

    <span class="fb-sep"></span>

    <span class="fb-item fb-dim">v{manifest.version}</span>

    <span class="fb-sep"></span>

    <span class="fb-item fb-clock" title={m.footer.clock}>🕐 {clockText}</span>
  </footer>
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
      <button onclick={() => { printPreview(); previewMenu = null; }}>{m.preview.menuPrint}</button>
      <button onclick={() => { copyPreviewHtml(); previewMenu = null; }}>{m.preview.menuCopyHtml}</button>
      <div class="divider"></div>
      <button onclick={() => { runTransform(); previewMenu = null; }}>{m.preview.menuRetransform}</button>
      <button onclick={() => { openDevTools(); previewMenu = null; }}>{m.preview.menuDevtools}</button>
    {/snippet}
  </ContextMenu>
{/if}

<!-- ─── Otomatik güncelleme bildirimi ─────────────────────────────── -->
<UpdateModal />

<!-- ─── Yardım penceresi ──────────────────────────────────────────── -->
{#if helpOpen}
  <HelpModal onclose={() => (helpOpen = false)} />
{/if}

<!-- ─── Toplu regresyon koşusu ────────────────────────────────────── -->
{#if batchOpen}
  <BatchRunner
    xsltText={editorState.xsltText}
    onclose={() => (batchOpen = false)}
    onopen={loadXmlFromPath}
  />
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
    <div class="exit-modal" role="alertdialog" aria-label={m.common.unsavedChanges}>
      <h3>{m.exit.title}</h3>
      <p>
        {f(m.exit.body, { files: dirtyLabel })}
      </p>
      <div class="exit-actions">
        <button class="exit-btn cancel" onclick={cancelExit} disabled={exitInProgress}>{m.common.cancel}</button>
        <button class="exit-btn discard" onclick={confirmDiscardAndExit} disabled={exitInProgress}>
          {m.exit.discard}
        </button>
        <button class="exit-btn save" onclick={confirmSaveAndExit} disabled={exitInProgress}>
          {exitInProgress ? m.exit.saving : m.exit.saveExit}
        </button>
      </div>
    </div>
  </div>
{/if}

<!-- ─── DevTools stilini XSLT'ye uygula onayı ─────────────────────── -->
{#if styleApplyOpen}
  <div class="exit-overlay" role="presentation">
    <div class="style-modal" role="alertdialog" aria-label={m.styleModal.title}>
      <h3>{m.styleModal.title}</h3>
      <p>{m.styleModal.body}</p>
      <pre class="style-preview">{capturedCss}</pre>
      <div class="exit-actions">
        <button class="exit-btn cancel" onclick={cancelStyleApply}>{m.common.cancel}</button>
        <button class="exit-btn save" onclick={applyCapturedCssToXslt}>{m.common.apply}</button>
      </div>
    </div>
  </div>
{/if}

<!-- ─── AI önerisini uygulama onayı ───────────────────────────────── -->
{#if aiApplyOpen}
  <div class="exit-overlay" role="presentation">
    <div class="style-modal ai-apply-modal" role="alertdialog" aria-label={m.aiApply.title}>
      <h3>{m.aiApply.title}</h3>
      <div class="ai-apply-body">
      <div class="ai-apply-left">
      {#if aiApplySuggestion?.kind === 'edits'}
        <p>{f(m.aiApply.editsBody, { target: aiApplyTarget.toUpperCase(), n: aiApplySuggestion.edits.length })}</p>
        {#if aiApplyUnmatched.length > 0}
          <p class="ai-partial-warning">{f(m.aiApply.unmatched, { n: aiApplyUnmatched.length })}</p>
        {/if}
        {#each aiApplySuggestion.edits as ed, i}
          <div class="ai-edit-diff">
            <div class="ai-edit-diff-label">
              {f(m.aiApply.editN, { i: i + 1 })}
              {#if aiApplyUnmatched.includes(ed)}<span class="ai-edit-skip">{m.aiApply.skipped}</span>{/if}
            </div>
            <pre class="ai-diff-old">{ed.search}</pre>
            <pre class="ai-diff-new">{ed.replace}</pre>
          </div>
        {/each}
      {:else}
        <p>{f(m.aiApply.fullBody, { target: aiApplyTarget.toUpperCase() })}</p>
        {#if aiApplyLooksPartial}
          <p class="ai-partial-warning">{f(m.aiApply.partialWarn, { target: aiApplyTarget.toUpperCase() })}</p>
        {/if}
        <!-- Büyük dosyalarda 600 KB'lık metni doğrudan basmak WebView'i dondurur;
             kod metni varsayılan gizli, istekle açılır. Sonuç sağ panelde canlı. -->
        {#if aiApplyShowFull}
          <pre class="style-preview">{aiApplyNewText}</pre>
          <button class="link-btn" onclick={() => (aiApplyShowFull = false)}>{m.aiApply.hideCode}</button>
        {:else}
          <p class="ai-fulltext-note">
            {f(m.aiApply.resultStats, { lines: aiApplyNewText.split('\n').length, kb: (aiApplyNewText.length / 1024).toFixed(1) })}
            <button class="link-btn" onclick={() => (aiApplyShowFull = true)}>{m.aiApply.showCode}</button>
          </p>
        {/if}
      {/if}

      {#if aiApplyPreviewError}
        <p class="ai-partial-warning">{f(m.aiApply.invalidResult, { msg: aiApplyPreviewError })}</p>
      {/if}
      </div>

      <!-- Sağ sütun: uygulanınca oluşacak sonucun canlı, ölçeklenebilir önizlemesi -->
      <div class="ai-apply-right">
        <div class="ai-result-preview-head">
          <span>{m.aiApply.resultPreview}</span>
          {#if aiApplyPreviewLoading}<span class="ai-preview-loading-tag">{m.aiApply.generating}</span>{/if}
          <div class="ai-preview-zoom">
            <button onclick={() => zoomAiApply(-0.1)} title={m.preview.zoomOut}>−</button>
            <span class="ai-preview-zoom-val">{Math.round(aiApplyZoom * 100)}%</span>
            <button onclick={() => zoomAiApply(0.1)} title={m.preview.zoomIn}>+</button>
            <button onclick={() => (aiApplyZoom = 0.6)} title={m.preview.zoomReset}>⟲</button>
          </div>
        </div>
        <div class="ai-preview-frame-wrap">
          <iframe
            class="ai-result-preview"
            title={m.preview.result}
            srcdoc={aiApplyPreviewHtml}
            sandbox="allow-same-origin"
            style:transform="scale({aiApplyZoom})"
            style:transform-origin="top left"
            style:width="{100 / aiApplyZoom}%"
            style:height="{100 / aiApplyZoom}%"
          ></iframe>
        </div>
      </div>
      </div>

      <div class="exit-actions">
        <button class="exit-btn cancel" onclick={cancelAiApply}>{m.common.cancel}</button>
        <button class="exit-btn save" onclick={confirmAiApply} disabled={!aiApplyHasChange}>
          {m.common.apply}
        </button>
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

  /* Satırlar: toolbar · içerik · alt bilgi çubuğu */
  .app { display: grid; grid-template-rows: auto 1fr auto; height: 100vh; background: #f5f6f8; }
  .app.dark { color: #e6e6e6; background: #1e1e1e; }

  /* ─── Alt bilgi çubuğu ─── */
  .footbar {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.25rem 0.75rem;
    border-top: 1px solid #d9dce1;
    background: #eef0f3;
    font-size: 11px;
    color: #4b5563;
    white-space: nowrap;
    overflow-x: auto;
  }
  .fb-item {
    display: inline-flex;
    align-items: center;
    gap: 0.3rem;
  }
  .fb-item b {
    font-weight: 600;
    color: #6b7280;
    font-size: 10px;
    letter-spacing: 0.03em;
  }
  .fb-dim { color: #9099a5; }
  .fb-dirty { color: #f59e0b; }
  .fb-sep {
    width: 1px;
    height: 12px;
    background: #d0d4da;
    flex: none;
  }
  .fb-spacer { flex: 1 1 auto; }
  .fb-clock {
    font-variant-numeric: tabular-nums; /* rakam genişliği sabit → saat titremesin */
    color: #6b7280;
  }
  :global(html.dark) .fb-clock { color: #9aa1ac; }

  /* Motor rozeti — bozulduğunda gözden kaçmamalı. */
  .fb-engine {
    display: inline-flex;
    align-items: center;
    gap: 0.3rem;
    padding: 0.1rem 0.4rem;
    border: 1px solid transparent;
    border-radius: 4px;
    background: transparent;
    color: #15803d;
    font-size: 11px;
    font-family: inherit;
    cursor: default;
  }
  .fb-engine.degraded {
    background: #fef3c7;
    border-color: #fcd34d;
    color: #92400e;
    font-weight: 600;
    cursor: pointer;
  }
  .fb-engine.degraded:hover { background: #fde68a; }

  :global(html.dark) .footbar {
    background: #26272b;
    border-top-color: #3f3f46;
    color: #c9ccd1;
  }
  :global(html.dark) .fb-item b { color: #9aa1ac; }
  :global(html.dark) .fb-dim { color: #7d848e; }
  :global(html.dark) .fb-sep { background: #3f3f46; }
  :global(html.dark) .fb-engine { color: #4ade80; }
  :global(html.dark) .fb-engine.degraded {
    background: #422006;
    border-color: #713f12;
    color: #fde68a;
  }

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

  /* Snippets + AI Asistan (sol sütun, dikey bölünmüş) */
  .snippets { display: grid; background: #fafbfc; overflow: hidden; }
  .app.dark .snippets { background: #252526; }
  .snippets-top { display: flex; flex-direction: column; overflow: hidden; min-height: 0; }
  .ai-dock { overflow: hidden; min-height: 0; border-top: 1px solid #e5e7eb; }
  .app.dark .ai-dock { border-top-color: #3f3f46; }
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
  /* XPath konsolu açılınca editör + konsol dikey paylaşır. */
  .xml-slot { display: flex; flex-direction: column; }
  .ed-fill { flex: 1; min-height: 0; }

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
  /* ── Görsel düzenleyici paneli (WYSIWYG Faz 1) ────────────────────── */
  .wz-panel {
    flex-shrink: 0;
    max-height: 40vh;
    overflow-y: auto;
    padding: 0.6rem 0.75rem;
    background: #fff;
    border-bottom: 1px solid #d5d8dc;
    font-size: 12px;
  }
  .wz-hint { margin: 0; color: #6b7280; }
  .wz-head {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    margin-bottom: 0.6rem;
    flex-wrap: wrap;
  }
  .wz-sel {
    font-family: ui-monospace, Menlo, monospace;
    background: #eef4ff;
    color: #0a5cff;
    border: 1px solid #bfdbfe;
    border-radius: 4px;
    padding: 0.15rem 0.4rem;
    font-size: 11px;
  }
  .wz-tag { color: #9ca3af; font-size: 11px; }
  .wz-reset,
  .wz-apply {
    padding: 0.25rem 0.6rem;
    border-radius: 5px;
    font-size: 11px;
    cursor: pointer;
    border: 1px solid #cbd0d6;
    background: #fff;
  }
  .wz-reset { margin-left: auto; }
  .wz-apply {
    background: #0a5cff;
    border-color: #0a5cff;
    color: #fff;
    font-weight: 600;
  }
  .wz-apply:disabled { opacity: 0.5; cursor: not-allowed; }
  /* XSLT motoru uyarı bandı — kalıcı, kullanıcı kapatana kadar durur. */
  .engine-warn {
    display: flex;
    align-items: center;
    gap: 0.6rem;
    flex-wrap: wrap;
    padding: 0.45rem 0.8rem;
    background: #fef3c7;
    border-bottom: 1px solid #fcd34d;
    color: #78350f;
    font-size: 12px;
  }
  .engine-warn strong {
    white-space: nowrap;
  }
  .engine-warn code {
    font-family: var(--mono, ui-monospace, monospace);
    font-size: 11px;
  }
  .engine-warn details {
    font-size: 11px;
  }
  .engine-warn summary {
    cursor: pointer;
  }
  .engine-reason {
    display: block;
    margin-top: 0.3rem;
    max-width: 70ch;
    word-break: break-word;
    opacity: 0.85;
  }
  .engine-close {
    margin-left: auto;
    border: none;
    background: transparent;
    color: inherit;
    cursor: pointer;
    font-size: 13px;
  }
  :global(html.dark) .engine-warn {
    background: #422006;
    border-bottom-color: #713f12;
    color: #fde68a;
  }

  /* Dosya bırakma göstergesi — tüm pencereyi kaplar, tıklamayı engellemez. */
  .drop-overlay {
    position: fixed;
    inset: 0;
    z-index: 9999;
    display: grid;
    place-items: center;
    background: rgba(15, 23, 42, 0.45);
    backdrop-filter: blur(2px);
    pointer-events: none;
  }
  .drop-card {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 0.4rem;
    padding: 2rem 3rem;
    border: 3px dashed #60a5fa;
    border-radius: 14px;
    background: #fff;
    color: #1e293b;
    box-shadow: 0 12px 40px rgba(0, 0, 0, 0.3);
  }
  .drop-icon {
    font-size: 40px;
  }
  .drop-hint {
    font-size: 12px;
    color: #64748b;
  }
  :global(html.dark) .drop-card {
    background: #1e293b;
    color: #e5e7eb;
    border-color: #3b82f6;
  }
  :global(html.dark) .drop-hint { color: #9aa1ac; }

  /* Kaynak eşlemesi satırı — tıklanınca XSLT editöründe ilgili satıra atlar. */
  .wz-src {
    margin-bottom: 0.6rem;
  }
  .wz-src-btn {
    display: inline-flex;
    align-items: center;
    gap: 0.3rem;
    padding: 0.2rem 0.45rem;
    border: 1px solid #c7d7ee;
    border-radius: 4px;
    background: #eef4fc;
    color: #1d4ed8;
    font-size: 11px;
    font-family: var(--mono, ui-monospace, monospace);
    cursor: pointer;
  }
  .wz-src-btn:hover {
    background: #dbe8fa;
    border-color: #93b4e0;
  }
  .wz-src-approx {
    color: #6b7280;
    font-style: italic;
  }
  .wz-src-none {
    font-size: 11px;
    color: #9ca3af;
  }
  :global(html.dark) .wz-src-btn {
    background: #1e293b;
    border-color: #35507a;
    color: #93c5fd;
  }
  :global(html.dark) .wz-src-btn:hover { background: #26344b; }
  :global(html.dark) .wz-src-approx { color: #9aa1ac; }
  :global(html.dark) .wz-src-none { color: #6b7280; }

  .wz-text-row {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    margin-bottom: 0.6rem;
    padding-bottom: 0.6rem;
    border-bottom: 1px dashed #d5d8dc;
  }
  .wz-text-row label {
    font-size: 11px;
    color: #4b5563;
    white-space: nowrap;
  }
  .wz-text-row input {
    flex: 1;
    min-width: 0;
    padding: 0.25rem 0.4rem;
    border: 1px solid #cbd0d6;
    border-radius: 4px;
    font-size: 12px;
    background: #fff;
  }
  :global(html.dark) .wz-text-row { border-bottom-color: #3f3f46; }
  :global(html.dark) .wz-text-row label { color: #c9ccd1; }
  :global(html.dark) .wz-text-row input {
    background: #2d2d30;
    border-color: #4b5563;
    color: #e6e6e6;
  }
  .wz-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(150px, 1fr));
    gap: 0.4rem 0.75rem;
  }
  .wz-grid label {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 0.4rem;
    font-size: 11px;
    color: #4b5563;
  }
  .wz-grid input[type='text'],
  .wz-grid select {
    width: 90px;
    padding: 0.15rem 0.3rem;
    border: 1px solid #cbd0d6;
    border-radius: 4px;
    font-size: 11px;
    background: #fff;
  }
  .wz-grid input[type='color'] {
    width: 34px;
    height: 22px;
    padding: 0;
    border: 1px solid #cbd0d6;
    border-radius: 4px;
    cursor: pointer;
  }
  .wz-rule {
    margin: 0.6rem 0 0.4rem;
    padding: 0.4rem 0.5rem;
    background: #f5f6f8;
    border: 1px solid #d5d8dc;
    border-radius: 5px;
    font-family: ui-monospace, Menlo, monospace;
    font-size: 11px;
    white-space: pre;
    overflow-x: auto;
  }
  .wz-warn {
    margin: 0;
    font-size: 11px;
    color: #92400e;
    background: #fffbeb;
    border: 1px solid #fde68a;
    border-radius: 5px;
    padding: 0.35rem 0.5rem;
    line-height: 1.4;
  }
  :global(html.dark) .wz-panel { background: #252526; border-bottom-color: #3f3f46; }
  :global(html.dark) .wz-grid label { color: #c9ccd1; }
  :global(html.dark) .wz-grid input[type='text'],
  :global(html.dark) .wz-grid select,
  :global(html.dark) .wz-reset {
    background: #2d2d30;
    border-color: #4b5563;
    color: #e6e6e6;
  }
  :global(html.dark) .wz-rule { background: #1e1e1e; border-color: #3f3f46; color: #d4d4d8; }
  :global(html.dark) .wz-sel { background: #172554; border-color: #1e3a8a; color: #93c5fd; }
  :global(html.dark) .wz-warn { background: #3d3117; border-color: #6b5320; color: #fbbf24; }

  .preview { position: relative; display: flex; flex-direction: column; background: #f0f2f5; overflow: hidden; }
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
    letter-spacing: 0.5px;
    white-space: nowrap; overflow: hidden;
    display: flex; align-items: center; gap: 0.5rem;
  }
  .ph-kind {
    flex: none;
    font-weight: 700;
    text-transform: uppercase;
    color: #4b5563;
  }
  /* Tam yol. Uzunsa SOLDAN kısalsın — dosya adı her zaman görünür kalmalı;
     sağdan kısaltmak tam da en gerekli kısmı (dosya adını) yutardı. */
  .ph-path {
    flex: 1 1 auto;
    min-width: 0;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    direction: rtl;        /* kısaltma baştan olsun */
    text-align: left;
    unicode-bidi: plaintext; /* yolun karakter sırası bozulmasın */
    font-family: var(--mono, ui-monospace, monospace);
    letter-spacing: 0;
    color: #374151;
  }
  .ph-new {
    flex: 1 1 auto;
    min-width: 0;
    font-style: italic;
    color: #9ca3af;
    letter-spacing: 0;
  }
  .ph-meta {
    flex: none;
    color: #9ca3af;
    letter-spacing: 0;
    font-variant-numeric: tabular-nums;
  }
  .app.dark .ph-kind { color: #c9ccd1; }
  .app.dark .ph-path { color: #d4d4d8; }
  .app.dark .ph-new,
  .app.dark .ph-meta { color: #7d848e; }
  .app.dark .panel-header {
    background: #2d2d30; color: #a0a0a0; border-bottom-color: #3f3f46;
  }
  .dirty-mark { color: #f59e0b; font-size: 14px; line-height: 1; }

  /* Katla/aç düğmeleri — fold gutter oklarına klavye/fare alternatifi. */
  .ph-fold {
    background: none;
    border: none;
    color: inherit;
    opacity: 0.55;
    cursor: pointer;
    font-size: 12px;
    line-height: 1;
    padding: 0 2px;
  }
  .ph-fold:hover { opacity: 1; }
  .ph-fold.active { opacity: 1; color: #0a5cff; font-weight: 700; }

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
  :global(html.dark) .exit-modal {
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
    width: min(720px, 94vw);
    max-height: 85vh;
    background: #fff;
    border-radius: 10px;
    box-shadow: 0 20px 60px rgba(0, 0, 0, 0.35);
    padding: 1.5rem;
    display: flex;
    flex-direction: column;
    overflow-y: auto;
  }
  :global(html.dark) .style-modal { background: #2d2d30; color: #e6e6e6; }
  .style-modal h3 { margin: 0 0 0.75rem; font-size: 16px; color: #0a5cff; }
  .style-modal p { margin: 0 0 0.75rem; font-size: 13px; line-height: 1.5; color: #374151; }

  /* ── Koyu tema: modallar `.app` div'inin DIŞINDA render edildiğinden
     `.app.dark` ile yakalanamaz; kök `<html class="dark">` üzerinden. ── */
  :global(html.dark) .style-modal p,
  :global(html.dark) .exit-modal p { color: #c9ccd1; }
  :global(html.dark) .exit-modal { background: #2d2d30; color: #e6e6e6; }
  :global(html.dark) .ai-fulltext-note { color: #9ca3af; }
  :global(html.dark) .ai-result-preview-head { color: #d4d4d8; }
  :global(html.dark) .ai-preview-zoom-val { color: #9ca3af; }
  :global(html.dark) .ai-preview-zoom button,
  :global(html.dark) .exit-btn.cancel {
    background: #3a3a3d;
    border-color: #4b5563;
    color: #e6e6e6;
  }
  :global(html.dark) .ai-preview-zoom button:hover,
  :global(html.dark) .exit-btn.cancel:hover { background: #4b4b4f; }
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
  :global(html.dark) .style-preview { background: #1e1e1e; border-color: #3f3f46; }
  .ai-partial-warning {
    background: #fef2f2;
    border: 1px solid #fca5a5;
    color: #b91c1c;
    border-radius: 6px;
    padding: 0.6rem 0.75rem;
    font-size: 12px;
    line-height: 1.45;
    margin: 0 0 0.9rem;
  }
  :global(html.dark) .ai-partial-warning { background: #3b1111; border-color: #7f1d1d; color: #fca5a5; }
  .ai-edit-diff {
    border: 1px solid #d5d8dc;
    border-radius: 6px;
    margin-bottom: 0.6rem;
    overflow: hidden;
  }
  .ai-edit-diff-label {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.3rem 0.5rem;
    background: #f5f6f8;
    font-size: 11px;
    font-weight: 600;
    color: #4b5563;
  }
  .ai-edit-skip {
    color: #b91c1c;
    font-weight: 500;
  }
  .ai-diff-old,
  .ai-diff-new {
    margin: 0;
    padding: 0.5rem;
    font-family: ui-monospace, Menlo, monospace;
    font-size: 11px;
    line-height: 1.45;
    max-height: 30vh;
    overflow: auto;
    white-space: pre-wrap;
    border-left: 3px solid;
  }
  .ai-diff-old { background: #fef2f2; border-left-color: #fca5a5; }
  .ai-diff-new { background: #f0fdf4; border-left-color: #86efac; }
  :global(html.dark) .ai-edit-diff { border-color: #3f3f46; }
  :global(html.dark) .ai-edit-diff-label { background: #27272a; color: #d4d4d8; }
  :global(html.dark) .ai-diff-old { background: #3b1111; }
  :global(html.dark) .ai-diff-new { background: #0f2a17; }
  .exit-btn.save:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
  /* AI onay modalı: solda diff/açıklama, sağda ölçeklenebilir canlı önizleme. */
  .ai-apply-modal {
    width: min(1180px, 96vw);
  }
  .ai-apply-body {
    display: grid;
    grid-template-columns: minmax(0, 1fr) minmax(0, 1.1fr);
    gap: 1rem;
    min-height: 0;
    flex: 1;
  }
  .ai-apply-left {
    overflow-y: auto;
    min-width: 0;
    max-height: 62vh;
  }
  .ai-apply-right {
    display: flex;
    flex-direction: column;
    min-width: 0;
  }
  .ai-result-preview-head {
    font-size: 12px;
    font-weight: 600;
    color: #4b5563;
    margin: 0.25rem 0 0.4rem;
    display: flex;
    align-items: center;
    gap: 0.5rem;
  }
  .ai-preview-loading-tag {
    font-weight: 400;
    color: #9ca3af;
    font-style: italic;
  }
  .ai-preview-zoom {
    margin-left: auto;
    display: flex;
    align-items: center;
    gap: 0.2rem;
  }
  .ai-preview-zoom button {
    padding: 0.1rem 0.4rem;
    border: 1px solid #cbd0d6;
    background: #fff;
    border-radius: 4px;
    font-size: 12px;
    cursor: pointer;
    line-height: 1.3;
  }
  .ai-preview-zoom button:hover { background: #eef4ff; }
  .ai-preview-zoom-val {
    font-family: ui-monospace, Menlo, monospace;
    font-size: 11px;
    color: #6b7280;
    min-width: 38px;
    text-align: center;
  }
  .ai-preview-frame-wrap {
    flex: 1;
    min-height: 320px;
    max-height: 62vh;
    overflow: auto;
    border: 1px solid #d5d8dc;
    border-radius: 6px;
    background: #fff;
  }
  .ai-result-preview {
    display: block;
    border: none;
    background: #fff;
  }
  :global(html.dark) .ai-preview-frame-wrap { border-color: #3f3f46; }
  .ai-fulltext-note {
    font-size: 12px;
    color: #6b7280;
    margin: 0 0 1rem;
  }
  .link-btn {
    background: none;
    border: none;
    color: #0a5cff;
    cursor: pointer;
    font-size: 12px;
    padding: 0;
    text-decoration: underline;
  }
  :global(html.dark) .ai-result-preview-hint { background: #172554; border-color: #1e3a8a; color: #bfdbfe; }
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
