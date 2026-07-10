/**
 * Kullanıcı örnekleri yönetimi.
 *
 * <b>Konum:</b> `$APPDATA/com.zaferbilgisayar.efaturaedit/samples/`
 * (macOS: `~/Library/Application Support/com.zaferbilgisayar.efaturaedit/samples/`)
 *
 * <b>Kural:</b> Her örnek bir <c>&lt;isim&gt;.xslt</c> + <c>&lt;isim&gt;.xml</c> çiftidir.
 * Base name aynı olmak zorundadır. Tek başına duran XSLT veya XML listede
 * görünmez (eksik eş uyarısı verilebilir).
 */

import { appDataDir, join, basename } from '@tauri-apps/api/path';
import { readDir, mkdir, exists, copyFile, remove, readTextFile } from '@tauri-apps/plugin-fs';
import { openPath } from '@tauri-apps/plugin-opener';

export interface UserSample {
  /** Base ad (uzantısız) — ör. "muhasebe-2026" */
  name: string;
  xsltPath: string;
  xmlPath: string;
}

const FOLDER_NAME = 'samples';

/**
 * Örnekler klasörünün tam yolunu döndürür. Yoksa oluşturur.
 */
export async function getSamplesDir(): Promise<string> {
  const dir = await join(await appDataDir(), FOLDER_NAME);
  if (!(await exists(dir))) {
    await mkdir(dir, { recursive: true });
  }
  return dir;
}

/**
 * Klasördeki geçerli örnek çiftlerini listeler (aynı base ada sahip .xslt + .xml).
 * Base ada göre alfabetik sıralar.
 */
export async function listUserSamples(): Promise<UserSample[]> {
  const dir = await getSamplesDir();
  const entries = await readDir(dir);

  const xsltFiles = new Map<string, string>(); // baseName → fullPath
  const xmlFiles = new Map<string, string>();

  for (const entry of entries) {
    if (!entry.isFile || !entry.name) continue;
    const lower = entry.name.toLowerCase();
    const fullPath = await join(dir, entry.name);
    if (lower.endsWith('.xslt') || lower.endsWith('.xsl')) {
      const base = entry.name.replace(/\.(xslt|xsl)$/i, '');
      xsltFiles.set(base, fullPath);
    } else if (lower.endsWith('.xml')) {
      const base = entry.name.replace(/\.xml$/i, '');
      xmlFiles.set(base, fullPath);
    }
  }

  const pairs: UserSample[] = [];
  for (const [name, xsltPath] of xsltFiles) {
    const xmlPath = xmlFiles.get(name);
    if (xmlPath) pairs.push({ name, xsltPath, xmlPath });
  }
  pairs.sort((a, b) => a.name.localeCompare(b.name, 'tr'));
  return pairs;
}

/**
 * Bir örnek çiftinin içeriğini oku.
 */
export async function loadUserSample(sample: UserSample): Promise<{ xslt: string; xml: string }> {
  const [xslt, xml] = await Promise.all([
    readTextFile(sample.xsltPath),
    readTextFile(sample.xmlPath),
  ]);
  return { xslt, xml };
}

/**
 * Kullanıcının seçtiği iki dosyayı örnekler klasörüne kopyalar.
 * Base adları aynı olmalıdır (`fatura.xslt` + `fatura.xml`).
 *
 * @returns Kopyalanan örnek meta bilgisi
 * @throws Base adlar farklı ise Error
 */
export async function addSamplePair(
  xsltSourcePath: string,
  xmlSourcePath: string,
  overwrite = false,
): Promise<UserSample> {
  const xsltName = await basename(xsltSourcePath);
  const xmlName = await basename(xmlSourcePath);
  const xsltBase = xsltName.replace(/\.(xslt|xsl)$/i, '');
  const xmlBase = xmlName.replace(/\.xml$/i, '');

  if (xsltBase.toLowerCase() !== xmlBase.toLowerCase()) {
    throw new Error(
      `Dosya isimleri eşleşmeli: "${xsltBase}" ≠ "${xmlBase}". ` +
        'XSLT ve XML aynı base ada sahip olmalıdır (örn. muhasebe.xslt + muhasebe.xml).',
    );
  }

  const dir = await getSamplesDir();
  const xsltTarget = await join(dir, `${xsltBase}.xslt`);
  const xmlTarget = await join(dir, `${xsltBase}.xml`);

  if (!overwrite && ((await exists(xsltTarget)) || (await exists(xmlTarget)))) {
    throw new Error(`Bu isimde bir örnek zaten var: "${xsltBase}". Farklı ad kullanın veya mevcut örneği önce silin.`);
  }

  await copyFile(xsltSourcePath, xsltTarget);
  await copyFile(xmlSourcePath, xmlTarget);

  return { name: xsltBase, xsltPath: xsltTarget, xmlPath: xmlTarget };
}

/**
 * Bir örnek çiftini (hem XSLT hem XML) siler.
 */
export async function removeUserSample(sample: UserSample): Promise<void> {
  await Promise.all([remove(sample.xsltPath), remove(sample.xmlPath)]);
}

/**
 * Kullanıcı örnekleri klasörünü Finder/Explorer'da açar.
 */
export async function openSamplesFolder(): Promise<void> {
  const dir = await getSamplesDir();
  await openPath(dir);
}
