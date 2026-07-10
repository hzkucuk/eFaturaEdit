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
  import { defaultKeymap, indentWithTab, history, historyKeymap } from '@codemirror/commands';
  import {
    bracketMatching,
    foldGutter,
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
  import { search, searchKeymap } from '@codemirror/search';
  import { settings } from '$lib/settings.svelte';

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
  }

  let {
    value = $bindable(''),
    language = 'xml',
    readonly = false,
    completions = [],
  }: Props = $props();

  let containerEl: HTMLDivElement;
  let view: EditorView | null = null;
  let suppressUpdate = false;

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
        keymap.of([...defaultKeymap, ...historyKeymap, ...searchKeymap, ...completionKeymap, indentWithTab]),
        buildLanguage(),

        // Türkçe UI çevirileri
        EditorState.phrases.of(TR_PHRASES),

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

<div class="editor-host" bind:this={containerEl}></div>

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
</style>
