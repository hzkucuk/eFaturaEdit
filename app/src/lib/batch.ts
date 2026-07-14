/**
 * Toplu regresyon koşusu — şablonu bir klasördeki TÜM faturalara karşı çalıştırır.
 *
 * <b>Neden var:</b> Tasarımcının gerçek korkusu şu: iskontolu faturada düzelttiğin
 * şey tevkifatlı faturayı bozuyor ve KİMSE FARK ETMİYOR. Editör tek seferde tek
 * fatura gösterdiği için diğer senaryolar kör noktada kalıyordu.
 *
 * İki şey yapar:
 *  1. <b>Çalıştır:</b> her XML'i şablondan geçirir; hangileri patladı, ne kadar
 *     sürdü, çıktı kaç bayt — hepsi listelenir.
 *  2. <b>Anlık görüntü (baseline):</b> her çıktının sha256'sını saklar. Şablonu
 *     değiştirip tekrar koştuğunda hangi faturaların çıktısının DEĞİŞTİĞİNİ söyler.
 *     Bu, projenin baştan beri savaştığı sessiz başarısızlığın panzehiri: "bir şeyi
 *     düzelttim, başka bir şeyi bozdum mu?" sorusunu tahminle değil ÖLÇÜMLE yanıtlar.
 *
 * Not: Örnek faturadan 159 KB ölü yük çıkarılırken (v2.27.3) tam olarak bu yöntem
 * elle uygulandı — Saxon çıktısının sha256'sı değişmediği için kaldırmanın güvenli
 * olduğu kanıtlanmıştı. Burada o hareket bir düğmeye dönüşüyor.
 */
import { open as openDialog } from '@tauri-apps/plugin-dialog';
import { readDir, readTextFile, writeTextFile, exists } from '@tauri-apps/plugin-fs';
import { appDataDir, join } from '@tauri-apps/api/path';
import { transformXml, XsltError } from '$lib/xslt';

const BASELINE_FILE = 'batch-baseline.json';

export type RowChange = 'new' | 'same' | 'changed' | 'none';

export interface BatchRow {
  name: string;
  path: string;
  ok: boolean;
  /** Hata mesajı (Saxon'un gerçek metni) — başarısızsa dolu. */
  error: string;
  ms: number;
  bytes: number;
  /** Çıktının sha256'sı (ilk 12 karakter gösterilir). */
  hash: string;
  /** Anlık görüntüye göre durum. Baseline yoksa 'none'. */
  change: RowChange;
}

export interface BatchReport {
  folder: string;
  rows: BatchRow[];
  okCount: number;
  failCount: number;
  changedCount: number;
  hasBaseline: boolean;
  totalMs: number;
}

interface BaselineFile {
  /** klasör yolu → { dosya adı → sha256 } */
  [folder: string]: { savedAt: string; files: Record<string, string> };
}

/** Kullanıcıdan fatura klasörü seçmesini ister. İptal ederse null. */
export async function pickFolder(): Promise<string | null> {
  const picked = await openDialog({ directory: true, multiple: false });
  return typeof picked === 'string' ? picked : null;
}

async function sha256(text: string): Promise<string> {
  const data = new TextEncoder().encode(text);
  const digest = await crypto.subtle.digest('SHA-256', data);
  return Array.from(new Uint8Array(digest))
    .map((b) => b.toString(16).padStart(2, '0'))
    .join('');
}

async function baselinePath(): Promise<string> {
  return join(await appDataDir(), BASELINE_FILE);
}

async function readBaselines(): Promise<BaselineFile> {
  const path = await baselinePath();
  if (!(await exists(path))) return {};
  try {
    return JSON.parse(await readTextFile(path)) as BaselineFile;
  } catch {
    // Bozuk baseline dosyası koşuyu engellemesin — yoksayıp yenisini yazarız.
    return {};
  }
}

/** Bu koşunun çıktı imzalarını anlık görüntü olarak saklar. */
export async function saveBaseline(report: BatchReport): Promise<number> {
  const all = await readBaselines();
  const files: Record<string, string> = {};
  for (const row of report.rows) {
    if (row.ok) files[row.name] = row.hash;
  }
  all[report.folder] = { savedAt: new Date().toISOString(), files };
  await writeTextFile(await baselinePath(), JSON.stringify(all, null, 2));
  return Object.keys(files).length;
}

/** Bu klasörün anlık görüntüsü var mı? */
export async function hasBaseline(folder: string): Promise<boolean> {
  const all = await readBaselines();
  return Boolean(all[folder]);
}

/** Klasördeki .xml dosyalarını (alfabetik) döndürür. */
async function listXmlFiles(folder: string): Promise<{ name: string; path: string }[]> {
  const entries = await readDir(folder);
  const files = entries
    .filter((e) => e.isFile && /\.xml$/i.test(e.name))
    .map((e) => ({ name: e.name, path: `${folder}/${e.name}` }));
  files.sort((a, b) => a.name.localeCompare(b.name, 'tr'));
  return files;
}

/**
 * Şablonu klasördeki her XML'e karşı çalıştırır.
 *
 * Sıralı çalışır (paralel değil): 50 faturaya aynı anda 50 sidecar süreci
 * başlatmak makineyi dizlerinin üstüne çökertir.
 */
export async function runBatch(
  folder: string,
  xsltText: string,
  onProgress?: (done: number, total: number, name: string) => void,
): Promise<BatchReport> {
  const files = await listXmlFiles(folder);
  const baselines = await readBaselines();
  const baseline = baselines[folder]?.files;

  const rows: BatchRow[] = [];
  const t0 = performance.now();

  for (let i = 0; i < files.length; i++) {
    const file = files[i];
    onProgress?.(i, files.length, file.name);

    const started = performance.now();
    const row: BatchRow = {
      name: file.name,
      path: file.path,
      ok: false,
      error: '',
      ms: 0,
      bytes: 0,
      hash: '',
      change: 'none',
    };

    try {
      const xmlText = await readTextFile(file.path);
      const html = await transformXml(xmlText, xsltText);
      row.ok = true;
      row.bytes = new TextEncoder().encode(html).length;
      row.hash = await sha256(html);

      if (baseline) {
        const before = baseline[file.name];
        row.change = !before ? 'new' : before === row.hash ? 'same' : 'changed';
      }
    } catch (err) {
      // Saxon'un GERÇEK mesajı gösterilir; "bilinmeyen hata" teşhisi kör eder.
      row.error =
        err instanceof XsltError
          ? err.line
            ? `${err.message} (satır ${err.line})`
            : err.message
          : ((err as Error).message ?? String(err));
    }

    row.ms = Math.round(performance.now() - started);
    rows.push(row);
  }

  onProgress?.(files.length, files.length, '');

  return {
    folder,
    rows,
    okCount: rows.filter((r) => r.ok).length,
    failCount: rows.filter((r) => !r.ok).length,
    changedCount: rows.filter((r) => r.change === 'changed').length,
    hasBaseline: Boolean(baseline),
    totalMs: Math.round(performance.now() - t0),
  };
}
