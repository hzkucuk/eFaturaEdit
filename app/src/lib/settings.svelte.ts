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

/**
 * AI sağlayıcıları — hepsi BYOK (bring your own key): kullanıcı kendi
 * anahtarını girer, hiçbir anahtar uygulamaya gömülü/paylaşılı değildir.
 * openai/ollama/nvidia OpenAI-uyumlu chat completion formatını kullanır;
 * yalnızca base URL/model farklı — Rust tarafında tek kod yolu.
 */
export type AiProvider = 'anthropic' | 'openai' | 'gemini' | 'ollama' | 'nvidia';

export interface AiProviderConfig {
  apiKey: string;
  model: string;
  baseUrl: string;
  /** Son "Getir" çağrısından önbelleklenen model listesi (API anahtarı girilince otomatik doldurulur). */
  cachedModels: string[];
}

export const AI_PROVIDER_OPTIONS: { value: AiProvider; label: string; needsKey: boolean }[] = [
  { value: 'anthropic', label: 'Claude (Anthropic)', needsKey: true },
  { value: 'openai', label: 'ChatGPT (OpenAI)', needsKey: true },
  { value: 'gemini', label: 'Gemini (Google)', needsKey: true },
  { value: 'ollama', label: 'Ollama (yerel)', needsKey: false },
  { value: 'nvidia', label: 'NVIDIA NIM', needsKey: true },
];

const AI_PROVIDER_DEFAULTS: Record<AiProvider, AiProviderConfig> = {
  anthropic: {
    apiKey: '',
    model: 'claude-sonnet-5',
    baseUrl: 'https://api.anthropic.com/v1',
    cachedModels: [],
  },
  openai: { apiKey: '', model: 'gpt-4o', baseUrl: 'https://api.openai.com/v1', cachedModels: [] },
  gemini: {
    apiKey: '',
    // Google zaman zaman tarihli model sürümlerini yeni kullanıcılar için kapatıyor
    // (ör. gemini-2.5-flash → 404). "-latest" takma adları Google tarafından
    // güncel tutulan alias'lar, bu yüzden sabit tarihli isimden daha dayanıklı.
    model: 'gemini-flash-latest',
    baseUrl: 'https://generativelanguage.googleapis.com/v1beta',
    cachedModels: [],
  },
  ollama: {
    apiKey: '',
    model: 'llama3.1',
    baseUrl: 'http://localhost:11434/v1',
    cachedModels: [],
  },
  nvidia: {
    apiKey: '',
    model: 'meta/llama-3.1-70b-instruct',
    baseUrl: 'https://integrate.api.nvidia.com/v1',
    cachedModels: [],
  },
};

export interface Settings {
  fontSize: number;
  tabWidth: number;
  wordWrap: boolean;
  theme: Theme;
  showLineNumbers: boolean;
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
    aiPanelHeight: number;
  };
  aiProvider: AiProvider;
  aiProviders: Record<AiProvider, AiProviderConfig>;
}

const DEFAULTS: Settings = {
  fontSize: 13,
  tabWidth: 2,
  wordWrap: true,
  theme: 'light',
  showLineNumbers: true,
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
    aiPanelHeight: 320,
  },
  aiProvider: 'anthropic',
  aiProviders: AI_PROVIDER_DEFAULTS,
};

const STORAGE_KEY = 'efaturaEdit.settings.v3';

/**
 * Google yeni hesaplar için tarihli/eski Gemini modellerini kapattığından
 * (ör. gemini-2.5-flash → 404 "no longer available to new users"), daha önce
 * bu değerlerden birini kaydetmiş kullanıcıları otomatik olarak Google'ın
 * her zaman güncel tuttuğu "-latest" takma adına taşıyoruz. Böylece kullanıcı
 * Model kutusunu elle düzeltmek zorunda kalmıyor.
 */
function migrateAiProviderConfig(provider: AiProvider, cfg: AiProviderConfig): AiProviderConfig {
  if (provider !== 'gemini') return cfg;
  const dead = /^gemini-(1\.|1_|2\.|2_|pro$|1\.0|1\.5)/i.test(cfg.model) || cfg.model === 'gemini-2.5-flash';
  if (dead && !/-latest$/i.test(cfg.model)) {
    return { ...cfg, model: 'gemini-flash-latest' };
  }
  return cfg;
}

function loadInitial(): Settings {
  if (!browser) return DEFAULTS;
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    const parsed = raw ? JSON.parse(raw) : {};
    return {
      ...DEFAULTS,
      ...parsed,
      panelSizes: { ...DEFAULTS.panelSizes, ...(parsed.panelSizes ?? {}) },
      aiProviders: Object.fromEntries(
        (Object.keys(AI_PROVIDER_DEFAULTS) as AiProvider[]).map((p) => [
          p,
          migrateAiProviderConfig(p, {
            ...AI_PROVIDER_DEFAULTS[p],
            ...(parsed.aiProviders?.[p] ?? {}),
            cachedModels: [...(parsed.aiProviders?.[p]?.cachedModels ?? [])],
          }),
        ]),
      ) as Record<AiProvider, AiProviderConfig>,
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

export function updateAiProviderConfig<K extends keyof AiProviderConfig>(
  provider: AiProvider,
  key: K,
  value: AiProviderConfig[K],
): void {
  settings.aiProviders[provider][key] = value;
  persist();
}

export function resetSettings(): void {
  Object.assign(settings, DEFAULTS);
  settings.panelSizes = { ...DEFAULTS.panelSizes };
  settings.aiProviders = Object.fromEntries(
    (Object.keys(AI_PROVIDER_DEFAULTS) as AiProvider[]).map((p) => [
      p,
      { ...AI_PROVIDER_DEFAULTS[p], cachedModels: [] as string[] },
    ]),
  ) as Record<AiProvider, AiProviderConfig>;
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
