/**
 * XPath fonksiyon snippet'leri — "XPath Fonksiyonları" kategorisi.
 *
 * Fatura/irsaliye şablonlarında sık kullanılan XPath 1.0 ve XPath 2.0/3.0
 * fonksiyonları. Genelde bir `xsl:value-of select="..."` veya öznitelik değeri
 * içine yerleştirilir. Uygulamanın motoru Saxon-HE olduğundan 2.0/3.0
 * fonksiyonları da çalışır.
 */
import type { Snippet } from './types';

function fn(
  key: string,
  version: '1.0' | '2.0/3.0',
  displayName: string,
  description: string,
  code: string,
): Snippet {
  const vkey = version === '1.0' ? '10' : '2030';
  return {
    key: `XP_${vkey}_${key}`,
    category: 'XPath Fonksiyonları',
    subCategory: version === '1.0' ? 'XPath 1.0' : 'XPath 2.0 / 3.0',
    displayName,
    description,
    iconText: 'ƒ',
    xsltCode: code,
    dragDataString: `<!-- EFATURA_SNIPPET:XP_${vkey}_${key} -->`,
  };
}

// ─── XPath 1.0 — her XSLT sürümünde çalışır ─────────────────────────────────
const v1: Snippet[] = [
  fn('SUM', '1.0', 'sum()', 'Sayısal bir düğüm kümesini topla (ör. satır tutarları).',
    'sum(cac:InvoiceLine/cbc:LineExtensionAmount)'),
  fn('COUNT', '1.0', 'count()', 'Düğüm sayısını al (ör. kalem adedi).',
    'count(cac:InvoiceLine)'),
  fn('FORMAT_NUMBER', '1.0', 'format-number()', 'Sayıyı biçimlendir (TR: binlik nokta, kuruş virgül).',
    "format-number(cbc:PayableAmount, '#.##0,00', 'tr')"),
  fn('SUBSTRING', '1.0', 'substring()', 'Alt dize al (ör. ISO tarihten gün/ay/yıl).',
    'substring(cbc:IssueDate, 9, 2)'),
  fn('CONCAT', '1.0', 'concat()', 'Dizeleri birleştir (ör. gün-ay-yıl).',
    "concat(substring(cbc:IssueDate,9,2), '.', substring(cbc:IssueDate,6,2), '.', substring(cbc:IssueDate,1,4))"),
  fn('TRANSLATE', '1.0', 'translate()', 'Karakter çevir (basit büyük harf: türkçe hariç).',
    "translate(., 'abcçdefgğh" + "ıijklmnoöprsştuüvyz', 'ABCÇDEFGĞH" + "IİJKLMNOÖPRSŞTUÜVYZ')"),
  fn('NORMALIZE_SPACE', '1.0', 'normalize-space()', 'Baş/son boşlukları at, iç boşlukları teke indir.',
    'normalize-space(cbc:Note)'),
  fn('STRING_LENGTH', '1.0', 'string-length()', 'Dize uzunluğu.',
    'string-length(cbc:ID)'),
  fn('CONTAINS', '1.0', 'contains()', 'Alt dize içeriyor mu? (koşullarda).',
    "contains(cbc:ProfileID, 'IHRACAT')"),
  fn('STARTS_WITH', '1.0', 'starts-with()', 'İle başlıyor mu?',
    "starts-with(cbc:ID, 'EFS')"),
  fn('POSITION', '1.0', 'position()', 'Döngüde geçerli sıra (ör. satır no).',
    'position()'),
  fn('ROUND', '1.0', 'round() / floor() / ceiling()', 'Yuvarlama işlevleri.',
    'round(cbc:TaxAmount)'),
  fn('NUMBER', '1.0', 'number()', 'Metni sayıya çevir.',
    'number(cbc:Percent)'),
  fn('BOOLEAN_NOT', '1.0', 'not() / boolean()', 'Mantıksal değil/dönüştür.',
    'not(cbc:Note)'),
];

// ─── XPath 2.0 / 3.0 — Saxon ile çalışır ────────────────────────────────────
const v2: Snippet[] = [
  fn('FORMAT_DATE', '2.0/3.0', 'format-date()', 'ISO tarihi Türkçe biçimle (gg.aa.yyyy).',
    "format-date(xs:date(cbc:IssueDate), '[D01].[M01].[Y0001]')"),
  fn('FORMAT_DATETIME', '2.0/3.0', 'format-dateTime()', 'Tarih-saat biçimlendir.',
    "format-dateTime(current-dateTime(), '[D01].[M01].[Y0001] [H01]:[m01]')"),
  fn('CURRENT_DATE', '2.0/3.0', 'current-date()', 'Bugünün tarihi (baskı tarihi vb.).',
    'current-date()'),
  fn('STRING_JOIN', '2.0/3.0', 'string-join()', 'Diziyi ayırıcıyla birleştir.',
    "string-join(cac:InvoiceLine/cac:Item/cbc:Name, ', ')"),
  fn('TOKENIZE', '2.0/3.0', 'tokenize()', 'Metni ayırıcıya göre diziye böl.',
    "tokenize(cbc:Note, ';')"),
  fn('REPLACE', '2.0/3.0', 'replace()', 'Regex ile değiştir (ör. VKN biçimle).',
    "replace(cbc:ID, '(\\d{3})(\\d{3})(\\d{4})', '$1-$2-$3')"),
  fn('MATCHES', '2.0/3.0', 'matches()', 'Regex eşleşmesi (koşullarda).',
    "matches(cbc:ID, '^EFS\\d+$')"),
  fn('DISTINCT_VALUES', '2.0/3.0', 'distinct-values()', 'Yinelenenleri kaldır (ör. KDV oranları).',
    'distinct-values(cac:TaxTotal/cac:TaxSubtotal/cbc:Percent)'),
  fn('UPPER_LOWER', '2.0/3.0', 'upper-case() / lower-case()', 'Büyük/küçük harf.',
    'upper-case(cac:PartyName/cbc:Name)'),
  fn('ROUND_HALF', '2.0/3.0', 'round-half-to-even()', 'Bankacı yuvarlaması (kuruş).',
    'round-half-to-even(cbc:TaxAmount, 2)'),
  fn('AVG_MINMAX', '2.0/3.0', 'avg() / min() / max()', 'Toplu sayısal işlevler.',
    'max(cac:InvoiceLine/cbc:LineExtensionAmount)'),
  fn('IF_THEN_ELSE', '2.0/3.0', 'if/then/else', 'XPath içi koşullu ifade.',
    "if (cbc:Percent = 0) then 'İSTİSNA' else concat(cbc:Percent, '%')"),
];

export const xpathSnippets: Snippet[] = [...v1, ...v2];
