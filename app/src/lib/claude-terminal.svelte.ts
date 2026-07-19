/**
 * Claude Code **Terminal görünümü** — canlı oturum MODÜLDE yaşar.
 *
 * <b>Neden modülde (bileşende değil):</b> kullanıcı sekmeler arasında gezindiğinde
 * (Panel ↔ Terminal, hatta Öneri/Klasör Ajanı) bileşen unmount olur. Durum bileşende
 * dursaydı — ve ilk sürümde öyleydi — `onDestroy` PTY'yi öldürüp **çalışan claude
 * oturumunu uçuruyordu**: kullanıcı yan sekmeye bakıp döndüğünde ekran bomboştu.
 * Bu, projenin bilinen dersi ("uçuştaki durumu modülde tut", bkz. `ai-sessions`).
 *
 * <b>Nasıl korunuyor:</b> xterm örneği ve onun DOM kabı burada bir kez yaratılır;
 * bileşen mount olunca kap DOM'a **takılır**, unmount olunca yalnızca **sökülür**
 * (dispose YOK, kill YOK). Çıktı dinleyicisi de modül düzeyindedir → sekme kapalıyken
 * gelen çıktı bile tampona yazılır, dönünce ekranda durur.
 *
 * Oturum yalnızca kullanıcı **"Kapat"** derse (veya uygulama kapanırsa) biter.
 */
import { invoke } from '@tauri-apps/api/core';
import { listen, type UnlistenFn } from '@tauri-apps/api/event';

export const terminalState = $state({
  /** PTY çalışıyor mu (claude ayakta). */
  calisiyor: false,
  /** Süreç sonlandı (EOF) — kullanıcı yeniden başlatabilir. */
  bitti: false,
  hata: '' as string,
});

// ── Modül düzeyi canlı nesneler (bileşen ömründen bağımsız) ────────────────
type XTerm = {
  write: (d: Uint8Array) => void;
  onData: (cb: (d: string) => void) => void;
  open: (el: HTMLElement) => void;
  dispose: () => void;
  cols: number;
  rows: number;
  loadAddon: (a: unknown) => void;
};

let term: XTerm | null = null;
let fit: { fit: () => void } | null = null;
/** xterm'in yaşadığı kap — DOM'dan sökülüp takılır, ASLA yeniden yaratılmaz. */
let host: HTMLDivElement | null = null;
let unlistenOut: UnlistenFn | null = null;
let unlistenExit: UnlistenFn | null = null;

/** xterm + dinleyiciler bir kez kurulur. */
async function ensure(): Promise<void> {
  if (term) return;
  const [{ Terminal }, { FitAddon }] = await Promise.all([
    import('@xterm/xterm'),
    import('@xterm/addon-fit'),
  ]);
  host = document.createElement('div');
  host.style.width = '100%';
  host.style.height = '100%';

  const t = new Terminal({
    fontSize: 13,
    fontFamily: 'ui-monospace, Menlo, Consolas, monospace',
    cursorBlink: true,
    scrollback: 5000,
    theme: { background: '#1e1e1e', foreground: '#e0e0e0' },
  }) as unknown as XTerm;
  const f = new FitAddon() as unknown as { fit: () => void };
  t.loadAddon(f);
  t.open(host);
  term = t;
  fit = f;

  // Dinleyiciler MODÜLDE: sekme kapalıyken gelen çıktı da tampona yazılsın.
  unlistenOut = await listen<string>('claude-terminal-output', (e) => {
    const bytes = Uint8Array.from(atob(e.payload), (c) => c.charCodeAt(0));
    term?.write(bytes);
  });
  unlistenExit = await listen('claude-terminal-exit', () => {
    terminalState.bitti = true;
    terminalState.calisiyor = false;
  });
  t.onData((d: string) => void invoke('claude_terminal_write', { data: d }));
}

/** Görünür kaba tak (bileşen mount). Oturum sürüyorsa ekran aynen geri gelir. */
export async function attachTerminal(container: HTMLElement): Promise<void> {
  await ensure();
  if (host && host.parentElement !== container) container.appendChild(host);
  fit?.fit();
  if (terminalState.calisiyor) void syncSize();
}

/** DOM'dan sök (bileşen unmount) — oturumu ÖLDÜRME. */
export function detachTerminal(): void {
  host?.remove();
}

/** PTY boyutunu xterm'e eşitle. */
export async function syncSize(): Promise<void> {
  if (!term) return;
  fit?.fit();
  try {
    await invoke('claude_terminal_resize', { cols: term.cols, rows: term.rows });
  } catch {
    // Oturum yoksa önemsiz.
  }
}

/** Oturumu başlat. */
export async function startTerminal(kok: string, sistemIkili: boolean): Promise<void> {
  terminalState.hata = '';
  terminalState.bitti = false;
  await ensure();
  fit?.fit();
  try {
    await invoke('claude_terminal_start', {
      kok,
      sistemIkili,
      cols: term?.cols ?? 80,
      rows: term?.rows ?? 24,
    });
    terminalState.calisiyor = true;
  } catch (e) {
    terminalState.hata = String((e as Error)?.message ?? e);
    terminalState.calisiyor = false;
  }
}

/** Oturumu kapat — **yalnızca kullanıcı isteğiyle**. */
export async function stopTerminal(): Promise<void> {
  try {
    await invoke('claude_terminal_kill');
  } catch {
    // zaten kapalı olabilir
  }
  terminalState.calisiyor = false;
  terminalState.bitti = false;
  // xterm'i de sıfırla ki yeniden başlatınca eski ekran karışmasın.
  unlistenOut?.();
  unlistenExit?.();
  unlistenOut = unlistenExit = null;
  term?.dispose();
  term = null;
  fit = null;
  host?.remove();
  host = null;
}
