/**
 * Manual drag-and-drop yönetimi.
 *
 * HTML5 native drag API'si WebKit (macOS Tauri) içinde bazen tetiklenmiyor
 * — özellikle intra-app source→target. Bunun yerine saf mouse event'leri
 * ile takip ederiz: mousedown (threshold sonra drag başlat) → mousemove
 * (hedef takip) → mouseup (drop). Escape ile iptal.
 *
 * Kullanım (snippet button):
 *   onmousedown={(e) => beginPossibleDrag(e, snippet.key, snippet.xsltCode, () => insertSnippet(snippet), handleDrop)}
 *
 * Hedef editör tanıma: hedef DOM'a `data-editor-kind="xslt"` veya `"xml"` koy.
 */

export interface DragState {
  active: boolean;
  snippetKey: string;
  snippetText: string;
  currentX: number;
  currentY: number;
  targetEditor: 'xslt' | 'xml' | null;
}

const INITIAL: DragState = {
  active: false,
  snippetKey: '',
  snippetText: '',
  currentX: 0,
  currentY: 0,
  targetEditor: null,
};

export const dragState = $state<DragState>({ ...INITIAL });

/** Mouse 5px'den fazla hareket ederse drag başlat, yoksa tıklama sayılır. */
const DRAG_THRESHOLD = 5;

export type DropCallback = (
  editorKind: 'xslt' | 'xml' | null,
  x: number,
  y: number,
  snippetKey: string,
  snippetText: string,
) => void;

/**
 * mousedown olayında çağır. Kullanıcı tıklarsa (mouse hareket etmeden bırakırsa)
 * `onClick` çalıştırılır; sürüklerse drag başlar, `onDrop` mouse-up'ta çalışır.
 */
export function beginPossibleDrag(
  e: MouseEvent,
  snippetKey: string,
  snippetText: string,
  onClick: () => void,
  onDrop: DropCallback,
): void {
  if (e.button !== 0) return; // sadece sol tık
  e.preventDefault();

  const startX = e.clientX;
  const startY = e.clientY;
  let dragStarted = false;

  function threshold(ev: MouseEvent): boolean {
    return (
      Math.abs(ev.clientX - startX) > DRAG_THRESHOLD ||
      Math.abs(ev.clientY - startY) > DRAG_THRESHOLD
    );
  }

  function onMoveInit(ev: MouseEvent) {
    if (dragStarted) return;
    if (!threshold(ev)) return;
    dragStarted = true;
    dragState.active = true;
    dragState.snippetKey = snippetKey;
    dragState.snippetText = snippetText;
    dragState.currentX = ev.clientX;
    dragState.currentY = ev.clientY;
    updateTarget(ev);
    document.body.style.cursor = 'grabbing';
  }

  function onMove(ev: MouseEvent) {
    onMoveInit(ev);
    if (!dragStarted) return;
    dragState.currentX = ev.clientX;
    dragState.currentY = ev.clientY;
    updateTarget(ev);
  }

  function onUp(ev: MouseEvent) {
    document.removeEventListener('mousemove', onMove);
    document.removeEventListener('mouseup', onUp);
    document.removeEventListener('keydown', onKey);
    document.body.style.cursor = '';
    if (!dragStarted) {
      onClick();
      return;
    }
    // Değerleri reset'ten ÖNCE yakala, sonra reset et
    const target = dragState.targetEditor;
    const x = ev.clientX;
    const y = ev.clientY;
    const key = dragState.snippetKey;
    const text = dragState.snippetText;
    resetState();
    onDrop(target, x, y, key, text);
  }

  function onKey(ev: KeyboardEvent) {
    if (ev.key === 'Escape') {
      document.removeEventListener('mousemove', onMove);
      document.removeEventListener('mouseup', onUp);
      document.removeEventListener('keydown', onKey);
      document.body.style.cursor = '';
      const key = dragState.snippetKey;
      const text = dragState.snippetText;
      resetState();
      onDrop(null, 0, 0, key, text);
    }
  }

  document.addEventListener('mousemove', onMove);
  document.addEventListener('mouseup', onUp);
  document.addEventListener('keydown', onKey);
}

function updateTarget(e: MouseEvent): void {
  const el = document.elementFromPoint(e.clientX, e.clientY);
  const host = el?.closest('[data-editor-kind]');
  const kind = host?.getAttribute('data-editor-kind');
  dragState.targetEditor = kind === 'xslt' || kind === 'xml' ? kind : null;
}

function resetState(): void {
  Object.assign(dragState, INITIAL);
}
