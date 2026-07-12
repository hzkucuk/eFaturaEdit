/**
 * Çoklu dil (i18n) — kullanıcı tarafından DÜZENLENEBİLİR.
 *
 * <b>Üç katman, üstteki alttakini ezer:</b>
 *   kullanıcı düzenlemesi → seçili dilin yerleşik sözlüğü → referans (tr)
 * Böylece bir çeviri eksikse metin KAYBOLMAZ, referansa düşer. (Sessizce boş
 * dize dönmek bu projede yeterince canımızı yaktı.)
 *
 * <b>Neden fonksiyon değil şablon:</b> Parametreli metinler `(n) => ...` şeklinde
 * JS fonksiyonu olsaydı TypeScript parametreleri denetlerdi — ama o zaman metin
 * KOD olurdu ve kullanıcı arayüzden düzenleyemezdi. Bunun yerine yer tutuculu
 * şablon: `'XSLT satır {line}'`. Kaybettiğimiz derleme-zamanı denetimini
 * `validateLocales()` açılışta telafi eder.
 *
 * <b>Kullanım:</b>
 *   {m.toolbar.save}                            // düz metin
 *   {f(m.wysiwyg.sourceLine, { line, tag })}    // parametreli
 */
import { browser } from '$app/environment';
import { tr } from './locales/tr';
import { en } from './locales/en';
import { es } from './locales/es';
import { ru } from './locales/ru';
import { pl } from './locales/pl';

/** Sözlüğün şekli — Türkçe referans alınır. Yalnızca dize içerir. */
export type Messages = typeof tr;

/** bölüm → anahtar → metin */
export type LocaleDict = Record<string, Record<string, string>>;

export interface LocaleOption {
  code: string;
  /** Dilin KENDİ dilindeki adı — kullanıcı kendi dilini tanısın. */
  label: string;
  flag: string;
  /** Kullanıcının eklediği bir dil mi? (silinebilir) */
  custom?: boolean;
}

const BUILTIN: Record<string, LocaleDict> = {
  tr: tr as unknown as LocaleDict,
  en: en as unknown as LocaleDict,
  es: es as unknown as LocaleDict,
  ru: ru as unknown as LocaleDict,
  pl: pl as unknown as LocaleDict,
};

const BUILTIN_OPTIONS: LocaleOption[] = [
  { code: 'tr', label: 'Türkçe', flag: '🇹🇷' },
  { code: 'en', label: 'English', flag: '🇬🇧' },
  { code: 'es', label: 'Español', flag: '🇪🇸' },
  { code: 'ru', label: 'Русский', flag: '🇷🇺' },
  { code: 'pl', label: 'Polski', flag: '🇵🇱' },
];

/** Referans dil — metin hiçbir yerde bulunamazsa buraya düşülür. */
const REFERENCE = 'tr';

const OVERRIDES_KEY = 'efatura-edit:i18n-overrides';
const CUSTOM_KEY = 'efatura-edit:i18n-custom-locales';

type Overrides = Record<string, LocaleDict>;

function loadJson<T>(key: string, fallback: T): T {
  if (!browser) return fallback;
  try {
    const raw = localStorage.getItem(key);
    return raw ? (JSON.parse(raw) as T) : fallback;
  } catch {
    return fallback;
  }
}

const store = $state({
  code: REFERENCE,
  overrides: loadJson<Overrides>(OVERRIDES_KEY, {}),
  customLocales: loadJson<LocaleOption[]>(CUSTOM_KEY, []),
});

function persist(): void {
  if (!browser) return;
  localStorage.setItem(OVERRIDES_KEY, JSON.stringify(store.overrides));
  localStorage.setItem(CUSTOM_KEY, JSON.stringify(store.customLocales));
}

/** Seçilebilir tüm diller (yerleşik + kullanıcının eklediği). */
export function allLocales(): LocaleOption[] {
  return [...BUILTIN_OPTIONS, ...store.customLocales];
}

export function getLocale(): string {
  return store.code;
}

export function setLocale(code: string): void {
  store.code = allLocales().some((o) => o.code === code) ? code : REFERENCE;
  if (browser) document.documentElement.lang = store.code;
}

/** İşletim sisteminin dilinden bir kod tahmin et. */
export function detectSystemLocale(): string {
  if (typeof navigator === 'undefined') return REFERENCE;
  const opts = allLocales();
  for (const l of [navigator.language, ...(navigator.languages ?? [])]) {
    const base = l.toLowerCase().split('-')[0];
    if (opts.some((o) => o.code === base)) return base;
  }
  // Tanımadığımız bir sistem dili: Türkçe olmayan birine Türkçe göstermektense
  // İngilizce göster.
  return 'en';
}

/** Etkin metin: kullanıcı düzenlemesi → dilin sözlüğü → referans. */
function resolve(section: string, key: string): string {
  const user = store.overrides[store.code]?.[section]?.[key];
  if (user) return user;

  const own = BUILTIN[store.code]?.[section]?.[key];
  if (own) return own;

  // Kullanıcının eklediği dilin yerleşik sözlüğü yoktur → referansa düş.
  // Hiç bulunamazsa GÖRÜNÜR bir işaret bırak; sessizce boş dönme.
  return BUILTIN[REFERENCE][section]?.[key] ?? `⟨${section}.${key}⟩`;
}

/**
 * Etkin sözlük. İki katlı Proxy — `m.toolbar.save` erişimi anında çözümlenir ve
 * `store` okunduğu için Svelte bağımlılığı izler → dil değişince arayüz yenilenir.
 */
export const m: Messages = new Proxy({} as Messages, {
  get(_t, section: string) {
    return new Proxy(
      {},
      {
        get: (_t2, key: string) => resolve(section, key),
        ownKeys: () => Reflect.ownKeys(BUILTIN[REFERENCE][section] ?? {}),
        getOwnPropertyDescriptor: () => ({ enumerable: true, configurable: true }),
      },
    );
  },
  ownKeys: () => Reflect.ownKeys(BUILTIN[REFERENCE]),
  getOwnPropertyDescriptor: () => ({ enumerable: true, configurable: true }),
});

/**
 * Şablondaki `{ad}` yer tutucularını doldur.
 *
 * Eksik parametre SESSİZCE boş bırakılmaz — yer tutucu olduğu gibi kalır ve
 * konsola yazılır; bozuk çeviri ekranda görünür olur.
 */
export function f(template: string, params: Record<string, string | number>): string {
  return template.replace(/\{(\w+)\}/g, (whole, name: string) => {
    const v = params[name];
    if (v == null) {
      console.warn(`[i18n] eksik parametre "${name}" — şablon: ${template}`);
      return whole;
    }
    return String(v);
  });
}

/** Şablondaki yer tutucu adları (sıralı). */
export function placeholders(template: string): string[] {
  return [...template.matchAll(/\{(\w+)\}/g)].map((x) => x[1]).sort();
}

export interface LocaleIssue {
  locale: string;
  section: string;
  key: string;
  problem: 'eksik' | 'yer-tutucu';
  detail: string;
}

/**
 * Sözlükleri referansa göre denetle — şablona geçince kaybettiğimiz
 * derleme-zamanı denetiminin yerine geçer. Sorunlar düzenleyicide gösterilir.
 */
export function validateLocales(): LocaleIssue[] {
  const ref = BUILTIN[REFERENCE];
  const issues: LocaleIssue[] = [];

  const compare = (locale: string, dict: LocaleDict, requireAll: boolean) => {
    for (const [section, keys] of Object.entries(ref)) {
      for (const [key, refText] of Object.entries(keys)) {
        const text = dict[section]?.[key];
        if (!text) {
          if (requireAll) issues.push({ locale, section, key, problem: 'eksik', detail: refText });
          continue;
        }
        const want = placeholders(refText).join(',');
        const got = placeholders(text).join(',');
        if (want !== got) {
          issues.push({
            locale,
            section,
            key,
            problem: 'yer-tutucu',
            detail: `beklenen {${want || '—'}}, bulunan {${got || '—'}}`,
          });
        }
      }
    }
  };

  for (const [code, dict] of Object.entries(BUILTIN)) {
    if (code !== REFERENCE) compare(code, dict, true);
  }
  // Kullanıcı düzenlemeleri de bozuk olabilir (eksik olması normal, yer tutucu hatası değil).
  for (const [code, dict] of Object.entries(store.overrides)) compare(code, dict, false);

  return issues;
}

// ─── Çeviri düzenleyici API'si ────────────────────────────────────────

export interface EditableEntry {
  section: string;
  key: string;
  /** Referans (Türkçe) metin — çevirmen neyi çevirdiğini görsün. */
  reference: string;
  /** Yerleşik çeviri (kullanıcı düzenlemesi hariç). */
  builtin: string;
  /** Kullanıcının yazdığı metin (yoksa ''). */
  override: string;
  /** Çevirmenin KORUMASI gereken yer tutucular. */
  placeholders: string[];
}

export function listEntries(locale: string): EditableEntry[] {
  const ref = BUILTIN[REFERENCE];
  const dict = BUILTIN[locale] ?? {};
  const out: EditableEntry[] = [];
  for (const [section, keys] of Object.entries(ref)) {
    for (const [key, refText] of Object.entries(keys)) {
      out.push({
        section,
        key,
        reference: refText,
        builtin: dict[section]?.[key] ?? '',
        override: store.overrides[locale]?.[section]?.[key] ?? '',
        placeholders: placeholders(refText),
      });
    }
  }
  return out;
}

export function setOverride(locale: string, section: string, key: string, text: string): void {
  const byLocale = (store.overrides[locale] ??= {});
  const bySection = (byLocale[section] ??= {});
  if (text.trim() === '') delete bySection[key];
  else bySection[key] = text;
  persist();
}

export function clearOverrides(locale: string): void {
  delete store.overrides[locale];
  persist();
}

export function hasOverrides(locale: string): boolean {
  const d = store.overrides[locale];
  return !!d && Object.values(d).some((sec) => Object.keys(sec).length > 0);
}

export function addCustomLocale(code: string, label: string, flag: string): void {
  const clean = code.trim().toLowerCase();
  if (!clean || allLocales().some((o) => o.code === clean)) return;
  store.customLocales.push({
    code: clean,
    label: label.trim() || clean,
    flag: flag || '🏳️',
    custom: true,
  });
  persist();
}

export function removeCustomLocale(code: string): void {
  store.customLocales = store.customLocales.filter((o) => o.code !== code);
  delete store.overrides[code];
  if (store.code === code) setLocale(REFERENCE);
  persist();
}

/** Dili JSON olarak dışa aktar (paylaşmak/GitHub'a katkı için). */
export function exportLocale(locale: string): string {
  const merged: LocaleDict = {};
  for (const e of listEntries(locale)) {
    (merged[e.section] ??= {})[e.key] = e.override || e.builtin || e.reference;
  }
  const opt = allLocales().find((o) => o.code === locale);
  return JSON.stringify(
    { code: locale, label: opt?.label, flag: opt?.flag, messages: merged },
    null,
    2,
  );
}

/** Dışa aktarılmış JSON'u geri yükle. Bozuk dosyada SESSİZ KALMAZ, fırlatır. */
export function importLocale(json: string): string {
  const data = JSON.parse(json) as {
    code?: string;
    label?: string;
    flag?: string;
    messages?: LocaleDict;
  };
  if (!data.code || !data.messages) {
    throw new Error('Geçersiz dil dosyası: "code" ve "messages" alanları gerekli.');
  }
  if (!allLocales().some((o) => o.code === data.code)) {
    addCustomLocale(data.code, data.label ?? data.code, data.flag ?? '🏳️');
  }
  store.overrides[data.code] = data.messages;
  persist();
  return data.code;
}
