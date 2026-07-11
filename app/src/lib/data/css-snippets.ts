/**
 * CSS stil snippet'leri — "CSS Stilleri" kategorisi.
 *
 * Bunlar C# UBL-TR veri kaynağından değil, uygulama düzeyinde tanımlanır
 * (bkz. snippets.json C# tarafından üretilir, data:sync ile ezilir). İmlecin
 * bulunduğu yere — özellikle XSLT'deki <style> bloğunun içine — hazır CSS
 * kuralları/özellikleri ekler.
 */
import type { Snippet } from './types';

function css(
  key: string,
  subCategory: string,
  displayName: string,
  description: string,
  iconText: string,
  code: string,
): Snippet {
  return {
    key: `CSS_${key}`,
    category: 'CSS Stilleri',
    subCategory,
    displayName,
    description,
    iconText,
    xsltCode: code,
    dragDataString: `<!-- EFATURA_SNIPPET:CSS_${key} -->`,
  };
}

export const cssSnippets: Snippet[] = [
  // ── Metin ─────────────────────────────────────────────
  css('TEXT_CENTER', 'Metin', 'Ortala', 'Metni yatayda ortalar.', '↔', 'text-align: center;'),
  css('TEXT_RIGHT', 'Metin', 'Sağa Yasla', 'Metni sağa yaslar.', '➡', 'text-align: right;'),
  css('FONT_BOLD', 'Metin', 'Kalın', 'Yazıyı kalınlaştırır.', 'B', 'font-weight: bold;'),
  css('FONT_SIZE', 'Metin', 'Yazı Boyutu', 'Yazı tipi boyutunu ayarlar.', '🔠', 'font-size: 12px;'),
  css('FONT_COLOR', 'Metin', 'Yazı Rengi', 'Metin rengini ayarlar.', '🎨', 'color: #333333;'),
  css('FONT_FAMILY', 'Metin', 'Yazı Tipi', 'Yazı tipi ailesini ayarlar.', '🔡', 'font-family: Arial, Helvetica, sans-serif;'),
  css('TEXT_UPPER', 'Metin', 'Büyük Harf', 'Metni büyük harfe çevirir.', 'AA', 'text-transform: uppercase;'),
  css('LINE_HEIGHT', 'Metin', 'Satır Yüksekliği', 'Satır aralığını ayarlar.', '↕', 'line-height: 1.4;'),

  // ── Kutu & Kenarlık ───────────────────────────────────
  css('BORDER', 'Kutu & Kenarlık', 'Kenarlık', 'Dört kenara çizgi ekler.', '⬛', 'border: 1px solid #cccccc;'),
  css('BORDER_BOTTOM', 'Kutu & Kenarlık', 'Alt Kenarlık', 'Yalnızca alt kenara çizgi ekler.', '▁', 'border-bottom: 1px solid #000000;'),
  css('RADIUS', 'Kutu & Kenarlık', 'Köşe Yuvarlama', 'Köşeleri yuvarlar.', '◜', 'border-radius: 6px;'),
  css('PADDING', 'Kutu & Kenarlık', 'İç Boşluk', 'İçerik ile kenar arası boşluk.', '⧉', 'padding: 8px;'),
  css('MARGIN', 'Kutu & Kenarlık', 'Dış Boşluk', 'Öğenin dış boşluğu.', '⬚', 'margin: 8px 0;'),
  css('BG_COLOR', 'Kutu & Kenarlık', 'Arka Plan', 'Arka plan rengini ayarlar.', '🟦', 'background-color: #f5f5f5;'),
  css('SHADOW', 'Kutu & Kenarlık', 'Gölge', 'Hafif kutu gölgesi ekler.', '🌫', 'box-shadow: 0 2px 6px rgba(0, 0, 0, 0.1);'),

  // ── Yerleşim ──────────────────────────────────────────
  css(
    'FLEX_ROW',
    'Yerleşim',
    'Flex Satır',
    'Yatay hizalı esnek kutu (araları açık).',
    '⬌',
    'display: flex; align-items: center; justify-content: space-between;',
  ),
  css('WIDTH_FULL', 'Yerleşim', 'Tam Genişlik', 'Öğeyi %100 genişletir.', '⇔', 'width: 100%;'),
  css('HIDE', 'Yerleşim', 'Gizle', 'Öğeyi gizler.', '🚫', 'display: none;'),

  // ── Tablo ─────────────────────────────────────────────
  css('TABLE_COLLAPSE', 'Tablo', 'Kenarları Birleştir', 'Tablo hücre kenarlarını birleştirir.', '▦', 'border-collapse: collapse;'),
  css('CELL', 'Tablo', 'Hücre Stili', 'Tablo hücresi iç boşluk + kenarlık.', '▤', 'padding: 6px; border: 1px solid #dddddd;'),
  css('ZEBRA', 'Tablo', 'Zebra Satır', 'Değişen satır arka planı (nth-child ile kullanın).', '🦓', 'background-color: #fafafa;'),

  // ── Sayfa & Baskı ─────────────────────────────────────
  css('PAGE_A4', 'Sayfa & Baskı', 'A4 Sayfa', 'Baskı için A4 sayfa boyutu ve kenar boşluğu.', '📄', '@page {\n  size: A4 portrait;\n  margin: 10mm 10mm 10mm 10mm;\n}'),
  css('PRINT_HIDE', 'Sayfa & Baskı', 'Baskıda Gizle', 'Yalnızca baskıda gizlenecek alan.', '🖨', '@media print {\n  .no-print { display: none; }\n}'),
  css('RULE', 'Sayfa & Baskı', 'Boş Kural', 'Boş bir CSS sınıf kuralı iskeleti.', '{}', '.yeniSinif {\n  \n}'),
];
