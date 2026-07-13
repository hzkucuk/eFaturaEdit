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

/**
 * Açık sohbetin CANLI durumu — bileşende değil, modülde.
 *
 * Neden bileşende değil: Ayarlar'a gidip dönünce AIAssistant unmount/remount
 * oluyor. Durum bileşende tutulursa **uçuşta olan istek sahipsiz kalır**:
 * yanıt geldiğinde artık yok olmuş bileşenin state'ine yazılır, yeni bileşen
 * ise ayrı bir reaktif kopya oluşturduğu için cevabı hiç görmez. Üstelik
 * `sending` de sıfırlandığından "Düşünüyor…" göstergesi kaybolur — kullanıcıya
 * sohbet ölmüş gibi görünür.
 *
 * Modül, uygulama çalıştığı sürece yaşar → hem istek hem gösterge sayfa
 * geçişlerinden sağ çıkar. (Uygulama yeniden başlayınca sıfırlanır, yani
 * "açılışta yeni sohbet" davranışı korunur.)
 */
export const aiRuntime = $state<{
  active: AiSession | null;
  /** Model şu an yanıt üretiyor mu? (sayfa geçişinde de doğru kalmalı) */
  sending: boolean;
  error: string;
  /**
   * Uçuştaki isteğin kimliği. İptal, `invoke`'u gerçekten durduramaz (Tauri
   * komutu Rust tarafında koşmaya devam eder); bunun yerine kimliği artırırız →
   * geç gelen yanıt "artık benim değil" diye YOK SAYILIR. Kullanıcı beklemekten
   * kurtulur, geç yanıt da sohbete sızmaz.
   */
  requestId: number;
}>({
  active: null,
  sending: false,
  error: '',
  requestId: 0,
});

/** Yeni bir uçuş başlat; dönen kimlik yanıt geldiğinde hâlâ geçerli mi diye bakılır. */
export function beginAiRequest(): number {
  aiRuntime.requestId += 1;
  aiRuntime.sending = true;
  return aiRuntime.requestId;
}

/** Uçuştaki istek hâlâ güncel mi? (İptal edildiyse veya yenisi başladıysa hayır.) */
export function isCurrentAiRequest(id: number): boolean {
  return aiRuntime.requestId === id;
}

/** Kullanıcı "Durdur" dedi: göstergeyi kapat, geç gelecek yanıtı geçersiz kıl. */
export function cancelAiRequest(): void {
  if (!aiRuntime.sending) return;
  aiRuntime.requestId += 1;
  aiRuntime.sending = false;
}

export function getActiveSession(): AiSession | null {
  return aiRuntime.active;
}

export function setActiveSession(session: AiSession | null): void {
  aiRuntime.active = session;
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
