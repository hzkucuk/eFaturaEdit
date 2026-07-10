/**
 * Son açılan XSLT/XML dosyaların LocalStorage-persistent listesi.
 * En son açılan en üstte, en çok 10 kayıt.
 */
import { browser } from '$app/environment';

export interface RecentFile {
  path: string;
  kind: 'xslt' | 'xml';
  openedAt: number; // ms epoch
}

const STORAGE_KEY = 'efaturaEdit.recentFiles.v1';
const MAX = 10;

export const recentFiles = $state<RecentFile[]>(load());

function load(): RecentFile[] {
  if (!browser) return [];
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return [];
    const arr = JSON.parse(raw) as RecentFile[];
    return Array.isArray(arr) ? arr.slice(0, MAX) : [];
  } catch {
    return [];
  }
}

function persist(): void {
  if (!browser) return;
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(recentFiles));
  } catch {
    // sessiz geç
  }
}

/** Dosya açıldığında/kaydedildiğinde listeye ekle (en üste). */
export function pushRecent(path: string, kind: 'xslt' | 'xml'): void {
  const idx = recentFiles.findIndex((r) => r.path === path);
  if (idx >= 0) recentFiles.splice(idx, 1);
  recentFiles.unshift({ path, kind, openedAt: Date.now() });
  if (recentFiles.length > MAX) recentFiles.length = MAX;
  persist();
}

export function removeRecent(path: string): void {
  const idx = recentFiles.findIndex((r) => r.path === path);
  if (idx >= 0) {
    recentFiles.splice(idx, 1);
    persist();
  }
}

export function clearRecent(): void {
  recentFiles.length = 0;
  persist();
}

export function basename(path: string): string {
  return path.split(/[/\\]/).pop() ?? path;
}
