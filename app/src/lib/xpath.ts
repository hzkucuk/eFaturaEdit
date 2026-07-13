/**
 * XPath test konsolu — yüklü XML verisine karşı XPath ifadesi çalıştırır.
 *
 * <b>Neden var:</b> Şablon yazarken en çok zaman kaybettiren şey, bir alanın
 * önizlemede boş gelmesi ve SEBEBİNİN görünmemesi: XPath mi yanlış, namespace
 * mi kaçtı, veri mi yok? Tek yol "XSLT'yi kurcala → dönüştür → bak" döngüsüydü.
 * Burada ifade doğrudan veriye karşı çalışır; kaç düğüm eşleşti, değerleri ne —
 * anında görünür.
 *
 * <b>Neden sidecar'a dokunmuyoruz:</b> Sidecar'ın tel protokolü
 * `[len][XSLT][len][XML]` — yeni bir "XPath değerlendir" modu eklemek protokolü
 * değiştirip GraalVM native-image'ı 5 platformda yeniden derlemek demekti.
 * Bunun yerine XPath'i minik bir XSLT sarmalayıcısına gömüp MEVCUT dönüşüm
 * hattından geçiriyoruz: protokol aynı, Saxon'un XPath 2.0/3.0'ı bedavaya geliyor.
 *
 * <b>Geri düşüş GÖRÜNÜR:</b> Saxon yoksa `transformXml` sessizce tarayıcının
 * XSLT 1.0'ına düşer — bizim 2.0 sarmalayıcımız orada hata vermeden yanlış
 * sonuç üretebilirdi. O yüzden Saxon'u DOĞRUDAN çağırıyoruz; motor yoksa
 * tarayıcının XPath 1.0'ına düşüp bunu sonuçta `engine: 'browser'` olarak
 * açıkça bildiriyoruz (arayüz rozet basar).
 */
import { invoke } from '@tauri-apps/api/core';
import { validateXml, engineStatus, ENGINE_UNAVAILABLE } from '$lib/xslt';

/** Tek seferde gösterilecek en fazla sonuç (500 kalemli fatura arayüzü kilitlemesin). */
export const MAX_ITEMS = 100;

export type XPathItemKind = 'element' | 'attribute' | 'text' | 'node' | 'atomic';

export interface XPathItem {
  kind: XPathItemKind;
  /** Düğüm adı (`cbc:PayableAmount`); atomik değerlerde boş. */
  name: string;
  value: string;
}

export interface XPathOutcome {
  /** Eşleşen toplam öğe sayısı (MAX_ITEMS ile kırpılmadan ÖNCE). */
  count: number;
  items: XPathItem[];
  /** `count > items.length` — liste kırpıldı. */
  truncated: boolean;
  /** Hangi motor değerlendirdi: Saxon (2.0/3.0) mi, tarayıcı (yalnızca 1.0) mı. */
  engine: 'saxon' | 'browser';
  /** Değerlendirme süresi (ms). */
  ms: number;
}

/** XPath ifadesi hatalıysa (sözdizimi, bilinmeyen önek, tip hatası). */
export class XPathError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'XPathError';
  }
}

/** XML öznitelik değeri için kaçış — kullanıcının ifadesi `select="..."` içine girer. */
function escapeAttr(text: string): string {
  return text
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

interface Namespaces {
  /** önek → URI (varsayılan namespace hariç). */
  prefixed: Record<string, string>;
  /** Kök öğenin varsayılan namespace'i (`xmlns="..."`) — yoksa boş. */
  default: string;
}

/**
 * Belgenin KÖK öğesindeki namespace bildirimlerini toplar.
 *
 * Neden kökten: kullanıcı XPath'ini kendi belgesinin önekleriyle yazar
 * (`cac:`, `cbc:`, `ext:`). Önekleri biz sabit listeden uydurursak, farklı önek
 * kullanan bir belgede ifade sessizce hiçbir şey eşleştirmez.
 */
function collectNamespaces(doc: Document): Namespaces {
  const root = doc.documentElement;
  const prefixed: Record<string, string> = {};
  let def = '';

  for (const attr of Array.from(root.attributes)) {
    if (attr.name === 'xmlns') def = attr.value;
    else if (attr.name.startsWith('xmlns:')) prefixed[attr.name.slice(6)] = attr.value;
  }
  return { prefixed, default: def };
}

/** XPath'i çalıştırıp sonucu `<xpath-result>` olarak döken sarmalayıcı XSLT. */
function buildWrapper(expr: string, ns: Namespaces): string {
  const decls = Object.entries(ns.prefixed)
    .map(([p, uri]) => `xmlns:${p}="${escapeAttr(uri)}"`)
    .join('\n  ');

  // xpath-default-namespace: UBL faturalarının kökü varsayılan namespace'tedir
  // (xmlns="urn:...Invoice-2"). Bu olmadan `/Invoice/cbc:ID` gibi doğal görünen
  // bir ifade HİÇBİR ŞEY eşleştirmez ve kullanıcı sebebini anlayamaz.
  const defaultNs = ns.default ? ` xpath-default-namespace="${escapeAttr(ns.default)}"` : '';
  const e = escapeAttr(expr);

  return `<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="2.0"
  xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
  ${decls}${defaultNs}>
  <xsl:output method="xml" indent="no" omit-xml-declaration="yes"/>
  <xsl:template match="/">
    <xsl:variable name="r" select="${e}"/>
    <xpath-result count="{count($r)}">
      <xsl:for-each select="subsequence($r, 1, ${MAX_ITEMS})">
        <item>
          <xsl:attribute name="kind">
            <xsl:choose>
              <xsl:when test=". instance of element()">element</xsl:when>
              <xsl:when test=". instance of attribute()">attribute</xsl:when>
              <xsl:when test=". instance of text()">text</xsl:when>
              <xsl:when test=". instance of node()">node</xsl:when>
              <xsl:otherwise>atomic</xsl:otherwise>
            </xsl:choose>
          </xsl:attribute>
          <xsl:attribute name="name">
            <xsl:if test=". instance of node()"><xsl:value-of select="name()"/></xsl:if>
          </xsl:attribute>
          <xsl:value-of select="."/>
        </item>
      </xsl:for-each>
    </xpath-result>
  </xsl:template>
</xsl:stylesheet>`;
}

/** Motor kaynaklı hata mı (sidecar yok/ölü), yoksa kullanıcının ifadesi mi hatalı? */
function isEngineFailure(message: string): boolean {
  return (
    message.includes(ENGINE_UNAVAILABLE) ||
    /bulunamadı|başlatılamadı|not found|sidecar/i.test(message)
  );
}

/** Saxon çıktısındaki `<xpath-result>` belgesini ayrıştır. */
function parseOutcome(xml: string, ms: number): XPathOutcome {
  // Sidecar çıktı yöntemini HTML'e zorluyor ve başa `<!DOCTYPE html>` ekliyor
  // (ölçüldü). XML ayrıştırıcısı bunu şimdilik kabul ediyor ama buna bel
  // bağlamıyoruz — sidecar'ın çıktısı değişirse sessizce bozulmasın.
  const cleaned = xml.replace(/^\s*<!DOCTYPE[^>]*>\s*/i, '').trim();

  const doc = new DOMParser().parseFromString(cleaned, 'application/xml');
  const root = doc.documentElement;
  if (!root || root.nodeName !== 'xpath-result' || root.querySelector('parsererror')) {
    throw new XPathError(`Beklenmeyen sonuç biçimi: ${cleaned.slice(0, 200)}`);
  }

  const count = parseInt(root.getAttribute('count') ?? '0', 10);
  const items: XPathItem[] = Array.from(root.children).map((el) => ({
    kind: (el.getAttribute('kind') ?? 'atomic') as XPathItemKind,
    name: el.getAttribute('name') ?? '',
    value: el.textContent ?? '',
  }));

  return { count, items, truncated: count > items.length, engine: 'saxon', ms };
}

/**
 * Tarayıcının yerleşik XPath'i — YALNIZCA 1.0 (yedek yol).
 *
 * Saxon yokken kullanılır. `subsequence`, `instance of`, `format-dateTime` gibi
 * 2.0 fonksiyonları BURADA ÇALIŞMAZ; ayrıca XPath 1.0'da varsayılan namespace
 * kavramı olmadığından öneksiz adlar eşleşmez. Bu yüzden sonuçta motor açıkça
 * bildirilir — kullanıcı hangi motorun konuştuğunu bilmelidir.
 */
function evaluateInBrowser(xmlText: string, expr: string, ms0: number): XPathOutcome {
  const doc = new DOMParser().parseFromString(xmlText, 'application/xml');
  const ns = collectNamespaces(doc);
  const resolver = (prefix: string | null): string | null =>
    (prefix && ns.prefixed[prefix]) || null;

  let result: globalThis.XPathResult;
  try {
    result = doc.evaluate(expr, doc, resolver, 0 /* ANY_TYPE */, null);
  } catch (err) {
    throw new XPathError((err as Error).message ?? String(err));
  }

  const ms = Math.round(performance.now() - ms0);
  const atomic = (value: string): XPathOutcome => ({
    count: 1,
    items: [{ kind: 'atomic', name: '', value }],
    truncated: false,
    engine: 'browser',
    ms,
  });

  switch (result.resultType) {
    case 1: // NUMBER
      return atomic(String(result.numberValue));
    case 2: // STRING
      return atomic(result.stringValue);
    case 3: // BOOLEAN
      return atomic(String(result.booleanValue));
    default: {
      const items: XPathItem[] = [];
      let count = 0;
      for (let n = result.iterateNext(); n; n = result.iterateNext()) {
        count++;
        if (items.length < MAX_ITEMS) {
          const kind: XPathItemKind =
            n.nodeType === 1 ? 'element' : n.nodeType === 2 ? 'attribute' : n.nodeType === 3 ? 'text' : 'node';
          items.push({ kind, name: n.nodeName, value: n.textContent ?? '' });
        }
      }
      return { count, items, truncated: count > items.length, engine: 'browser', ms };
    }
  }
}

/**
 * XPath ifadesini yüklü XML'e karşı değerlendirir.
 *
 * @throws {XsltError} XML iyi-biçimli değilse (satır/sütun ile).
 * @throws {XPathError} İfade hatalıysa — Saxon'un mesajı olduğu gibi iletilir;
 *         "bilinmeyen hata" demeyiz, gerçek sebep kullanıcıya gösterilir.
 */
export async function evaluateXPath(xmlText: string, expr: string): Promise<XPathOutcome> {
  const trimmed = expr.trim();
  if (!trimmed) throw new XPathError('Boş ifade.');

  // Bozuk XML Saxon'a gitmesin: Xerces hatayı BİLDİRİRKEN çöküyor ve kullanıcı
  // sebeple ilgisiz bir Java hatası görüyor (bkz. xslt.ts).
  validateXml(xmlText, 'xml');

  const t0 = performance.now();
  const doc = new DOMParser().parseFromString(xmlText, 'application/xml');
  const ns = collectNamespaces(doc);

  if (!engineStatus.saxon) return evaluateInBrowser(xmlText, trimmed, t0);

  try {
    const out = await invoke<string>('xslt_transform', {
      xslt: buildWrapper(trimmed, ns),
      xml: xmlText.replace(/^﻿/, '').trimStart(),
    });
    return parseOutcome(out, Math.round(performance.now() - t0));
  } catch (err) {
    const message = typeof err === 'string' ? err : ((err as Error)?.message ?? String(err));

    // MOTOR mu bozuk, İFADE mi? Bu ayrım kritik: ifade hatasında geri düşmek,
    // kullanıcının hatasını gizleyip başka bir motorun cevabını sunmak olurdu.
    if (isEngineFailure(message)) {
      engineStatus.saxon = false;
      engineStatus.reason = message.replace(`${ENGINE_UNAVAILABLE}: `, '').trim();
      return evaluateInBrowser(xmlText, trimmed, t0);
    }
    throw new XPathError(message);
  }
}
