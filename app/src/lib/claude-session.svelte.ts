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
  tool?: { name: string; summary: string; ok?: boolean };
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

async function baglan(): Promise<void> {
  await listen<{ id: string; arac: string; girdi: Record<string, unknown> }>(
    'claude-hook-request',
    (e) => {
      const { id, arac, girdi } = e.payload;
      const ozet = toolSummary(arac, girdi);

      // "Hep izin ver" denmiş araç: kuyruğa koymadan geç.
      if (claude.trusted.has(arac)) {
        void decide(id, true);
        claude.feed.push({ kind: 'tool', text: '', tool: { name: arac, summary: ozet } });
        return;
      }
      claude.queue.push({ id, arac, girdi, ozet });
    },
  );

  await listen<ClaudeEvent>('claude-agent-event', (e) => {
    const o = e.payload;
    switch (o.tur) {
      case 'Baslangic':
        claude.feed.push({ kind: 'info', text: `Motor başladı — ${o.model}` });
        break;
      case 'Dusunuyor':
        break; // İçeriği taşımıyoruz; "çalışıyor" göstergesi zaten var.
      case 'Metin':
        if (o.metin.trim()) claude.feed.push({ kind: 'assistant', text: o.metin });
        break;
      case 'AracCagrisi':
        claude.feed.push({
          kind: 'tool',
          text: '',
          tool: { name: o.arac, summary: toolSummary(o.arac, o.girdi) },
        });
        break;
      case 'AracSonucu': {
        const son = [...claude.feed].reverse().find((f) => f.kind === 'tool' && f.tool);
        if (son?.tool) son.tool.ok = !o.hata;
        break;
      }
      case 'Bitti':
        // ⚠️ `basarili`'ya ALDANMA: ölçüldü — her araç reddedilse bile motor
        // "success" der. Kullanıcıya doğruyu söylemek için `reddedilen`e bakılır.
        claude.lastDenied = o.reddedilen.map((r) => r.tool_name);
        if (claude.lastDenied.length > 0) {
          claude.feed.push({
            kind: 'info',
            text: `Koşu bitti — ${claude.lastDenied.length} işlem reddedildiği için yapılmadı (${claude.lastDenied.join(', ')}).`,
          });
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

/** Görevi Claude Code motoruyla sür. */
export async function runClaude(gorev: string, sistemIkili: boolean): Promise<void> {
  if (claude.running || !claude.root || !gorev.trim()) return;
  await hazir; // dinleyiciler kurulmadan koşu başlarsa ilk olaylar düşer

  claude.error = '';
  claude.lastDenied = [];
  claude.feed.push({ kind: 'user', text: gorev });
  claude.running = true;
  try {
    await invoke<unknown>('claude_agent_run', {
      kok: claude.root,
      gorev,
      sistemIkili,
    });
  } catch (e) {
    claude.error = String((e as Error)?.message ?? e);
    claude.feed.push({ kind: 'error', text: claude.error });
  } finally {
    claude.running = false;
    claude.queue = []; // koşu bitti; bekleyen onaylar artık cevapsız
  }
}

export function resetClaudeChat(): void {
  if (claude.running) return;
  claude.feed = [];
  claude.error = '';
  claude.lastDenied = [];
}
