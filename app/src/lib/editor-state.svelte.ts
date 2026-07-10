/**
 * Global editör state — modül scope $state ile tüm sayfalarda paylaşılır.
 *
 * Neden? SvelteKit route değişince (`/` → `/settings` → `/`) ana sayfa
 * component'i unmount olur ve local state kaybolur. Bu store sayesinde
 * editör içerikleri, dosya yolları ve önizleme kalıcı hale gelir.
 */

export interface EditorState {
  xsltText: string;
  xmlText: string;
  xsltPath: string | null;
  xmlPath: string | null;
  xsltDirty: boolean;
  xmlDirty: boolean;
  previewHtml: string;
}

const DEFAULTS: EditorState = {
  xsltText: '',
  xmlText: '',
  xsltPath: null,
  xmlPath: null,
  xsltDirty: false,
  xmlDirty: false,
  previewHtml: '',
};

/**
 * Reactive editör state (Svelte 5 rune).
 * Ana sayfa bunu doğrudan `editorState.xsltText` gibi kullanır.
 * Doğrudan mutasyon reactive'dir.
 */
export const editorState = $state<EditorState>({ ...DEFAULTS });

export function resetEditorState(): void {
  Object.assign(editorState, DEFAULTS);
}
