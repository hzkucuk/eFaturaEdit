/**
 * AI sohbet oturumları — kalıcı, kullanıcı tarafından seçilebilir liste.
 *
 * Uygulama her açılışta YENİ (boş) bir oturumla başlar; kullanıcı panelin
 * üstündeki açılır listeden önceki oturumlara geçebilir. Bir oturum, hangi
 * XSLT/XML dosya çifti için başlatıldığını da tutar; seçilen oturum o an açık
 * dosyadan farklı bir dosyaya aitse AIAssistant bir uyarı gösterir.
 *
 * Kaynak-doğru (source of truth) reaktif `sessions` dizisidir; localStorage'a
 * yansıtılır (bkz. settings.svelte.ts ile aynı desen).
 */
import { browser } from '$app/environment';

export interface AiChatEntry {
  role: 'user' | 'assistant';
  content: string;
  /**
   * Bu tura eklenen dosya bağlamının anlık görüntüsü (yalnızca "bağlam gönder"
   * açıkken ve dosya bir önceki gönderime göre değiştiğinde doldurulur).
   * Ekranda gösterilmez; API'ye gönderilirken içeriğe eklenir.
   */
  ctxXslt?: string;
  ctxXml?: string;
}

export interface AiSession {
  id: string;
  xsltPath: string | null;
  xmlPath: string | null;
  history: AiChatEntry[];
  createdAt: number;
  updatedAt: number;
}

const STORAGE_KEY = 'efaturaEdit.aiSessions.v1';

function load(): AiSession[] {
  if (!browser) return [];
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    const parsed = raw ? (JSON.parse(raw) as AiSession[]) : [];
    // Eski kayıtlarda createdAt olmayabilir — geriye dönük doldur.
    return parsed.map((s) => ({ ...s, createdAt: s.createdAt ?? s.updatedAt ?? Date.now() }));
  } catch {
    return [];
  }
}

/** Reactive state (Svelte 5 rune) — modül scope'ta, tüm bileşenlerde paylaşılır. */
export const sessions = $state<AiSession[]>(load());

function persist(): void {
  if (!browser) return;
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(sessions));
  } catch {
    // Sessiz geç
  }
}

/** Kayıtlı oturumlar, en son güncellenen en üstte. */
export function listSessions(): AiSession[] {
  return [...sessions].sort((a, b) => b.updatedAt - a.updatedAt);
}

/** Bellekte yeni (henüz kaydedilmemiş) boş oturum oluşturur. */
export function newSession(xsltPath: string | null, xmlPath: string | null): AiSession {
  const now = Date.now();
  return { id: crypto.randomUUID(), xsltPath, xmlPath, history: [], createdAt: now, updatedAt: now };
}

/** Oturumu ekler ya da (id'ye göre) günceller ve kalıcılaştırır. */
export function upsertSession(session: AiSession): void {
  const snapshot: AiSession = {
    ...session,
    history: session.history.map((h) => ({ ...h })),
    updatedAt: Date.now(),
  };
  const idx = sessions.findIndex((s) => s.id === session.id);
  if (idx === -1) sessions.push(snapshot);
  else sessions[idx] = snapshot;
  persist();
}

export function deleteSession(id: string): void {
  const idx = sessions.findIndex((s) => s.id === id);
  if (idx !== -1) {
    sessions.splice(idx, 1);
    persist();
  }
}

/** Açılır listede gösterilecek kısa etiket. */
export function sessionLabel(session: AiSession): string {
  const base = session.xsltPath ? (session.xsltPath.split(/[\\/]/).pop() ?? '') : '';
  const firstUser = session.history.find((h) => h.role === 'user');
  const snippet = firstUser ? firstUser.content.replace(/\s+/g, ' ').trim().slice(0, 40) : '(boş sohbet)';
  return base ? `${base} — ${snippet}` : snippet;
}
