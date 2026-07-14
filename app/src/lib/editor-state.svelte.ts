/**
 * Global editör state — modül scope $state ile tüm sayfalarda paylaşılır.
 *
 * Neden? SvelteKit route değişince (`/` → `/settings` → `/`) ana sayfa
 * component'i unmount olur ve local state kaybolur. Bu store sayesinde
 * editör içerikleri, dosya yolları ve önizleme kalıcı hale gelir.
 *
 * <b>Sekmeler (v2.30.0):</b> Bir sekme = bir <b>çalışma</b>, yani bir XSLT+XML
 * <b>çifti</b> ve o çiftin önizlemesi. Uygulama zaten baştan sona çift üzerine
 * kurulu (dönüşümün girdisi şablon + veri; `needsSave` yorumu: "XSLT+XML tek bir
 * çalışma birimidir"), bu yüzden sekme de çifti temsil eder — pano başına ayrı
 * sekme değil.
 *
 * <b>`editorState` neden Proxy:</b> Sekmeler eklenmeden önce burada tek bir düz
 * `$state` nesnesi vardı ve `+page.svelte` ona yüzden fazla yerde
 * `editorState.xsltText` diye dokunuyordu. `editorState`'i "aktif sekmeye bakan"
 * bir Proxy yapınca o çağrı yerlerinin <b>hiçbiri değişmedi</b>: okuma da yazma da
 * aktif sekmeye yönlenir. Reaktivite korunur, çünkü Proxy'nin get tuzağı hem
 * `active.id`'yi hem de okunan alanı $state üzerinden okur — yani bir $effect
 * hem sekme değişiminde hem de alan değişiminde yeniden çalışır.
 */

/** Bir sekmenin içeriği. (Sekmeler öncesi tüm uygulamanın state'i buydu.) */
export interface EditorState {
  xsltText: string;
  xmlText: string;
  xsltPath: string | null;
  xmlPath: string | null;
  xsltDirty: boolean;
  xmlDirty: boolean;
  previewHtml: string;
}

export interface EditorTab extends EditorState {
  /** Kalıcı kimlik. Dizin değil — sekme kapanınca diğerlerinin indeksi kayar. */
  id: number;
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

let nextId = 1;

function createTab(init: Partial<EditorState> = {}): EditorTab {
  return { id: nextId++, ...DEFAULTS, ...init };
}

/** Açık sekmeler ve aktif olanın kimliği. En az bir sekme <b>daima</b> vardır. */
export const tabsState = $state<{ list: EditorTab[]; activeId: number }>({
  list: [createTab()],
  activeId: 1,
});

/**
 * Aktif sekme. Kimlik bir şekilde kaybolursa ilk sekmeye düşer — `undefined`
 * dönmek yerine, çünkü çağrı yerlerinin tamamı burada bir nesne olduğunu varsayar.
 */
function activeTab(): EditorTab {
  const found = tabsState.list.find((t) => t.id === tabsState.activeId);
  return found ?? tabsState.list[0];
}

type Field = keyof EditorState;

/**
 * Aktif sekmeye yönlenen görünüm. `editorState.xsltText` = aktif sekmenin XSLT'si.
 * Sekmelerden habersiz kodun (tüm eski çağrı yerleri) çalışmaya devam etmesini sağlar.
 */
export const editorState: EditorState = new Proxy({} as EditorState, {
  get(_t, prop) {
    return activeTab()[prop as Field];
  },
  set(_t, prop, value) {
    (activeTab() as Record<Field, unknown>)[prop as Field] = value;
    return true;
  },
  has(_t, prop) {
    return prop in activeTab();
  },
  ownKeys() {
    return Reflect.ownKeys(activeTab()).filter((k) => k !== 'id');
  },
  getOwnPropertyDescriptor(_t, prop) {
    if (prop === 'id') return undefined;
    return { configurable: true, enumerable: true, value: activeTab()[prop as Field], writable: true };
  },
});

/** Sekme başlığı: şablon adı, yoksa veri adı, o da yoksa "Yeni". */
export function tabTitle(tab: EditorTab, fallback = 'Yeni'): string {
  const path = tab.xsltPath ?? tab.xmlPath;
  if (!path) return fallback;
  const i = Math.max(path.lastIndexOf('/'), path.lastIndexOf('\\'));
  return i >= 0 ? path.slice(i + 1) : path;
}

export function isTabDirty(tab: EditorTab): boolean {
  return tab.xsltDirty || tab.xmlDirty;
}

/** Kaydedilmemiş değişikliği olan sekmeler — çıkış korumasının dayanağı. */
export function dirtyTabs(): EditorTab[] {
  return tabsState.list.filter(isTabDirty);
}

/** Hiç dokunulmamış sekme mi? (Yeni dosya buraya yüklenebilir, ezilecek bir şey yok.) */
export function isTabEmpty(tab: EditorTab): boolean {
  return !tab.xsltText && !tab.xmlText && !tab.xsltPath && !tab.xmlPath;
}

/** Yeni sekme açar ve aktifleştirir. */
export function newTab(init: Partial<EditorState> = {}): EditorTab {
  const tab = createTab(init);
  tabsState.list.push(tab);
  tabsState.activeId = tab.id;
  return tab;
}

export function activateTab(id: number): void {
  if (tabsState.list.some((t) => t.id === id)) tabsState.activeId = id;
}

/**
 * Sekmeyi kapatır. Son sekme kapanırsa yerine boş bir sekme konur — "sıfır sekme"
 * diye bir durum yok; `activeTab()` daima bir nesne döndürebilmeli.
 *
 * Aktif sekme kapanırsa odak sağdakine, o yoksa soldakine geçer (editör geleneği).
 */
export function closeTab(id: number): void {
  const i = tabsState.list.findIndex((t) => t.id === id);
  if (i === -1) return;

  tabsState.list.splice(i, 1);

  if (tabsState.list.length === 0) {
    const fresh = createTab();
    tabsState.list.push(fresh);
    tabsState.activeId = fresh.id;
    return;
  }

  if (tabsState.activeId === id) {
    const next = tabsState.list[i] ?? tabsState.list[i - 1];
    tabsState.activeId = next.id;
  }
}

/** Sekmeyi sürükle-bırakla taşır (kaynak indeksten hedef indekse). */
export function moveTab(from: number, to: number): void {
  const list = tabsState.list;
  if (from === to || from < 0 || from >= list.length || to < 0 || to >= list.length) return;
  const [moved] = list.splice(from, 1);
  list.splice(to, 0, moved);
}

/** Her şeyi sıfırla: tek bir boş sekme. */
export function resetEditorState(): void {
  const fresh = createTab();
  tabsState.list = [fresh];
  tabsState.activeId = fresh.id;
}
