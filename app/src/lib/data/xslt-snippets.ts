/**
 * XSLT komut seti snippet'leri — "XSLT Komutları" kategorisi, sürüme göre
 * alt kategorilere ayrılmış: XSLT 1.0, XSLT 2.0, XSLT 3.0.
 *
 * Bunlar C# UBL-TR veri kaynağından değil, uygulama düzeyinde tanımlanır
 * (bkz. css-snippets.ts ile aynı desen). İmlecin bulunduğu yere ilgili XSLT
 * öğesini/iskeletini ekler.
 *
 * Not: Uygulamanın dönüşüm motoru Saxon-HE'dir; 1.0, 2.0 ve 3.0 komutlarının
 * hepsi çalışır (şablonun kök `version` özniteliğine uygun olanı kullanın).
 */
import type { Snippet } from './types';

function xs(
  key: string,
  version: '1.0' | '2.0' | '3.0',
  displayName: string,
  description: string,
  code: string,
): Snippet {
  return {
    key: `XSL_${version.replace('.', '')}_${key}`,
    category: 'XSLT Komutları',
    subCategory: `XSLT ${version}`,
    displayName,
    description,
    iconText: '{ }',
    xsltCode: code,
    dragDataString: `<!-- EFATURA_SNIPPET:XSL_${version.replace('.', '')}_${key} -->`,
  };
}

// ─── XSLT 1.0 — Tüm çekirdek öğeler ─────────────────────────────────────────
const v1: Snippet[] = [
  xs('STYLESHEET', '1.0', 'stylesheet', 'XSLT 1.0 kök öğe (stylesheet).',
    '<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">\n  \n</xsl:stylesheet>'),
  xs('OUTPUT', '1.0', 'output', 'Çıktı biçimi (HTML/XML/text).',
    '<xsl:output method="html" encoding="UTF-8" indent="yes"/>'),
  xs('TEMPLATE_MATCH', '1.0', 'template match', 'Bir düğüm desenine uyan şablon.',
    '<xsl:template match="/">\n  \n</xsl:template>'),
  xs('TEMPLATE_NAME', '1.0', 'template name', 'Adlandırılmış şablon (call-template ile çağrılır).',
    '<xsl:template name="ad">\n  \n</xsl:template>'),
  xs('APPLY_TEMPLATES', '1.0', 'apply-templates', 'Eşleşen şablonları uygula.',
    '<xsl:apply-templates select="."/>'),
  xs('CALL_TEMPLATE', '1.0', 'call-template', 'Adlandırılmış şablonu çağır.',
    '<xsl:call-template name="ad"/>'),
  xs('VALUE_OF', '1.0', 'value-of', 'Bir ifadenin metin değerini yaz.',
    '<xsl:value-of select="."/>'),
  xs('FOR_EACH', '1.0', 'for-each', 'Düğüm kümesi üzerinde döngü.',
    '<xsl:for-each select="node">\n  \n</xsl:for-each>'),
  xs('IF', '1.0', 'if', 'Koşullu blok.',
    '<xsl:if test="koşul">\n  \n</xsl:if>'),
  xs('CHOOSE', '1.0', 'choose', 'Çok-yollu koşul (when/otherwise).',
    '<xsl:choose>\n  <xsl:when test="koşul">\n    \n  </xsl:when>\n  <xsl:otherwise>\n    \n  </xsl:otherwise>\n</xsl:choose>'),
  xs('VARIABLE', '1.0', 'variable', 'Değişken tanımı.',
    '<xsl:variable name="ad" select="ifade"/>'),
  xs('PARAM', '1.0', 'param', 'Parametre (şablon/global).',
    '<xsl:param name="ad" select="varsayılan"/>'),
  xs('WITH_PARAM', '1.0', 'with-param', 'Çağrıya parametre geçir.',
    '<xsl:with-param name="ad" select="değer"/>'),
  xs('ATTRIBUTE', '1.0', 'attribute', 'Öğeye öznitelik ekle.',
    '<xsl:attribute name="class"><xsl:value-of select="."/></xsl:attribute>'),
  xs('ELEMENT', '1.0', 'element', 'Dinamik öğe oluştur.',
    '<xsl:element name="div">\n  \n</xsl:element>'),
  xs('TEXT', '1.0', 'text', 'Değişmez metin (boşluk korumalı).',
    '<xsl:text> </xsl:text>'),
  xs('COPY', '1.0', 'copy', 'Geçerli düğümü (sığ) kopyala.',
    '<xsl:copy>\n  <xsl:apply-templates select="@*|node()"/>\n</xsl:copy>'),
  xs('COPY_OF', '1.0', 'copy-of', 'Derin kopya (alt ağaç dahil).',
    '<xsl:copy-of select="."/>'),
  xs('SORT', '1.0', 'sort', 'Sıralama anahtarı (for-each/apply-templates içinde).',
    '<xsl:sort select="." order="ascending" data-type="text"/>'),
  xs('NUMBER', '1.0', 'number', 'Otomatik/biçimli numaralandırma.',
    '<xsl:number value="position()" format="1"/>'),
  xs('KEY', '1.0', 'key', 'Anahtar tanımı (Muenchian gruplama için).',
    '<xsl:key name="anahtar" match="öğe" use="alan"/>'),
  xs('DECIMAL_FORMAT', '1.0', 'decimal-format', 'Sayı biçimi (TR: virgül/nokta).',
    '<xsl:decimal-format name="tr" decimal-separator="," grouping-separator="."/>'),
  xs('ATTRIBUTE_SET', '1.0', 'attribute-set', 'Yeniden kullanılabilir öznitelik kümesi.',
    '<xsl:attribute-set name="hucre">\n  <xsl:attribute name="style">padding:4px;border:1px solid #ccc</xsl:attribute>\n</xsl:attribute-set>'),
  xs('IMPORT', '1.0', 'import', 'Başka bir stylesheet içe aktar (düşük öncelik).',
    '<xsl:import href="ortak.xslt"/>'),
  xs('INCLUDE', '1.0', 'include', 'Başka bir stylesheet dahil et (aynı öncelik).',
    '<xsl:include href="ortak.xslt"/>'),
  xs('COMMENT', '1.0', 'comment', 'Çıktıya XML yorumu yaz.',
    '<xsl:comment>yorum</xsl:comment>'),
  xs('PI', '1.0', 'processing-instruction', 'İşleme yönergesi oluştur.',
    '<xsl:processing-instruction name="xml-stylesheet">href="stil.css"</xsl:processing-instruction>'),
  xs('MESSAGE', '1.0', 'message', 'Hata ayıklama mesajı (isteğe bağlı sonlandır).',
    '<xsl:message terminate="no">mesaj</xsl:message>'),
  xs('STRIP_SPACE', '1.0', 'strip-space', 'Boşluk-only metin düğümlerini kaldır.',
    '<xsl:strip-space elements="*"/>'),
];

// ─── XSLT 2.0 — Ek öğeler ───────────────────────────────────────────────────
const v2: Snippet[] = [
  xs('STYLESHEET', '2.0', 'stylesheet 2.0', 'XSLT 2.0 kök öğe (xs ad alanıyla).',
    '<xsl:stylesheet version="2.0"\n  xmlns:xsl="http://www.w3.org/1999/XSL/Transform"\n  xmlns:xs="http://www.w3.org/2001/XMLSchema"\n  exclude-result-prefixes="xs">\n  \n</xsl:stylesheet>'),
  xs('FUNCTION', '2.0', 'function', 'Kullanıcı tanımlı XPath fonksiyonu.',
    '<xsl:function name="my:adi" as="xs:string">\n  <xsl:param name="giris" as="xs:string"/>\n  <xsl:sequence select="$giris"/>\n</xsl:function>'),
  xs('FOR_EACH_GROUP', '2.0', 'for-each-group', 'Gruplama (group-by/adjacent).',
    '<xsl:for-each-group select="kalem" group-by="kategori">\n  <xsl:value-of select="current-grouping-key()"/> (<xsl:value-of select="count(current-group())"/>)\n</xsl:for-each-group>'),
  xs('SEQUENCE', '2.0', 'sequence', 'Değer/düğüm dizisi döndür (function içinde).',
    '<xsl:sequence select="ifade"/>'),
  xs('ANALYZE_STRING', '2.0', 'analyze-string', 'Regex ile metin ayrıştır.',
    '<xsl:analyze-string select="metin" regex="(\\d+)">\n  <xsl:matching-substring>\n    <b><xsl:value-of select="regex-group(1)"/></b>\n  </xsl:matching-substring>\n  <xsl:non-matching-substring>\n    <xsl:value-of select="."/>\n  </xsl:non-matching-substring>\n</xsl:analyze-string>'),
  xs('VALUE_OF_SEP', '2.0', 'value-of (separator)', 'Diziyi ayırıcıyla birleştir.',
    '<xsl:value-of select="kalem" separator=", "/>'),
  xs('CHARACTER_MAP', '2.0', 'character-map', 'Karakter eşleme tablosu (output ile kullanılır).',
    '<xsl:character-map name="harita">\n  <xsl:output-character character="&#160;" string="&amp;nbsp;"/>\n</xsl:character-map>'),
  xs('RESULT_DOCUMENT', '2.0', 'result-document', 'İkincil çıktı belgesi.',
    '<xsl:result-document href="ek.html" method="html">\n  \n</xsl:result-document>'),
  xs('NAMESPACE', '2.0', 'namespace', 'Ad alanı düğümü oluştur.',
    '<xsl:namespace name="pre" select="\'urn:...\'"/>'),
  xs('PERFORM_SORT', '2.0', 'perform-sort', 'Bir diziyi sıralayıp döndür.',
    '<xsl:perform-sort select="kalem">\n  <xsl:sort select="ad"/>\n</xsl:perform-sort>'),
  xs('NEXT_MATCH', '2.0', 'next-match', 'Bir sonraki eşleşen şablona devret.',
    '<xsl:next-match/>'),
  xs('FORMAT_DATE', '2.0', 'format-date()', 'Tarih biçimlendirme (TR).',
    '<xsl:value-of select="format-date(xs:date(cbc:IssueDate), \'[D01].[M01].[Y0001]\')"/>'),
  xs('FORMAT_DATETIME', '2.0', 'format-dateTime()', 'Tarih-saat biçimlendirme.',
    '<xsl:value-of select="format-dateTime(current-dateTime(), \'[D01].[M01].[Y0001] [H01]:[m01]\')"/>'),
  xs('TOKENIZE', '2.0', 'tokenize()', 'Metni ayırıcıya göre böl.',
    '<xsl:value-of select="count(tokenize(etiketler, \',\'))"/>'),
  xs('REPLACE', '2.0', 'replace()', 'Regex ile değiştir.',
    '<xsl:value-of select="replace(vkn, \'(\\d{3})(\\d{3})(\\d{4})\', \'$1-$2-$3\')"/>'),
];

// ─── XSLT 3.0 — Ek öğeler ───────────────────────────────────────────────────
const v3: Snippet[] = [
  xs('STYLESHEET', '3.0', 'stylesheet 3.0', 'XSLT 3.0 kök öğe.',
    '<xsl:stylesheet version="3.0"\n  xmlns:xsl="http://www.w3.org/1999/XSL/Transform"\n  xmlns:xs="http://www.w3.org/2001/XMLSchema"\n  exclude-result-prefixes="xs">\n  \n</xsl:stylesheet>'),
  xs('MODE', '3.0', 'mode', 'Mod bildirimi (streaming/varsayılan şablon davranışı).',
    '<xsl:mode name="varsayilan" on-no-match="shallow-copy"/>'),
  xs('ITERATE', '3.0', 'iterate', 'Akümülatörlü sıralı yineleme.',
    '<xsl:iterate select="kalem">\n  <xsl:param name="toplam" select="0"/>\n  <xsl:on-completion>\n    <xsl:value-of select="$toplam"/>\n  </xsl:on-completion>\n  <xsl:next-iteration>\n    <xsl:with-param name="toplam" select="$toplam + tutar"/>\n  </xsl:next-iteration>\n</xsl:iterate>'),
  xs('TRY_CATCH', '3.0', 'try / catch', 'Hata yakalama.',
    '<xsl:try>\n  \n  <xsl:catch>\n    <xsl:value-of select="$err:description"/>\n  </xsl:catch>\n</xsl:try>'),
  xs('MERGE', '3.0', 'merge', 'Birden çok girdiyi anahtara göre birleştir.',
    '<xsl:merge>\n  <xsl:merge-source select="kalem">\n    <xsl:merge-key select="kod"/>\n  </xsl:merge-source>\n  <xsl:merge-action>\n    \n  </xsl:merge-action>\n</xsl:merge>'),
  xs('WHERE_POPULATED', '3.0', 'where-populated', 'Boş içerik üretilirse öğeyi atla.',
    '<xsl:where-populated>\n  <td><xsl:value-of select="not"/></td>\n</xsl:where-populated>'),
  xs('ON_EMPTY', '3.0', 'on-empty', 'İçerik boşsa alternatif üret.',
    '<xsl:on-empty select="\'—\'"/>'),
  xs('ASSERT', '3.0', 'assert', 'Doğrulama (test başarısızsa hata).',
    '<xsl:assert test="koşul" select="\'hata mesajı\'"/>'),
  xs('MAP', '3.0', 'map / map-entry', 'Anahtar-değer haritası.',
    '<xsl:variable name="harita" as="map(xs:string, xs:string)">\n  <xsl:map>\n    <xsl:map-entry key="\'a\'" select="\'1\'"/>\n  </xsl:map>\n</xsl:variable>'),
  xs('EVALUATE', '3.0', 'evaluate', 'Dinamik XPath değerlendir.',
    '<xsl:evaluate xpath="$ifade" context-item="."/>'),
  xs('SOURCE_DOCUMENT', '3.0', 'source-document', 'Harici belgeyi (akış dahil) işle.',
    '<xsl:source-document href="veri.xml">\n  <xsl:apply-templates/>\n</xsl:source-document>'),
  xs('PACKAGE', '3.0', 'package', 'Paket (modüler XSLT).',
    '<xsl:package name="urn:paket" version="1.0"\n  xmlns:xsl="http://www.w3.org/1999/XSL/Transform">\n  \n</xsl:package>'),
];

export const xsltSnippets: Snippet[] = [...v1, ...v2, ...v3];
