/**
 * **Ajan etkinliği** — hangi dosyaya bu oturumda dokunuldu (Gezgin rozetlerinin kaynağı).
 *
 * <b>Neden git değil:</b> kullanıcıların fatura/şablon klasörleri genelde git deposu
 * değildir; git'e dayansaydık rozetler çoğu klasörde **hiç görünmezdi**. Burada soru
 * "sürüm kontrolüne göre ne değişti" değil, **"ajan az önce neye dokundu"** — kullanıcının
 * canlı izlemek istediği şey bu.
 *
 * <b>Yeni mi değişti mi:</b> araç çağrısı geldiği anda dosya diskte VAR MI diye bakılır
 * (`exists`). Sonradan bakılsaydı yazma bitmiş olurdu ve her şey "değişti" görünürdü —
 * ölçümü doğru ana bağlamak şart (ders 11: ölçüm aracının kendisi de yanıltır).
 *
 * <b>Modülde (bileşende değil):</b> panel/sekme değişince kaybolmamalı; Gezgin kapalıyken
 * de ajan çalışmaya devam eder ve dönünce rozetler yerinde olmalı.
 */
import { exists } from '@tauri-apps/plugin-fs';

/** Bir dosyanın bu oturumdaki durumu. */
export type AgentFileState = 'yeni' | 'degisti' | 'silindi';

export const agentActivity = $state({
  /** Hangi kök için biriktirildi — kök değişince temizlenir. */
  kok: '' as string,
  /** Mutlak yol → durum. */
  dosyalar: {} as Record<string, AgentFileState>,
});

/** Kök değiştiyse etkinliği sıfırla (başka klasörün rozetleri sızmasın). */
export function setActivityRoot(kok: string): void {
  if (agentActivity.kok === kok) return;
  agentActivity.kok = kok;
  agentActivity.dosyalar = {};
}

/** Etkinliği temizle ("Yeni oturum" / kullanıcı isteği). */
export function clearAgentActivity(): void {
  agentActivity.dosyalar = {};
}

/**
 * Bir araç çağrısını kaydet. `arac` yazma sınıfı değilse hiçbir şey yapmaz.
 *
 * **Çağrı anında** `exists` ile bakar: yoksa `yeni`, varsa `degisti`. Zaten `yeni`
 * işaretlenmiş bir dosya sonraki düzenlemede `yeni` kalır (oturum boyunca "bunu ajan
 * oluşturdu" bilgisi daha değerli).
 */
export async function recordToolWrite(arac: string, path: string | undefined): Promise<void> {
  if (!path) return;
  if (!['Write', 'Edit', 'MultiEdit', 'NotebookEdit'].includes(arac)) return;
  if (agentActivity.dosyalar[path] === 'yeni') return; // oluşturan biziz, öyle kalsın
  try {
    const vardi = await exists(path);
    agentActivity.dosyalar[path] = vardi ? 'degisti' : 'yeni';
  } catch {
    // Yol okunamadıysa (izin/ağ sürücüsü) yine de değişiklik olduğunu göster.
    agentActivity.dosyalar[path] = 'degisti';
  }
}

/**
 * Gezgin tazelenince: bildiğimiz ama artık diskte olmayan dosyaları `silindi` yap.
 * `mevcutlar` = o an listelenen mutlak yollar (yalnızca listelenen klasörler için geçerli).
 */
export function reconcileDeleted(klasorYolu: string, mevcutlar: string[]): void {
  const kume = new Set(mevcutlar);
  for (const [yol, durum] of Object.entries(agentActivity.dosyalar)) {
    if (durum === 'silindi') continue;
    // Yalnızca bu klasörün DOĞRUDAN çocuklarını değerlendir — alt klasör listelenmemiş
    // olabilir, oradaki dosyayı "silindi" saymak yanlış olurdu.
    const parent = yol.slice(0, Math.max(yol.lastIndexOf('/'), yol.lastIndexOf('\\')));
    if (parent === klasorYolu && !kume.has(yol)) {
      agentActivity.dosyalar[yol] = 'silindi';
    }
  }
}

/** Rozet harfi + rengi (Gezgin'de gösterilir). */
export function activityBadge(durum: AgentFileState): { harf: string; renk: string; baslik: string } {
  switch (durum) {
    case 'yeni':
      return { harf: 'Y', renk: '#1a7f37', baslik: 'Ajan bu dosyayı oluşturdu' };
    case 'degisti':
      return { harf: 'M', renk: '#9a6700', baslik: 'Ajan bu dosyayı değiştirdi' };
    case 'silindi':
      return { harf: 'S', renk: '#b3261e', baslik: 'Dosya artık yok (silinmiş)' };
  }
}
