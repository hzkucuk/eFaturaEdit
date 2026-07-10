/**
 * Kullanıcı tanımlı snippet'ler.
 *
 * <b>Konum:</b> `$APPDATA/com.zaferbilgisayar.efaturaedit/user-snippets.json`
 *
 * Bundled `snippets.json` (eFaturaEdit.Core'dan `npm run data:sync` ile üretilir)
 * asla değiştirilmez — kullanıcı snippet'leri ayrı bir dosyada tutulur ve
 * arayüzde bundled snippet'lerle birleştirilerek gösterilir.
 */
import { appDataDir, join } from '@tauri-apps/api/path';
import { readTextFile, writeTextFile, exists, mkdir } from '@tauri-apps/plugin-fs';
import type { Snippet } from './data/types';

const FILE_NAME = 'user-snippets.json';

async function getFilePath(): Promise<string> {
  const dir = await appDataDir();
  if (!(await exists(dir))) {
    await mkdir(dir, { recursive: true });
  }
  return join(dir, FILE_NAME);
}

/** Kaydedilmiş kullanıcı snippet'lerini oku (dosya yoksa boş liste). */
export async function listUserSnippets(): Promise<Snippet[]> {
  const path = await getFilePath();
  if (!(await exists(path))) return [];
  const raw = await readTextFile(path);
  if (!raw.trim()) return [];
  return JSON.parse(raw) as Snippet[];
}

async function persist(list: Snippet[]): Promise<void> {
  const path = await getFilePath();
  await writeTextFile(path, JSON.stringify(list, null, 2));
}

/**
 * Yeni bir kullanıcı snippet'i ekler veya var olanı günceller.
 * @param snippet Kaydedilecek snippet
 * @param originalKey Düzenleme modunda snippet'in eski anahtarı (yeniden adlandırma için)
 * @throws Anahtar (yeni değilse) başka bir kullanıcı snippet'i tarafından kullanılıyorsa Error
 */
export async function saveUserSnippet(snippet: Snippet, originalKey?: string): Promise<Snippet[]> {
  const list = await listUserSnippets();
  const targetKey = originalKey ?? snippet.key;
  const idx = list.findIndex((s) => s.key === targetKey);

  const collidesWithOther = list.some((s, i) => s.key === snippet.key && i !== idx);
  if (collidesWithOther) {
    throw new Error(`"${snippet.key}" anahtarı başka bir kullanıcı snippet'i tarafından kullanılıyor.`);
  }

  if (idx === -1) {
    list.push(snippet);
  } else {
    list[idx] = snippet;
  }
  await persist(list);
  return list;
}

/** Bir kullanıcı snippet'ini anahtarına göre siler. */
export async function removeUserSnippet(key: string): Promise<Snippet[]> {
  const list = await listUserSnippets();
  const next = list.filter((s) => s.key !== key);
  await persist(next);
  return next;
}
