/**
 * Otomatik güncelleme (Tauri updater).
 *
 * Akış: açılışta sessizce denetle → yeni sürüm varsa kullanıcıya SOR →
 * onaylarsa indir (ilerleme çubuğuyla) → kur → yeniden başlat.
 *
 * Güvenlik: paketler yayın anahtarıyla imzalanır ve uygulama, `tauri.conf.json`
 * içine gömülü açık anahtarla doğrulayamadığı hiçbir güncellemeyi kurmaz.
 * Doğrulama tamamen Rust tarafında yapılır — buradaki kod onu atlayamaz.
 */
import { check, type Update } from '@tauri-apps/plugin-updater';
import { relaunch } from '@tauri-apps/plugin-process';

export type UpdateStage =
  | 'idle' // henüz denetlenmedi
  | 'checking'
  | 'available' // yeni sürüm var, kullanıcı onayı bekleniyor
  | 'downloading'
  | 'installing'
  | 'none' // güncel
  | 'error';

export const updater = $state({
  stage: 'idle' as UpdateStage,
  version: '',
  notes: '',
  date: '',
  /** İndirilen bayt / toplam bayt (toplam bilinmiyorsa 0). */
  downloaded: 0,
  total: 0,
  error: '',
  /** Kullanıcı bu sürümü "sonra" diye kapattı mı? */
  dismissed: false,
});

/** Denetim sonucu gelen güncelleme nesnesi (indirme için gerekli). */
let pending: Update | null = null;

/**
 * Güncelleme denetle.
 *
 * @param manual Kullanıcı elle mi tetikledi? Açılıştaki sessiz denetimde
 *   hatalar yutulur (internet yok, sunucu erişilemez, dev modunda çalışıyoruz);
 *   kullanıcıyı rahatsız etmenin anlamı yok. Elle denetimde ise hata gösterilir.
 */
export async function checkForUpdate(manual = false): Promise<void> {
  if (updater.stage === 'checking' || updater.stage === 'downloading') return;

  updater.stage = 'checking';
  updater.error = '';
  try {
    const update = await check();
    if (update) {
      pending = update;
      updater.version = update.version;
      updater.notes = update.body ?? '';
      updater.date = update.date ?? '';
      updater.dismissed = false;
      updater.stage = 'available';
    } else {
      pending = null;
      updater.stage = 'none';
    }
  } catch (err) {
    pending = null;
    updater.error = (err as Error)?.message ?? String(err);
    updater.stage = manual ? 'error' : 'idle';
    if (!manual) console.warn('Güncelleme denetimi başarısız (sessiz):', err);
  }
}

/** Onaylanan güncellemeyi indir, kur ve uygulamayı yeniden başlat. */
export async function downloadAndInstall(): Promise<void> {
  if (!pending) return;

  updater.stage = 'downloading';
  updater.downloaded = 0;
  updater.total = 0;
  try {
    await pending.downloadAndInstall((event) => {
      switch (event.event) {
        case 'Started':
          updater.total = event.data.contentLength ?? 0;
          break;
        case 'Progress':
          updater.downloaded += event.data.chunkLength;
          break;
        case 'Finished':
          updater.stage = 'installing';
          break;
      }
    });
    // Windows'ta kurulum çalıştırıcısı uygulamayı zaten kapatır; macOS/Linux'ta
    // yeni sürümün devreye girmesi için biz yeniden başlatırız.
    await relaunch();
  } catch (err) {
    updater.error = (err as Error)?.message ?? String(err);
    updater.stage = 'error';
  }
}

/** "Sonra" — bu oturumda bir daha gösterme. */
export function dismissUpdate(): void {
  updater.dismissed = true;
}
