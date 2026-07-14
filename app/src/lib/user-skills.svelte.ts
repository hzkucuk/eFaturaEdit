/**
 * AI yetenekleri (skill) — hazır katalog + kullanıcı tanımlı paketler.
 *
 * <b>Kullanıcı dosyası:</b> `$APPDATA/com.zaferbilgisayar.efaturaedit/user-skills.json`
 *
 * Bundled `skills.json` (eFaturaEdit.Core'dan `npm run data:sync` ile üretilir) asla
 * değiştirilmez — kullanıcı yetenekleri ayrı dosyada tutulur ve arayüzde yalnızca
 * GÖSTERİM sırasında birleştirilir. Aynı id ile bundled bir yeteneğin üzerine yazılamaz.
 *
 * Durum MODÜLDE tutulur (bileşende değil): Ayarlar sayfası ile AI panosu aynı listeyi
 * görür ve sayfa geçişinde liste kaybolmaz.
 */
import { appDataDir, join } from '@tauri-apps/api/path';
import { readTextFile, writeTextFile, exists, mkdir } from '@tauri-apps/plugin-fs';
import { aiSkills as bundledSkills } from './data';
import type { AiSkill } from './data/types';

const FILE_NAME = 'user-skills.json';

/** Kullanıcının kendi yazdığı yetenekler (reaktif). */
export const userSkills = $state<AiSkill[]>([]);

/** Kullanıcı yeteneklerini ayırt etmek için: bundled listede olmayan her id kullanıcınındır. */
export function isUserSkill(id: string): boolean {
  return !bundledSkills.some((s) => s.id === id);
}

async function getFilePath(): Promise<string> {
  const dir = await appDataDir();
  if (!(await exists(dir))) {
    await mkdir(dir, { recursive: true });
  }
  return join(dir, FILE_NAME);
}

let loaded: Promise<void> | null = null;

/**
 * Kullanıcı yeteneklerini diskten bir kez yükler (sonraki çağrılar aynı sözü döndürür).
 * Yetenekler prompta girdiği için, GÖNDERMEDEN ÖNCE beklenmesi gerekir — yarım yüklenmiş
 * liste, kullanıcının açtığı bir yeteneğin sessizce prompta girmemesi demektir.
 */
export function ensureUserSkillsLoaded(): Promise<void> {
  if (!loaded) {
    loaded = (async () => {
      const path = await getFilePath();
      if (!(await exists(path))) return;
      const raw = await readTextFile(path);
      if (!raw.trim()) return;
      const list = JSON.parse(raw) as AiSkill[];
      userSkills.splice(0, userSkills.length, ...list);
    })().catch((e) => {
      // Bozuk dosya tüm AI panosunu kilitlememelidir; boş listeyle devam et ama sessiz kalma.
      console.error('Kullanıcı yetenekleri okunamadı:', e);
    });
  }
  return loaded;
}

async function persist(): Promise<void> {
  const path = await getFilePath();
  await writeTextFile(path, JSON.stringify(userSkills, null, 2));
}

/**
 * Kullanıcı yeteneği ekler veya günceller.
 * @param skill Kaydedilecek yetenek
 * @param originalId Düzenleme modunda eski id (yeniden adlandırma için)
 * @throws Id boşsa, bundled bir yetenekle çakışıyorsa veya başka bir kullanıcı yeteneğinde kullanılıyorsa
 */
export async function saveUserSkill(skill: AiSkill, originalId?: string): Promise<void> {
  const id = skill.id.trim();
  if (!id) throw new Error('Yetenek kimliği (id) boş olamaz.');
  if (!skill.prompt.trim()) throw new Error('Yetenek talimatı boş olamaz.');
  if (bundledSkills.some((s) => s.id === id)) {
    throw new Error(`"${id}" hazır bir yeteneğin kimliği — başka bir kimlik seçin.`);
  }

  const targetId = originalId ?? id;
  const idx = userSkills.findIndex((s) => s.id === targetId);
  if (userSkills.some((s, i) => s.id === id && i !== idx)) {
    throw new Error(`"${id}" kimliği başka bir yetenek tarafından kullanılıyor.`);
  }

  const next = { ...skill, id };
  if (idx === -1) userSkills.push(next);
  else userSkills[idx] = next;
  await persist();
}

/** Kullanıcı yeteneğini siler. */
export async function removeUserSkill(id: string): Promise<void> {
  const idx = userSkills.findIndex((s) => s.id === id);
  if (idx === -1) return;
  userSkills.splice(idx, 1);
  await persist();
}

/** Hazır + kullanıcı yetenekleri (gösterim için birleşik liste; dosyalar ayrı kalır). */
export function allSkills(): AiSkill[] {
  return [...bundledSkills, ...userSkills];
}

/**
 * Seçili id'leri gerçek yeteneklere çevirir. Bilinmeyen id (silinmiş kullanıcı yeteneği,
 * kaldırılmış hazır paket) sessizce ATLANIR — ayarda ölü id kalması promptu bozmamalı.
 */
export function resolveSkills(ids: string[] | undefined): AiSkill[] {
  if (!ids?.length) return [];
  const all = allSkills();
  return ids.map((id) => all.find((s) => s.id === id)).filter((s): s is AiSkill => s !== undefined);
}

/**
 * Seçili yetenekleri sistem promptunun sonuna eklenecek tek bir bloğa çevirir.
 * Hiç yetenek seçili değilse BOŞ string döner — prompt aynen eskisi gibi kalır
 * (yeteneksiz davranış birebir korunur).
 */
export function skillBlock(list: AiSkill[]): string {
  if (list.length === 0) return '';
  return (
    '\n\n# EK YETENEKLER\n\n' +
    'Kullanıcı aşağıdaki uzmanlık paketlerini etkinleştirdi. Bunlar yukarıdaki kuralların ' +
    'YERİNE GEÇMEZ, üzerine derinlik ekler. Çelişki olursa yukarıdaki KAPSAM KİLİDİ ve ' +
    'ÇIKTI BİÇİMİ kuralları her zaman üstündür.\n\n' +
    list.map((s) => s.prompt.trim()).join('\n\n---\n\n')
  );
}

/** Kaba token tahmini — Türkçe/kod karışımı metinde ~3.5 karakter ≈ 1 token. */
export function estimateTokens(chars: number): number {
  return Math.round(chars / 3.5);
}
