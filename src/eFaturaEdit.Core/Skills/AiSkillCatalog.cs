using System.Collections.Generic;

namespace eFaturaEdit
{
    /// <summary>
    /// Uygulamayla birlikte gelen hazır AI yetenek paketleri.
    ///
    /// <para>
    /// Kullanıcı bunları Ayarlar → AI → Yetenekler'den sağlayıcı bazında açar/kapatır.
    /// Kullanıcının kendi yazdığı paketler bu listeye KARIŞMAZ; ayrı bir dosyada
    /// (<c>$APPDATA/user-skills.json</c>) tutulur.
    /// </para>
    ///
    /// <para>
    /// Yazım kuralı: her paket, sistem promptunun zaten söylediğini TEKRARLAMAZ —
    /// üzerine derinlik ekler. Sistem promptu temel UBL-TR bilgisini, kapsam kilidini
    /// ve SEARCH/REPLACE çıktı biçimini zaten kurar.
    /// </para>
    /// </summary>
    public static class AiSkillCatalog
    {
        public const string CategoryDesign = "Tasarım";
        public const string CategoryTechnical = "Teknik";

        public static readonly IList<AiSkillInfo> All = new List<AiSkillInfo>
        {
            // ═══════════════════════════════════════════════════════════════
            // Tasarım
            // ═══════════════════════════════════════════════════════════════

            new AiSkillInfo(
                id: "modern-design",
                category: CategoryDesign,
                displayName: "Modern & sanatsal tasarım",
                description: "Tipografi ölçeği, renk/kontrast, boşluk ritmi ve görsel hiyerarşi — baskıda ayakta kalan estetik.",
                prompt:
@"## Yetenek: Modern & sanatsal belge tasarımı

Fatura tasarımını ""veri dökümü"" değil, **tasarlanmış bir belge** olarak ele al. Estetik kararları
gerekçelendir; süsleme için süsleme yapma.

**Tipografi**
- Tek bir aile yeter (sistem yığını: `-apple-system, ""Segoe UI"", Roboto, ""Helvetica Neue"", Arial, sans-serif`).
  Baskıda güvenli olmayan web font indirmeye ÇALIŞMA — çıktı çevrimdışı basılır.
- **Ölçek kur, rastgele boyut verme.** Örn. 8/9/10/12/16/22 px basamakları. Gövde metni 9–10 pt
  (≈12–13 px) altına düşme; kasa/arşiv çıktıları taranabilir kalmalı.
- Hiyerarşiyi boyutla DEĞİL, ağırlık + renk + boşlukla kur: ağırlık (400/600/700), gri tonu
  (#111 başlık, #444 gövde, #767676 etiket) ve harf aralığı (etiketlerde `letter-spacing: .04em; text-transform: uppercase`).
- Rakamlar tabloda hizalanmalı: `font-variant-numeric: tabular-nums;` (yoksa kuruşlar kayar).

**Renk**
- 1 vurgu rengi + nötr gri skalası yeter. Vurguyu yalnızca başlık şeridi, toplam kutusu ve ince
  ayraçlarda kullan; her yere serpme.
- **Baskı kontrastı gerçektir:** açık gri metin (#999 altı) ekranda şık, kâğıtta okunmaz. Metin/zemin
  kontrastı en az 4.5:1 olsun. Geniş dolu renk zeminlerinden kaçın (toner/mürekkep yer, tarama kirlenir).
- Zebra satır kullanacaksan çok hafif olsun (#fafafa–#f5f5f5); daha koyusu baskıda şeritlenir.

**Boşluk ve ritim**
- Tek bir boşluk birimi seç (4px veya 6px) ve TÜM padding/margin bunun katı olsun. Karışık
  boşluk değerleri belgeyi ""özensiz"" gösteren asıl şeydir.
- Ayraç için çizgi yerine **boşluk** tercih et. Çizgi gerekiyorsa 1px ve açık (#e5e5e5) — kalın
  kenarlıklarla kutulanmış tablo eski görünür.
- Hizalama sütunları: etiket sol, tutar SAĞA hizalı, miktar sağa hizalı, açıklama sol.

**Yerleşim**
- Belgeyi bölgelere ayır: başlık şeridi (logo + belge tipi + no/tarih) · taraflar (satıcı | alıcı,
  iki sütun) · kalem tablosu · toplam kutusu (sağa yaslı) · dipnot/QR.
- Toplam kutusu görsel olarak en ağır öğe olmalı — ödenecek tutar belgenin cevabıdır.
- Logo alanına sabit yükseklik ver (`height: 48px; width: auto`), aksi halde farklı logolar yerleşimi
  bozar.

Bir tasarım değişikliği önerirken **hangi ilkeye dayandığını tek cümleyle söyle** (ör. ""etiketleri
gri + küçük harfe alarak tutarların önüne geçmelerini engelledim"")."),

            new AiSkillInfo(
                id: "print-a4",
                category: CategoryDesign,
                displayName: "A4 / baskı ustalığı",
                description: "@page, mm ölçüler, sayfa kırılımı, çok sayfada başlık/altbilgi tekrarı ve nakli yekûn.",
                prompt:
@"## Yetenek: A4 / PDF baskı ustalığı

Çıktı kâğıda gider. Ekranda doğru görünen çok şey baskıda dağılır; kararlarını **kâğıda göre** ver.

**Sayfa kurulumu**
- `@page { size: A4 portrait; margin: 10mm; }` — kenar boşluğunu 8mm altına indirme (çoğu yazıcı
  basamaz, içerik kırpılır).
- Ölçüleri **mm** ver (px değil): A4 = 210×297mm; 10mm kenarla kullanılabilir genişlik **190mm**.
- `body { -webkit-print-color-adjust: exact; print-color-adjust: exact; }` — yoksa zemin renkleri ve
  zebra satırlar baskıda KAYBOLUR (yazıcı arka planı basmaz).

**Sayfa kırılımı**
- Bölünmemesi gerekenlere `page-break-inside: avoid;` (toplam kutusu, taraf bilgileri, tek bir kalem satırı).
- `page-break-after: avoid;` başlık satırlarında — başlık sayfanın dibinde yalnız kalmasın.
- Tabloyu `<thead>`/`<tbody>`/`<tfoot>` ile kur: **`<thead>` her sayfada otomatik tekrarlanır.**
  Başlık satırını `<tbody>` içine koyarsan ikinci sayfada kalem tablosu başlıksız kalır.
- `tfoot` her sayfada tekrarlanır — sayfa altına ""devam ediyor"" notu için kullanılabilir, ama
  toplamları oraya KOYMA (her sayfada basılır).

**Çok sayfalı fatura ve nakli yekûn**
- Kalem sayısı sayfaya sığmayınca ara sayfaların altına ""nakli yekûn / sonraki sayfaya devir"",
  sonraki sayfanın üstüne ""nakli yekûn / önceki sayfadan devir"" satırı gelir.
- **Devir satırı SADECE BİR KEZ basılmalı.** Bu tam olarak bu projede kırılmış bir yerdir: satır hem
  kalem tablosunun sonuna hem de alt toplam kutusuna basılınca ara sayfalarda **iki kez** göründü.
  Devir satırını üreten şablonu yazarken, aynı bilgiyi başka bir bölümün de basıp basmadığını kontrol et.
- Devir tutarı = o sayfaya kadarki kalemlerin `LineExtensionAmount` toplamı — genel toplam DEĞİL.
- Sayfa numarası CSS ile: `@page { @bottom-right { content: counter(page) "" / "" counter(pages); } }`
  (Saxon+tarayıcı baskısında destek sınırlıdır; garanti isteniyorsa sayfalamayı XSLT'de kalem sayısına
  göre yap ve sayfa no'yu şablonda bas).

**Yasak**
- **Baskıda JavaScript ÇALIŞMAZ.** Yerleşimi, sayfalamayı, toplamı JS ile hesaplama — hepsi XSLT/CSS
  ile çözülmeli. JS yalnızca uygulama içi önizlemede süsleme/etkileşim içindir.
- `position: fixed` baskıda beklenmedik davranır; sabit üstbilgi/altbilgi için `thead`/`tfoot` kullan.
- `vh/vw` birimleri baskıda anlamsızdır — mm/pt kullan."),

            // ═══════════════════════════════════════════════════════════════
            // Teknik
            // ═══════════════════════════════════════════════════════════════

            new AiSkillInfo(
                id: "xslt-advanced",
                category: CategoryTechnical,
                displayName: "XSLT 2.0/3.0 ileri",
                description: "for-each-group, xsl:function, sequence tipleri, mode ve tunnel parametreleri.",
                prompt:
@"## Yetenek: İleri XSLT 2.0/3.0

Motor **Saxon-HE** — 2.0 ve 3.0 serbesttir. 1.0 refleksiyle (`node-set()`, Muenchian gruplama,
`xsl:key` hileleri) yazma; doğrudan 2.0 yapılarını kullan.

**Gruplama** — `xsl:for-each-group` (Muenchian'ın yerine geçer)
- `group-by=""...""` → değere göre grupla (ör. KDV oranına göre vergi özeti):
  `<xsl:for-each-group select=""cac:InvoiceLine"" group-by=""cac:TaxTotal/cac:TaxSubtotal/cbc:Percent"">`
  içeride `current-grouping-key()` ve `current-group()` kullanılır.
- `group-adjacent=""...""` → bitişik olanları grupla (sayfalama, ardışık satır blokları).
- `group-starting-with=""...""` → belirli bir düğümle başlayan blokları böl.
- Sayfalama için: `group-adjacent=""(position() - 1) idiv $satirSayisi""` — kalemleri N'erli sayfalara böler.

**Fonksiyonlar** — tekrar eden hesabı kopyalama
```
<xsl:function name=""f:tutar"" as=""xs:string"">
  <xsl:param name=""v"" as=""xs:decimal?""/>
  <xsl:value-of select=""format-number(($v, 0)[1], '#.##0,00', 'tr')""/>
</xsl:function>
```
Kök öğede `xmlns:f=""urn:local""` ve `exclude-result-prefixes=""f xs""` bildir; `xs` için
`xmlns:xs=""http://www.w3.org/2001/XMLSchema""` gerekir.

**Tipler ve sequence**
- `as=""xs:decimal""`, `as=""element()*""`, `as=""xs:string?""` yaz — tip bildirimi hatayı ÇALIŞMA
  ZAMANINDA değil, derlemede yakalar.
- Toplam: `sum(cac:InvoiceLine/cbc:LineExtensionAmount)` — `xs:decimal` dönüşümü otomatiktir ama
  boş dizide 0 döner (istediğin bu olmayabilir).
- `xs:date(cbc:IssueDate)`, `xs:decimal(cbc:PayableAmount)` ile açıkça dönüştür; örtük dönüşüme güvenme.

**Mode ve tunnel**
- Aynı düğümü farklı bağlamlarda basmak için `mode`: `<xsl:template match=""cac:InvoiceLine"" mode=""ozet"">`.
- Derin şablon zincirinde parametre taşımak için `tunnel=""yes""` — her katmanda tekrar tekrar
  `<xsl:with-param>` yazmaktan kurtarır:
  `<xsl:apply-templates select=""..."" ><xsl:with-param name=""sayfa"" select=""$s"" tunnel=""yes""/></xsl:apply-templates>`
  alıcı tarafta `<xsl:param name=""sayfa"" tunnel=""yes""/>`.

**Sık kullanılan 2.0 fonksiyonları**
`format-date`, `format-dateTime`, `format-number`, `tokenize`, `replace`, `matches`, `upper-case`,
`lower-case`, `string-join`, `distinct-values`, `current-dateTime`, `if/then/else` (XPath içinde).

**Tuzak:** `xsl:number` yerine `position()` kullanırken filtrelenmiş sequence'te position() FİLTRE
SONRASI sıradır — kalem sıra numarası için `cbc:ID` varsa onu bas."),

            new AiSkillInfo(
                id: "xpath-advanced",
                category: CategoryTechnical,
                displayName: "XPath ileri",
                description: "Eksenler, predicate, tip dönüşümleri ve UBL'de en sık düşülen namespace tuzağı.",
                prompt:
@"## Yetenek: İleri XPath 2.0/3.1

**EN SIK DÜŞÜLEN TUZAK — namespace.** UBL belgesinin kökü **varsayılan namespace'tedir**
(`xmlns=""urn:oasis:names:specification:ubl:schema:xsd:Invoice-2""`). Bu yüzden:
- `/Invoice/cbc:ID` → **HİÇBİR ŞEY eşleşmez.** Hata da vermez, sessizce boş döner. Sıfır sonuç +
  sıfır hata = kullanıcı sebebi anlayamaz.
- Doğrusu ikisinden biri: kök öğede `xpath-default-namespace=""urn:oasis:...:Invoice-2""` bildir
  (sonra `/Invoice/cbc:ID` çalışır), **veya** köke de önek ver (`/n1:Invoice/cbc:ID`).
- `cbc:`/`cac:` önekli alt öğeler zaten kendi namespace'lerinde — onlar için sorun yok.
- Bir yol boş dönüyorsa **ilk şüphelin namespace olsun**, yazım hatası değil.

**Eksenler (axis)** — UBL'de gerçekten işe yarayanlar
- `ancestor::cac:InvoiceLine` — bir alt düğümden kalem satırına çık.
- `following-sibling::cac:InvoiceLine[1]` — sonraki kalem (devir/karşılaştırma hesapları).
- `parent::*`, `self::cbc:*` — mode'lu şablonlarda tip ayırt etmek için.
- `//` pahalıdır (tüm ağacı tarar); 150–600 KB'lık faturada tam yolu yazmak belirgin şekilde hızlıdır.

**Predicate**
- Konum: `cac:InvoiceLine[1]`, `cac:InvoiceLine[position() le 20]`, `cac:InvoiceLine[last()]`.
- Değer: `cac:PartyIdentification[cbc:ID/@schemeID = 'VKN']/cbc:ID` — VKN ile TCKN'yi böyle ayır.
- Varlık: `cac:AllowanceCharge[cbc:ChargeIndicator = 'false']` (iskonto; `true` = masraf).
- Zincirleme predicate soldan sağa uygulanır: `x[@a='1'][2]` ≠ `x[2][@a='1']`.

**Tip dönüşümleri (sessiz hatanın diğer kaynağı)**
- XML'den gelen her şey **metindir**. `cbc:PayableAmount > 1000` string karşılaştırması yapabilir →
  ""900"" > ""1000"" DOĞRU çıkar. `xs:decimal(cbc:PayableAmount) > 1000` yaz.
- Tarih: `xs:date(cbc:IssueDate)` (gelen biçim ISO: 2016-09-26). Saat: `xs:time(cbc:IssueTime)`.
- Boş/eksik düğüm: `(cbc:Note, '')[1]` veya `if (cbc:Note) then ... else ...` — yoksa boş sequence
  sessizce her şeyi boşaltır.

**Yararlı fonksiyonlar**
`sum()`, `count()`, `distinct-values()`, `string-join(x, ', ')`, `normalize-space()`, `substring()`,
`translate()`, `number()`, `round-half-to-even(x, 2)` (para yuvarlama), `empty()`, `exists()`.

Bir XPath önerirken **belgedeki gerçek yola** dayan (sana verilen XML'i oku), hatırladığın şemaya değil."),

            new AiSkillInfo(
                id: "css-print-layout",
                category: CategoryTechnical,
                displayName: "Modern CSS (baskı-güvenli)",
                description: "Grid/flex'i baskıyı bozmadan kullanma; @media print farkları ve tablo yerleşimi.",
                prompt:
@"## Yetenek: Modern CSS — baskı-güvenli

Modern CSS'i kullan, ama **her özelliğin baskıdaki davranışını** bil. Ekran ve kâğıt aynı ortam değil.

**Neyi nerede kullanmalı**
- **Kalem tablosu → gerçek `<table>`.** Sayfalar arası bölünme, `<thead>` tekrarı ve sütun hizası
  yalnızca tabloda güvenilir çalışır. Grid ile yapılan ""tablo"" ikinci sayfada başlıksız kalır ve
  satırları ortadan bölünür. Bu, baskıda tablo kullanmanın TEK gerçek gerekçesidir — nostalji değil.
- **Başlık/taraflar/toplam kutusu → flex veya grid serbest.** Bunlar tek sayfada, bölünmeyen bloklardır.
  `display: flex; justify-content: space-between;` satıcı|alıcı ikilisi için idealdir.
- Grid kullanacaksan sütunları `fr` yerine sabit ver (`grid-template-columns: 95mm 95mm`) — baskıda
  `fr` hesabı yazıcıdan yazıcıya oynayabilir.

**Baskıda kırılan şeyler (ölçülmüş)**
- Arka plan renkleri/zebra satırlar **basılmaz** → `print-color-adjust: exact` şart.
- `position: fixed` / `sticky` → sayfa akışında beklenmedik; sabit başlık için `<thead>` kullan.
- `box-shadow`, `filter`, `opacity` → toner israfı ve gri bulanıklık; baskıda kaldır.
- `vh/vw`, `dvh` → kâğıtta viewport yok. mm/pt kullan.
- `overflow: hidden` → taşan içeriği baskıda **sessizce kırpar** (uzun ürün adları kaybolur).
  Bunun yerine `word-break: break-word;` + `hyphens: auto;`.

**@media print**
```
@media print {
  .no-print, .toolbar, .btn { display: none; }
  a[href]::after { content: """"; }   /* URL'leri parantez içinde basma */
  body { margin: 0; }
}
```
Önizlemede görünüp kâğıtta olmaması gerekenleri `.no-print` ile işaretle.

**Tablo detayları**
- `table { width: 100%; border-collapse: collapse; table-layout: fixed; }` — `fixed` olmadan uzun
  ürün adı sütun genişliklerini bozar.
- Sütun genişliklerini `<colgroup><col style=""width: 12mm"">` ile ver (yüzde değil, mm).
- Tutar sütunları: `text-align: right; font-variant-numeric: tabular-nums;`
- `td { padding: 1.5mm 2mm; vertical-align: top; }` — dikey ortalama uzun satırlarda kayar."),

            new AiSkillInfo(
                id: "js-preview",
                category: CategoryTechnical,
                displayName: "Önizlemede JavaScript",
                description: "Etkileşim/süsleme için JS; yapısal düzen ve hesap için ASLA (baskıda çalışmaz).",
                prompt:
@"## Yetenek: Önizlemede JavaScript

**Mutlak sınır:** JavaScript yalnızca uygulama içi önizlemede çalışır. **Baskıda / PDF'te çalışmaz.**
Bu yüzden:

**JS ile YAPILMAZ (kâğıtta kaybolur, fatura yanlış basılır)**
- Toplam/KDV/ara toplam hesabı → XSLT'de yap. JS'le hesaplanan toplam baskıda **boş** çıkar.
- Sayfalama, nakli yekûn, sayfa numarası → XSLT + CSS.
- Sütun/satır oluşturma, veri filtreleme, sıralama → XSLT.
- Yerleşim düzeltmesi (""JS ile ölçüp hizalarım"") → CSS.

**JS ile YAPILABİLİR (yalnızca ekran deneyimi)**
- Uzun kalem listesinde arama/vurgulama kutusu (`.no-print` ile işaretle).
- Katlanır/açılır detay bölümleri (baskıda hepsi AÇIK basılmalı — `@media print { details { display: block; } }`).
- Kopyala butonu, tema geçişi, önizleme yardımcıları.
- QR kodu **gösterimi** — ama QR verisini XSLT'den üret; JS'le üretilen QR baskıda çıkmaz.

**Yazım kuralları**
- Betiği belgenin sonuna koy ve `<xsl:text disable-output-escaping=""yes"">` ile değil, **CDATA** ile
  sar — XSLT içinde `&&`, `<`, `>` karakterleri XML'i bozar:
  `<script><![CDATA[ ... ]]></script>`
- XSLT içinde `{` ve `}` karakterleri **attribute value template** olarak yorumlanır; JS bloğunda
  sorun değildir ama bir özniteliğin İÇİNDE JS yazıyorsan `{{` / `}}` ile kaçır.
- Betiği `.no-print` sınıflı öğelere bağla; baskı öncesi DOM'u değiştiren `window.onbeforeprint`
  kullanma (Saxon çıktısı doğrudan basılabilir olmalı).

Bir kullanıcı ""JS ile toplamı hesapla"" derse: **yap ama uyar** — kâğıtta boş çıkacağını söyle ve
XSLT karşılığını öner."),
        };
    }
}
