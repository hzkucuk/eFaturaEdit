/**
 * Klasör Ajanı — **Claude Code motoru** oturumu (modül-düzeyi durum).
 *
 * "Klasör Ajanı" (BYOK) modundan farkı: araç döngüsü BURADA dönmez. `claude` ikilisi
 * kendi araçlarını kendi çalıştırır; biz yalnızca **onay kapısıyız** ve akışı gösteririz.
 * Bu yüzden burada TOOLS/SYSTEM_PROMPT yok — bkz. `agent-session.svelte.ts` (o BYOK).
 *
 * <b>Neden MODÜLDE (bileşende değil):</b> uçuştaki koşu ve bekleyen onaylar sayfa
 * geçişinde ölmemeli (proje dersi: unmount'ta buharlaşan AI yanıtı).
 *
 * <b>Neden KUYRUK (tek modal değil):</b> **ölçüldü** — aynı anda 3 onay uçuşta olabilir;
 * model salt-okunur araçları paralelleştirir, yazmayı seri yapar. Tek slot olsaydı ikinci
 * istek birincinin üstüne yazar, bir araç sessizce cevapsız kalırdı.
 *
 * <b>Güvenlik sınırı (dürüst olmak zorundayız — CLAUDE.md ders 15/17):</b>
 * - Kök dışına **YAZAMAZ** (mac/Linux'ta sandbox; Windows'ta sandbox YOK → bash sorulur).
 * - Diski **OKUYABİLİR** — sandbox okumayı engellemiyor (ölçüldü). "Klasöre kilitli"
 *   demek yarısı yalan olur. Tek gerçek garanti: **onaysız hiçbir araç çalışmaz.**
 */
import { invoke } from '@tauri-apps/api/core';
import { listen } from '@tauri-apps/api/event';

/** Rust `ClaudeEvent` ile birebir (`#[serde(tag = "tur")]`). */
type ClaudeEvent =
  | { tur: 'Baslangic'; model: string; oturum: string }
  | { tur: 'Dusunuyor' }
  | { tur: 'Metin'; metin: string }
  | { tur: 'AracCagrisi'; arac: string; girdi: Record<string, unknown> }
  | { tur: 'AracSonucu'; hata: boolean; icerik: string }
  | {
      tur: 'Bitti';
      basarili: boolean;
      sure_ms: number;
      ozet: string | null;
      reddedilen: { tool_name: string; tool_input: Record<string, unknown> }[];
      maliyet_usd: number | null;
    };

/** Rust `EngineStatus`. */
export interface EngineStatus {
  source: 'managed' | 'system' | 'none';
  path: string | null;
  version: string | null;
  ready: boolean;
  reason: string | null;
  system_version: string | null;
  pinned_version: string;
  /**
   * Bu platformda sandbox var mı (Windows'ta YOK). Bilgilendirme metninin dürüstlüğü
   * buna bağlı — arayüz platformu **tahmin etmez**, Rust söyler.
   */
  sandbox: boolean;
}

export interface ClaudeFeedItem {
  kind: 'user' | 'assistant' | 'tool' | 'error' | 'info';
  text: string;
  /** Olayın geldiği an (Date.now ms) — kartta HH:MM:SS, tooltip'te tam tarih. */
  ts: number;
  /** Bu adımın süresi (ms). Araç: çağrı→sonuç; düşünme: başla→sonraki olay. */
  durationMs?: number;
  /**
   * Araç kartı. `in`/`out` = girdi (komut/dosya/içerik) ve çıktı (stdout/sonuç) —
   * kullanıcı kartı açınca görür (Claude Code arayüzündeki IN/OUT gibi).
   */
  tool?: { name: string; summary: string; ok?: boolean; in?: string; out?: string };
}

/** Yeni feed item — zaman damgası otomatik. */
function feedItem(item: Omit<ClaudeFeedItem, 'ts'>): ClaudeFeedItem {
  return { ...item, ts: Date.now() };
}

/** Kuyrukta bekleyen bir onay. */
export interface ClaudeApproval {
  id: string;
  arac: string;
  girdi: Record<string, unknown>;
  ozet: string;
}

export const claude = $state({
  /** Çalışma klasörü. Boşsa motor sürülemez. */
  root: '' as string,
  running: false,
  feed: [] as ClaudeFeedItem[],
  /** Bekleyen onaylar — **kuyruk**; ilki modalda gösterilir. */
  queue: [] as ClaudeApproval[],
  error: '',
  /** Motor durumu (kurulu mu, hangi sürüm). */
  engine: null as EngineStatus | null,
  /** Kurulum ilerlemesi; kurulum yokken `null`. */
  install: null as { phase: string; downloaded: number; total: number } | null,
  /** Bu oturumda "hep izin ver" denen araçlar. */
  trusted: new Set<string>(),
  /** Son koşunun reddedilen araçları — "başarılı" yalanına karşı gerçek karne. */
  lastDenied: [] as string[],
  /**
   * Aktif oturumun `session_id`'si. `null` = yeni oturum. Sonraki `runClaude` bunu
   * `--resume` ile geçirir → model önceki turları HATIRLAR (kullanıcının kırdığı senaryo).
   */
  sessionId: null as string | null,
  /**
   * Bu oturumda "Motor başladı" bir kez gösterildi mi? Resume turlarında tekrar
   * göstermeyiz — kullanıcı her turda "yeni oturum" sanıyordu.
   */
  started: false,
  /** Tüm araçlar otomatik onaylansın mı ("Otomatik düzenle" toggle'ı). */
  autoApprove: false,
});

/** Bir araç çağrısı için insanca özet (onay kartında + akışta). */
function toolSummary(arac: string, girdi: Record<string, unknown>): string {
  const s = (k: string) => String(girdi[k] ?? '');
  switch (arac) {
    case 'Read':
      return `oku: ${s('file_path')}`;
    case 'Write':
      return `yaz: ${s('file_path')}`;
    case 'Edit':
      return `düzenle: ${s('file_path')}`;
    case 'Bash':
      return `komut: ${s('command')}`;
    case 'Glob':
      return `ara: ${s('pattern')}`;
    case 'Grep':
      return `içerik ara: ${s('pattern')}`;
    default:
      return arac;
  }
}

// ─── Olay köprüsü ──────────────────────────────────────────────────────────
//
// Dinleyiciler MODÜL yüklenirken bir kez kurulur. `runClaude` başlamadan önce
// `hazir`'ı bekler — aksi halde koşunun ilk olayları dinleyici kurulmadan gelir
// ve sessizce düşerdi (kullanıcı boş ekran görür, hata görmez).

/** Araç girdisini IN olarak biçimle (komut ham, diğerleri JSON). */
function toolIn(arac: string, girdi: Record<string, unknown>): string {
  if (arac === 'Bash') return String(girdi.command ?? '');
  return JSON.stringify(girdi, null, 2);
}

/**
 * Açık "Düşünüyor…" göstergesini kapat — süresini hesaplayıp "Düşündü — Xs" yapar
 * (Claude Code arayüzündeki "Thought for 14s" gibi). Açık gösterge yoksa hiçbir şey yapmaz.
 */
function kapatDusunme(): void {
  const son = claude.feed.at(-1);
  if (son?.kind === 'info' && son.text === '__thinking__') {
    son.durationMs = Date.now() - son.ts;
    son.text = `Düşündü — ${(son.durationMs / 1000).toFixed(1)} sn`;
  }
}

/** Bir araç kartına sonucu bağla + süresini hesapla (çağrı→sonuç). */
function attachResult(hata: boolean, icerik: string): void {
  const son = [...claude.feed].reverse().find((f) => f.kind === 'tool' && f.tool);
  if (!son?.tool) return;
  son.tool.ok = !hata;
  son.tool.out = icerik;
  son.durationMs = Date.now() - son.ts;
}

async function baglan(): Promise<void> {
  await listen<{ id: string; arac: string; girdi: Record<string, unknown> }>(
    'claude-hook-request',
    (e) => {
      const { id, arac, girdi } = e.payload;
      const ozet = toolSummary(arac, girdi);
      // "Hep izin ver" / "Otomatik düzenle" → kuyruğa koymadan geç.
      if (claude.autoApprove || claude.trusted.has(arac)) {
        void decide(id, true);
        return; // araç kartı AracCagrisi olayında eklenir
      }
      claude.queue.push({ id, arac, girdi, ozet });
    },
  );

  await listen<ClaudeEvent>('claude-agent-event', (e) => {
    const o = e.payload;
    switch (o.tur) {
      case 'Baslangic':
        claude.sessionId = o.oturum; // sonraki tur --resume ile devam
        // "Motor başladı" YALNIZCA ilk turda (resume spam'i kullanıcıyı yanılttı).
        if (!claude.started) {
          claude.started = true;
          claude.feed.push(feedItem({ kind: 'info', text: `Motor başladı — ${o.model}` }));
        }
        break;
      case 'Dusunuyor':
        // "Düşünüyor" göstergesi: bir kez ekle, süresi sonraki olayda kapanır.
        if (claude.feed.at(-1)?.text !== '__thinking__') {
          claude.feed.push(feedItem({ kind: 'info', text: '__thinking__' }));
        }
        break;
      case 'Metin':
        kapatDusunme();
        if (o.metin.trim()) claude.feed.push(feedItem({ kind: 'assistant', text: o.metin }));
        break;
      case 'AracCagrisi':
        kapatDusunme();
        claude.feed.push(
          feedItem({
            kind: 'tool',
            text: '',
            tool: {
              name: o.arac,
              summary: toolSummary(o.arac, o.girdi),
              in: toolIn(o.arac, o.girdi),
            },
          }),
        );
        break;
      case 'AracSonucu':
        attachResult(o.hata, o.icerik);
        break;
      case 'Bitti':
        kapatDusunme();
        // ⚠️ `basarili`'ya ALDANMA: ölçüldü — her araç reddedilse bile motor
        // "success" der. Kullanıcıya doğruyu söylemek için `reddedilen`e bakılır.
        claude.lastDenied = o.reddedilen.map((r) => r.tool_name);
        if (claude.lastDenied.length > 0) {
          claude.feed.push(
            feedItem({
              kind: 'info',
              text: `Koşu bitti — ${claude.lastDenied.length} işlem reddedildiği için yapılmadı (${claude.lastDenied.join(', ')}).`,
            }),
          );
        }
        break;
    }
  });

  await listen<{ phase: string; downloaded: number; total: number }>(
    'claude-engine-progress',
    (e) => {
      claude.install = e.payload;
    },
  );
}
const hazir = baglan();

// ─── Dışa açık işlemler ────────────────────────────────────────────────────

export function setClaudeRoot(root: string): void {
  claude.root = root;
}

/** Motor durumunu tazele (arayüz "kur" düğmesi mi, "hazır" rozeti mi gösterecek). */
export async function refreshEngine(preferSystem: boolean): Promise<void> {
  try {
    claude.engine = await invoke<EngineStatus>('claude_engine_status', {
      preferSystem,
    });
  } catch (e) {
    claude.error = String((e as Error)?.message ?? e);
  }
}

/** Motoru indir + kur. İlerleme `claude.install` üzerinden akar. */
export async function installEngine(preferSystem: boolean): Promise<void> {
  claude.error = '';
  claude.install = { phase: 'indiriliyor', downloaded: 0, total: 0 };
  try {
    await invoke('claude_engine_install');
    await refreshEngine(preferSystem);
  } catch (e) {
    // Sessiz geri düşüş YASAK — kurulum başarısızsa kullanıcı bilmeli (ders 3).
    claude.error = `Motor kurulamadı: ${String((e as Error)?.message ?? e)}`;
  } finally {
    claude.install = null;
  }
}

/** Kuyruktaki bir onaya karar ver. */
export async function decide(id: string, izin: boolean, sebep = ''): Promise<void> {
  claude.queue = claude.queue.filter((q) => q.id !== id);
  try {
    await invoke('claude_hook_decide', { id, izin, sebep });
  } catch (e) {
    claude.error = String((e as Error)?.message ?? e);
  }
}

/** Kuyruktaki ilk onayı yanıtla (modalın çağırdığı). */
export async function resolveFirst(
  karar: 'allow' | 'deny' | 'always',
): Promise<void> {
  const p = claude.queue[0];
  if (!p) return;
  if (karar === 'always') claude.trusted.add(p.arac);
  await decide(p.id, karar !== 'deny');
}

/**
 * Görevi Claude Code motoruyla sür.
 *
 * `claude.sessionId` doluysa `--resume` ile **önceki turlar sürdürülür** (model hatırlar).
 * Giriş `/clear` ise claude'a gönderilmez — yeni oturum açılır (Claude Code sözleşmesi).
 */
export async function runClaude(gorev: string, sistemIkili: boolean): Promise<void> {
  const metin = gorev.trim();
  if (claude.running || !claude.root || !metin) return;

  // /clear = yeni oturum (claude'a gitmez, bizde session_id'yi sıfırlar).
  if (metin === '/clear') {
    resetClaudeChat();
    return;
  }

  await hazir; // dinleyiciler kurulmadan koşu başlarsa ilk olaylar düşer

  claude.error = '';
  claude.lastDenied = [];
  claude.feed.push(feedItem({ kind: 'user', text: metin }));
  claude.running = true;
  try {
    await invoke<unknown>('claude_agent_run', {
      kok: claude.root,
      gorev: metin,
      sistemIkili,
      resumeSession: claude.sessionId, // null = yeni oturum
    });
  } catch (e) {
    claude.error = String((e as Error)?.message ?? e);
    claude.feed.push(feedItem({ kind: 'error', text: claude.error }));
  } finally {
    claude.running = false;
    claude.queue = []; // koşu bitti; bekleyen onaylar artık cevapsız
  }
}

/** Uçuştaki koşuyu durdur — çalışan `claude` sürecini öldürür. */
export async function cancelClaude(): Promise<void> {
  try {
    await invoke('claude_agent_cancel');
    claude.feed.push(feedItem({ kind: 'info', text: 'Durduruldu.' }));
  } catch (e) {
    claude.error = String((e as Error)?.message ?? e);
  }
}

/** Yeni oturum: feed'i ve session_id'yi sıfırla (bağlam sıfırdan başlar). */
export function resetClaudeChat(): void {
  if (claude.running) return;
  claude.feed = [];
  claude.error = '';
  claude.lastDenied = [];
  claude.sessionId = null;
  claude.started = false;
}
