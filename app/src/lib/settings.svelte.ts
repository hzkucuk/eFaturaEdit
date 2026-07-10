/**
 * Uygulama ayarları — Svelte 5 reactive state (rune) + localStorage persist.
 */
import { browser } from '$app/environment';

/**
 * Kullanılabilir tema kimlikleri.
 * `light` = CodeMirror varsayılan (extension yok).
 * Diğerleri `thememirror` paketinden yüklenir; `dark` = `@codemirror/theme-one-dark`.
 */
export type Theme =
  | 'light'
  | 'dark'
  | 'dracula'
  | 'solarized-light'
  | 'cobalt'
  | 'espresso'
  | 'noctis-lilac'
  | 'rose-pine-dawn'
  | 'ayu-light'
  | 'clouds'
  | 'smoothy';

export const THEME_OPTIONS: { value: Theme; label: string; kind: 'light' | 'dark' }[] = [
  { value: 'light', label: 'Açık (Varsayılan)', kind: 'light' },
  { value: 'dark', label: 'Koyu (One Dark)', kind: 'dark' },
  { value: 'dracula', label: 'Dracula', kind: 'dark' },
  { value: 'cobalt', label: 'Cobalt', kind: 'dark' },
  { value: 'espresso', label: 'Espresso', kind: 'dark' },
  { value: 'solarized-light', label: 'Solarized Light', kind: 'light' },
  { value: 'ayu-light', label: 'Ayu Light', kind: 'light' },
  { value: 'noctis-lilac', label: 'Noctis Lilac', kind: 'light' },
  { value: 'rose-pine-dawn', label: 'Rosé Pine Dawn', kind: 'light' },
  { value: 'clouds', label: 'Clouds', kind: 'light' },
  { value: 'smoothy', label: 'Smoothy', kind: 'light' },
];

export interface Settings {
  fontSize: number;
  tabWidth: number;
  wordWrap: boolean;
  theme: Theme;
  showLineNumbers: boolean;
  showMinimap: boolean;
  autoTransformOnLoad: boolean;
  autoTransformOnSave: boolean;
  autoTransformDebounceMs: number; // 0 = kapalı, 500 = önerilen
  autoSave: boolean;
  autoSaveDelayMs: number;
  autocomplete: boolean;
  previewZoom: number; // 0.5 - 2.0
  previewWidth: number | null; // null = full, 320/768/1200
  panelSizes: {
    snippetsWidth: number;
    editorsWidth: number;
    xsltHeight: number;
  };
}

const DEFAULTS: Settings = {
  fontSize: 13,
  tabWidth: 2,
  wordWrap: true,
  theme: 'light',
  showLineNumbers: true,
  showMinimap: false,
  autoTransformOnLoad: true,
  autoTransformOnSave: true,
  autoTransformDebounceMs: 700,
  autoSave: false,
  autoSaveDelayMs: 3000,
  autocomplete: true,
  previewZoom: 1.0,
  previewWidth: null,
  panelSizes: {
    snippetsWidth: 280,
    editorsWidth: 560,
    xsltHeight: 300,
  },
};

const STORAGE_KEY = 'efaturaEdit.settings.v3';

function loadInitial(): Settings {
  if (!browser) return DEFAULTS;
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return DEFAULTS;
    const parsed = JSON.parse(raw);
    return {
      ...DEFAULTS,
      ...parsed,
      panelSizes: { ...DEFAULTS.panelSizes, ...(parsed.panelSizes ?? {}) },
    };
  } catch {
    return DEFAULTS;
  }
}

/** Reactive state (Svelte 5 rune) — modül scope'ta $state, tüm sayfalarda paylaşılır. */
export const settings = $state<Settings>(loadInitial());

/** `light` veya `dark` — arayüz genelinde tema seçimi için özet. */
export function themeKind(theme: Theme): 'light' | 'dark' {
  return THEME_OPTIONS.find((t) => t.value === theme)?.kind ?? 'light';
}

export function updateSetting<K extends keyof Settings>(key: K, value: Settings[K]): void {
  settings[key] = value;
  persist();
}

export function updatePanelSize<K extends keyof Settings['panelSizes']>(
  key: K,
  value: Settings['panelSizes'][K],
): void {
  settings.panelSizes[key] = value;
  persist();
}

export function resetSettings(): void {
  Object.assign(settings, DEFAULTS);
  settings.panelSizes = { ...DEFAULTS.panelSizes };
  persist();
}

function persist(): void {
  if (!browser) return;
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(settings));
  } catch {
    // Sessiz geç
  }
}
