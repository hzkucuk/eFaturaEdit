/**
 * Uygulama ayarları — Svelte 5 reactive state (rune) + localStorage persist.
 */
import { browser } from '$app/environment';
import { invoke } from '@tauri-apps/api/core';

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
 * openai/ollama/nvidia/deepseek OpenAI-uyumlu chat completion formatını
 * kullanır; yalnızca base URL/model farklı — Rust tarafında tek kod yolu.
 */
export type AiProvider = 'anthropic' | 'openai' | 'gemini' | 'ollama' | 'nvidia' | 'deepseek';

export interface AiProviderConfig {
  apiKey: string;
  model: string;
  baseUrl: string;
  /** Son "Getir" çağrısından önbelleklenen model listesi (API anahtarı girilince otomatik doldurulur). */
  cachedModels: string[];
  /** Derin düşünme (extended thinking / reasoning). Yalnızca destekleyen sağlayıcı+modelde gönderilir. */
  thinking: boolean;
  /** Yaratıcılık; `null` = sağlayıcı varsayılanı (istekte hiç gönderilmez). */
  temperature: number | null;
  /**
   * Etkin AI yeteneklerinin (skill) id'leri — hazır katalogdan ve kullanıcının
   * kendi paketlerinden. Seçim SAĞLAYICI BAŞINA tutulur: güçlü bir modele altı
   * paketi birden açarken, token bütçesi dar bir yerel modele hiçbirini
   * açmayabilirsin. Boş liste = yeteneksiz (eski) davranış birebir korunur.
   */
  skills: string[];
}

/**
 * Dinamik AI parametreleri — hangi kontrolün hangi sağlayıcı+model için
 * gösterileceğini tanımlar. UI (Ayarlar + AI paneli) bu kayıtlardan üretilir;
 * yeni bir parametre (ör. web araması) eklemek = buraya bir kayıt eklemek.
 */
export interface AiParamDescriptor {
  key: 'thinking' | 'temperature';
  appliesTo: (provider: AiProvider, model: string) => boolean;
}

export const AI_PARAM_DESCRIPTORS: AiParamDescriptor[] = [
  {
    key: 'thinking',
    appliesTo: (provider, model) => {
      switch (provider) {
        // Extended thinking claude-3-7'den itibaren; bilinen eskileri ele,
        // gerisini (gelecek modeller dahil) destekliyor say.
        case 'anthropic':
          return !/claude-(1|2|3-[05])/i.test(model);
        // Chat Completions'ta reasoning_effort yalnızca reasoning modellerinde.
        case 'openai':
          return /^(o[134]|gpt-5)/i.test(model);
        // thinkingConfig Gemini 2.5+ ve "-latest" takma adlarında geçerli.
        case 'gemini':
          return /2\.5|latest/i.test(model);
        // DeepSeek'te düşünme ayrı modeldir (deepseek-reasoner) — anahtar yok.
        default:
          return false;
      }
    },
  },
  { key: 'temperature', appliesTo: () => true },
];

/** Temperature üst sınırı sağlayıcıya göre değişir (Anthropic 0–1, diğerleri 0–2). */
export function temperatureMax(provider: AiProvider): number {
  return provider === 'anthropic' ? 1 : 2;
}

export const AI_PROVIDER_OPTIONS: { value: AiProvider; label: string; needsKey: boolean }[] = [
  { value: 'anthropic', label: 'Claude (Anthropic)', needsKey: true },
  { value: 'openai', label: 'ChatGPT (OpenAI)', needsKey: true },
  { value: 'gemini', label: 'Gemini (Google)', needsKey: true },
  { value: 'deepseek', label: 'DeepSeek', needsKey: true },
  { value: 'ollama', label: 'Ollama (yerel)', needsKey: false },
  { value: 'nvidia', label: 'NVIDIA NIM', needsKey: true },
];

const PARAM_DEFAULTS = { thinking: false, temperature: null as number | null };

const AI_PROVIDER_DEFAULTS: Record<AiProvider, AiProviderConfig> = {
  anthropic: {
    apiKey: '',
    model: 'claude-sonnet-5',
    baseUrl: 'https://api.anthropic.com/v1',
    cachedModels: [],
    skills: [],
    ...PARAM_DEFAULTS,
  },
  openai: {
    apiKey: '',
    model: 'gpt-4o',
    baseUrl: 'https://api.openai.com/v1',
    cachedModels: [],
    skills: [],
    ...PARAM_DEFAULTS,
  },
  gemini: {
    apiKey: '',
    // Google zaman zaman tarihli model sürümlerini yeni kullanıcılar için kapatıyor
    // (ör. gemini-2.5-flash → 404). "-latest" takma adları Google tarafından
    // güncel tutulan alias'lar, bu yüzden sabit tarihli isimden daha dayanıklı.
    model: 'gemini-flash-latest',
    baseUrl: 'https://generativelanguage.googleapis.com/v1beta',
    cachedModels: [],
    skills: [],
    ...PARAM_DEFAULTS,
  },
  deepseek: {
    apiKey: '',
    // Derin düşünme için ayrı model: deepseek-reasoner (model listesinde çıkar).
    model: 'deepseek-chat',
    baseUrl: 'https://api.deepseek.com/v1',
    cachedModels: [],
    skills: [],
    ...PARAM_DEFAULTS,
  },
  ollama: {
    apiKey: '',
    model: 'llama3.1',
    baseUrl: 'http://localhost:11434/v1',
    cachedModels: [],
    skills: [],
    ...PARAM_DEFAULTS,
  },
  nvidia: {
    apiKey: '',
    model: 'meta/llama-3.1-70b-instruct',
    baseUrl: 'https://integrate.api.nvidia.com/v1',
    cachedModels: [],
    skills: [],
    ...PARAM_DEFAULTS,
  },
};

export interface Settings {
  /** Arayüz dili. `null` = henüz seçilmedi → sistem dilinden algılanır. */
  language: string | null;
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
  /** Açılışta sessizce güncelleme denetle. Kapalıysa yalnızca Ayarlar → "Şimdi denetle" ile bakılır. */
  autoCheckUpdates: boolean;
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
  /**
   * AI panosu modu.
   * - `suggest` = güvenli salt-öneri (varsayılan; dosya sistemine erişmez)
   * - `agent`   = Klasör Ajanı, BYOK motor (dosya okur/yazar; araç döngüsü uygulamada)
   * - `claude`  = Klasör Ajanı, **Claude Code motoru** (`claude` ikilisi sürülür)
   */
  aiMode: 'suggest' | 'agent' | 'claude';
  /** Klasör Ajanının "hep izin ver" dendiği güvenilir kök klasörler (kalıcı). */
  agentTrustedFolders: string[];
  /**
   * Claude Code motoru: uygulamanın indirdiği sabit sürüm yerine **sistemdeki**
   * `claude`'u kullan (gelişmiş). Sistemdeki sürüm de taban denetiminden geçer —
   * sürüklenme sessiz davranış değişikliği demektir, o yüzden eskiyse reddedilir.
   */
  claudeUseSystemBinary: boolean;
}

const DEFAULTS: Settings = {
  language: null, // ilk açılışta sistem dilinden algılanır
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
  autoCheckUpdates: true, // açılışta sessiz denetim varsayılan açık
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
  aiMode: 'suggest', // opt-in: Klasör Ajanı varsayılan KAPALI, kullanıcı açar
  agentTrustedFolders: [],
  claudeUseSystemBinary: false, // varsayılan: sürümü BİZ sabitleriz (ölçülmüş davranış)
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
            // Diziler her sağlayıcı için TAZE kopyalanır; ortak referans kalırsa
            // bir sağlayıcıya eklenen yetenek diğerlerinde de görünür.
            // Ayarları v2.31 ve öncesinde kaydetmiş kullanıcıda alan hiç yoktur → boş liste.
            skills: [...(parsed.aiProviders?.[p]?.skills ?? [])],
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
  // API anahtarı düz metin localStorage'a YAZILMAZ — OS anahtar zincirinde
  // (Keychain / Credential Manager / Secret Service) şifreli saklanır.
  if (key === 'apiKey') {
    void setApiKey(provider, value as string);
    return;
  }
  settings.aiProviders[provider][key] = value;
  persist();
}

function keychainAccount(provider: AiProvider): string {
  return `ai.${provider}`;
}

/** API anahtarını bellekte güncelle + OS anahtar zincirine şifreli yaz. */
export async function setApiKey(provider: AiProvider, value: string): Promise<void> {
  settings.aiProviders[provider].apiKey = value;
  persist(); // apiKey persist sırasında zaten strip edilir
  if (!browser) return;
  try {
    await invoke('secret_set', { account: keychainAccount(provider), value });
  } catch (e) {
    console.error('API anahtarı anahtar zincirine yazılamadı:', e);
  }
}

let apiKeysLoaded = false;
/**
 * Uygulama açılışında anahtarları OS anahtar zincirinden belleğe yükler.
 * Eski sürümlerde düz metin localStorage'da kalmış anahtar varsa anahtar
 * zincirine taşır (migrasyon) ve localStorage'dan temizler.
 */
export async function loadApiKeys(): Promise<void> {
  if (!browser || apiKeysLoaded) return;
  apiKeysLoaded = true;
  for (const p of Object.keys(AI_PROVIDER_DEFAULTS) as AiProvider[]) {
    try {
      const stored = await invoke<string | null>('secret_get', { account: keychainAccount(p) });
      if (stored) {
        settings.aiProviders[p].apiKey = stored;
      } else if (settings.aiProviders[p].apiKey) {
        // Migrasyon: eski düz-metin anahtarı keychain'e taşı.
        await invoke('secret_set', { account: keychainAccount(p), value: settings.aiProviders[p].apiKey });
      }
    } catch (e) {
      console.error('API anahtarı anahtar zincirinden okunamadı:', e);
    }
  }
  persist(); // düz-metin anahtarları localStorage'dan temizle
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
  // Anahtar zincirindeki kayıtlı API anahtarlarını da temizle.
  if (browser) {
    for (const p of Object.keys(AI_PROVIDER_DEFAULTS) as AiProvider[]) {
      invoke('secret_set', { account: keychainAccount(p), value: '' }).catch(() => {});
    }
  }
  persist();
}

function persist(): void {
  if (!browser) return;
  try {
    // API anahtarları localStorage'a düz metin YAZILMAZ — yalnızca OS anahtar
    // zincirinde tutulur. Kaydederken apiKey alanını boşalt.
    const sanitized = {
      ...settings,
      aiProviders: Object.fromEntries(
        (Object.keys(settings.aiProviders) as AiProvider[]).map((p) => [
          p,
          { ...settings.aiProviders[p], apiKey: '' },
        ]),
      ),
    };
    localStorage.setItem(STORAGE_KEY, JSON.stringify(sanitized));
  } catch {
    // Sessiz geç
  }
}
