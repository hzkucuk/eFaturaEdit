/**
 * Claude Code motoru — **kalıcı oturum geçmişi** (kök başına).
 *
 * `claude`'un kendi `--resume` kalıcılığı (jsonl) bağlamı taşır; biz yalnızca
 * **session_id + gösterdiğimiz feed'i** saklarız — jsonl'yi ayrıştırmayız (kırılgan, ders 13).
 * Oturum kimliği = **claude session_id** (ölçüldü: `--resume` boyunca SABİT kalıyor, yeni id
 * üretmiyor → doğrudan kimlik olarak kullanılabilir).
 *
 * Depo deseni [`ai-sessions.svelte.ts`] ile aynı: reaktif `$state` dizi kaynak-doğru,
 * `localStorage`'a yansıtılır.
 */
import { browser } from '$app/environment';
import type { ClaudeFeedItem } from '$lib/claude-session.svelte';

export interface ClaudeHistorySession {
  /** = claude `session_id`. Bir sonraki mesaj bununla `--resume` eder (ölçüldü: sabit). */
  id: string;
  /** Hangi çalışma klasörü için — liste köke göre süzülür. */
  kok: string;
  /** İlk kullanıcı mesajından türetilen kısa başlık. */
  baslik: string;
  /** Gösterilen akışın anlık görüntüsü (yeniden yüklenince ekrana basılır). */
  feed: ClaudeFeedItem[];
  createdAt: number;
  updatedAt: number;
}

const STORAGE_KEY = 'efaturaEdit.claudeSessions.v1';

function load(): ClaudeHistorySession[] {
  if (!browser) return [];
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? (JSON.parse(raw) as ClaudeHistorySession[]) : [];
  } catch {
    return [];
  }
}

/** Kaynak-doğru reaktif liste (modül scope — tüm bileşenlerde paylaşılır). */
export const claudeSessions = $state<ClaudeHistorySession[]>(load());

function persist(): void {
  if (!browser) return;
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(claudeSessions));
  } catch {
    // Sessiz geç (kota dolabilir; geçmiş kritik veri değil).
  }
}

/** Feed'ten kısa başlık: ilk kullanıcı mesajının ilk ~40 karakteri. */
function baslikTuret(feed: ClaudeFeedItem[]): string {
  const ilk = feed.find((f) => f.kind === 'user');
  const metin = ilk ? ilk.text.replace(/\s+/g, ' ').trim().slice(0, 40) : '';
  return metin || '(boş oturum)';
}

/**
 * Oturumu kaydet/güncelle (session_id kimliğiyle). Koşu bitince çağrılır.
 * Boş feed veya kimliksiz oturum kaydedilmez (kaydedecek bir şey yok).
 */
export function saveClaudeSession(s: { id: string; kok: string; feed: ClaudeFeedItem[] }): void {
  if (!s.id || s.feed.length === 0) return;
  const now = Date.now();
  // Feed'i derin kopyala — sonradan mutasyon kayda sızmasın.
  const feed = s.feed.map((f) => ({ ...f, tool: f.tool ? { ...f.tool } : undefined }));
  const snapshot: ClaudeHistorySession = {
    id: s.id,
    kok: s.kok,
    baslik: baslikTuret(feed),
    feed,
    createdAt: now,
    updatedAt: now,
  };
  const idx = claudeSessions.findIndex((x) => x.id === s.id);
  if (idx === -1) {
    claudeSessions.push(snapshot);
  } else {
    // Var olanın createdAt'ını koru.
    snapshot.createdAt = claudeSessions[idx].createdAt;
    claudeSessions[idx] = snapshot;
  }
  persist();
}

export function deleteClaudeSession(id: string): void {
  const idx = claudeSessions.findIndex((x) => x.id === id);
  if (idx !== -1) {
    claudeSessions.splice(idx, 1);
    persist();
  }
}

/** Bir kök için kayıtlı oturumlar, en son güncellenen en üstte. */
export function listClaudeSessions(kok: string): ClaudeHistorySession[] {
  return claudeSessions.filter((s) => s.kok === kok).sort((a, b) => b.updatedAt - a.updatedAt);
}
