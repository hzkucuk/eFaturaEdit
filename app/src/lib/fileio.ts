/**
 * Tauri v2 dialog + fs API sarmalayıcısı.
 *
 * Tauri runtime dışında (örn. `npm run dev` sadece tarayıcı) çağrıldığında
 * fallback davranışlar sunar (tarayıcıda dosya sistemi yok — hata döner).
 */
import { open as openDialog, save as saveDialog } from '@tauri-apps/plugin-dialog';
import { readTextFile, writeTextFile, exists } from '@tauri-apps/plugin-fs';

const XSLT_FILTERS = [
  { name: 'XSLT / XML', extensions: ['xslt', 'xsl', 'xml'] },
  { name: 'XSLT', extensions: ['xslt', 'xsl'] },
  { name: 'XML', extensions: ['xml'] },
  { name: 'Tüm dosyalar', extensions: ['*'] },
];

const XML_FILTERS = [
  { name: 'XML', extensions: ['xml'] },
  { name: 'Tüm dosyalar', extensions: ['*'] },
];

/** Kullanıcıya dosya aç dialog'u gösterir ve seçilen dosyanın yolunu + içeriğini döndürür. */
export async function openFile(
  kind: 'xslt' | 'xml' = 'xslt',
): Promise<{ path: string; content: string } | null> {
  const path = await openDialog({
    title: kind === 'xslt' ? 'XSLT şablonu aç' : 'XML dosyası aç',
    filters: kind === 'xslt' ? XSLT_FILTERS : XML_FILTERS,
    multiple: false,
    directory: false,
  });

  if (!path || Array.isArray(path)) return null;

  const content = await readTextFile(path);
  return { path, content };
}

/** Verilen içeriği belirtilen yola yazar. */
export async function saveFile(path: string, content: string): Promise<void> {
  await writeTextFile(path, content);
}

/** Bilinen bir yoldan dosyayı yeniden aç (recent files için). */
export async function reopenFile(path: string): Promise<{ path: string; content: string }> {
  if (!(await exists(path))) {
    throw new Error(`Dosya bulunamadı: ${path}`);
  }
  const content = await readTextFile(path);
  return { path, content };
}

/** Kullanıcıya "farklı kaydet" dialog'u gösterir ve dosyayı yazar. */
export async function saveFileAs(
  content: string,
  kind: 'xslt' | 'xml' = 'xslt',
  defaultName?: string,
): Promise<string | null> {
  const path = await saveDialog({
    title: kind === 'xslt' ? 'XSLT şablonunu kaydet' : 'XML dosyasını kaydet',
    filters: kind === 'xslt' ? XSLT_FILTERS : XML_FILTERS,
    defaultPath: defaultName,
  });

  if (!path) return null;
  await writeTextFile(path, content);
  return path;
}
