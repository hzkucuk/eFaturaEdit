/**
 * Günlükleme — arayüz tarafı.
 *
 * Rust'taki `tauri-plugin-log`'a bağlanır; yazılan her şey **diske** düşer
 * (Ayarlar → Günlükler'den açılabilir). Konsola değil dosyaya yazmak şart:
 * kullanıcı makinesindeki sorunlarda DevTools'a erişemiyoruz, elimizde yalnızca
 * "çalışmıyor" cümlesi kalıyordu.
 *
 * Kullanım: `log.info(...)`, `log.error(...)`. Tauri dışında (tarayıcıda dev)
 * sessizce console'a düşer — çağıran tarafın ortam kontrolü yapması gerekmez.
 */
import { info, warn, error, debug } from '@tauri-apps/plugin-log';

type Sink = (message: string) => Promise<void>;

/** Tauri yoksa (saf tarayıcı) console'a düş — çağıranı ilgilendirmesin. */
function safe(sink: Sink, fallback: (...a: unknown[]) => void): (msg: string) => void {
  return (msg: string) => {
    try {
      void sink(msg).catch(() => fallback(msg));
    } catch {
      fallback(msg);
    }
  };
}

export const log = {
  debug: safe(debug, console.debug),
  info: safe(info, console.info),
  warn: safe(warn, console.warn),
  error: safe(error, console.error),
};

/** Hata nesnesini okunur tek satıra indir (stack dahil, kısaltılmış). */
export function describeError(err: unknown): string {
  if (err instanceof Error) {
    const stack = err.stack?.split('\n').slice(0, 4).join(' | ') ?? '';
    return `${err.name}: ${err.message}${stack ? ` — ${stack}` : ''}`;
  }
  return String(err);
}

let installed = false;

/**
 * Yakalanmayan hataları ve reddedilen promise'leri günlüğe bağla.
 *
 * Bunlar olmadan bir hata sessizce yutulup ekranda "hiçbir şey olmuyor" olarak
 * görünüyordu; artık en azından diskte izi kalır.
 */
export function installGlobalErrorLogging(): void {
  if (installed || typeof window === 'undefined') return;
  installed = true;

  window.addEventListener('error', (e) => {
    log.error(`[yakalanmayan] ${e.message} @ ${e.filename}:${e.lineno}:${e.colno}`);
  });

  window.addEventListener('unhandledrejection', (e) => {
    log.error(`[reddedilen promise] ${describeError(e.reason)}`);
  });
}
