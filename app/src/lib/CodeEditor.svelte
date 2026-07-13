<!--
  CodeEditor — CodeMirror 6 tabanlı XSLT/XML editörü.

  Özellikler:
  - Satır numarası, syntax highlight (XML), kod katlama, bracket matching
  - 10+ tema (thememirror + one-dark)
  - Autocomplete (XSLT tag + XPath + Snippet)
  - Snippet drag-drop kabulü (CodeMirror built-in text drop)
  - Bind:this ile parent'a expose: insertAtCursor, goToLine, focus, getView

  Kullanım:
    let editor: CodeEditor;
    <CodeEditor bind:this={editor} bind:value={xsltText} language="xml" />
    editor.insertAtCursor('<xsl:if test=""/>');
    editor.goToLine(42, 10);
-->
<script lang="ts">
  import { onMount, onDestroy } from 'svelte';
  import { EditorState, Compartment, EditorSelection } from '@codemirror/state';
  import { EditorView, lineNumbers, keymap, highlightActiveLine, drawSelection } from '@codemirror/view';
  import {
    defaultKeymap,
    indentWithTab,
    history,
    historyKeymap,
    selectAll,
    undo as undoCmd,
    redo as redoCmd,
  } from '@codemirror/commands';
  import {
    bracketMatching,
    foldGutter,
    foldKeymap,
    foldAll,
    unfoldAll,
    foldCode,
    unfoldCode,
    indentOnInput,
    indentUnit,
    HighlightStyle,
    syntaxHighlighting,
    defaultHighlightStyle,
  } from '@codemirror/language';
  import { tags as t } from '@lezer/highlight';
  import { xml } from '@codemirror/lang-xml';
  import { html } from '@codemirror/lang-html';
  import { oneDark } from '@codemirror/theme-one-dark';
  import {
    autocompletion,
    completionKeymap,
    type CompletionContext,
    type CompletionResult,
    type Completion,
  } from '@codemirror/autocomplete';
  import { search, searchKeymap, openSearchPanel, gotoLine } from '@codemirror/search';
  import { readText, writeText } from '@tauri-apps/plugin-clipboard-manager';
  import { settings } from '$lib/settings.svelte';
  import { getLocale, m, f } from '$lib/i18n.svelte';
  import ContextMenu from '$lib/ContextMenu.svelte';
  import type { Snippet as SnippetItem } from '$lib/data/types';

  /**
   * CodeMirror arama panelinin Türkçe çevirileri.
   * `EditorState.phrases` extension'ı ile plug edilir.
   */
  const TR_PHRASES: Record<string, string> = {
    // Search panel
    'Find': 'Ara',
    'Replace': 'Değiştir',
    'next': 'sonraki',
    'previous': 'önceki',
    'all': 'tümü',
    'match case': 'BÜYÜK/küçük',
    'by word': 'kelime',
    'regexp': 'regex',
    'replace': 'değiştir',
    'replace all': 'tümünü değiştir',
    'close': 'kapat',
    'current match': 'geçerli eşleşme',
    'replaced $ matches': '$ eşleşme değiştirildi',
    'replaced match on line $': '$. satırdaki eşleşme değiştirildi',
    'on line': 'satır',
    // Autocomplete
    'No completions': 'Öneri yok',
    'Completions': 'Öneriler',
    // Fold gutter
    'Folded lines': 'Katlanmış satırlar',
    'Unfolded lines': 'Açık satırlar',
    'to': '→',
    'folded code': 'katlı kod',
    'unfold': 'aç',
    'Fold line': 'Satırı katla',
    'Unfold line': 'Satırı aç',
    // Genel
    'Selection deleted': 'Seçim silindi',
    'Go to line': 'Satıra git',
    'go': 'git',
    'Enter to save, Escape to cancel': 'Kaydet için Enter, iptal için Escape',
  };

  interface Props {
    value: string;
    language?: 'xml' | 'html';
    readonly?: boolean;
    /** Autocomplete için tam öneri listesi (opsiyonel). */
    completions?: Completion[];
    /** Sağ tık menüsündeki "Snippet ekle" alt menüsü. Boşsa bölüm görünmez. */
    snippets?: SnippetItem[];
    /** Menüden snippet seçilince. Verilmezse snippet doğrudan imlece eklenir. */
    onsnippet?: (snippet: SnippetItem) => void;
    /** Pano hatası gibi kullanıcıya gösterilmesi gereken durumlar. */
    onerror?: (message: string) => void;
  }

  let {
    value = $bindable(''),
    language = 'xml',
    readonly = false,
    completions = [],
    snippets = [],
    onsnippet,
    onerror,
  }: Props = $props();

  let containerEl: HTMLDivElement;
  let view: EditorView | null = null;
  let suppressUpdate = false;

  // ─── Sağ tık menüsü ────────────────────────────────────────────────
  // Kısayol etiketleri @codemirror keymap'lerinden BİREBİR alınmıştır —
  // tahmin edilmemiştir. Özellikle foldAll/unfoldAll'ın mac varyantı YOKTUR:
  // macOS'ta da Ctrl+Alt+[ / ]'dir (tek blok katlama ise Cmd+Alt+[).
  const isMac =
    typeof navigator !== 'undefined' && /mac/i.test(navigator.platform || navigator.userAgent);
  const sc = (mac: string, other: string) => (isMac ? mac : other);

  let menu = $state<{ x: number; y: number } | null>(null);
  let menuHasSelection = $state(false);
  let openCategory = $state<string | null>(null);

  /** Snippet'ler menüde kategoriye göre gruplanır (255 snippet düz listede işe yaramaz). */
  const snippetCategories = $derived.by(() => {
    const groups = new Map<string, SnippetItem[]>();
    for (const s of snippets) {
      const list = groups.get(s.category);
      if (list) list.push(s);
      else groups.set(s.category, [s]);
    }
    return [...groups.entries()];
  });

  function onContextMenu(e: MouseEvent) {
    if (!view) return;
    e.preventDefault();
    // Seçim dışına sağ tıklandıysa imleci oraya taşı (masaüstü editör davranışı).
    const pos = view.posAtCoords({ x: e.clientX, y: e.clientY });
    const sel = view.state.selection.main;
    if (pos != null && (pos < sel.from || pos > sel.to)) {
      view.dispatch({ selection: EditorSelection.cursor(pos) });
    }
    menuHasSelection = !view.state.selection.main.empty;
    openCategory = null;
    menu = { x: e.clientX, y: e.clientY };
  }

  function closeMenu() {
    menu = null;
    openCategory = null;
  }

  /** Menü eylemi: menüyü kapat, editöre odağı geri ver, komutu çalıştır. */
  function run(action: (v: EditorView) => void) {
    const v = view;
    closeMenu();
    if (!v) return;
    v.focus();
    action(v);
  }

  function selectedText(v: EditorView): string {
    const { from, to } = v.state.selection.main;
    return v.state.sliceDoc(from, to);
  }

  async function doCopy(cut: boolean) {
    const v = view;
    closeMenu();
    if (!v) return;
    const text = selectedText(v);
    if (!text) return;
    try {
      await writeText(text);
    } catch (err) {
      // Sessizce yutma — pano erişimi kullanıcının göreceği bir hatadır.
      onerror?.(f(m.ctx.clipboardErr, { msg: (err as Error).message ?? String(err) }));
      return;
    }
    if (cut && !readonly) {
      const { from, to } = v.state.selection.main;
      v.dispatch({ changes: { from, to, insert: '' } });
    }
    v.focus();
  }

  async function doPaste() {
    const v = view;
    closeMenu();
    if (!v || readonly) return;
    try {
      const text = await readText();
      if (text) insertAtCursor(text);
      else v.focus();
    } catch (err) {
      onerror?.(f(m.ctx.clipboardErr, { msg: (err as Error).message ?? String(err) }));
    }
  }

  function pickSnippet(s: SnippetItem) {
    closeMenu();
    if (onsnippet) onsnippet(s);
    else insertAtCursor(s.xsltCode);
  }

  /**
   * Snippet alt menüsünü ekran sınırları içine yerleştirir.
   *
   * `position: absolute; left: 100%` yetmiyordu: menü ekranın altına yakınsa
   * (CSS Stilleri gibi son kategoriler) flyout aşağı doğru açılıp ekrandan
   * taşıyordu. Burada viewport'a göre ölçülür — sağda yer yoksa SOLA, aşağıda
   * yer yoksa YUKARI kayar; sığmıyorsa kendi içinde kaydırılır.
   */
  function placeFlyout(node: HTMLDivElement) {
    const PAD = 6;
    const row = node.parentElement;
    if (!row) return;

    const anchor = row.getBoundingClientRect();
    const vw = window.innerWidth;
    const vh = window.innerHeight;

    // Önce yüksekliği viewport'a sığdır, SONRA ölç (sıra önemli: kırpılmış
    // yükseklik ölçülmezse konum yanlış çıkar).
    node.style.maxHeight = `${Math.min(360, vh - 2 * PAD)}px`;
    const rect = node.getBoundingClientRect();

    let x = anchor.right;
    if (x + rect.width > vw - PAD) {
      const flipped = anchor.left - rect.width;
      x = flipped >= PAD ? flipped : Math.max(PAD, vw - PAD - rect.width);
    }

    let y = anchor.top - 4;
    if (y + rect.height > vh - PAD) y = vh - PAD - rect.height;
    y = Math.max(PAD, y);

    node.style.left = `${x}px`;
    node.style.top = `${y}px`;
    node.style.visibility = 'visible';
  }

  const themeCompartment = new Compartment();
  const wrapCompartment = new Compartment();
  const tabCompartment = new Compartment();
  const fontCompartment = new Compartment();
  const lineNumCompartment = new Compartment();
  const autocompleteCompartment = new Compartment();
  const highlightCompartment = new Compartment();

  /**
   * Açık tema (light) için canlı XML/XSLT syntax highlight.
   * Dark ve thememirror temaları kendi highlight'ını sağlar; bu style
   * yalnızca `light` seçildiğinde compartment üzerinden aktif edilir.
   */
  const lightXmlHighlight = HighlightStyle.define([
    { tag: t.tagName, color: '#0000c0', fontWeight: '600' },          // <element>
    { tag: t.attributeName, color: '#b91c1c' },                        // attr=
    { tag: t.attributeValue, color: '#0a6b2f' },                       // "value"
    { tag: t.string, color: '#0a6b2f' },
    { tag: t.number, color: '#a05100' },
    { tag: t.bool, color: '#a05100', fontWeight: '600' },
    { tag: t.null, color: '#a05100', fontWeight: '600' },
    { tag: t.comment, color: '#6b7280', fontStyle: 'italic' },
    { tag: t.angleBracket, color: '#4b5563' },                         // < >
    { tag: t.punctuation, color: '#4b5563' },
    { tag: t.processingInstruction, color: '#7c3aed', fontWeight: '600' }, // <?xml
    { tag: t.definitionKeyword, color: '#7c3aed' },
    { tag: t.keyword, color: '#7c3aed', fontWeight: '600' },
    { tag: t.operator, color: '#4b5563' },
    { tag: t.meta, color: '#6b7280' },                                  // DOCTYPE
    { tag: t.escape, color: '#a05100' },                                // &amp; vb.
    { tag: t.namespace, color: '#7c3aed', fontStyle: 'italic' },        // xsl:
    { tag: t.typeName, color: '#0000c0' },
    { tag: t.className, color: '#0000c0' },
  ]);

  /** Highlight extension seçici — `light` temada custom, diğerlerinde default. */
  function buildHighlight() {
    if (settings.theme === 'light') {
      return syntaxHighlighting(lightXmlHighlight);
    }
    return syntaxHighlighting(defaultHighlightStyle, { fallback: true });
  }

  function buildLanguage() {
    return language === 'html' ? html() : xml();
  }

  async function loadTheme() {
    switch (settings.theme) {
      case 'light':
        return [];
      case 'dark':
        return oneDark;
      default: {
        // thememirror'dan dinamik yükle: /themes/<name>.js
        try {
          const mod = (await import(/* @vite-ignore */ `thememirror/dist/themes/${settings.theme}.js`)) as Record<string, unknown>;
          const camelKey = settings.theme.replace(/-([a-z])/g, (_, c) => c.toUpperCase());
          // eslint-disable-next-line @typescript-eslint/no-explicit-any
          return (mod[camelKey] ?? mod.default ?? []) as any;
        } catch {
          return [];
        }
      }
    }
  }

  function buildFontTheme() {
    return EditorView.theme({
      '&': {
        fontSize: `${settings.fontSize}px`,
        height: '100%',
      },
      '.cm-scroller': {
        fontFamily:
          'ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", monospace',
      },
      '.cm-content': {
        padding: '8px 0',
      },
    });
  }

  function buildAutocomplete() {
    if (!settings.autocomplete || completions.length === 0) return [];
    const source = (ctx: CompletionContext): CompletionResult | null => {
      const word = ctx.matchBefore(/[<\w:./@-]*/);
      if (!word) return null;
      if (word.from === word.to && !ctx.explicit) return null;
      return { from: word.from, options: completions };
    };
    return autocompletion({ override: [source] });
  }

  onMount(async () => {
    const theme = await loadTheme();
    const state = EditorState.create({
      doc: value,
      extensions: [
        history(),
        drawSelection(),
        highlightActiveLine(),
        bracketMatching(),
        foldGutter(),
        indentOnInput(),
        search(),
        // foldKeymap: katlama kısayolları (Cmd/Ctrl+Alt+[ katla, +] aç,
        // Cmd/Ctrl+Alt+Shift+[ hepsini katla). Fold gutter (fare ile ok) zaten
        // vardı ama klavye kısayolları keymap'e hiç eklenmemişti.
        keymap.of([
          ...defaultKeymap,
          ...historyKeymap,
          ...searchKeymap,
          ...completionKeymap,
          ...foldKeymap,
          indentWithTab,
        ]),
        buildLanguage(),

        // Arama paneli sözleri: tr → Türkçe; diğer diller → CodeMirror'ın
        // İngilizce varsayılanları. Editör oluşturulurken seçilir.
        ...(getLocale() === 'tr' ? [EditorState.phrases.of(TR_PHRASES)] : []),

        lineNumCompartment.of(settings.showLineNumbers ? lineNumbers() : []),
        wrapCompartment.of(settings.wordWrap ? EditorView.lineWrapping : []),
        tabCompartment.of(indentUnit.of(' '.repeat(settings.tabWidth))),
        themeCompartment.of(theme),
        fontCompartment.of(buildFontTheme()),
        autocompleteCompartment.of(buildAutocomplete()),
        highlightCompartment.of(buildHighlight()),

        EditorState.readOnly.of(readonly),

        // ─── Snippet drag-drop desteği ─────────────────────────────
        // CodeMirror'un default'una güvenmek yerine explicit handler ile
        // text/plain dataTransfer'ı yakala ve imleç pozisyonuna ekle.
        // WebKit için: dragenter + dragover + drop üçü de preventDefault olmalı.
        EditorView.domEventHandlers({
          dragenter(event) {
            if (event.dataTransfer?.types.includes('text/plain')) {
              event.preventDefault();
            }
          },
          dragover(event) {
            if (event.dataTransfer?.types.includes('text/plain')) {
              event.preventDefault();
              if (event.dataTransfer) event.dataTransfer.dropEffect = 'copy';
            }
          },
          drop(event, editorView) {
            const text = event.dataTransfer?.getData('text/plain');
            if (!text) return false;
            // İmleç konumu: koordinatlardan çöz, olmazsa mevcut selection'a düş
            const pos =
              editorView.posAtCoords({ x: event.clientX, y: event.clientY }) ??
              editorView.state.selection.main.head;
            editorView.dispatch({
              changes: { from: pos, to: pos, insert: text },
              selection: EditorSelection.cursor(pos + text.length),
            });
            editorView.focus();
            event.preventDefault();
            event.stopPropagation();
            return true;
          },
        }),

        EditorView.updateListener.of((update) => {
          if (update.docChanged && !suppressUpdate) {
            value = update.state.doc.toString();
          }
        }),
      ],
    });

    view = new EditorView({ state, parent: containerEl });
  });

  onDestroy(() => {
    view?.destroy();
    view = null;
  });

  // Prop → editör (döngü engelleyici)
  $effect(() => {
    if (!view) return;
    const current = view.state.doc.toString();
    if (current === value) return;
    suppressUpdate = true;
    view.dispatch({ changes: { from: 0, to: current.length, insert: value } });
    suppressUpdate = false;
  });

  // Ayar değişimi → reactive reconfigure
  $effect(() => {
    if (!view) return;
    // settings.theme dependency olsun
    const _theme = settings.theme;
    void _theme;
    loadTheme().then((theme) => {
      view?.dispatch({
        effects: [
          lineNumCompartment.reconfigure(settings.showLineNumbers ? lineNumbers() : []),
          wrapCompartment.reconfigure(settings.wordWrap ? EditorView.lineWrapping : []),
          tabCompartment.reconfigure(indentUnit.of(' '.repeat(settings.tabWidth))),
          themeCompartment.reconfigure(theme),
          fontCompartment.reconfigure(buildFontTheme()),
          highlightCompartment.reconfigure(buildHighlight()),
        ],
      });
    });
  });

  // Autocomplete listesi değişince yeniden yapılandır
  $effect(() => {
    if (!view) return;
    // dependency
    const _len = completions.length;
    const _on = settings.autocomplete;
    void _len;
    void _on;
    view.dispatch({
      effects: autocompleteCompartment.reconfigure(buildAutocomplete()),
    });
  });

  // ─── Expose ─────────────────────────────────────────────────────────
  export function insertAtCursor(text: string): void {
    if (!view) return;
    const { from, to } = view.state.selection.main;
    view.dispatch({
      changes: { from, to, insert: text },
      selection: EditorSelection.cursor(from + text.length),
    });
    view.focus();
  }

  /**
   * Editör içeriğini komple değiştir. `bind:value` yerine explicit çağrı;
   * nested $state proxy binding'inin edge case'lerini bypass eder.
   * Undo geçmişini temizler (yeni dosya yükleme senaryosu).
   */
  export function setValue(text: string): void {
    if (!view) return;
    suppressUpdate = true;
    try {
      view.dispatch({
        changes: { from: 0, to: view.state.doc.length, insert: text },
        selection: EditorSelection.cursor(0),
      });
      value = text;
    } finally {
      suppressUpdate = false;
    }
  }

  /**
   * Verilen viewport koordinatlarına en yakın metin pozisyonuna ekle.
   * Custom mouse-tracking drag-drop için kullanılır.
   */
  export function insertAtCoords(x: number, y: number, text: string): void {
    if (!view) return;
    const pos =
      view.posAtCoords({ x, y }) ??
      view.state.selection.main.head;
    view.dispatch({
      changes: { from: pos, to: pos, insert: text },
      selection: EditorSelection.cursor(pos + text.length),
    });
    view.focus();
  }

  export function undo(): void {
    if (!view) return;
    import('@codemirror/commands').then(({ undo }) => {
      undo(view!);
      view!.focus();
    });
  }

  export function redo(): void {
    if (!view) return;
    import('@codemirror/commands').then(({ redo }) => {
      redo(view!);
      view!.focus();
    });
  }

  /** Tüm katlanabilir blokları katla (UBL-TR belgeleri derin iç içedir). */
  export function collapseAll(): void {
    if (view) {
      foldAll(view);
      view.focus();
    }
  }

  /** Tüm katlı blokları aç. */
  export function expandAll(): void {
    if (view) {
      unfoldAll(view);
      view.focus();
    }
  }

  export function goToLine(line: number, column = 1): void {
    if (!view) return;
    const total = view.state.doc.lines;
    const safeLine = Math.max(1, Math.min(total, line));
    const lineInfo = view.state.doc.line(safeLine);
    const pos = Math.min(lineInfo.to, lineInfo.from + Math.max(0, column - 1));
    view.dispatch({
      selection: EditorSelection.cursor(pos),
      effects: EditorView.scrollIntoView(pos, { y: 'center' }),
    });
    view.focus();
  }

  export function focusEditor(): void {
    view?.focus();
  }
</script>

<!-- CodeMirror kendi erişilebilir textbox'ını bu host'un içine kurar; sağ tık
     menüsü Escape ile kapanır ve tüm eylemleri klavye kısayollarıyla da erişilebilir. -->
<!-- svelte-ignore a11y_no_static_element_interactions -->
<div class="editor-host" bind:this={containerEl} oncontextmenu={onContextMenu}></div>

{#if menu}
  <ContextMenu x={menu.x} y={menu.y} onclose={closeMenu}>
    <button onclick={() => doCopy(true)} disabled={!menuHasSelection || readonly}>
      <span>{m.ctx.cut}</span><kbd>{sc('⌘X', 'Ctrl+X')}</kbd>
    </button>
    <button onclick={() => doCopy(false)} disabled={!menuHasSelection}>
      <span>{m.ctx.copy}</span><kbd>{sc('⌘C', 'Ctrl+C')}</kbd>
    </button>
    <button onclick={doPaste} disabled={readonly}>
      <span>{m.ctx.paste}</span><kbd>{sc('⌘V', 'Ctrl+V')}</kbd>
    </button>
    <button onclick={() => run(selectAll)}>
      <span>{m.ctx.selectAll}</span><kbd>{sc('⌘A', 'Ctrl+A')}</kbd>
    </button>

    <div class="divider"></div>
    <button onclick={() => run(openSearchPanel)}>
      <span>{m.ctx.find}</span><kbd>{sc('⌘F', 'Ctrl+F')}</kbd>
    </button>
    <button onclick={() => run(gotoLine)}>
      <span>{m.ctx.gotoLine}</span><kbd>{sc('⌘⌥G', 'Ctrl+Alt+G')}</kbd>
    </button>

    <div class="divider"></div>
    <button onclick={() => run(foldCode)}>
      <span>{m.ctx.foldBlock}</span><kbd>{sc('⌘⌥[', 'Ctrl+Shift+[')}</kbd>
    </button>
    <button onclick={() => run(unfoldCode)}>
      <span>{m.ctx.unfoldBlock}</span><kbd>{sc('⌘⌥]', 'Ctrl+Shift+]')}</kbd>
    </button>
    <button onclick={() => run(foldAll)}>
      <span>{m.ctx.foldAll}</span><kbd>Ctrl+Alt+[</kbd>
    </button>
    <button onclick={() => run(unfoldAll)}>
      <span>{m.ctx.unfoldAll}</span><kbd>Ctrl+Alt+]</kbd>
    </button>

    <div class="divider"></div>
    <button onclick={() => run(undoCmd)} disabled={readonly}>
      <span>{m.ctx.undo}</span><kbd>{sc('⌘Z', 'Ctrl+Z')}</kbd>
    </button>
    <button onclick={() => run(redoCmd)} disabled={readonly}>
      <span>{m.ctx.redo}</span><kbd>{sc('⇧⌘Z', 'Ctrl+Y')}</kbd>
    </button>

    {#if snippetCategories.length > 0 && !readonly}
      <div class="divider"></div>
      {#each snippetCategories as [category, items] (category)}
        <div
          class="ctx-sub"
          role="menuitem"
          tabindex="-1"
          onmouseenter={() => (openCategory = category)}
          onmouseleave={() => (openCategory = null)}
        >
          <button class="ctx-sub-head">
            <span>{category}</span><kbd class="ctx-arrow">▸</kbd>
          </button>
          {#if openCategory === category}
            <div class="ctx-flyout" use:placeFlyout>
              {#each items as s (s.key)}
                <button onclick={() => pickSnippet(s)} title={s.description}>
                  <span>{s.iconText} {s.displayName}</span>
                </button>
              {/each}
            </div>
          {/if}
        </div>
      {/each}
    {/if}
  </ContextMenu>
{/if}

<style>
  .editor-host {
    height: 100%;
    overflow: hidden;
  }
  :global(.editor-host .cm-editor) {
    height: 100%;
  }
  :global(.editor-host .cm-editor.cm-focused) {
    outline: none;
  }

  /* ─── Arama paneli (Cmd+F) — Türkçe + daha büyük ────────────── */
  :global(.cm-editor .cm-panels) {
    border-color: #cbd0d6;
  }
  :global(.cm-editor .cm-panel.cm-search) {
    padding: 8px 12px;
    background: #f5f6f8;
    border-top: 1px solid #cbd0d6;
    font-size: 13px;
    display: flex;
    flex-wrap: wrap;
    gap: 6px;
    align-items: center;
  }
  :global(.cm-editor .cm-panel.cm-search input) {
    padding: 5px 10px;
    font-size: 13px;
    min-width: 220px;
    border: 1px solid #cbd0d6;
    border-radius: 4px;
    background: #fff;
    color: #1a1a1a;
    font-family: ui-monospace, Menlo, monospace;
  }
  :global(.cm-editor .cm-panel.cm-search input:focus) {
    outline: none;
    border-color: #0a5cff;
    box-shadow: 0 0 0 2px rgba(10, 92, 255, 0.15);
  }
  :global(.cm-editor .cm-panel.cm-search button) {
    padding: 5px 10px;
    font-size: 12px;
    background: #fff;
    color: #1a1a1a;
    border: 1px solid #cbd0d6;
    border-radius: 4px;
    cursor: pointer;
    margin: 0;
    font-weight: 500;
  }
  :global(.cm-editor .cm-panel.cm-search button:hover) {
    background: #eef4ff;
    border-color: #0a5cff;
  }
  :global(.cm-editor .cm-panel.cm-search label) {
    font-size: 12px;
    padding: 4px 8px;
    display: inline-flex;
    align-items: center;
    gap: 4px;
    color: #4b5563;
    background: #fff;
    border: 1px solid #cbd0d6;
    border-radius: 4px;
    cursor: pointer;
  }
  :global(.cm-editor .cm-panel.cm-search label input[type="checkbox"]) {
    min-width: 0;
    width: 14px;
    height: 14px;
    padding: 0;
    margin: 0;
    cursor: pointer;
  }
  :global(.cm-editor .cm-panel.cm-search [name="close"]) {
    position: absolute;
    top: 4px;
    right: 6px;
    font-size: 16px;
    padding: 2px 8px;
    background: transparent;
    border: none;
    color: #6b7280;
  }
  :global(.cm-editor .cm-panel.cm-search [name="close"]:hover) {
    background: #fee2e2;
    color: #b91c1c;
  }

  /* "Satıra git" paneli — CodeMirror bunu showDialog() ile kurar: sınıfı
     .cm-dialog'dur (.cm-gotoLine diye bir şey YOKTUR) ve kendi teması label'a
     font-size: 80% verir. Arama paneliyle aynı ölçüye getiriyoruz. */
  :global(.cm-editor .cm-panel.cm-dialog) {
    padding: 8px 34px 8px 12px;
    background: #f5f6f8;
    border-top: 1px solid #cbd0d6;
    font-size: 13px;
  }
  :global(.cm-editor .cm-panel.cm-dialog form) {
    display: flex;
    flex-wrap: wrap;
    gap: 6px;
    align-items: center;
  }
  :global(.cm-editor .cm-panel.cm-dialog label) {
    display: flex;
    align-items: center;
    gap: 6px;
    font-size: 13px; /* CodeMirror'ın %80'ini ezer */
    color: #4b5563;
  }
  :global(.cm-editor .cm-panel.cm-dialog input.cm-textfield) {
    padding: 5px 10px;
    font-size: 13px;
    min-width: 220px;
    border: 1px solid #cbd0d6;
    border-radius: 4px;
    background: #fff;
    color: #1a1a1a;
    font-family: ui-monospace, Menlo, monospace;
  }
  :global(.cm-editor .cm-panel.cm-dialog input.cm-textfield:focus) {
    outline: none;
    border-color: #0a5cff;
    box-shadow: 0 0 0 2px rgba(10, 92, 255, 0.15);
  }
  :global(.cm-editor .cm-panel.cm-dialog button.cm-button) {
    padding: 5px 12px;
    font-size: 12px;
    background: #fff;
    color: #1a1a1a;
    border: 1px solid #cbd0d6;
    border-radius: 4px;
    background-image: none;
    cursor: pointer;
    margin: 0;
    font-weight: 500;
  }
  :global(.cm-editor .cm-panel.cm-dialog button.cm-button:hover) {
    background: #eef4ff;
    border-color: #0a5cff;
  }
  :global(.cm-editor .cm-panel.cm-dialog .cm-dialog-close) {
    top: 6px;
    right: 8px;
    font-size: 16px;
    padding: 2px 8px;
    background: transparent;
    border: none;
    color: #6b7280;
    cursor: pointer;
  }
  :global(.cm-editor .cm-panel.cm-dialog .cm-dialog-close:hover) {
    background: #fee2e2;
    color: #b91c1c;
  }

  /* Autocomplete popup — daha büyük ve okunabilir */
  :global(.cm-editor .cm-tooltip.cm-tooltip-autocomplete) {
    background: #fff;
    border: 1px solid #cbd0d6;
    border-radius: 6px;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.12);
    font-size: 13px;
  }
  :global(.cm-editor .cm-tooltip-autocomplete ul li) {
    padding: 4px 10px;
  }
  :global(.cm-editor .cm-tooltip-autocomplete ul li[aria-selected]) {
    background: #0a5cff;
    color: white;
  }
  :global(.cm-editor .cm-tooltip.cm-completionInfo) {
    padding: 8px 12px;
    background: #f9fafb;
    border-left: 3px solid #0a5cff;
    font-size: 12px;
    max-width: 400px;
  }

  /* ─── Sağ tık menüsü ───────────────────────────────────────────────
     ContextMenu.svelte buton/disabled/dark stillerini zaten veriyor;
     burada yalnızca kısayol etiketi ve snippet alt menüsü eklenir. */
  :global(.context-menu button) {
    display: flex !important;
    align-items: center;
    justify-content: space-between;
    gap: 24px;
    white-space: nowrap;
  }
  :global(.context-menu kbd) {
    font-family: inherit;
    font-size: 11px;
    color: #8b8f96;
    letter-spacing: 0.02em;
  }
  :global(.context-menu button:disabled kbd) {
    color: #c3c6cb;
  }
  :global(.app.dark .context-menu kbd) {
    color: #9aa0a6;
  }

  .ctx-sub {
    position: relative;
  }
  .ctx-arrow {
    color: #8b8f96;
  }
  /* Konum placeFlyout() içinde viewport'a göre hesaplanır. Ölçülmeden önce
     görünmesin diye visibility: hidden — yoksa bir kare yanlış yerde parlar. */
  .ctx-flyout {
    position: fixed;
    left: 0;
    top: 0;
    visibility: hidden;
    min-width: 240px;
    overflow-y: auto;
    background: #fff;
    border: 1px solid #cbd0d6;
    border-radius: 4px;
    box-shadow: 0 4px 16px rgba(0, 0, 0, 0.16);
    padding: 4px 0;
    z-index: 10001;
  }
  :global(.app.dark) .ctx-flyout {
    background: #2d2d30;
    border-color: #555;
  }
</style>
