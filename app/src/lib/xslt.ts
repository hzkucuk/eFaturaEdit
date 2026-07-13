/**
 * XSLT dönüşüm sarmalayıcısı + XML/XSLT syntax doğrulama + hata satır ayıklama.
 *
 * <b>Motor seçimi:</b> Öncelikli olarak Saxon-HE tabanlı yerel sidecar kullanılır
 * (`xslt_transform` Tauri komutu) — tam XSLT 1.0/2.0/3.0 desteği. GİB ve müşteri
 * şablonları `version="2.0"` bildirip `format-dateTime`, `upper-case`, `tokenize`,
 * `for-each-group`, `xsl:function` gibi 2.0+ özellikleri kullanabilir; tarayıcının
 * yerleşik `XSLTProcessor`'ı bunları SESSİZCE bozar (yalnızca XSLT 1.0 destekler).
 *
 * Sidecar erişilemezse (ör. Tauri dışı ortam) tarayıcı işlemcisine düşülür —
 * bu durumda yalnızca XSLT 1.0 çalışır.
 */
import { invoke } from '@tauri-apps/api/core';

export interface XmlError {
  message: string;
  line: number;
  column: number;
}

/** XSLT dönüşüm hatası — line/column bilgisi opsiyonel. */
export class XsltError extends Error {
  constructor(
    message: string,
    public readonly line?: number,
    public readonly column?: number,
    public readonly source?: 'xml' | 'xslt' | 'transform',
  ) {
    super(message);
    this.name = 'XsltError';
  }
}

/**
 * Ayrıştırıcıya verilmeden önceki normalize: BOM + `<?xml` bildiriminden ÖNCEKİ
 * boşluk/yeni satır temizlenir.
 *
 * NEDEN: Bildirimden önce tek bir yeni satır bile Xerces (Saxon) için ÖLÜMCÜL
 * hatadır — ama WebKit'in `DOMParser`'ı bunu hoş görür. Yani tarayıcı "belge
 * sağlam" derken Saxon çöker; hata da (resource bundle eksikliği yüzünden)
 * anlamsız bir mesaja dönüşürdü. AI'ın kod bloğundan çıkarılan "tam dosya"
 * önerileri tam olarak böyle başlıyordu. Ölçüldü: sidecar exit=1.
 *
 * Dönen `lineOffset`, kırpılan satır sayısıdır — Saxon'un bildirdiği satır
 * numarasına geri eklenir, yoksa imleç yanlış satıra atlar.
 */
function normalizeDocument(text: string): { text: string; lineOffset: number } {
  const noBom = text.replace(/^﻿/, '');
  const lead = noBom.match(/^\s+/)?.[0] ?? '';
  if (!lead) return { text: noBom, lineOffset: 0 };
  return { text: noBom.slice(lead.length), lineOffset: (lead.match(/\n/g) ?? []).length };
}

/**
 * XML/XSLT metnini ayrıştır, syntax hatası varsa fırlat.
 *
 * Baştaki BOM/boşluk `normalizeDocument` ile temizlenir; aksi halde BOM'lu
 * geçerli belgeler hatalı görünür (DOMParser BOM'u "prolog öncesi içerik" sayar).
 */
export function validateXml(text: string, source: 'xml' | 'xslt'): void {
  const { text: clean, lineOffset } = normalizeDocument(text);
  const doc = new DOMParser().parseFromString(clean, 'application/xml');
  const err = extractParseError(doc);
  if (err) {
    throw new XsltError(err.message, err.line ? err.line + lineOffset : err.line, err.column, source);
  }
}

/** Belge iyi-biçimli mi? (Fırlatmaz — "uygulamadan önce kontrol" için.) */
export function isWellFormed(text: string): boolean {
  try {
    validateXml(text, 'xml');
    return true;
  } catch {
    return false;
  }
}

/** Sidecar bir kez bulunamazsa tekrar tekrar denemeyelim. */
let saxonAvailable = true;

/**
 * Motor durumu — arayüz bunu okuyup kullanıcıyı uyarır.
 *
 * Sessiz geri düşüş TEHLİKELİDİR: tarayıcı işlemcisi XSLT 2.0 komutlarını hata
 * vermeden yok sayar; kullanıcı şablonunun çalıştığını sanır, oysa çıktı yanlıştır.
 * O yüzden düşüldüğünde bunu görünür şekilde söylüyoruz.
 */
export const engineStatus = {
  /** Saxon (XSLT 2.0/3.0) kullanılıyor mu? */
  saxon: true,
  /** Düşüldüyse sebebi (kullanıcıya gösterilir). */
  reason: '',
};

/** Rust tarafının "motorun kendisi çalışmıyor" işareti (bkz. xslt.rs). */
const ENGINE_UNAVAILABLE = 'XSLT_ENGINE_UNAVAILABLE';

/** Saxon hata metninden satır/sütun ayıkla ("... on line 59 column 40"). */
function parseSaxonPosition(message: string): { line?: number; column?: number } {
  const m = message.match(/on line (\d+)(?:\s+column (\d+))?/i);
  if (!m) return {};
  return { line: parseInt(m[1], 10), column: m[2] ? parseInt(m[2], 10) : undefined };
}

/**
 * XML ve XSLT metinlerini dönüştürüp HTML çıktı döndürür.
 *
 * Önce Saxon-HE sidecar'ı (XSLT 1.0/2.0/3.0) denenir; sidecar yoksa tarayıcının
 * XSLT 1.0 işlemcisine düşülür.
 */
export async function transformXml(xmlText: string, xsltText: string): Promise<string> {
  // Saxon'a göndermeden ÖNCE iyi-biçimlilik denetimi + normalize. Sebep: bozuk
  // belge Saxon'a ulaşınca Xerces hatayı bildirmek için bir resource bundle
  // yüklemeye çalışır; native-image'da bu paketler yoksa parser hatayı
  // BİLDİRİRKEN çöker ve kullanıcı sebeple ilgisiz bir mesaj görür.
  // Buradaki denetim anında çalışır, satır/sütun verir → imleç hatalı satıra gider.
  validateXml(xmlText, 'xml');
  validateXml(xsltText, 'xslt');

  // `<?xml` öncesi boşluk WebKit'e göre sorunsuz ama Xerces'e göre ölümcül —
  // kırpılmış metni gönder (kullanıcının dosyası değişmez, yalnızca istek).
  const xml = normalizeDocument(xmlText);
  const xslt = normalizeDocument(xsltText);

  if (saxonAvailable) {
    try {
      return await invoke<string>('xslt_transform', { xslt: xslt.text, xml: xml.text });
    } catch (err) {
      const message = typeof err === 'string' ? err : ((err as Error)?.message ?? String(err));

      // MOTORUN KENDİSİ mi çalışmıyor, yoksa ŞABLON mu hatalı?
      //
      // Bu ayrım kritik: şablon hatasında geri düşmek, kullanıcının hatasını
      // gizleyip sessizce yanlış çıktı üretmek olurdu. Rust tarafı motor
      // kaynaklı hataları ENGINE_UNAVAILABLE ile işaretler (bulunamadı,
      // başlatılamadı, veri yazılamadan öldü, tek kelime etmeden çıktı...).
      // Eski sürümde bu ayrım metin eşleştirmeyle yapılıyordu ve Windows'taki
      // "Boru sonlandı (os error 109)" hiçbir kalıba uymadığından uygulama
      // geri düşmek yerine sert hata veriyordu — kullanıcı hiçbir şey yapamıyordu.
      if (message.includes(ENGINE_UNAVAILABLE) || /bulunamadı|başlatılamadı|not found|sidecar/i.test(message)) {
        saxonAvailable = false;
        engineStatus.saxon = false;
        engineStatus.reason = message.replace(`${ENGINE_UNAVAILABLE}: `, '').trim();
        console.warn('Saxon (XSLT 2.0/3.0) motoru kullanılamıyor — tarayıcı XSLT 1.0 işlemcisine düşülüyor.', message);
      } else {
        const { line, column } = parseSaxonPosition(message);
        // Saxon, kırpılmış metne göre satır bildirir; kırpılan satırları geri ekle
        // ki imleç kullanıcının GERÇEK dosyasında doğru satıra gitsin.
        throw new XsltError(message, line ? line + xslt.lineOffset : line, column, 'transform');
      }
    }
  }

  return transformWithBrowser(xmlText, xsltText);
}

/** Tarayıcının yerleşik XSLTProcessor'ı — YALNIZCA XSLT 1.0 (yedek yol). */
async function transformWithBrowser(xmlText: string, xsltText: string): Promise<string> {
  if (typeof XSLTProcessor === 'undefined') {
    throw new XsltError("Tarayıcı XSLTProcessor API'sini desteklemiyor.");
  }

  const parser = new DOMParser();

  const xmlDoc = parser.parseFromString(xmlText, 'application/xml');
  const xmlErr = extractParseError(xmlDoc);
  if (xmlErr) throw new XsltError(xmlErr.message, xmlErr.line, xmlErr.column, 'xml');

  const xsltDoc = parser.parseFromString(xsltText, 'application/xml');
  const xsltErr = extractParseError(xsltDoc);
  if (xsltErr) throw new XsltError(xsltErr.message, xsltErr.line, xsltErr.column, 'xslt');

  const processor = new XSLTProcessor();
  try {
    processor.importStylesheet(xsltDoc);
  } catch (err) {
    throw new XsltError(
      `XSLT şablonu yüklenemedi: ${(err as Error).message ?? String(err)}`,
      undefined,
      undefined,
      'xslt',
    );
  }

  let resultDoc: Document;
  try {
    // transformToDocument (transformToFragment yerine): tam bir Document
    // döndürür. Böylece <html>/<head>/<body> yapısını ve DOCTYPE'ı
    // XMLSerializer ile doğru şekilde yeniden üretebiliriz.
    resultDoc = processor.transformToDocument(xmlDoc);
  } catch (err) {
    throw new XsltError(
      `Dönüşüm hatası: ${(err as Error).message ?? String(err)}`,
      undefined,
      undefined,
      'transform',
    );
  }

  return serializeAsHtmlDocument(resultDoc);
}

/**
 * XSLTProcessor çıktısını (genelde DOCTYPE'sız bir HTML dokümanı) tam ve
 * standart bir HTML5 dokümanına dönüştürür.
 *
 * <b>Neden gerekli:</b> `XSLTProcessor.transformToFragment/Document()`
 * DOCTYPE bilgisini korumaz (yalnızca DOM ağacı döner). DOCTYPE'sız HTML,
 * tarayıcılar tarafından "Quirks Mode" ile render edilir — bu modda tablo
 * hücre kenarlıkları, genişlik hesaplamaları ve box model farklı çalışır.
 * Bu fonksiyon `<!DOCTYPE html>` + `<meta charset="utf-8">` ekleyerek
 * Standards Mode'u garanti eder; hem uygulama içi önizleme (iframe) hem de
 * dışa aktarılan HTML dosyası (harici tarayıcıda açılan) aynı şekilde
 * render edilir.
 */
function serializeAsHtmlDocument(doc: Document): string {
  // <head> yoksa oluştur, meta charset ekle (yoksa)
  let head = doc.querySelector('head');
  if (!head) {
    head = doc.createElement('head');
    doc.documentElement?.insertBefore(head, doc.documentElement.firstChild);
  }
  if (!head.querySelector('meta[charset]')) {
    const meta = doc.createElement('meta');
    meta.setAttribute('charset', 'utf-8');
    head.insertBefore(meta, head.firstChild);
  }

  // HTML documentElement ise outerHTML kullan (standart HTML5 serileştirme —
  // boş elementleri "<td></td>" olarak yazar, self-closing "<td/>" değil).
  // XML documentElement ise (nadiren) XMLSerializer'a düş.
  const root = doc.documentElement;
  const html =
    root && 'outerHTML' in root
      ? (root as unknown as HTMLElement).outerHTML
      : new XMLSerializer().serializeToString(doc);

  return '<!DOCTYPE html>\n' + html;
}

/**
 * `DOMParser` <parsererror> düğümünden hata bilgisi çıkar.
 * Chrome/WebKit farklı formatlar döndürür — regex ile line/col yakalanır.
 */
function extractParseError(doc: Document): XmlError | null {
  const node = doc.querySelector('parsererror');
  if (!node) return null;

  const text = node.textContent ?? 'ayrıştırma hatası';

  // WebKit format: "error on line 42 at column 12: message" veya
  // "Opening and ending tag mismatch: ... line 42, column 12"
  // Chrome format: "This page contains ... error occurred while parsing ...\nSystem ID: ...\nLine Number 42, Column 12"
  const patterns = [
    /line\s*(\d+)\s*(?:,|\s+)?\s*(?:at\s+)?column\s*(\d+)/i,
    /Line\s*Number\s*(\d+)\s*,\s*Column\s*(\d+)/i,
    /line\s+(\d+):(\d+)/i,
    /^(\d+):(\d+)/,
  ];

  let line = 1;
  let column = 1;
  for (const re of patterns) {
    const m = text.match(re);
    if (m) {
      line = parseInt(m[1], 10) || 1;
      column = parseInt(m[2], 10) || 1;
      break;
    }
  }

  // Görünen mesajı biraz temizle
  const clean = text.replace(/^This page contains .*?parsing.*?$/im, '').trim();
  return { message: clean || 'XML syntax hatası', line, column };
}
