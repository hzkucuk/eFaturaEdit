using System.Collections.Generic;

namespace eFaturaEdit
{
    /// <summary>
    /// E-Fatura XSLT şablonlarına eklenebilecek tüm snippet tanımları.
    /// Kategoriler: HTML Öğeleri, XSLT Komutları, Sayfa Düzeni, UBL-TR e-Fatura, UBL-TR e-Arşiv.
    /// </summary>
    public static class XsltSnippets
    {
        public const string SnippetPrefix = "EFATURA_SNIPPET:";
        public const string DragPrefix = "<!-- EFATURA_SNIPPET:";
        public const string DragSuffix = " -->";

        public static readonly Dictionary<string, SnippetInfo> Elements = new Dictionary<string, SnippetInfo>
        {
            // ═══════════════════════════════════════════════════════════════
            // HTML Öğeleri
            // ═══════════════════════════════════════════════════════════════

            ["IMAGE"] = new SnippetInfo(
                key: "IMAGE",
                displayName: "Resim",
                iconText: "🖼",
                category: "HTML Öğeleri",
                description: "HTML <img> etiketi ekler. src, width ve height öznitelikleriyle görsel yerleştirmek için kullanılır. Logo, imza veya ürün görseli eklemek için idealdir.",
                xsltCode:
@"<img src=""logo.png"" alt=""Logo"" style=""width:200px; height:auto;"" />"),

            ["TABLE"] = new SnippetInfo(
                key: "TABLE",
                displayName: "Tablo",
                iconText: "📊",
                category: "HTML Öğeleri",
                description: "HTML <table> etiketi ekler. Satır ve sütunlarla düzenli veri gösterimi için kullanılır. Fatura bilgileri, kalem listesi gibi yapılandırılmış veriler için idealdir.",
                xsltCode:
@"<table style=""width:100%; border-collapse:collapse;"" border=""1"">
  <tr style=""background-color:#f0f0f0; font-weight:bold;"">
    <td style=""padding:4px;"">Başlık 1</td>
    <td style=""padding:4px;"">Başlık 2</td>
    <td style=""padding:4px;"">Başlık 3</td>
  </tr>
  <tr>
    <td style=""padding:4px;"">Veri 1</td>
    <td style=""padding:4px;"">Veri 2</td>
    <td style=""padding:4px;"">Veri 3</td>
  </tr>
</table>"),

            ["TEXT"] = new SnippetInfo(
                key: "TEXT",
                displayName: "Metin",
                iconText: "📝",
                category: "HTML Öğeleri",
                description: "HTML <p> paragraf etiketi ekler. Düz metin içeriği, açıklama veya bilgilendirme metinleri için kullanılır.",
                xsltCode:
@"<p style=""font-size:11px; color:#333333;"">Metin içeriği buraya yazılır.</p>"),

            ["LINK"] = new SnippetInfo(
                key: "LINK",
                displayName: "Bağlantı",
                iconText: "🔗",
                category: "HTML Öğeleri",
                description: "HTML <a> bağlantı etiketi ekler. Tıklanabilir bir link oluşturur. Web sitesi, e-posta veya belge referansı için kullanılır.",
                xsltCode:
@"<a href=""https://www.example.com"" target=""_blank"" style=""color:#0066cc; text-decoration:underline;"">Bağlantı Metni</a>"),

            ["HR"] = new SnippetInfo(
                key: "HR",
                displayName: "Yatay Çizgi",
                iconText: "📏",
                category: "HTML Öğeleri",
                description: "HTML <hr> yatay çizgi etiketi ekler. Bölümler arası görsel ayırıcı olarak kullanılır. Fatura bölümlerini ayırmak için idealdir.",
                xsltCode:
@"<hr style=""border:none; border-top:1px solid #cccccc; margin:10px 0;"" />"),

            ["DIV"] = new SnippetInfo(
                key: "DIV",
                displayName: "Kutu (Div)",
                iconText: "📦",
                category: "HTML Öğeleri",
                description: "HTML <div> kutu etiketi ekler. İçerik bloğu oluşturur, kenarlık ve arka plan rengi ile stillenebilir. Bölüm gruplamak için kullanılır.",
                xsltCode:
@"<div style=""border:1px solid #cccccc; padding:10px; margin:5px 0; background-color:#fafafa;"">
  İçerik buraya yazılır.
</div>"),

            ["BOLD"] = new SnippetInfo(
                key: "BOLD",
                displayName: "Kalın Metin",
                iconText: "🅱️",
                category: "HTML Öğeleri",
                description: "HTML <strong> kalın metin etiketi ekler. Önemli bilgileri vurgulamak için kullanılır. Başlık, etiket veya öne çıkarılacak değerler için idealdir.",
                xsltCode:
@"<strong style=""font-size:12px;"">Kalın metin</strong>"),

            ["SPAN"] = new SnippetInfo(
                key: "SPAN",
                displayName: "Etiket",
                iconText: "🏷",
                category: "HTML Öğeleri",
                description: "HTML <span> satır içi etiket ekler. Metin içinde bir bölümü stillemek veya XSLT value-of ile değer göstermek için kullanılır.",
                xsltCode:
@"<span style=""font-size:11px; color:#000000;""><xsl:value-of select=""/n1:Invoice/cbc:ID"" /></span>"),

            // ═══════════════════════════════════════════════════════════════
            // XSLT Komutları
            // ═══════════════════════════════════════════════════════════════

            ["VALUEOF"] = new SnippetInfo(
                key: "VALUEOF",
                displayName: "XSL Değer",
                iconText: "🔖",
                category: "XSLT Komutları",
                description: "XSLT <xsl:value-of> komutu ekler. XML belgesinden belirtilen XPath ifadesine göre tek bir değer okuyup görüntüler. En temel XSLT veri çekme komutu.",
                xsltCode:
@"<xsl:value-of select=""/n1:Invoice/cbc:ID"" />"),

            ["FOREACH"] = new SnippetInfo(
                key: "FOREACH",
                displayName: "Döngü",
                iconText: "🔄",
                category: "XSLT Komutları",
                description: "XSLT <xsl:for-each> döngü komutu ekler. XML'deki tekrarlanan elemanları (ör: fatura kalemleri) sırayla işlemek için kullanılır. select özniteliğiyle hedef node-set belirtilir.",
                xsltCode:
@"<xsl:for-each select=""/n1:Invoice/cac:InvoiceLine"">
  <xsl:value-of select=""cbc:ID"" /> - <xsl:value-of select=""cac:Item/cbc:Name"" />
</xsl:for-each>"),

            ["IF"] = new SnippetInfo(
                key: "IF",
                displayName: "Koşul",
                iconText: "❓",
                category: "XSLT Komutları",
                description: "XSLT <xsl:if> koşul komutu ekler. Belirtilen test ifadesi doğruysa içindeki blok işlenir. Koşullu görüntüleme için kullanılır (ör: alan doluysa göster).",
                xsltCode:
@"<xsl:if test=""/n1:Invoice/cbc:Note"">
  <div><xsl:value-of select=""/n1:Invoice/cbc:Note"" /></div>
</xsl:if>"),

            // ═══════════════════════════════════════════════════════════════
            // Sayfa Düzeni
            // ═══════════════════════════════════════════════════════════════

            ["BARCODE"] = new SnippetInfo(
                key: "BARCODE",
                displayName: "Barkod",
                iconText: "🧾",
                category: "Sayfa Düzeni",
                description: "Harici barkod API servisi kullanarak Code128 formatında barkod görüntüsü ekler. data parametresine dinamik veri (fatura no, ETTN vb.) bağlanabilir.",
                xsltCode:
@"<img src=""https://barcode.tec-it.com/barcode.ashx?data={BARCODE_DATA}&amp;code=Code128"" alt=""Barkod"" style=""height:40px;"" />"),

            ["QR"] = new SnippetInfo(
                key: "QR",
                displayName: "QR Kod",
                iconText: "🔲",
                category: "Sayfa Düzeni",
                description: "QR kod görüntüsü ekler. data parametresine fatura URL'i, ETTN veya diğer bilgiler bağlanabilir. 100x100 piksel varsayılan boyut.",
                xsltCode:
@"<img src=""https://api.qrserver.com/v1/create-qr-code/?size=100x100&amp;data={QR_DATA}"" alt=""QR Kod"" style=""width:100px; height:100px;"" />"),

            ["PAGEBREAK"] = new SnippetInfo(
                key: "PAGEBREAK",
                displayName: "Sayfa Sonu",
                iconText: "📃",
                category: "Sayfa Düzeni",
                description: "CSS page-break-after özelliğiyle yazdırma sırasında sayfa sonu ekler. Faturanın farklı bölümlerini ayrı sayfalarda yazdırmak için kullanılır.",
                xsltCode:
@"<div style=""page-break-after:always;""></div>"),

            ["HEADER"] = new SnippetInfo(
                key: "HEADER",
                displayName: "Üst Bilgi",
                iconText: "⬆",
                category: "Sayfa Düzeni",
                description: "Sayfanın üst kısmına sabit üst bilgi alanı ekler. Firma adı, tarih ve logo için kullanılır. Alt kenarlık çizgisi ile gövdeden ayrılır.",
                xsltCode:
@"<div id=""pageHeader"" style=""width:100%; border-bottom:2px solid #000; padding-bottom:5px; margin-bottom:10px;"">
  <table style=""width:100%;"">
    <tr>
      <td style=""text-align:left; font-weight:bold;"">Firma Adı</td>
      <td style=""text-align:right;""><xsl:value-of select=""/n1:Invoice/cbc:IssueDate"" /></td>
    </tr>
  </table>
</div>"),

            ["FOOTER"] = new SnippetInfo(
                key: "FOOTER",
                displayName: "Alt Bilgi",
                iconText: "⬇",
                category: "Sayfa Düzeni",
                description: "Sayfanın alt kısmına sabit alt bilgi alanı ekler. Yasal uyarılar, sayfa numarası veya 'elektronik olarak oluşturulmuştur' notu için kullanılır.",
                xsltCode:
@"<div id=""pageFooter"" style=""width:100%; border-top:1px solid #999; padding-top:5px; margin-top:10px; font-size:9px; color:#999; text-align:center;"">
  Bu belge elektronik olarak oluşturulmuştur.
</div>"),

            // ═══════════════════════════════════════════════════════════════
            // UBL-TR e-Fatura — Başlık
            // ═══════════════════════════════════════════════════════════════

            ["UBL_INVOICEHEADER"] = new SnippetInfo(
                key: "UBL_INVOICEHEADER",
                displayName: "Fatura Başlığı",
                iconText: "📄",
                category: "UBL-TR e-Fatura",
                subCategory: "Başlık",
                description: "Fatura No, Tarih, Tipi, Senaryo, Para Birimi ve Not bilgilerini tablo olarak gösterir.\nXPath: /n1:Invoice/cbc:ID, cbc:IssueDate, cbc:InvoiceTypeCode, cbc:ProfileID, cbc:DocumentCurrencyCode, cbc:Note",
                xsltCode:
@"<!-- Fatura Başlık Bilgileri -->
<table style=""width:100%; border:1px solid #000; padding:5px;"">
  <tr>
    <td style=""font-weight:bold; width:30%;"">Fatura No:</td>
    <td><xsl:value-of select=""/n1:Invoice/cbc:ID"" /></td>
  </tr>
  <tr>
    <td style=""font-weight:bold;"">Fatura Tarihi:</td>
    <td><xsl:value-of select=""/n1:Invoice/cbc:IssueDate"" /></td>
  </tr>
  <tr>
    <td style=""font-weight:bold;"">Fatura Tipi:</td>
    <td><xsl:value-of select=""/n1:Invoice/cbc:InvoiceTypeCode"" /></td>
  </tr>
  <tr>
    <td style=""font-weight:bold;"">Senaryo:</td>
    <td><xsl:value-of select=""/n1:Invoice/cbc:ProfileID"" /></td>
  </tr>
  <tr>
    <td style=""font-weight:bold;"">Para Birimi:</td>
    <td><xsl:value-of select=""/n1:Invoice/cbc:DocumentCurrencyCode"" /></td>
  </tr>
  <tr>
    <td style=""font-weight:bold;"">Not:</td>
    <td><xsl:value-of select=""/n1:Invoice/cbc:Note"" /></td>
  </tr>
</table>"),

            ["UBL_UUID"] = new SnippetInfo(
                key: "UBL_UUID",
                displayName: "ETTN",
                iconText: "🔑",
                category: "UBL-TR e-Fatura",
                subCategory: "Başlık",
                description: "Faturanın Evrensel Tekil Tanımlama Numarası (ETTN/UUID). GİB tarafından atanan, her faturaya özgü benzersiz kimlik numarasıdır.\nXPath: /n1:Invoice/cbc:UUID",
                xsltCode:
@"<xsl:value-of select=""/n1:Invoice/cbc:UUID"" />"),

            ["UBL_ISSUETIME"] = new SnippetInfo(
                key: "UBL_ISSUETIME",
                displayName: "Düzenleme Saati",
                iconText: "🕐",
                category: "UBL-TR e-Fatura",
                subCategory: "Başlık",
                description: "Faturanın düzenleme saatini gösterir. IssueDate ile birlikte kullanılarak tam tarih-saat bilgisi oluşturulabilir.\nXPath: /n1:Invoice/cbc:IssueTime",
                xsltCode:
@"<xsl:value-of select=""/n1:Invoice/cbc:IssueTime"" />"),

            ["UBL_NOTES"] = new SnippetInfo(
                key: "UBL_NOTES",
                displayName: "Fatura Notları",
                iconText: "📝",
                category: "UBL-TR e-Fatura",
                subCategory: "Başlık",
                description: "Faturadaki tüm not alanlarını döngüyle listeler. Birden fazla Note elemanı olabilir (Sicil No, İşletme Merkezi, vade bilgisi vb.).\nXPath: //n1:Invoice/cbc:Note (for-each)",
                xsltCode:
@"<!-- Fatura Notları -->
<xsl:for-each select=""//n1:Invoice/cbc:Note"">
  <div style=""margin:2px 0; font-size:11px;""><xsl:value-of select=""."" /></div>
</xsl:for-each>"),

            ["UBL_LINECOUNT"] = new SnippetInfo(
                key: "UBL_LINECOUNT",
                displayName: "Kalem Sayısı",
                iconText: "🔢",
                category: "UBL-TR e-Fatura",
                subCategory: "Başlık",
                description: "Faturadaki toplam kalem sayısını gösterir.\nXPath: /n1:Invoice/cbc:LineCountNumeric",
                xsltCode:
@"<xsl:value-of select=""/n1:Invoice/cbc:LineCountNumeric"" />"),

            ["UBL_COPYINDICATOR"] = new SnippetInfo(
                key: "UBL_COPYINDICATOR",
                displayName: "Asıl/Kopya",
                iconText: "📋",
                category: "UBL-TR e-Fatura",
                subCategory: "Başlık",
                description: "Faturanın asıl mı yoksa kopya mı olduğunu belirten gösterge. false=Asıl, true=Kopya.\nXPath: /n1:Invoice/cbc:CopyIndicator",
                xsltCode:
@"<xsl:choose>
  <xsl:when test=""/n1:Invoice/cbc:CopyIndicator = 'true'"">
    <span style=""color:red; font-weight:bold;"">KOPYA</span>
  </xsl:when>
  <xsl:otherwise>
    <span style=""font-weight:bold;"">ASIL</span>
  </xsl:otherwise>
</xsl:choose>"),

            ["UBL_PROFILEID"] = new SnippetInfo(
                key: "UBL_PROFILEID",
                displayName: "Senaryo Kontrolü",
                iconText: "🔀",
                category: "UBL-TR e-Fatura",
                subCategory: "Başlık",
                description: "Fatura senaryosuna göre koşullu renk ve metin gösterimi. TEMELFATURA, TICARIFATURA, IHRACAT, EARSIVFATURA senaryolarını tanır.\nXPath: /n1:Invoice/cbc:ProfileID",
                xsltCode:
@"<!-- Fatura Senaryosuna Göre Koşullu Gösterim -->
<xsl:choose>
  <xsl:when test=""/n1:Invoice/cbc:ProfileID = 'TEMELFATURA'"">
    <span style=""color:green; font-weight:bold;"">TEMEL FATURA</span>
  </xsl:when>
  <xsl:when test=""/n1:Invoice/cbc:ProfileID = 'TICARIFATURA'"">
    <span style=""color:blue; font-weight:bold;"">TİCARİ FATURA</span>
  </xsl:when>
  <xsl:when test=""/n1:Invoice/cbc:ProfileID = 'IHRACAT'"">
    <span style=""color:orange; font-weight:bold;"">İHRACAT FATURASI</span>
  </xsl:when>
  <xsl:when test=""/n1:Invoice/cbc:ProfileID = 'EARSIVFATURA'"">
    <span style=""color:purple; font-weight:bold;"">E-ARŞİV FATURA</span>
  </xsl:when>
  <xsl:otherwise>
    <span><xsl:value-of select=""/n1:Invoice/cbc:ProfileID"" /></span>
  </xsl:otherwise>
</xsl:choose>"),

            ["UBL_TYPECODE"] = new SnippetInfo(
                key: "UBL_TYPECODE",
                displayName: "Fatura Tipi Kontrolü",
                iconText: "🏷",
                category: "UBL-TR e-Fatura",
                subCategory: "Başlık",
                description: "Fatura tipine göre koşullu renk ve metin gösterimi. SATIS, IADE, TEVKIFAT, ISTISNA, OZELMATRAH, IHRACKAYITLI tiplerini tanır.\nXPath: /n1:Invoice/cbc:InvoiceTypeCode",
                xsltCode:
@"<!-- Fatura Tipine Göre Koşullu Gösterim -->
<xsl:choose>
  <xsl:when test=""/n1:Invoice/cbc:InvoiceTypeCode = 'SATIS'"">
    <span style=""font-weight:bold;"">SATIŞ FATURASI</span>
  </xsl:when>
  <xsl:when test=""/n1:Invoice/cbc:InvoiceTypeCode = 'IADE'"">
    <span style=""color:red; font-weight:bold;"">İADE FATURASI</span>
  </xsl:when>
  <xsl:when test=""/n1:Invoice/cbc:InvoiceTypeCode = 'TEVKIFAT'"">
    <span style=""color:darkred; font-weight:bold;"">TEVKİFAT FATURASI</span>
  </xsl:when>
  <xsl:when test=""/n1:Invoice/cbc:InvoiceTypeCode = 'ISTISNA'"">
    <span style=""color:gray; font-weight:bold;"">İSTİSNA FATURASI</span>
  </xsl:when>
  <xsl:when test=""/n1:Invoice/cbc:InvoiceTypeCode = 'OZELMATRAH'"">
    <span style=""font-weight:bold;"">ÖZEL MATRAH FATURASI</span>
  </xsl:when>
  <xsl:when test=""/n1:Invoice/cbc:InvoiceTypeCode = 'IHRACKAYITLI'"">
    <span style=""color:orange; font-weight:bold;"">İHRAÇ KAYITLI FATURA</span>
  </xsl:when>
  <xsl:otherwise>
    <span><xsl:value-of select=""/n1:Invoice/cbc:InvoiceTypeCode"" /></span>
  </xsl:otherwise>
</xsl:choose>"),

            ["UBL_CURRENCYID"] = new SnippetInfo(
                key: "UBL_CURRENCYID",
                displayName: "Para Birimi Kontrolü",
                iconText: "💱",
                category: "UBL-TR e-Fatura",
                subCategory: "Başlık",
                description: "Faturanın para birimi kodunu gösterir. TRY/TRL dışı para birimlerinde 'Dövizli Fatura' uyarısı verir.\nXPath: /n1:Invoice/cbc:DocumentCurrencyCode",
                xsltCode:
@"<xsl:value-of select=""/n1:Invoice/cbc:DocumentCurrencyCode"" />
<xsl:if test=""/n1:Invoice/cbc:DocumentCurrencyCode != 'TRY' and /n1:Invoice/cbc:DocumentCurrencyCode != 'TRL'"">
  <span style=""color:red; font-weight:bold;""> (Dövizli Fatura)</span>
</xsl:if>"),

            ["UBL_TAXCURRENCYCODE"] = new SnippetInfo(
                key: "UBL_TAXCURRENCYCODE",
                displayName: "Vergi Para Birimi",
                iconText: "💲",
                category: "UBL-TR e-Fatura",
                subCategory: "Başlık",
                description: "Belge para birimi dışında vergi ödemelerinde kullanılacak para birimini gösterir.\nXPath: /n1:Invoice/cbc:TaxCurrencyCode",
                xsltCode:
@"<xsl:if test=""/n1:Invoice/cbc:TaxCurrencyCode"">
  <span style=""font-weight:bold;"">Vergi Para Birimi: </span>
  <xsl:value-of select=""/n1:Invoice/cbc:TaxCurrencyCode"" />
</xsl:if>"),

            ["UBL_INVOICEPERIOD"] = new SnippetInfo(
                key: "UBL_INVOICEPERIOD",
                displayName: "Fatura Dönemi",
                iconText: "📆",
                category: "UBL-TR e-Fatura",
                subCategory: "Başlık",
                description: "Faturanın ait olduğu dönem bilgisini gösterir. Başlangıç ve bitiş tarihleri ile açıklama alanlarını içerir.\nXPath: /n1:Invoice/cac:InvoicePeriod (cbc:StartDate, cbc:EndDate, cbc:Description)",
                xsltCode:
@"<!-- Fatura Dönemi -->
<xsl:if test=""/n1:Invoice/cac:InvoicePeriod"">
  <div style=""padding:4px;"">
    <span style=""font-weight:bold;"">Fatura Dönemi: </span>
    <xsl:value-of select=""/n1:Invoice/cac:InvoicePeriod/cbc:StartDate"" />
    <xsl:if test=""/n1:Invoice/cac:InvoicePeriod/cbc:EndDate"">
      <xsl:text> — </xsl:text><xsl:value-of select=""/n1:Invoice/cac:InvoicePeriod/cbc:EndDate"" />
    </xsl:if>
    <xsl:if test=""/n1:Invoice/cac:InvoicePeriod/cbc:Description"">
      <xsl:text> (</xsl:text><xsl:value-of select=""/n1:Invoice/cac:InvoicePeriod/cbc:Description"" /><xsl:text>)</xsl:text>
    </xsl:if>
  </div>
</xsl:if>"),

            ["UBL_ACCOUNTINGCOST"] = new SnippetInfo(
                key: "UBL_ACCOUNTINGCOST",
                displayName: "İlave Fatura Tipi",
                iconText: "🏷",
                category: "UBL-TR e-Fatura",
                subCategory: "Başlık",
                description: "Mükellefin fatura tipi olarak ilave bir ayrıma gitmesi gerekiyorsa belirlediği farklı fatura tipini gösterir.\nXPath: /n1:Invoice/cbc:AccountingCost",
                xsltCode:
@"<xsl:if test=""/n1:Invoice/cbc:AccountingCost"">
  <span style=""font-weight:bold;"">İlave Fatura Tipi: </span>
  <xsl:value-of select=""/n1:Invoice/cbc:AccountingCost"" />
</xsl:if>"),

            // ═══════════════════════════════════════════════════════════════
            // UBL-TR e-Fatura — Taraflar
            // ═══════════════════════════════════════════════════════════════

            ["UBL_SUPPLIER"] = new SnippetInfo(
                key: "UBL_SUPPLIER",
                displayName: "Satıcı Bilgisi",
                iconText: "🏢",
                category: "UBL-TR e-Fatura",
                subCategory: "Taraflar",
                description: "Satıcı (tedarikçi) firma bilgileri tablosu. VKN/TCKN, unvan, adres ve vergi dairesi alanlarını gösterir.\nXPath: /n1:Invoice/cac:AccountingSupplierParty/cac:Party",
                xsltCode:
@"<!-- Satıcı (Tedarikçi) Bilgileri -->
<table style=""width:100%; border:1px solid #ccc; padding:5px;"">
  <tr>
    <td style=""font-weight:bold; width:30%;"">Satıcı VKN/TCKN:</td>
    <td><xsl:value-of select=""/n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PartyIdentification/cbc:ID"" /></td>
  </tr>
  <tr>
    <td style=""font-weight:bold;"">Unvan:</td>
    <td><xsl:value-of select=""/n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PartyName/cbc:Name"" /></td>
  </tr>
  <tr>
    <td style=""font-weight:bold;"">Adres:</td>
    <td>
      <xsl:value-of select=""/n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PostalAddress/cbc:StreetName"" />
      <xsl:text> </xsl:text>
      <xsl:value-of select=""/n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PostalAddress/cbc:CitySubdivisionName"" />
      / <xsl:value-of select=""/n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PostalAddress/cbc:CityName"" />
    </td>
  </tr>
  <tr>
    <td style=""font-weight:bold;"">Vergi Dairesi:</td>
    <td><xsl:value-of select=""/n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PartyTaxScheme/cac:TaxScheme/cbc:Name"" /></td>
  </tr>
</table>"),

            ["UBL_CUSTOMER"] = new SnippetInfo(
                key: "UBL_CUSTOMER",
                displayName: "Alıcı Bilgisi",
                iconText: "👤",
                category: "UBL-TR e-Fatura",
                subCategory: "Taraflar",
                description: "Alıcı (müşteri) firma bilgileri tablosu. VKN/TCKN, unvan, adres ve vergi dairesi alanlarını gösterir.\nXPath: /n1:Invoice/cac:AccountingCustomerParty/cac:Party",
                xsltCode:
@"<!-- Alıcı (Müşteri) Bilgileri -->
<table style=""width:100%; border:1px solid #ccc; padding:5px;"">
  <tr>
    <td style=""font-weight:bold; width:30%;"">Alıcı VKN/TCKN:</td>
    <td><xsl:value-of select=""/n1:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PartyIdentification/cbc:ID"" /></td>
  </tr>
  <tr>
    <td style=""font-weight:bold;"">Unvan:</td>
    <td><xsl:value-of select=""/n1:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PartyName/cbc:Name"" /></td>
  </tr>
  <tr>
    <td style=""font-weight:bold;"">Adres:</td>
    <td>
      <xsl:value-of select=""/n1:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PostalAddress/cbc:StreetName"" />
      <xsl:text> </xsl:text>
      <xsl:value-of select=""/n1:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PostalAddress/cbc:CitySubdivisionName"" />
      / <xsl:value-of select=""/n1:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PostalAddress/cbc:CityName"" />
    </td>
  </tr>
  <tr>
    <td style=""font-weight:bold;"">Vergi Dairesi:</td>
    <td><xsl:value-of select=""/n1:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PartyTaxScheme/cac:TaxScheme/cbc:Name"" /></td>
  </tr>
</table>"),

            ["UBL_SUPPLIER_CONTACT"] = new SnippetInfo(
                key: "UBL_SUPPLIER_CONTACT",
                displayName: "Satıcı İletişim",
                iconText: "📞",
                category: "UBL-TR e-Fatura",
                subCategory: "Taraflar",
                description: "Satıcı firmanın iletişim bilgileri. Telefon, faks ve e-posta adresini tablo olarak gösterir.\nXPath: /n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:Contact (cbc:Telephone, cbc:Telefax, cbc:ElectronicMail)",
                xsltCode:
@"<!-- Satıcı İletişim Bilgileri -->
<table style=""width:100%; border:1px solid #ccc; padding:5px;"">
  <tr>
    <td style=""font-weight:bold; width:30%;"">Telefon:</td>
    <td><xsl:value-of select=""/n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:Contact/cbc:Telephone"" /></td>
  </tr>
  <tr>
    <td style=""font-weight:bold;"">Faks:</td>
    <td><xsl:value-of select=""/n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:Contact/cbc:Telefax"" /></td>
  </tr>
  <tr>
    <td style=""font-weight:bold;"">E-posta:</td>
    <td><xsl:value-of select=""/n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:Contact/cbc:ElectronicMail"" /></td>
  </tr>
</table>"),

            ["UBL_CUSTOMER_CONTACT"] = new SnippetInfo(
                key: "UBL_CUSTOMER_CONTACT",
                displayName: "Alıcı İletişim",
                iconText: "📱",
                category: "UBL-TR e-Fatura",
                subCategory: "Taraflar",
                description: "Alıcı firmanın iletişim bilgileri. Telefon, faks ve e-posta adresini tablo olarak gösterir.\nXPath: /n1:Invoice/cac:AccountingCustomerParty/cac:Party/cac:Contact (cbc:Telephone, cbc:Telefax, cbc:ElectronicMail)",
                xsltCode:
@"<!-- Alıcı İletişim Bilgileri -->
<table style=""width:100%; border:1px solid #ccc; padding:5px;"">
  <tr>
    <td style=""font-weight:bold; width:30%;"">Telefon:</td>
    <td><xsl:value-of select=""/n1:Invoice/cac:AccountingCustomerParty/cac:Party/cac:Contact/cbc:Telephone"" /></td>
  </tr>
  <tr>
    <td style=""font-weight:bold;"">Faks:</td>
    <td><xsl:value-of select=""/n1:Invoice/cac:AccountingCustomerParty/cac:Party/cac:Contact/cbc:Telefax"" /></td>
  </tr>
  <tr>
    <td style=""font-weight:bold;"">E-posta:</td>
    <td><xsl:value-of select=""/n1:Invoice/cac:AccountingCustomerParty/cac:Party/cac:Contact/cbc:ElectronicMail"" /></td>
  </tr>
</table>"),

            ["UBL_SUPPLIER_ADDRESS"] = new SnippetInfo(
                key: "UBL_SUPPLIER_ADDRESS",
                displayName: "Satıcı Detaylı Adres",
                iconText: "🏠",
                category: "UBL-TR e-Fatura",
                subCategory: "Taraflar",
                description: "Satıcı firmanın detaylı adres bilgileri. Oda, bina adı, bina no, sokak, ilçe, il, posta kodu, ülke alanlarını gösterir.\nXPath: /n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PostalAddress (Room, BuildingName, BuildingNumber, StreetName, CitySubdivisionName, CityName, PostalZone, Country)",
                xsltCode:
@"<!-- Satıcı Detaylı Adres -->
<table style=""width:100%; border:1px solid #ccc; padding:5px;"">
  <tr>
    <td style=""font-weight:bold; width:30%;"">Sokak:</td>
    <td><xsl:value-of select=""/n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PostalAddress/cbc:StreetName"" /></td>
  </tr>
  <tr>
    <td style=""font-weight:bold;"">Bina Adı:</td>
    <td><xsl:value-of select=""/n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PostalAddress/cbc:BuildingName"" /></td>
  </tr>
  <tr>
    <td style=""font-weight:bold;"">Bina No / Oda:</td>
    <td><xsl:value-of select=""/n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PostalAddress/cbc:BuildingNumber"" /> / <xsl:value-of select=""/n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PostalAddress/cbc:Room"" /></td>
  </tr>
  <tr>
    <td style=""font-weight:bold;"">İlçe / İl:</td>
    <td><xsl:value-of select=""/n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PostalAddress/cbc:CitySubdivisionName"" /> / <xsl:value-of select=""/n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PostalAddress/cbc:CityName"" /></td>
  </tr>
  <tr>
    <td style=""font-weight:bold;"">Posta Kodu:</td>
    <td><xsl:value-of select=""/n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PostalAddress/cbc:PostalZone"" /></td>
  </tr>
  <tr>
    <td style=""font-weight:bold;"">Ülke:</td>
    <td><xsl:value-of select=""/n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PostalAddress/cac:Country/cbc:Name"" /></td>
  </tr>
</table>"),

            ["UBL_CUSTOMER_ADDRESS"] = new SnippetInfo(
                key: "UBL_CUSTOMER_ADDRESS",
                displayName: "Alıcı Detaylı Adres",
                iconText: "📍",
                category: "UBL-TR e-Fatura",
                subCategory: "Taraflar",
                description: "Alıcı firmanın detaylı adres bilgileri. Oda, bina adı, bina no, sokak, ilçe, il, posta kodu, ülke alanlarını gösterir.\nXPath: /n1:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PostalAddress",
                xsltCode:
@"<!-- Alıcı Detaylı Adres -->
<table style=""width:100%; border:1px solid #ccc; padding:5px;"">
  <tr>
    <td style=""font-weight:bold; width:30%;"">Sokak:</td>
    <td><xsl:value-of select=""/n1:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PostalAddress/cbc:StreetName"" /></td>
  </tr>
  <tr>
    <td style=""font-weight:bold;"">Bina Adı:</td>
    <td><xsl:value-of select=""/n1:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PostalAddress/cbc:BuildingName"" /></td>
  </tr>
  <tr>
    <td style=""font-weight:bold;"">Bina No / Oda:</td>
    <td><xsl:value-of select=""/n1:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PostalAddress/cbc:BuildingNumber"" /> / <xsl:value-of select=""/n1:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PostalAddress/cbc:Room"" /></td>
  </tr>
  <tr>
    <td style=""font-weight:bold;"">İlçe / İl:</td>
    <td><xsl:value-of select=""/n1:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PostalAddress/cbc:CitySubdivisionName"" /> / <xsl:value-of select=""/n1:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PostalAddress/cbc:CityName"" /></td>
  </tr>
  <tr>
    <td style=""font-weight:bold;"">Posta Kodu:</td>
    <td><xsl:value-of select=""/n1:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PostalAddress/cbc:PostalZone"" /></td>
  </tr>
  <tr>
    <td style=""font-weight:bold;"">Ülke:</td>
    <td><xsl:value-of select=""/n1:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PostalAddress/cac:Country/cbc:Name"" /></td>
  </tr>
</table>"),

            ["UBL_PERSON"] = new SnippetInfo(
                key: "UBL_PERSON",
                displayName: "Kişi Bilgileri",
                iconText: "👥",
                category: "UBL-TR e-Fatura",
                subCategory: "Taraflar",
                description: "Gerçek kişi bilgileri. Unvan (Title), Ad, İkinci Ad, Soyad, Ad Soneki ve Uyruk bilgilerini gösterir. TCKN ile düzenlenen faturalarda kullanılır.\nXPath: cac:Party/cac:Person (cbc:Title, cbc:FirstName, cbc:MiddleName, cbc:FamilyName, cbc:NameSuffix, cbc:NationalityID)",
                xsltCode:
@"<!-- Kişi Bilgileri (Gerçek Kişi) -->
<xsl:for-each select=""cac:Person"">
  <xsl:if test=""cbc:Title""><xsl:value-of select=""cbc:Title"" /><xsl:text> </xsl:text></xsl:if>
  <xsl:value-of select=""cbc:FirstName"" />
  <xsl:if test=""cbc:MiddleName""><xsl:text> </xsl:text><xsl:value-of select=""cbc:MiddleName"" /></xsl:if>
  <xsl:text> </xsl:text><xsl:value-of select=""cbc:FamilyName"" />
  <xsl:if test=""cbc:NameSuffix""><xsl:text> </xsl:text><xsl:value-of select=""cbc:NameSuffix"" /></xsl:if>
</xsl:for-each>"),

            ["UBL_PARTYIDS"] = new SnippetInfo(
                key: "UBL_PARTYIDS",
                displayName: "Taraf Kimlikleri",
                iconText: "🆔",
                category: "UBL-TR e-Fatura",
                subCategory: "Taraflar",
                description: "Tarafın tüm kimlik numaralarını schemeID ile birlikte döngüyle listeler. VKN, TCKN, MERSİS No, Ticaret Sicil No gibi çoklu kimlik bilgilerini gösterir.\nXPath: cac:PartyIdentification/cbc:ID ve @schemeID",
                xsltCode:
@"<!-- Taraf Kimlik Numaraları -->
<xsl:for-each select=""cac:PartyIdentification"">
  <div>
    <span style=""font-weight:bold;""><xsl:value-of select=""cbc:ID/@schemeID"" />: </span>
    <xsl:value-of select=""cbc:ID"" />
  </div>
</xsl:for-each>"),

            ["UBL_WEBSITEURI"] = new SnippetInfo(
                key: "UBL_WEBSITEURI",
                displayName: "Web Sitesi",
                iconText: "🌐",
                category: "UBL-TR e-Fatura",
                subCategory: "Taraflar",
                description: "Firmanın web sitesi adresini gösterir. Tıklanabilir bağlantı olarak oluşturulur.\nXPath: cac:Party/cbc:WebsiteURI",
                xsltCode:
@"<xsl:if test=""cbc:WebsiteURI"">
  <a style=""color:#0066cc;""><xsl:attribute name=""href""><xsl:value-of select=""cbc:WebsiteURI"" /></xsl:attribute><xsl:value-of select=""cbc:WebsiteURI"" /></a>
</xsl:if>"),

            ["UBL_IDENTITYDOC"] = new SnippetInfo(
                key: "UBL_IDENTITYDOC",
                displayName: "Kimlik Belgesi",
                iconText: "🪪",
                category: "UBL-TR e-Fatura",
                subCategory: "Taraflar",
                description: "Kişinin kimlik belgesi referans numarasını gösterir. Gerçek kişi alıcılarda kimlik doğrulama için kullanılır.\nXPath: cac:IdentityDocumentReference/cbc:ID",
                xsltCode:
@"<xsl:if test=""cac:IdentityDocumentReference"">
  <span style=""font-weight:bold;"">Kimlik No: </span>
  <xsl:value-of select=""cac:IdentityDocumentReference/cbc:ID"" />
</xsl:if>"),

            ["UBL_BUYERCUSTOMER"] = new SnippetInfo(
                key: "UBL_BUYERCUSTOMER",
                displayName: "Mal Alan Taraf",
                iconText: "🛒",
                category: "UBL-TR e-Fatura",
                subCategory: "Taraflar",
                description: "Mal alan taraf bilgileri. AccountingCustomerParty'den farklı olarak, ihracat ve TAXFREE senaryolarında ayrı bir alıcı tanımlamak için kullanılır.\nXPath: n1:Invoice/cac:BuyerCustomerParty/cac:Party",
                xsltCode:
@"<!-- Mal Alan Taraf (BuyerCustomerParty) -->
<xsl:for-each select=""n1:Invoice/cac:BuyerCustomerParty/cac:Party"">
  <table style=""width:100%; border:1px solid #ccc; padding:5px;"">
    <tr>
      <td style=""font-weight:bold; width:30%;"">Kimlik:</td>
      <td>
        <xsl:for-each select=""cac:PartyIdentification"">
          <xsl:value-of select=""cbc:ID/@schemeID"" />: <xsl:value-of select=""cbc:ID"" /><xsl:text> </xsl:text>
        </xsl:for-each>
      </td>
    </tr>
    <tr>
      <td style=""font-weight:bold;"">Unvan:</td>
      <td><xsl:value-of select=""cac:PartyName/cbc:Name"" /></td>
    </tr>
  </table>
</xsl:for-each>"),

            ["UBL_TAXREPRESENTATIVE"] = new SnippetInfo(
                key: "UBL_TAXREPRESENTATIVE",
                displayName: "Vergi Temsilcisi",
                iconText: "🏛",
                category: "UBL-TR e-Fatura",
                subCategory: "Taraflar",
                description: "Vergi temsilcisi / aracı kurum bilgileri. ARACIKURUMVKN schemeID ile tanımlanan aracı kurum VKN'sini ve muafiyet bilgilerini gösterir.\nXPath: n1:Invoice/cac:TaxRepresentativeParty/cac:PartyIdentification/cbc:ID[@schemeID='ARACIKURUMVKN']",
                xsltCode:
@"<!-- Vergi Temsilcisi / Aracı Kurum -->
<xsl:if test=""n1:Invoice/cac:TaxRepresentativeParty"">
  <table style=""width:100%; border:1px solid #ccc; padding:5px;"">
    <tr>
      <td style=""font-weight:bold; width:30%;"">Aracı Kurum VKN:</td>
      <td><xsl:value-of select=""n1:Invoice/cac:TaxRepresentativeParty/cac:PartyIdentification/cbc:ID[@schemeID='ARACIKURUMVKN']"" /></td>
    </tr>
    <xsl:if test=""n1:Invoice/cac:TaxRepresentativeParty/cac:PartyTaxScheme/cbc:ExemptionReasonCode"">
      <tr>
        <td style=""font-weight:bold;"">Muafiyet Kodu:</td>
        <td><xsl:value-of select=""n1:Invoice/cac:TaxRepresentativeParty/cac:PartyTaxScheme/cbc:ExemptionReasonCode"" /></td>
      </tr>
    </xsl:if>
  </table>
</xsl:if>"),

            ["UBL_SELLERSUPPLIER"] = new SnippetInfo(
                key: "UBL_SELLERSUPPLIER",
                displayName: "Mal Sağlayan Taraf",
                iconText: "🏭",
                category: "UBL-TR e-Fatura",
                subCategory: "Taraflar",
                description: "Faturayı düzenleyen haricinde faturadaki mal veya hizmeti sağlayan taraf bilgisi. İhracat ve özel senaryolarda kullanılır.\nXPath: n1:Invoice/cac:SellerSupplierParty/cac:Party",
                xsltCode:
@"<!-- Mal Sağlayan Taraf (SellerSupplierParty) -->
<xsl:if test=""n1:Invoice/cac:SellerSupplierParty"">
  <xsl:for-each select=""n1:Invoice/cac:SellerSupplierParty/cac:Party"">
    <table style=""width:100%; border:1px solid #ccc; padding:5px;"">
      <tr>
        <td style=""font-weight:bold; width:30%;"">Kimlik:</td>
        <td>
          <xsl:for-each select=""cac:PartyIdentification"">
            <xsl:value-of select=""cbc:ID/@schemeID"" />: <xsl:value-of select=""cbc:ID"" /><xsl:text> </xsl:text>
          </xsl:for-each>
        </td>
      </tr>
      <tr>
        <td style=""font-weight:bold;"">Unvan:</td>
        <td><xsl:value-of select=""cac:PartyName/cbc:Name"" /></td>
      </tr>
    </table>
  </xsl:for-each>
</xsl:if>"),

            // ═══════════════════════════════════════════════════════════════
            // UBL-TR e-Fatura — Kalemler
            // ═══════════════════════════════════════════════════════════════

            ["UBL_INVOICELINES"] = new SnippetInfo(
                key: "UBL_INVOICELINES",
                displayName: "Fatura Kalemleri",
                iconText: "📋",
                category: "UBL-TR e-Fatura",
                subCategory: "Kalemler",
                description: "Fatura kalemlerini tablo halinde döngüyle listeler. Sıra No, Mal/Hizmet Adı, Miktar, Birim Fiyat, KDV Oranı, KDV Tutarı ve Tutar sütunlarını içerir.\nXPath: /n1:Invoice/cac:InvoiceLine (cbc:ID, cac:Item/cbc:Name, cbc:InvoicedQuantity, cac:Price/cbc:PriceAmount, TaxTotal, cbc:LineExtensionAmount)",
                xsltCode:
@"<!-- Fatura Kalem Döngüsü -->
<table style=""width:100%; border-collapse:collapse;"" border=""1"">
  <tr style=""background-color:#f0f0f0; font-weight:bold;"">
    <td style=""padding:4px;"">Sıra</td>
    <td style=""padding:4px;"">Mal/Hizmet</td>
    <td style=""padding:4px; text-align:right;"">Miktar</td>
    <td style=""padding:4px; text-align:right;"">Birim Fiyat</td>
    <td style=""padding:4px; text-align:right;"">KDV %</td>
    <td style=""padding:4px; text-align:right;"">KDV Tutarı</td>
    <td style=""padding:4px; text-align:right;"">Tutar</td>
  </tr>
  <xsl:for-each select=""/n1:Invoice/cac:InvoiceLine"">
    <tr>
      <td style=""padding:4px;""><xsl:value-of select=""cbc:ID"" /></td>
      <td style=""padding:4px;""><xsl:value-of select=""cac:Item/cbc:Name"" /></td>
      <td style=""padding:4px; text-align:right;""><xsl:value-of select=""cbc:InvoicedQuantity"" /></td>
      <td style=""padding:4px; text-align:right;""><xsl:value-of select=""cac:Price/cbc:PriceAmount"" /></td>
      <td style=""padding:4px; text-align:right;""><xsl:value-of select=""cac:TaxTotal/cac:TaxSubtotal/cbc:Percent"" /></td>
      <td style=""padding:4px; text-align:right;""><xsl:value-of select=""cac:TaxTotal/cbc:TaxAmount"" /></td>
      <td style=""padding:4px; text-align:right;""><xsl:value-of select=""cbc:LineExtensionAmount"" /></td>
    </tr>
  </xsl:for-each>
</table>"),

            ["UBL_LINE_ALLOWANCE"] = new SnippetInfo(
                key: "UBL_LINE_ALLOWANCE",
                displayName: "Kalem İskontosu",
                iconText: "💲",
                category: "UBL-TR e-Fatura",
                subCategory: "Kalemler",
                description: "Fatura kalemi düzeyinde iskonto/artırım bilgisini gösterir. İskonto oranı (MultiplierFactorNumeric), tutarı (Amount), baz tutarı (BaseAmount) ve nedenini (AllowanceChargeReason) içerir.\nXPath: cac:InvoiceLine/cac:AllowanceCharge (cbc:ChargeIndicator, cbc:MultiplierFactorNumeric, cbc:Amount, cbc:BaseAmount, cbc:AllowanceChargeReason)",
                xsltCode:
@"<!-- Kalem İskonto/Artırım -->
<xsl:for-each select=""cac:AllowanceCharge"">
  <div style=""font-size:10px; color:#666;"">
    <xsl:choose>
      <xsl:when test=""cbc:ChargeIndicator = 'false'"">İskonto: </xsl:when>
      <xsl:otherwise>Artırım: </xsl:otherwise>
    </xsl:choose>
    %<xsl:value-of select=""format-number(cbc:MultiplierFactorNumeric * 100, '###.##0,00', 'european')"" />
    = <xsl:value-of select=""cbc:Amount"" />
    <xsl:if test=""cbc:AllowanceChargeReason""> (<xsl:value-of select=""cbc:AllowanceChargeReason"" />)</xsl:if>
  </div>
</xsl:for-each>"),

            ["UBL_LINE_WITHHOLDING"] = new SnippetInfo(
                key: "UBL_LINE_WITHHOLDING",
                displayName: "Kalem Tevkifatı",
                iconText: "🔒",
                category: "UBL-TR e-Fatura",
                subCategory: "Kalemler",
                description: "Fatura kalemi düzeyinde tevkifat bilgisini gösterir. WithholdingTaxTotal altındaki TaxSubtotal bilgilerini döngüyle listeler.\nXPath: cac:InvoiceLine/cac:WithholdingTaxTotal/cac:TaxSubtotal",
                xsltCode:
@"<!-- Kalem Tevkifat Bilgisi -->
<xsl:for-each select=""cac:WithholdingTaxTotal/cac:TaxSubtotal"">
  <div style=""font-size:10px; color:#990000;"">
    Tevkifat: <xsl:value-of select=""cac:TaxCategory/cac:TaxScheme/cbc:Name"" />
    %<xsl:value-of select=""cbc:Percent"" />
    = <xsl:value-of select=""cbc:TaxAmount"" />
  </div>
</xsl:for-each>"),

            ["UBL_LINE_NOTE"] = new SnippetInfo(
                key: "UBL_LINE_NOTE",
                displayName: "Satır Açıklaması",
                iconText: "📝",
                category: "UBL-TR e-Fatura",
                subCategory: "Kalemler",
                description: "Fatura kalemi düzeyinde açıklama/not alanını gösterir. Her kalem için serbest metin olarak girilmiş açıklamayı içerir.\nXPath: cac:InvoiceLine/cbc:Note",
                xsltCode:
@"<!-- Satır Açıklaması (Kalem Notu) -->
<xsl:if test=""cbc:Note and cbc:Note != ''"">
  <div style=""font-size:10px; color:#555;"">
    <xsl:value-of select=""cbc:Note"" />
  </div>
</xsl:if>"),

            ["UBL_LINE_DESC"] = new SnippetInfo(
                key: "UBL_LINE_DESC",
                displayName: "Ürün Açıklaması",
                iconText: "📄",
                category: "UBL-TR e-Fatura",
                subCategory: "Kalemler",
                description: "Fatura kalemindeki mal/hizmetin detaylı açıklaması. Item/Description alanında yer alan serbest metin ürün tarifini gösterir.\nXPath: cac:InvoiceLine/cac:Item/cbc:Description",
                xsltCode:
@"<!-- Ürün Açıklaması -->
<xsl:if test=""cac:Item/cbc:Description"">
  <div style=""font-size:10px; color:#666;"">
    <xsl:value-of select=""cac:Item/cbc:Description"" />
  </div>
</xsl:if>"),

            ["UBL_LINE_UNITCODE"] = new SnippetInfo(
                key: "UBL_LINE_UNITCODE",
                displayName: "Birim Kodu",
                iconText: "📐",
                category: "UBL-TR e-Fatura",
                subCategory: "Kalemler",
                description: "Fatura kaleminin miktar birim kodunu gösterir. UN/ECE Rec. 20 birim kodları kullanılır (C62=Adet, KGM=Kilogram, LTR=Litre, MTR=Metre, BX=Kutu vb.).\nXPath: cac:InvoiceLine/cbc:InvoicedQuantity/@unitCode",
                xsltCode:
@"<xsl:value-of select=""cbc:InvoicedQuantity/@unitCode"" />"),

            ["UBL_LINE_SELLERID"] = new SnippetInfo(
                key: "UBL_LINE_SELLERID",
                displayName: "Satıcı Ürün Kodu",
                iconText: "🏭",
                category: "UBL-TR e-Fatura",
                subCategory: "Kalemler",
                description: "Satıcı tarafından ürüne verilen stok/ürün kodunu gösterir.\nXPath: cac:InvoiceLine/cac:Item/cac:SellersItemIdentification/cbc:ID",
                xsltCode:
@"<xsl:if test=""cac:Item/cac:SellersItemIdentification/cbc:ID"">
  <span style=""font-size:10px; color:#666;"">
    <xsl:value-of select=""cac:Item/cac:SellersItemIdentification/cbc:ID"" />
  </span>
</xsl:if>"),

            ["UBL_LINE_BUYERID"] = new SnippetInfo(
                key: "UBL_LINE_BUYERID",
                displayName: "Alıcı Ürün Kodu",
                iconText: "🛒",
                category: "UBL-TR e-Fatura",
                subCategory: "Kalemler",
                description: "Alıcı tarafından ürüne verilen stok/ürün kodunu gösterir.\nXPath: cac:InvoiceLine/cac:Item/cac:BuyersItemIdentification/cbc:ID",
                xsltCode:
@"<xsl:if test=""cac:Item/cac:BuyersItemIdentification/cbc:ID"">
  <span style=""font-size:10px; color:#666;"">
    <xsl:value-of select=""cac:Item/cac:BuyersItemIdentification/cbc:ID"" />
  </span>
</xsl:if>"),

            ["UBL_LINE_BRANDMODEL"] = new SnippetInfo(
                key: "UBL_LINE_BRANDMODEL",
                displayName: "Marka / Model",
                iconText: "🏷",
                category: "UBL-TR e-Fatura",
                subCategory: "Kalemler",
                description: "Fatura kalemindeki ürünün marka ve model bilgisini gösterir.\nXPath: cac:InvoiceLine/cac:Item/cbc:BrandName, cac:Item/cbc:ModelName",
                xsltCode:
@"<!-- Marka / Model -->
<xsl:if test=""cac:Item/cbc:BrandName or cac:Item/cbc:ModelName"">
  <span style=""font-size:10px;"">
    <xsl:if test=""cac:Item/cbc:BrandName"">
      <xsl:value-of select=""cac:Item/cbc:BrandName"" />
    </xsl:if>
    <xsl:if test=""cac:Item/cbc:ModelName"">
      <xsl:text> </xsl:text><xsl:value-of select=""cac:Item/cbc:ModelName"" />
    </xsl:if>
  </span>
</xsl:if>"),

            ["UBL_LINE_MANUFACTURERID"] = new SnippetInfo(
                key: "UBL_LINE_MANUFACTURERID",
                displayName: "Üretici Kodu",
                iconText: "🔧",
                category: "UBL-TR e-Fatura",
                subCategory: "Kalemler",
                description: "Üretici tarafından ürüne verilen kimlik numarasını gösterir.\nXPath: cac:InvoiceLine/cac:Item/cac:ManufacturersItemIdentification/cbc:ID",
                xsltCode:
@"<xsl:if test=""cac:Item/cac:ManufacturersItemIdentification/cbc:ID"">
  <span style=""font-size:10px; color:#666;"">
    Üretici Kodu: <xsl:value-of select=""cac:Item/cac:ManufacturersItemIdentification/cbc:ID"" />
  </span>
</xsl:if>"),

            ["UBL_LINE_ADDPROPERTY"] = new SnippetInfo(
                key: "UBL_LINE_ADDPROPERTY",
                displayName: "Ek Ürün Kimlikleri",
                iconText: "🔖",
                category: "UBL-TR e-Fatura",
                subCategory: "Kalemler",
                description: "Ürüne ait ek kimlik bilgilerini döngüyle listeler. Barkod, GTIN, seri numarası gibi ek tanımlayıcıları schemeID ile birlikte gösterir.\nXPath: cac:InvoiceLine/cac:Item/cac:AdditionalItemIdentification",
                xsltCode:
@"<!-- Ek Ürün Kimlikleri -->
<xsl:for-each select=""cac:Item/cac:AdditionalItemIdentification"">
  <div style=""font-size:10px; color:#666;"">
    <xsl:value-of select=""cbc:ID/@schemeID"" />: <xsl:value-of select=""cbc:ID"" />
  </div>
</xsl:for-each>"),

            ["UBL_LINE_COMMODITY"] = new SnippetInfo(
                key: "UBL_LINE_COMMODITY",
                displayName: "Emtia Sınıflandırması",
                iconText: "📊",
                category: "UBL-TR e-Fatura",
                subCategory: "Kalemler",
                description: "Ürünün emtia sınıflandırma kodunu gösterir. GTİP kodu veya diğer ürün sınıflandırma sistemleri için kullanılır.\nXPath: cac:InvoiceLine/cac:Item/cac:CommodityClassification/cbc:ItemClassificationCode",
                xsltCode:
@"<xsl:for-each select=""cac:Item/cac:CommodityClassification"">
  <div style=""font-size:10px; color:#666;"">
    <xsl:if test=""cbc:ItemClassificationCode/@listID"">
      <xsl:value-of select=""cbc:ItemClassificationCode/@listID"" />:
    </xsl:if>
    <xsl:value-of select=""cbc:ItemClassificationCode"" />
  </div>
</xsl:for-each>"),

            ["UBL_LINE_TAXABLEAMT"] = new SnippetInfo(
                key: "UBL_LINE_TAXABLEAMT",
                displayName: "Kalem KDV Matrahı",
                iconText: "💵",
                category: "UBL-TR e-Fatura",
                subCategory: "Kalemler",
                description: "Fatura kalemi düzeyinde vergi matrah tutarını (KDV hesaplanan tutar) gösterir.\nXPath: cac:InvoiceLine/cac:TaxTotal/cac:TaxSubtotal/cbc:TaxableAmount",
                xsltCode:
@"<xsl:value-of select=""cac:TaxTotal/cac:TaxSubtotal/cbc:TaxableAmount"" />"),

            ["UBL_LINE_KDVPERCENT"] = new SnippetInfo(
                key: "UBL_LINE_KDVPERCENT",
                displayName: "Kalem KDV Oranı",
                iconText: "📊",
                category: "UBL-TR e-Fatura",
                subCategory: "Kalemler",
                description: "Fatura kalemi düzeyinde KDV oranını (%) tek değer olarak gösterir. Kalem döngüsü içinde kullanılır.\nXPath: cac:InvoiceLine/cac:TaxTotal/cac:TaxSubtotal/cbc:Percent",
                xsltCode:
@"<xsl:value-of select=""cac:TaxTotal/cac:TaxSubtotal/cbc:Percent"" />"),

            ["UBL_LINE_ISKONTOPERCENT"] = new SnippetInfo(
                key: "UBL_LINE_ISKONTOPERCENT",
                displayName: "Kalem İskonto Oranı",
                iconText: "💲",
                category: "UBL-TR e-Fatura",
                subCategory: "Kalemler",
                description: "Fatura kalemi düzeyinde iskonto oranını (%) tek değer olarak gösterir. AllowanceCharge/ChargeIndicator='false' olan ilk iskontonun oranını verir. Kalem döngüsü içinde kullanılır.\nXPath: cac:InvoiceLine/cac:AllowanceCharge/cbc:MultiplierFactorNumeric",
                xsltCode:
@"<xsl:value-of select=""format-number(cac:AllowanceCharge[cbc:ChargeIndicator='false'][1]/cbc:MultiplierFactorNumeric * 100, '###.##0,00', 'european')"" />"),

            ["UBL_LINE_ORDERREF"] = new SnippetInfo(
                key: "UBL_LINE_ORDERREF",
                displayName: "Kalem Sipariş Ref.",
                iconText: "📦",
                category: "UBL-TR e-Fatura",
                subCategory: "Kalemler",
                description: "Fatura kalemiyle ilişkili sipariş dokümanı kalemi referansını gösterir.\nXPath: cac:InvoiceLine/cac:OrderLineReference/cbc:LineID",
                xsltCode:
@"<xsl:if test=""cac:OrderLineReference/cbc:LineID"">
  <span style=""font-size:10px; color:#666;"">
    Sipariş Kalemi: <xsl:value-of select=""cac:OrderLineReference/cbc:LineID"" />
  </span>
</xsl:if>"),

            ["UBL_LINE_DESPATCHREF"] = new SnippetInfo(
                key: "UBL_LINE_DESPATCHREF",
                displayName: "Kalem İrsaliye Ref.",
                iconText: "🚚",
                category: "UBL-TR e-Fatura",
                subCategory: "Kalemler",
                description: "Fatura kalemiyle ilişkili irsaliye dokümanı kalemi referansını gösterir.\nXPath: cac:InvoiceLine/cac:DespatchLineReference/cac:LineReference/cbc:LineID",
                xsltCode:
@"<xsl:if test=""cac:DespatchLineReference"">
  <span style=""font-size:10px; color:#666;"">
    İrsaliye Kalemi: <xsl:value-of select=""cac:DespatchLineReference/cbc:LineID"" />
  </span>
</xsl:if>"),

            ["UBL_INVOICELINES_DETAIL"] = new SnippetInfo(
                key: "UBL_INVOICELINES_DETAIL",
                displayName: "Detaylı Fatura Kalemleri",
                iconText: "📋",
                category: "UBL-TR e-Fatura",
                subCategory: "Kalemler",
                description: "Fatura kalemlerini tüm detaylarıyla tablo halinde listeler. Sıra No, Ürün Kodu, Mal/Hizmet Adı, Açıklama, Miktar, Birim, Birim Fiyat, İskonto Oranı, İskonto Tutarı, KDV Oranı, KDV Tutarı ve Tutar sütunlarını içerir.\nXPath: /n1:Invoice/cac:InvoiceLine (tüm alt elemanlar)",
                xsltCode:
@"<!-- Detaylı Fatura Kalem Döngüsü -->
<table style=""width:100%; border-collapse:collapse;"" border=""1"">
  <tr style=""background-color:#f0f0f0; font-weight:bold; font-size:11px;"">
    <td style=""padding:4px;"">Sıra</td>
    <td style=""padding:4px;"">Ürün Kodu</td>
    <td style=""padding:4px;"">Mal/Hizmet</td>
    <td style=""padding:4px;"">Açıklama</td>
    <td style=""padding:4px; text-align:right;"">Miktar</td>
    <td style=""padding:4px;"">Birim</td>
    <td style=""padding:4px; text-align:right;"">Birim Fiyat</td>
    <td style=""padding:4px; text-align:right;"">İsk.%</td>
    <td style=""padding:4px; text-align:right;"">İsk.Tutar</td>
    <td style=""padding:4px; text-align:right;"">KDV %</td>
    <td style=""padding:4px; text-align:right;"">KDV Tutarı</td>
    <td style=""padding:4px; text-align:right;"">Tutar</td>
  </tr>
  <xsl:for-each select=""/n1:Invoice/cac:InvoiceLine"">
    <tr style=""font-size:11px;"">
      <td style=""padding:4px;""><xsl:value-of select=""cbc:ID"" /></td>
      <td style=""padding:4px;""><xsl:value-of select=""cac:Item/cac:SellersItemIdentification/cbc:ID"" /></td>
      <td style=""padding:4px;""><xsl:value-of select=""cac:Item/cbc:Name"" /></td>
      <td style=""padding:4px;"">
        <xsl:if test=""cbc:Note and cbc:Note != ''""><xsl:value-of select=""cbc:Note"" /></xsl:if>
        <xsl:if test=""cac:Item/cbc:Description""><xsl:value-of select=""cac:Item/cbc:Description"" /></xsl:if>
      </td>
      <td style=""padding:4px; text-align:right;""><xsl:value-of select=""cbc:InvoicedQuantity"" /></td>
      <td style=""padding:4px;""><xsl:value-of select=""cbc:InvoicedQuantity/@unitCode"" /></td>
      <td style=""padding:4px; text-align:right;""><xsl:value-of select=""cac:Price/cbc:PriceAmount"" /></td>
      <td style=""padding:4px; text-align:right;"">
        <xsl:if test=""cac:AllowanceCharge/cbc:MultiplierFactorNumeric"">
          <xsl:value-of select=""format-number(cac:AllowanceCharge/cbc:MultiplierFactorNumeric * 100, '###.##0,00', 'european')"" />
        </xsl:if>
      </td>
      <td style=""padding:4px; text-align:right;"">
        <xsl:if test=""cac:AllowanceCharge/cbc:Amount"">
          <xsl:value-of select=""cac:AllowanceCharge/cbc:Amount"" />
        </xsl:if>
      </td>
      <td style=""padding:4px; text-align:right;""><xsl:value-of select=""cac:TaxTotal/cac:TaxSubtotal/cbc:Percent"" /></td>
      <td style=""padding:4px; text-align:right;""><xsl:value-of select=""cac:TaxTotal/cbc:TaxAmount"" /></td>
      <td style=""padding:4px; text-align:right;""><xsl:value-of select=""cbc:LineExtensionAmount"" /></td>
    </tr>
  </xsl:for-each>
</table>"),

            // ═══════════════════════════════════════════════════════════════
            // UBL-TR e-Fatura — Vergi
            // ═══════════════════════════════════════════════════════════════

            ["UBL_TAXTOTAL"] = new SnippetInfo(
                key: "UBL_TAXTOTAL",
                displayName: "Vergi Toplamları",
                iconText: "💰",
                category: "UBL-TR e-Fatura",
                subCategory: "Vergi",
                description: "Fatura düzeyinde tüm vergi alt toplamlarını döngüyle listeler. Her vergi türü için vergi adı, oranı ve tutarını gösterir.\nXPath: /n1:Invoice/cac:TaxTotal/cac:TaxSubtotal (cbc:Percent, cbc:TaxAmount, cac:TaxCategory/cac:TaxScheme/cbc:Name)",
                xsltCode:
@"<!-- Vergi Toplamları -->
<table style=""width:50%; margin-left:auto; border-collapse:collapse;"" border=""1"">
  <xsl:for-each select=""/n1:Invoice/cac:TaxTotal/cac:TaxSubtotal"">
    <tr>
      <td style=""padding:4px;""><xsl:value-of select=""cac:TaxCategory/cac:TaxScheme/cbc:Name"" /> (%<xsl:value-of select=""cbc:Percent"" />)</td>
      <td style=""padding:4px; text-align:right;""><xsl:value-of select=""cbc:TaxAmount"" /></td>
    </tr>
  </xsl:for-each>
  <tr style=""font-weight:bold;"">
    <td style=""padding:4px;"">Toplam Vergi</td>
    <td style=""padding:4px; text-align:right;""><xsl:value-of select=""/n1:Invoice/cac:TaxTotal/cbc:TaxAmount"" /></td>
  </tr>
</table>"),

            ["UBL_WITHHOLDING"] = new SnippetInfo(
                key: "UBL_WITHHOLDING",
                displayName: "Tevkifat Toplamı",
                iconText: "⚖️",
                category: "UBL-TR e-Fatura",
                subCategory: "Vergi",
                description: "Fatura düzeyinde tevkifat (stopaj) toplamlarını gösterir. WithholdingTaxTotal altındaki alt toplamları döngüyle listeler. Tevkifat faturalarında zorunlu alandır.\nXPath: n1:Invoice/cac:WithholdingTaxTotal/cac:TaxSubtotal (cac:TaxCategory/cac:TaxScheme, cbc:Percent, cbc:TaxAmount)",
                xsltCode:
@"<!-- Tevkifat Toplamları -->
<xsl:if test=""n1:Invoice/cac:WithholdingTaxTotal"">
  <table style=""width:50%; margin-left:auto; border-collapse:collapse; border:1px solid #990000;"">
    <tr style=""background-color:#fff0f0; font-weight:bold;"">
      <td style=""padding:4px;"">Tevkifat</td>
      <td style=""padding:4px; text-align:right;"">Tutar</td>
    </tr>
    <xsl:for-each select=""n1:Invoice/cac:WithholdingTaxTotal/cac:TaxSubtotal"">
      <tr>
        <td style=""padding:4px;""><xsl:value-of select=""cac:TaxCategory/cac:TaxScheme/cbc:Name"" /> (%<xsl:value-of select=""cbc:Percent"" />)</td>
        <td style=""padding:4px; text-align:right;""><xsl:value-of select=""cbc:TaxAmount"" /></td>
      </tr>
    </xsl:for-each>
  </table>
</xsl:if>"),

            ["UBL_TAXEXEMPTION"] = new SnippetInfo(
                key: "UBL_TAXEXEMPTION",
                displayName: "KDV Muafiyeti",
                iconText: "🚫",
                category: "UBL-TR e-Fatura",
                subCategory: "Vergi",
                description: "KDV istisna/muafiyet nedeni ve kodunu gösterir. İstisna faturalarında vergi alt toplamında yer alan TaxExemptionReason ve TaxExemptionReasonCode alanlarını içerir.\nXPath: cac:TaxSubtotal/cac:TaxCategory/cbc:TaxExemptionReasonCode, cbc:TaxExemptionReason",
                xsltCode:
@"<!-- KDV Muafiyet Bilgisi -->
<xsl:for-each select=""/n1:Invoice/cac:TaxTotal/cac:TaxSubtotal"">
  <xsl:if test=""cac:TaxCategory/cbc:TaxExemptionReason"">
    <div style=""padding:4px; border:1px solid #cc9900; background-color:#fff9e6; margin:2px 0;"">
      <span style=""font-weight:bold;"">Muafiyet Kodu: </span>
      <xsl:value-of select=""cac:TaxCategory/cbc:TaxExemptionReasonCode"" />
      <br />
      <span style=""font-weight:bold;"">Muafiyet Nedeni: </span>
      <xsl:value-of select=""cac:TaxCategory/cbc:TaxExemptionReason"" />
    </div>
  </xsl:if>
</xsl:for-each>"),

            ["UBL_TAXTYPEFILTER"] = new SnippetInfo(
                key: "UBL_TAXTYPEFILTER",
                displayName: "Vergi Tipi Filtresi",
                iconText: "🔍",
                category: "UBL-TR e-Fatura",
                subCategory: "Vergi",
                description: "Vergi tipine göre (TaxTypeCode) filtrelenmiş vergi toplamlarını gösterir. 0015=KDV, 9015=Tevkifatlı KDV, 4171=ÖTV kodlarını ayrı ayrı listeler.\nXPath: cac:TaxSubtotal[cac:TaxCategory/cac:TaxScheme/cbc:TaxTypeCode='0015'] vb.",
                xsltCode:
@"<!-- Vergi Tipi Koduna Göre Ayrıştırılmış Vergi Tablosu -->
<table style=""width:60%; margin-left:auto; border-collapse:collapse;"" border=""1"">
  <tr style=""background-color:#f0f0f0; font-weight:bold;"">
    <td style=""padding:4px;"">Vergi Tipi</td>
    <td style=""padding:4px; text-align:right;"">Matrah</td>
    <td style=""padding:4px; text-align:right;"">Oran</td>
    <td style=""padding:4px; text-align:right;"">Tutar</td>
  </tr>
  <xsl:for-each select=""n1:Invoice/cac:TaxTotal/cac:TaxSubtotal"">
    <tr>
      <td style=""padding:4px;"">
        <xsl:value-of select=""cac:TaxCategory/cac:TaxScheme/cbc:Name"" />
        (<xsl:value-of select=""cac:TaxCategory/cac:TaxScheme/cbc:TaxTypeCode"" />)
      </td>
      <td style=""padding:4px; text-align:right;""><xsl:value-of select=""cbc:TaxableAmount"" /></td>
      <td style=""padding:4px; text-align:right;"">%<xsl:value-of select=""cbc:Percent"" /></td>
      <td style=""padding:4px; text-align:right;""><xsl:value-of select=""cbc:TaxAmount"" /></td>
    </tr>
  </xsl:for-each>
</table>"),

            // ═══════════════════════════════════════════════════════════════
            // UBL-TR e-Fatura — Toplamlar
            // ═══════════════════════════════════════════════════════════════

            ["UBL_TOTALS"] = new SnippetInfo(
                key: "UBL_TOTALS",
                displayName: "Genel Toplam",
                iconText: "🧮",
                category: "UBL-TR e-Fatura",
                subCategory: "Toplamlar",
                description: "Faturanın genel toplam tablosu. Mal/Hizmet toplamı, vergiler hariç, vergiler dahil, iskonto toplamı ve ödenecek tutar alanlarını gösterir.\nXPath: /n1:Invoice/cac:LegalMonetaryTotal (cbc:LineExtensionAmount, TaxExclusiveAmount, TaxInclusiveAmount, AllowanceTotalAmount, PayableAmount)",
                xsltCode:
@"<!-- Genel Toplam (LegalMonetaryTotal) -->
<table style=""width:50%; margin-left:auto; border-collapse:collapse;"" border=""1"">
  <tr>
    <td style=""padding:4px;"">Mal/Hizmet Toplamı</td>
    <td style=""padding:4px; text-align:right;""><xsl:value-of select=""/n1:Invoice/cac:LegalMonetaryTotal/cbc:LineExtensionAmount"" /></td>
  </tr>
  <tr>
    <td style=""padding:4px;"">Vergiler Hariç</td>
    <td style=""padding:4px; text-align:right;""><xsl:value-of select=""/n1:Invoice/cac:LegalMonetaryTotal/cbc:TaxExclusiveAmount"" /></td>
  </tr>
  <tr>
    <td style=""padding:4px;"">Vergiler Dahil</td>
    <td style=""padding:4px; text-align:right;""><xsl:value-of select=""/n1:Invoice/cac:LegalMonetaryTotal/cbc:TaxInclusiveAmount"" /></td>
  </tr>
  <tr>
    <td style=""padding:4px;"">İndirim Toplamı</td>
    <td style=""padding:4px; text-align:right;""><xsl:value-of select=""/n1:Invoice/cac:LegalMonetaryTotal/cbc:AllowanceTotalAmount"" /></td>
  </tr>
  <tr style=""font-weight:bold; font-size:13px;"">
    <td style=""padding:4px;"">Ödenecek Tutar</td>
    <td style=""padding:4px; text-align:right;""><xsl:value-of select=""/n1:Invoice/cac:LegalMonetaryTotal/cbc:PayableAmount"" /></td>
  </tr>
</table>"),

            ["UBL_CHARGETOTAL"] = new SnippetInfo(
                key: "UBL_CHARGETOTAL",
                displayName: "Artırım Toplamı",
                iconText: "➕",
                category: "UBL-TR e-Fatura",
                subCategory: "Toplamlar",
                description: "Fatura düzeyinde artırım (charge) toplamını gösterir. AllowanceTotalAmount'ın karşılığıdır.\nXPath: n1:Invoice/cac:LegalMonetaryTotal/cbc:ChargeTotalAmount",
                xsltCode:
@"<xsl:if test=""n1:Invoice/cac:LegalMonetaryTotal/cbc:ChargeTotalAmount"">
  <span style=""font-weight:bold;"">Artırım Toplamı: </span>
  <xsl:value-of select=""n1:Invoice/cac:LegalMonetaryTotal/cbc:ChargeTotalAmount"" />
</xsl:if>"),

            ["UBL_PAYABLEROUNDING"] = new SnippetInfo(
                key: "UBL_PAYABLEROUNDING",
                displayName: "Yuvarlama Tutarı",
                iconText: "🔄",
                category: "UBL-TR e-Fatura",
                subCategory: "Toplamlar",
                description: "Fatura düzeyinde yuvarlama tutarını gösterir. Ödenecek tutardaki yuvarlamadan kaynaklanan farkı belirtir.\nXPath: n1:Invoice/cac:LegalMonetaryTotal/cbc:PayableRoundingAmount",
                xsltCode:
@"<xsl:if test=""n1:Invoice/cac:LegalMonetaryTotal/cbc:PayableRoundingAmount"">
  <span style=""font-weight:bold;"">Yuvarlama: </span>
  <xsl:value-of select=""n1:Invoice/cac:LegalMonetaryTotal/cbc:PayableRoundingAmount"" />
</xsl:if>"),

            ["UBL_ALLOWANCECHARGE"] = new SnippetInfo(
                key: "UBL_ALLOWANCECHARGE",
                displayName: "İskonto/Artırım",
                iconText: "➖",
                category: "UBL-TR e-Fatura",
                subCategory: "Toplamlar",
                description: "Fatura düzeyinde iskonto ve artırım bilgilerini döngüyle listeler. ChargeIndicator false=iskonto, true=artırım. Tutar ve nedenini gösterir.\nXPath: /n1:Invoice/cac:AllowanceCharge (cbc:ChargeIndicator, cbc:Amount, cbc:AllowanceChargeReason)",
                xsltCode:
@"<!-- Fatura Düzeyinde İskonto/Artırım -->
<xsl:for-each select=""cac:AllowanceCharge"">
  <div style=""padding:3px; border-bottom:1px solid #eee;"">
    <xsl:choose>
      <xsl:when test=""cbc:ChargeIndicator = 'false'"">
        <span style=""color:green;"">İskonto: </span>
      </xsl:when>
      <xsl:otherwise>
        <span style=""color:red;"">Artırım: </span>
      </xsl:otherwise>
    </xsl:choose>
    <xsl:value-of select=""cbc:Amount"" />
    <xsl:if test=""cbc:AllowanceChargeReason"">
      (<xsl:value-of select=""cbc:AllowanceChargeReason"" />)
    </xsl:if>
  </div>
</xsl:for-each>"),

            ["UBL_EXCHANGERATE"] = new SnippetInfo(
                key: "UBL_EXCHANGERATE",
                displayName: "Döviz Kuru",
                iconText: "💹",
                category: "UBL-TR e-Fatura",
                subCategory: "Toplamlar",
                description: "Dövizli faturalarda kur bilgisini gösterir. Kaynak para birimi, hedef para birimi (TRY) ve hesaplama kurunu içerir. Dövizli toplam hesaplamasında kullanılır.\nXPath: n1:Invoice/cac:PricingExchangeRate/cbc:CalculationRate, cbc:SourceCurrencyCode, cbc:TargetCurrencyCode",
                xsltCode:
@"<!-- Döviz Kuru Bilgisi -->
<xsl:if test=""/n1:Invoice/cac:PricingExchangeRate"">
  <table style=""width:50%; margin-left:auto; border:1px solid #0066cc; padding:5px;"">
    <tr>
      <td style=""font-weight:bold;"">Döviz Kuru:</td>
      <td style=""text-align:right;"">
        1 <xsl:value-of select=""/n1:Invoice/cbc:DocumentCurrencyCode"" />
        = <xsl:value-of select=""/n1:Invoice/cac:PricingExchangeRate/cbc:CalculationRate"" /> TRY
      </td>
    </tr>
  </table>
</xsl:if>"),

            // ═══════════════════════════════════════════════════════════════
            // UBL-TR e-Fatura — Ödeme
            // ═══════════════════════════════════════════════════════════════

            ["UBL_PAYMENTMEANS"] = new SnippetInfo(
                key: "UBL_PAYMENTMEANS",
                displayName: "Ödeme Bilgileri",
                iconText: "💳",
                category: "UBL-TR e-Fatura",
                subCategory: "Ödeme",
                description: "Ödeme şekli ve banka hesap bilgilerini döngüyle listeler. Vade tarihi, talimat notu ve hesap bilgilerini gösterir.\nXPath: n1:Invoice/cac:PaymentMeans (cbc:PaymentDueDate, cbc:InstructionNote, cac:PayeeFinancialAccount/cbc:PaymentNote)",
                xsltCode:
@"<!-- Ödeme Bilgileri -->
<xsl:for-each select=""n1:Invoice/cac:PaymentMeans"">
  <table style=""width:100%; border:1px solid #ccc; padding:5px; margin:3px 0;"">
    <xsl:if test=""cbc:PaymentDueDate"">
      <tr>
        <td style=""font-weight:bold; width:30%;"">Vade Tarihi:</td>
        <td><xsl:value-of select=""cbc:PaymentDueDate"" /></td>
      </tr>
    </xsl:if>
    <xsl:if test=""cbc:InstructionNote"">
      <tr>
        <td style=""font-weight:bold;"">Talimat:</td>
        <td><xsl:value-of select=""cbc:InstructionNote"" /></td>
      </tr>
    </xsl:if>
    <xsl:if test=""cac:PayeeFinancialAccount/cbc:PaymentNote"">
      <tr>
        <td style=""font-weight:bold;"">Hesap Bilgisi:</td>
        <td><xsl:value-of select=""cac:PayeeFinancialAccount/cbc:PaymentNote"" /></td>
      </tr>
    </xsl:if>
  </table>
</xsl:for-each>"),

            ["UBL_PAYMENTTERMS"] = new SnippetInfo(
                key: "UBL_PAYMENTTERMS",
                displayName: "Ödeme Koşulları",
                iconText: "📅",
                category: "UBL-TR e-Fatura",
                subCategory: "Ödeme",
                description: "Ödeme koşulları ve vade notunu gösterir.\nXPath: //n1:Invoice/cac:PaymentTerms/cbc:Note",
                xsltCode:
@"<xsl:if test=""//n1:Invoice/cac:PaymentTerms/cbc:Note"">
  <div style=""padding:4px; border:1px solid #ccc; background-color:#f9f9f9;"">
    <span style=""font-weight:bold;"">Ödeme Koşulları: </span>
    <xsl:value-of select=""//n1:Invoice/cac:PaymentTerms/cbc:Note"" />
  </div>
</xsl:if>"),

            ["UBL_PAYMENTTERMS_DETAIL"] = new SnippetInfo(
                key: "UBL_PAYMENTTERMS_DETAIL",
                displayName: "Ödeme Koşulları Detay",
                iconText: "📋",
                category: "UBL-TR e-Fatura",
                subCategory: "Ödeme",
                description: "Ödeme koşullarının tüm detaylarını gösterir. Not, gecikme ceza oranı, gecikme ceza tutarı, ödeme tutarı ve son ödeme tarihini içerir.\nXPath: //n1:Invoice/cac:PaymentTerms (cbc:Note, cbc:PenaltySurchargePercent, cbc:PenaltyAmount, cbc:Amount, cbc:PaymentDueDate)",
                xsltCode:
@"<!-- Ödeme Koşulları Detay -->
<xsl:if test=""//n1:Invoice/cac:PaymentTerms"">
  <table style=""width:100%; border:1px solid #ccc; padding:5px;"">
    <xsl:if test=""//n1:Invoice/cac:PaymentTerms/cbc:Note"">
      <tr>
        <td style=""font-weight:bold; width:30%;"">Ödeme Notu:</td>
        <td><xsl:value-of select=""//n1:Invoice/cac:PaymentTerms/cbc:Note"" /></td>
      </tr>
    </xsl:if>
    <xsl:if test=""//n1:Invoice/cac:PaymentTerms/cbc:PenaltySurchargePercent"">
      <tr>
        <td style=""font-weight:bold;"">Gecikme Ceza Oranı:</td>
        <td>%<xsl:value-of select=""//n1:Invoice/cac:PaymentTerms/cbc:PenaltySurchargePercent"" /></td>
      </tr>
    </xsl:if>
    <xsl:if test=""//n1:Invoice/cac:PaymentTerms/cbc:PenaltyAmount"">
      <tr>
        <td style=""font-weight:bold;"">Gecikme Ceza Tutarı:</td>
        <td><xsl:value-of select=""//n1:Invoice/cac:PaymentTerms/cbc:PenaltyAmount"" /></td>
      </tr>
    </xsl:if>
    <xsl:if test=""//n1:Invoice/cac:PaymentTerms/cbc:Amount"">
      <tr>
        <td style=""font-weight:bold;"">Ödeme Tutarı:</td>
        <td><xsl:value-of select=""//n1:Invoice/cac:PaymentTerms/cbc:Amount"" /></td>
      </tr>
    </xsl:if>
    <xsl:if test=""//n1:Invoice/cac:PaymentTerms/cbc:PaymentDueDate"">
      <tr>
        <td style=""font-weight:bold;"">Son Ödeme Tarihi:</td>
        <td><xsl:value-of select=""//n1:Invoice/cac:PaymentTerms/cbc:PaymentDueDate"" /></td>
      </tr>
    </xsl:if>
  </table>
</xsl:if>"),

            ["UBL_PAYMENTMEANS_ACCOUNT"] = new SnippetInfo(
                key: "UBL_PAYMENTMEANS_ACCOUNT",
                displayName: "Banka Hesap Bilgileri",
                iconText: "🏦",
                category: "UBL-TR e-Fatura",
                subCategory: "Ödeme",
                description: "Ödeme şekli ile birlikte banka hesap bilgilerini detaylı gösterir. IBAN, para birimi, ödeme notu ve ödeme kanal kodu alanlarını içerir.\nXPath: n1:Invoice/cac:PaymentMeans/cac:PayeeFinancialAccount (cbc:ID, cbc:CurrencyCode, cbc:PaymentNote)",
                xsltCode:
@"<!-- Banka Hesap Bilgileri -->
<xsl:for-each select=""n1:Invoice/cac:PaymentMeans"">
  <table style=""width:100%; border:1px solid #ccc; padding:5px; margin:3px 0;"">
    <tr>
      <td style=""font-weight:bold; width:30%;"">Ödeme Şekli:</td>
      <td><xsl:value-of select=""cbc:PaymentMeansCode"" /></td>
    </tr>
    <xsl:if test=""cbc:PaymentChannelCode"">
      <tr>
        <td style=""font-weight:bold;"">Ödeme Kanalı:</td>
        <td><xsl:value-of select=""cbc:PaymentChannelCode"" /></td>
      </tr>
    </xsl:if>
    <xsl:if test=""cbc:PaymentDueDate"">
      <tr>
        <td style=""font-weight:bold;"">Vade Tarihi:</td>
        <td><xsl:value-of select=""cbc:PaymentDueDate"" /></td>
      </tr>
    </xsl:if>
    <xsl:if test=""cac:PayeeFinancialAccount/cbc:ID"">
      <tr>
        <td style=""font-weight:bold;"">IBAN / Hesap No:</td>
        <td><xsl:value-of select=""cac:PayeeFinancialAccount/cbc:ID"" /></td>
      </tr>
    </xsl:if>
    <xsl:if test=""cac:PayeeFinancialAccount/cbc:CurrencyCode"">
      <tr>
        <td style=""font-weight:bold;"">Hesap Para Birimi:</td>
        <td><xsl:value-of select=""cac:PayeeFinancialAccount/cbc:CurrencyCode"" /></td>
      </tr>
    </xsl:if>
    <xsl:if test=""cac:PayeeFinancialAccount/cbc:PaymentNote"">
      <tr>
        <td style=""font-weight:bold;"">Ödeme Açıklaması:</td>
        <td><xsl:value-of select=""cac:PayeeFinancialAccount/cbc:PaymentNote"" /></td>
      </tr>
    </xsl:if>
    <xsl:if test=""cbc:InstructionNote"">
      <tr>
        <td style=""font-weight:bold;"">Talimat Notu:</td>
        <td><xsl:value-of select=""cbc:InstructionNote"" /></td>
      </tr>
    </xsl:if>
  </table>
</xsl:for-each>"),

            // ═══════════════════════════════════════════════════════════════
            // UBL-TR e-Fatura — Referanslar
            // ═══════════════════════════════════════════════════════════════

            ["UBL_ORDERREF"] = new SnippetInfo(
                key: "UBL_ORDERREF",
                displayName: "Sipariş Referansı",
                iconText: "📦",
                category: "UBL-TR e-Fatura",
                subCategory: "Referanslar",
                description: "Faturanın bağlı olduğu sipariş numarası ve tarihini gösterir.\nXPath: n1:Invoice/cac:OrderReference/cbc:ID, cbc:IssueDate",
                xsltCode:
@"<xsl:if test=""n1:Invoice/cac:OrderReference"">
  <span style=""font-weight:bold;"">Sipariş No: </span>
  <xsl:value-of select=""n1:Invoice/cac:OrderReference/cbc:ID"" />
  <xsl:if test=""n1:Invoice/cac:OrderReference/cbc:IssueDate"">
    <span> — Tarih: <xsl:value-of select=""n1:Invoice/cac:OrderReference/cbc:IssueDate"" /></span>
  </xsl:if>
</xsl:if>"),

            ["UBL_DESPATCHREF"] = new SnippetInfo(
                key: "UBL_DESPATCHREF",
                displayName: "İrsaliye Referansı",
                iconText: "🚚",
                category: "UBL-TR e-Fatura",
                subCategory: "Referanslar",
                description: "Faturaya bağlı irsaliye numarası ve tarihini döngüyle listeler. Birden fazla irsaliye referansı olabilir.\nXPath: n1:Invoice/cac:DespatchDocumentReference (cbc:ID, cbc:IssueDate)",
                xsltCode:
@"<!-- İrsaliye Referansları -->
<xsl:for-each select=""n1:Invoice/cac:DespatchDocumentReference"">
  <div>
    <span style=""font-weight:bold;"">İrsaliye No: </span>
    <xsl:value-of select=""cbc:ID"" />
    <xsl:if test=""cbc:IssueDate"">
      <span> — Tarih: <xsl:value-of select=""cbc:IssueDate"" /></span>
    </xsl:if>
  </div>
</xsl:for-each>"),

            ["UBL_BILLINGREF"] = new SnippetInfo(
                key: "UBL_BILLINGREF",
                displayName: "Fatura Referansı",
                iconText: "📑",
                category: "UBL-TR e-Fatura",
                subCategory: "Referanslar",
                description: "İade faturalarında referans verilen önceki fatura bilgisi. Önceki fatura numarası ve tarihini gösterir.\nXPath: n1:Invoice/cac:BillingReference/cac:InvoiceDocumentReference (cbc:ID, cbc:IssueDate)",
                xsltCode:
@"<xsl:if test=""n1:Invoice/cac:BillingReference"">
  <div>
    <span style=""font-weight:bold;"">Referans Fatura No: </span>
    <xsl:value-of select=""n1:Invoice/cac:BillingReference/cac:InvoiceDocumentReference/cbc:ID"" />
    <xsl:if test=""n1:Invoice/cac:BillingReference/cac:InvoiceDocumentReference/cbc:IssueDate"">
      <span> — Tarih: <xsl:value-of select=""n1:Invoice/cac:BillingReference/cac:InvoiceDocumentReference/cbc:IssueDate"" /></span>
    </xsl:if>
  </div>
</xsl:if>"),

            ["UBL_ADDITIONALDOC"] = new SnippetInfo(
                key: "UBL_ADDITIONALDOC",
                displayName: "Ek Belgeler",
                iconText: "📎",
                category: "UBL-TR e-Fatura",
                subCategory: "Referanslar",
                description: "Faturaya eklenmiş ek belgeleri döngüyle listeler. XSLT dosyası, resim, PDF gibi gömülü ekleri ve belge tipini gösterir.\nXPath: n1:Invoice/cac:AdditionalDocumentReference (cbc:ID, cbc:IssueDate, cbc:DocumentType)",
                xsltCode:
@"<!-- Ek Belgeler -->
<xsl:for-each select=""n1:Invoice/cac:AdditionalDocumentReference"">
  <div style=""padding:2px 0;"">
    <span style=""font-weight:bold;"">Ek Belge: </span>
    <xsl:value-of select=""cbc:ID"" />
    <xsl:if test=""cbc:DocumentType""> — <xsl:value-of select=""cbc:DocumentType"" /></xsl:if>
    <xsl:if test=""cbc:IssueDate""> — <xsl:value-of select=""cbc:IssueDate"" /></xsl:if>
  </div>
</xsl:for-each>"),

            ["UBL_CONTRACTREF"] = new SnippetInfo(
                key: "UBL_CONTRACTREF",
                displayName: "Kontrat Referansı",
                iconText: "📜",
                category: "UBL-TR e-Fatura",
                subCategory: "Referanslar",
                description: "Faturanın bağlı olduğu kontrat/sözleşme bilgisini gösterir.\nXPath: n1:Invoice/cac:ContractDocumentReference (cbc:ID, cbc:IssueDate)",
                xsltCode:
@"<xsl:if test=""n1:Invoice/cac:ContractDocumentReference"">
  <div>
    <span style=""font-weight:bold;"">Kontrat No: </span>
    <xsl:value-of select=""n1:Invoice/cac:ContractDocumentReference/cbc:ID"" />
    <xsl:if test=""n1:Invoice/cac:ContractDocumentReference/cbc:IssueDate"">
      <span> — Tarih: <xsl:value-of select=""n1:Invoice/cac:ContractDocumentReference/cbc:IssueDate"" /></span>
    </xsl:if>
  </div>
</xsl:if>"),

            ["UBL_RECEIPTREF"] = new SnippetInfo(
                key: "UBL_RECEIPTREF",
                displayName: "Alındı Referansı",
                iconText: "📥",
                category: "UBL-TR e-Fatura",
                subCategory: "Referanslar",
                description: "Faturaya bağlı alındı belge numarası ve tarihini döngüyle listeler.\nXPath: n1:Invoice/cac:ReceiptDocumentReference (cbc:ID, cbc:IssueDate)",
                xsltCode:
@"<!-- Alındı Referansları -->
<xsl:for-each select=""n1:Invoice/cac:ReceiptDocumentReference"">
  <div>
    <span style=""font-weight:bold;"">Alındı No: </span>
    <xsl:value-of select=""cbc:ID"" />
    <xsl:if test=""cbc:IssueDate"">
      <span> — Tarih: <xsl:value-of select=""cbc:IssueDate"" /></span>
    </xsl:if>
  </div>
</xsl:for-each>"),

            ["UBL_ORIGINATORDOC"] = new SnippetInfo(
                key: "UBL_ORIGINATORDOC",
                displayName: "Başlangıç Dokümanı",
                iconText: "📄",
                category: "UBL-TR e-Fatura",
                subCategory: "Referanslar",
                description: "Faturanın düzenlenmesine referans teşkil eden başlangıç belgelerine ait bilgileri gösterir.\nXPath: n1:Invoice/cac:OriginatorDocumentReference (cbc:ID, cbc:IssueDate)",
                xsltCode:
@"<xsl:if test=""n1:Invoice/cac:OriginatorDocumentReference"">
  <div>
    <span style=""font-weight:bold;"">Başlangıç Doküman No: </span>
    <xsl:value-of select=""n1:Invoice/cac:OriginatorDocumentReference/cbc:ID"" />
    <xsl:if test=""n1:Invoice/cac:OriginatorDocumentReference/cbc:IssueDate"">
      <span> — Tarih: <xsl:value-of select=""n1:Invoice/cac:OriginatorDocumentReference/cbc:IssueDate"" /></span>
    </xsl:if>
  </div>
</xsl:if>"),

            // ═══════════════════════════════════════════════════════════════
            // UBL-TR e-Arşiv — Teslimat
            // ═══════════════════════════════════════════════════════════════

            ["UBL_EA_DELIVERY"] = new SnippetInfo(
                key: "UBL_EA_DELIVERY",
                displayName: "Teslimat Bilgisi",
                iconText: "🚛",
                category: "UBL-TR e-Arşiv",
                subCategory: "Teslimat",
                description: "E-Arşiv faturalarında teslimat bilgileri. Teslimat tarihi ve teslimat adresini gösterir.\nXPath: cac:Delivery (cbc:ActualDeliveryDate, cac:DeliveryAddress)",
                xsltCode:
@"<!-- Teslimat Bilgileri (e-Arşiv) -->
<xsl:if test=""//n1:Invoice/cac:Delivery"">
  <table style=""width:100%; border:1px solid #ccc; padding:5px;"">
    <xsl:if test=""//n1:Invoice/cac:Delivery/cbc:ActualDeliveryDate"">
      <tr>
        <td style=""font-weight:bold; width:30%;"">Teslimat Tarihi:</td>
        <td><xsl:value-of select=""//n1:Invoice/cac:Delivery/cbc:ActualDeliveryDate"" /></td>
      </tr>
    </xsl:if>
    <xsl:if test=""//n1:Invoice/cac:Delivery/cac:DeliveryAddress"">
      <tr>
        <td style=""font-weight:bold;"">Teslimat Adresi:</td>
        <td>
          <xsl:value-of select=""//n1:Invoice/cac:Delivery/cac:DeliveryAddress/cbc:StreetName"" />
          <xsl:text> </xsl:text>
          <xsl:value-of select=""//n1:Invoice/cac:Delivery/cac:DeliveryAddress/cbc:CitySubdivisionName"" />
          / <xsl:value-of select=""//n1:Invoice/cac:Delivery/cac:DeliveryAddress/cbc:CityName"" />
        </td>
      </tr>
    </xsl:if>
  </table>
</xsl:if>"),

            ["UBL_EA_DELIVERYDATE"] = new SnippetInfo(
                key: "UBL_EA_DELIVERYDATE",
                displayName: "Teslimat Tarihi",
                iconText: "🗓",
                category: "UBL-TR e-Arşiv",
                subCategory: "Teslimat",
                description: "Fiili teslimat tarihini gösterir. E-Arşiv faturalarında malın teslim edildiği tarihtir.\nXPath: cbc:ActualDeliveryDate",
                xsltCode:
@"<xsl:value-of select=""//n1:Invoice/cac:Delivery/cbc:ActualDeliveryDate"" />"),

            ["UBL_EA_SHIPMENT"] = new SnippetInfo(
                key: "UBL_EA_SHIPMENT",
                displayName: "Sevkiyat Bilgisi",
                iconText: "🚢",
                category: "UBL-TR e-Arşiv",
                subCategory: "Teslimat",
                description: "Sevkiyat (shipment) bilgilerini gösterir. İhracat faturalarında gümrük bilgileri, navlun ve taşıma detayları için kullanılır.\nXPath: cac:Delivery/cac:Shipment",
                xsltCode:
@"<!-- Sevkiyat Bilgileri (e-Arşiv) -->
<xsl:if test=""//n1:Invoice/cac:Delivery/cac:Shipment"">
  <table style=""width:100%; border:1px solid #ccc; padding:5px;"">
    <tr>
      <td style=""font-weight:bold; width:30%;"">Sevkiyat ID:</td>
      <td><xsl:value-of select=""//n1:Invoice/cac:Delivery/cac:Shipment/cbc:ID"" /></td>
    </tr>
  </table>
</xsl:if>"),

            ["UBL_EA_CARRIER"] = new SnippetInfo(
                key: "UBL_EA_CARRIER",
                displayName: "Taşıyıcı Bilgisi",
                iconText: "🏗",
                category: "UBL-TR e-Arşiv",
                subCategory: "Teslimat",
                description: "Taşıyıcı firma bilgilerini gösterir. E-Arşiv faturalarında malı taşıyan kargo/nakliye firması bilgisi.\nXPath: cac:CarrierParty (PartyIdentification, PartyName, PostalAddress)",
                xsltCode:
@"<!-- Taşıyıcı Bilgileri (e-Arşiv) -->
<xsl:for-each select=""cac:CarrierParty"">
  <table style=""width:100%; border:1px solid #ccc; padding:5px;"">
    <tr>
      <td style=""font-weight:bold; width:30%;"">Taşıyıcı VKN:</td>
      <td>
        <xsl:for-each select=""cac:PartyIdentification"">
          <xsl:value-of select=""cbc:ID"" /><xsl:text> </xsl:text>
        </xsl:for-each>
      </td>
    </tr>
    <tr>
      <td style=""font-weight:bold;"">Taşıyıcı Unvan:</td>
      <td><xsl:value-of select=""cac:PartyName/cbc:Name"" /></td>
    </tr>
  </table>
</xsl:for-each>"),

            // ═══════════════════════════════════════════════════════════════
            // UBL-TR e-Arşiv — E-Arşiv Özel
            // ═══════════════════════════════════════════════════════════════

            ["UBL_EA_INTERNETSALES"] = new SnippetInfo(
                key: "UBL_EA_INTERNETSALES",
                displayName: "İnternet Satışı",
                iconText: "🛒",
                category: "UBL-TR e-Arşiv",
                subCategory: "E-Arşiv Özel",
                description: "E-Arşiv faturalarında internet üzerinden yapılan satış bilgileri. Web sitesi URL'si, ödeme şekli ve ödeme tarihi alanlarını içerir.",
                xsltCode:
@"<!-- İnternet Satış Bilgisi (e-Arşiv) -->
<xsl:if test=""//n1:Invoice/cbc:ProfileID = 'EARSIVFATURA'"">
  <div style=""padding:4px; border:1px solid #6633cc; background-color:#f5f0ff;"">
    <span style=""font-weight:bold;"">İnternet Satışı</span>
  </div>
</xsl:if>"),

            ["UBL_EA_SENDINGTYPE"] = new SnippetInfo(
                key: "UBL_EA_SENDINGTYPE",
                displayName: "Gönderim Tipi",
                iconText: "📬",
                category: "UBL-TR e-Arşiv",
                subCategory: "E-Arşiv Özel",
                description: "E-Arşiv faturasının gönderim tipini belirler. KAGIT=Kağıt ortamında teslim, ELEKTRONIK=Elektronik ortamda gönderim.",
                xsltCode:
@"<!-- Gönderim Tipi (e-Arşiv) -->
<xsl:choose>
  <xsl:when test=""//n1:Invoice/cbc:ProfileID = 'EARSIVFATURA'"">
    <span style=""font-weight:bold; color:#6633cc;"">E-ARŞİV</span>
  </xsl:when>
</xsl:choose>"),

            ["UBL_EA_PAYMENTCHANNEL"] = new SnippetInfo(
                key: "UBL_EA_PAYMENTCHANNEL",
                displayName: "Ödeme Kanalı",
                iconText: "🏧",
                category: "UBL-TR e-Arşiv",
                subCategory: "E-Arşiv Özel",
                description: "E-Arşiv faturalarında ödeme kanal kodunu gösterir. Banka, kredi kartı, nakit gibi ödeme kanalı bilgisini içerir.\nXPath: cac:PaymentMeans/cbc:PaymentChannelCode",
                xsltCode:
@"<xsl:if test=""//n1:Invoice/cac:PaymentMeans/cbc:PaymentChannelCode"">
  <span style=""font-weight:bold;"">Ödeme Kanalı: </span>
  <xsl:value-of select=""//n1:Invoice/cac:PaymentMeans/cbc:PaymentChannelCode"" />
</xsl:if>"),

            ["UBL_EA_PAYMENTCODE"] = new SnippetInfo(
                key: "UBL_EA_PAYMENTCODE",
                displayName: "Ödeme Şekil Kodu",
                iconText: "💲",
                category: "UBL-TR e-Arşiv",
                subCategory: "E-Arşiv Özel",
                description: "E-Arşiv faturalarında ödeme şekil kodunu gösterir. UBL-TR PaymentMeansCode değerlerini (Nakit, Çek, Havale, Kredi Kartı vb.) içerir.\nXPath: cac:PaymentMeans/cbc:PaymentMeansCode",
                xsltCode:
@"<xsl:if test=""//n1:Invoice/cac:PaymentMeans/cbc:PaymentMeansCode"">
  <span style=""font-weight:bold;"">Ödeme Şekli: </span>
  <xsl:value-of select=""//n1:Invoice/cac:PaymentMeans/cbc:PaymentMeansCode"" />
</xsl:if>"),

            // ═══════════════════════════════════════════════════════════════
            // UBL-TR Standalone — Başlık Değerleri
            // ═══════════════════════════════════════════════════════════════

            ["UBL_ID"] = new SnippetInfo(
                key: "UBL_ID",
                displayName: "Fatura No",
                iconText: "🔢",
                category: "UBL-TR e-Fatura",
                subCategory: "Başlık",
                description: "Fatura numarasını tek değer olarak gösterir. Belgenin benzersiz sıra numarasıdır.\nXPath: /n1:Invoice/cbc:ID",
                xsltCode:
@"<xsl:value-of select=""/n1:Invoice/cbc:ID"" />"),

            ["UBL_ISSUEDATE"] = new SnippetInfo(
                key: "UBL_ISSUEDATE",
                displayName: "Fatura Tarihi",
                iconText: "📅",
                category: "UBL-TR e-Fatura",
                subCategory: "Başlık",
                description: "Faturanın düzenleme tarihini tek değer olarak gösterir. YYYY-MM-DD formatındadır.\nXPath: /n1:Invoice/cbc:IssueDate",
                xsltCode:
@"<xsl:value-of select=""/n1:Invoice/cbc:IssueDate"" />"),

            // ═══════════════════════════════════════════════════════════════
            // UBL-TR Standalone — Taraf Değerleri
            // ═══════════════════════════════════════════════════════════════

            ["UBL_SUPPLIER_VKN"] = new SnippetInfo(
                key: "UBL_SUPPLIER_VKN",
                displayName: "Satıcı VKN/TCKN",
                iconText: "🆔",
                category: "UBL-TR e-Fatura",
                subCategory: "Taraflar",
                description: "Satıcı firmanın VKN veya TCKN numarasını tek değer olarak gösterir.\nXPath: /n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PartyIdentification/cbc:ID",
                xsltCode:
@"<xsl:value-of select=""/n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PartyIdentification/cbc:ID"" />"),

            ["UBL_SUPPLIER_NAME"] = new SnippetInfo(
                key: "UBL_SUPPLIER_NAME",
                displayName: "Satıcı Unvan",
                iconText: "🏢",
                category: "UBL-TR e-Fatura",
                subCategory: "Taraflar",
                description: "Satıcı firmanın ticari unvanını tek değer olarak gösterir.\nXPath: /n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PartyName/cbc:Name",
                xsltCode:
@"<xsl:value-of select=""/n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PartyName/cbc:Name"" />"),

            ["UBL_SUPPLIER_TAXOFFICE"] = new SnippetInfo(
                key: "UBL_SUPPLIER_TAXOFFICE",
                displayName: "Satıcı Vergi Dairesi",
                iconText: "🏛",
                category: "UBL-TR e-Fatura",
                subCategory: "Taraflar",
                description: "Satıcı firmanın bağlı olduğu vergi dairesinin adını tek değer olarak gösterir.\nXPath: /n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PartyTaxScheme/cac:TaxScheme/cbc:Name",
                xsltCode:
@"<xsl:value-of select=""/n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PartyTaxScheme/cac:TaxScheme/cbc:Name"" />"),

            ["UBL_SUPPLIER_PHONE"] = new SnippetInfo(
                key: "UBL_SUPPLIER_PHONE",
                displayName: "Satıcı Telefon",
                iconText: "📞",
                category: "UBL-TR e-Fatura",
                subCategory: "Taraflar",
                description: "Satıcı firmanın telefon numarasını tek değer olarak gösterir.\nXPath: /n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:Contact/cbc:Telephone",
                xsltCode:
@"<xsl:value-of select=""/n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:Contact/cbc:Telephone"" />"),

            ["UBL_SUPPLIER_EMAIL"] = new SnippetInfo(
                key: "UBL_SUPPLIER_EMAIL",
                displayName: "Satıcı E-posta",
                iconText: "📧",
                category: "UBL-TR e-Fatura",
                subCategory: "Taraflar",
                description: "Satıcı firmanın e-posta adresini tek değer olarak gösterir.\nXPath: /n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:Contact/cbc:ElectronicMail",
                xsltCode:
@"<xsl:value-of select=""/n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:Contact/cbc:ElectronicMail"" />"),

            ["UBL_CUSTOMER_VKN"] = new SnippetInfo(
                key: "UBL_CUSTOMER_VKN",
                displayName: "Alıcı VKN/TCKN",
                iconText: "🆔",
                category: "UBL-TR e-Fatura",
                subCategory: "Taraflar",
                description: "Alıcı firmanın VKN veya TCKN numarasını tek değer olarak gösterir.\nXPath: /n1:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PartyIdentification/cbc:ID",
                xsltCode:
@"<xsl:value-of select=""/n1:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PartyIdentification/cbc:ID"" />"),

            ["UBL_CUSTOMER_NAME"] = new SnippetInfo(
                key: "UBL_CUSTOMER_NAME",
                displayName: "Alıcı Unvan",
                iconText: "👤",
                category: "UBL-TR e-Fatura",
                subCategory: "Taraflar",
                description: "Alıcı firmanın ticari unvanını tek değer olarak gösterir.\nXPath: /n1:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PartyName/cbc:Name",
                xsltCode:
@"<xsl:value-of select=""/n1:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PartyName/cbc:Name"" />"),

            ["UBL_CUSTOMER_TAXOFFICE"] = new SnippetInfo(
                key: "UBL_CUSTOMER_TAXOFFICE",
                displayName: "Alıcı Vergi Dairesi",
                iconText: "🏛",
                category: "UBL-TR e-Fatura",
                subCategory: "Taraflar",
                description: "Alıcı firmanın bağlı olduğu vergi dairesinin adını tek değer olarak gösterir.\nXPath: /n1:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PartyTaxScheme/cac:TaxScheme/cbc:Name",
                xsltCode:
@"<xsl:value-of select=""/n1:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PartyTaxScheme/cac:TaxScheme/cbc:Name"" />"),

            ["UBL_CUSTOMER_PHONE"] = new SnippetInfo(
                key: "UBL_CUSTOMER_PHONE",
                displayName: "Alıcı Telefon",
                iconText: "📱",
                category: "UBL-TR e-Fatura",
                subCategory: "Taraflar",
                description: "Alıcı firmanın telefon numarasını tek değer olarak gösterir.\nXPath: /n1:Invoice/cac:AccountingCustomerParty/cac:Party/cac:Contact/cbc:Telephone",
                xsltCode:
@"<xsl:value-of select=""/n1:Invoice/cac:AccountingCustomerParty/cac:Party/cac:Contact/cbc:Telephone"" />"),

            ["UBL_CUSTOMER_EMAIL"] = new SnippetInfo(
                key: "UBL_CUSTOMER_EMAIL",
                displayName: "Alıcı E-posta",
                iconText: "📧",
                category: "UBL-TR e-Fatura",
                subCategory: "Taraflar",
                description: "Alıcı firmanın e-posta adresini tek değer olarak gösterir.\nXPath: /n1:Invoice/cac:AccountingCustomerParty/cac:Party/cac:Contact/cbc:ElectronicMail",
                xsltCode:
@"<xsl:value-of select=""/n1:Invoice/cac:AccountingCustomerParty/cac:Party/cac:Contact/cbc:ElectronicMail"" />"),

            // ═══════════════════════════════════════════════════════════════
            // UBL-TR Standalone — Kalem Değerleri
            // ═══════════════════════════════════════════════════════════════

            ["UBL_LINE_ID"] = new SnippetInfo(
                key: "UBL_LINE_ID",
                displayName: "Kalem Sıra No",
                iconText: "#️⃣",
                category: "UBL-TR e-Fatura",
                subCategory: "Kalemler",
                description: "Fatura kaleminin sıra numarasını tek değer olarak gösterir. Kalem döngüsü (for-each InvoiceLine) içinde kullanılır.\nXPath: cac:InvoiceLine/cbc:ID",
                xsltCode:
@"<xsl:value-of select=""cbc:ID"" />"),

            ["UBL_LINE_NAME"] = new SnippetInfo(
                key: "UBL_LINE_NAME",
                displayName: "Mal/Hizmet Adı",
                iconText: "📦",
                category: "UBL-TR e-Fatura",
                subCategory: "Kalemler",
                description: "Fatura kalemindeki mal veya hizmetin adını tek değer olarak gösterir. Kalem döngüsü içinde kullanılır.\nXPath: cac:InvoiceLine/cac:Item/cbc:Name",
                xsltCode:
@"<xsl:value-of select=""cac:Item/cbc:Name"" />"),

            ["UBL_LINE_QUANTITY"] = new SnippetInfo(
                key: "UBL_LINE_QUANTITY",
                displayName: "Miktar",
                iconText: "🔢",
                category: "UBL-TR e-Fatura",
                subCategory: "Kalemler",
                description: "Fatura kaleminin miktarını tek değer olarak gösterir. Kalem döngüsü içinde kullanılır.\nXPath: cac:InvoiceLine/cbc:InvoicedQuantity",
                xsltCode:
@"<xsl:value-of select=""cbc:InvoicedQuantity"" />"),

            ["UBL_LINE_PRICE"] = new SnippetInfo(
                key: "UBL_LINE_PRICE",
                displayName: "Birim Fiyat",
                iconText: "💲",
                category: "UBL-TR e-Fatura",
                subCategory: "Kalemler",
                description: "Fatura kaleminin birim fiyatını tek değer olarak gösterir. Kalem döngüsü içinde kullanılır.\nXPath: cac:InvoiceLine/cac:Price/cbc:PriceAmount",
                xsltCode:
@"<xsl:value-of select=""cac:Price/cbc:PriceAmount"" />"),

            ["UBL_LINE_AMOUNT"] = new SnippetInfo(
                key: "UBL_LINE_AMOUNT",
                displayName: "Kalem Tutarı",
                iconText: "💵",
                category: "UBL-TR e-Fatura",
                subCategory: "Kalemler",
                description: "Fatura kaleminin toplam tutarını (miktar × birim fiyat - iskonto) tek değer olarak gösterir. Kalem döngüsü içinde kullanılır.\nXPath: cac:InvoiceLine/cbc:LineExtensionAmount",
                xsltCode:
@"<xsl:value-of select=""cbc:LineExtensionAmount"" />"),

            ["UBL_LINE_TAXAMOUNT"] = new SnippetInfo(
                key: "UBL_LINE_TAXAMOUNT",
                displayName: "Kalem KDV Tutarı",
                iconText: "💰",
                category: "UBL-TR e-Fatura",
                subCategory: "Kalemler",
                description: "Fatura kalemi düzeyinde KDV tutarını tek değer olarak gösterir. Kalem döngüsü içinde kullanılır.\nXPath: cac:InvoiceLine/cac:TaxTotal/cbc:TaxAmount",
                xsltCode:
@"<xsl:value-of select=""cac:TaxTotal/cbc:TaxAmount"" />"),

            ["UBL_LINE_DISCOUNTAMOUNT"] = new SnippetInfo(
                key: "UBL_LINE_DISCOUNTAMOUNT",
                displayName: "Kalem İskonto Tutarı",
                iconText: "🏷",
                category: "UBL-TR e-Fatura",
                subCategory: "Kalemler",
                description: "Fatura kalemi düzeyinde iskonto tutarını tek değer olarak gösterir. ChargeIndicator='false' olan ilk iskonto tutarını verir. Kalem döngüsü içinde kullanılır.\nXPath: cac:InvoiceLine/cac:AllowanceCharge[cbc:ChargeIndicator='false']/cbc:Amount",
                xsltCode:
@"<xsl:value-of select=""cac:AllowanceCharge[cbc:ChargeIndicator='false'][1]/cbc:Amount"" />"),

            // ═══════════════════════════════════════════════════════════════
            // UBL-TR Standalone — Toplam Değerleri
            // ═══════════════════════════════════════════════════════════════

            ["UBL_LINEEXTENSIONAMOUNT"] = new SnippetInfo(
                key: "UBL_LINEEXTENSIONAMOUNT",
                displayName: "Mal/Hizmet Toplamı",
                iconText: "🧮",
                category: "UBL-TR e-Fatura",
                subCategory: "Toplamlar",
                description: "Mal/hizmet toplam tutarını tek değer olarak gösterir. Tüm kalem tutarlarının toplamıdır.\nXPath: /n1:Invoice/cac:LegalMonetaryTotal/cbc:LineExtensionAmount",
                xsltCode:
@"<xsl:value-of select=""/n1:Invoice/cac:LegalMonetaryTotal/cbc:LineExtensionAmount"" />"),

            ["UBL_TAXEXCLUSIVEAMOUNT"] = new SnippetInfo(
                key: "UBL_TAXEXCLUSIVEAMOUNT",
                displayName: "Vergiler Hariç Toplam",
                iconText: "📊",
                category: "UBL-TR e-Fatura",
                subCategory: "Toplamlar",
                description: "Vergiler hariç toplam tutarı tek değer olarak gösterir.\nXPath: /n1:Invoice/cac:LegalMonetaryTotal/cbc:TaxExclusiveAmount",
                xsltCode:
@"<xsl:value-of select=""/n1:Invoice/cac:LegalMonetaryTotal/cbc:TaxExclusiveAmount"" />"),

            ["UBL_TAXINCLUSIVEAMOUNT"] = new SnippetInfo(
                key: "UBL_TAXINCLUSIVEAMOUNT",
                displayName: "Vergiler Dahil Toplam",
                iconText: "📊",
                category: "UBL-TR e-Fatura",
                subCategory: "Toplamlar",
                description: "Vergiler dahil toplam tutarı tek değer olarak gösterir.\nXPath: /n1:Invoice/cac:LegalMonetaryTotal/cbc:TaxInclusiveAmount",
                xsltCode:
@"<xsl:value-of select=""/n1:Invoice/cac:LegalMonetaryTotal/cbc:TaxInclusiveAmount"" />"),

            ["UBL_ALLOWANCETOTALAMOUNT"] = new SnippetInfo(
                key: "UBL_ALLOWANCETOTALAMOUNT",
                displayName: "İndirim Toplamı",
                iconText: "➖",
                category: "UBL-TR e-Fatura",
                subCategory: "Toplamlar",
                description: "Toplam indirim/iskonto tutarını tek değer olarak gösterir.\nXPath: /n1:Invoice/cac:LegalMonetaryTotal/cbc:AllowanceTotalAmount",
                xsltCode:
@"<xsl:value-of select=""/n1:Invoice/cac:LegalMonetaryTotal/cbc:AllowanceTotalAmount"" />"),

            ["UBL_PAYABLEAMOUNT"] = new SnippetInfo(
                key: "UBL_PAYABLEAMOUNT",
                displayName: "Ödenecek Tutar",
                iconText: "💰",
                category: "UBL-TR e-Fatura",
                subCategory: "Toplamlar",
                description: "Nihai ödenecek tutarı tek değer olarak gösterir. Vergiler dahil toplam ± artırım/indirimler.\nXPath: /n1:Invoice/cac:LegalMonetaryTotal/cbc:PayableAmount",
                xsltCode:
@"<xsl:value-of select=""/n1:Invoice/cac:LegalMonetaryTotal/cbc:PayableAmount"" />"),

            ["UBL_TOTALTAXAMOUNT"] = new SnippetInfo(
                key: "UBL_TOTALTAXAMOUNT",
                displayName: "Toplam Vergi",
                iconText: "💰",
                category: "UBL-TR e-Fatura",
                subCategory: "Vergi",
                description: "Fatura düzeyinde toplam vergi tutarını tek değer olarak gösterir.\nXPath: /n1:Invoice/cac:TaxTotal/cbc:TaxAmount",
                xsltCode:
@"<xsl:value-of select=""/n1:Invoice/cac:TaxTotal/cbc:TaxAmount"" />"),

            // ═══════════════════════════════════════════════════════════════
            // UBL-TR Standalone — Ödeme Değerleri
            // ═══════════════════════════════════════════════════════════════

            ["UBL_PAYMENTDUEDATE"] = new SnippetInfo(
                key: "UBL_PAYMENTDUEDATE",
                displayName: "Vade Tarihi",
                iconText: "📅",
                category: "UBL-TR e-Fatura",
                subCategory: "Ödeme",
                description: "Faturanın vade tarihini tek değer olarak gösterir.\nXPath: /n1:Invoice/cac:PaymentMeans/cbc:PaymentDueDate",
                xsltCode:
@"<xsl:value-of select=""/n1:Invoice/cac:PaymentMeans/cbc:PaymentDueDate"" />"),

            ["UBL_IBAN"] = new SnippetInfo(
                key: "UBL_IBAN",
                displayName: "IBAN/Hesap No",
                iconText: "🏦",
                category: "UBL-TR e-Fatura",
                subCategory: "Ödeme",
                description: "Ödeme yapılacak banka hesabının IBAN veya hesap numarasını tek değer olarak gösterir.\nXPath: /n1:Invoice/cac:PaymentMeans/cac:PayeeFinancialAccount/cbc:ID",
                xsltCode:
@"<xsl:value-of select=""/n1:Invoice/cac:PaymentMeans/cac:PayeeFinancialAccount/cbc:ID"" />"),

            // ═══════════════════════════════════════════════════════════════
            // UBL-TR Standalone — Döviz Kurları
            // ═══════════════════════════════════════════════════════════════

            ["UBL_PAYMENTEXCHANGERATE"] = new SnippetInfo(
                key: "UBL_PAYMENTEXCHANGERATE",
                displayName: "Ödeme Döviz Kuru",
                iconText: "💹",
                category: "UBL-TR e-Fatura",
                subCategory: "Toplamlar",
                description: "Ödeme döviz kuru bilgisini gösterir. Kaynak ve hedef para birimi ile hesaplama kurunu içerir.\nXPath: /n1:Invoice/cac:PaymentExchangeRate/cbc:CalculationRate",
                xsltCode:
@"<xsl:if test=""/n1:Invoice/cac:PaymentExchangeRate"">
  <span style=""font-weight:bold;"">Ödeme Kuru: </span>
  1 <xsl:value-of select=""/n1:Invoice/cac:PaymentExchangeRate/cbc:SourceCurrencyCode"" />
  = <xsl:value-of select=""/n1:Invoice/cac:PaymentExchangeRate/cbc:CalculationRate"" />
  <xsl:text> </xsl:text><xsl:value-of select=""/n1:Invoice/cac:PaymentExchangeRate/cbc:TargetCurrencyCode"" />
</xsl:if>"),

            ["UBL_TAXEXCHANGERATE"] = new SnippetInfo(
                key: "UBL_TAXEXCHANGERATE",
                displayName: "Vergi Döviz Kuru",
                iconText: "💹",
                category: "UBL-TR e-Fatura",
                subCategory: "Toplamlar",
                description: "Vergi hesaplamasında kullanılan döviz kuru bilgisini gösterir.\nXPath: /n1:Invoice/cac:TaxExchangeRate/cbc:CalculationRate",
                xsltCode:
@"<xsl:if test=""/n1:Invoice/cac:TaxExchangeRate"">
  <span style=""font-weight:bold;"">Vergi Kuru: </span>
  1 <xsl:value-of select=""/n1:Invoice/cac:TaxExchangeRate/cbc:SourceCurrencyCode"" />
  = <xsl:value-of select=""/n1:Invoice/cac:TaxExchangeRate/cbc:CalculationRate"" />
  <xsl:text> </xsl:text><xsl:value-of select=""/n1:Invoice/cac:TaxExchangeRate/cbc:TargetCurrencyCode"" />
</xsl:if>"),

            // ╔═══════════════════════════════════════════════════════════════╗
            // ║  UBL-TR e-İRSALİYE (DespatchAdvice)                         ║
            // ║  Kök eleman: /n1:DespatchAdvice                              ║
            // ║  Kılavuz: UBL-TR İrsaliye - V 1.2                           ║
            // ╚═══════════════════════════════════════════════════════════════╝

            // ═══════════════════════════════════════════════════════════════
            // UBL-TR e-İrsaliye — Başlık
            // ═══════════════════════════════════════════════════════════════

            ["UBL_IR_HEADER"] = new SnippetInfo(
                key: "UBL_IR_HEADER",
                displayName: "İrsaliye Başlık Tablosu",
                iconText: "📋",
                category: "UBL-TR e-İrsaliye",
                subCategory: "Başlık",
                description: "Sevk irsaliyesi başlık bilgilerini tablo halinde gösterir: İrsaliye No, ETTN, Tarih, Saat, Tip Kodu, Senaryo, Kalem Sayısı.\nXPath: /n1:DespatchAdvice/cbc:*",
                xsltCode:
@"<!-- İrsaliye Başlık Tablosu -->
<table style=""width:100%; border-collapse:collapse;"">
  <tr>
    <td style=""border:1px solid #999; padding:4px; font-weight:bold; width:30%;"">İrsaliye No:</td>
    <td style=""border:1px solid #999; padding:4px;""><xsl:value-of select=""/n1:DespatchAdvice/cbc:ID"" /></td>
  </tr>
  <tr>
    <td style=""border:1px solid #999; padding:4px; font-weight:bold;"">ETTN:</td>
    <td style=""border:1px solid #999; padding:4px;""><xsl:value-of select=""/n1:DespatchAdvice/cbc:UUID"" /></td>
  </tr>
  <tr>
    <td style=""border:1px solid #999; padding:4px; font-weight:bold;"">Düzenleme Tarihi:</td>
    <td style=""border:1px solid #999; padding:4px;""><xsl:value-of select=""/n1:DespatchAdvice/cbc:IssueDate"" /></td>
  </tr>
  <tr>
    <td style=""border:1px solid #999; padding:4px; font-weight:bold;"">Düzenleme Saati:</td>
    <td style=""border:1px solid #999; padding:4px;""><xsl:value-of select=""/n1:DespatchAdvice/cbc:IssueTime"" /></td>
  </tr>
  <tr>
    <td style=""border:1px solid #999; padding:4px; font-weight:bold;"">Tip Kodu:</td>
    <td style=""border:1px solid #999; padding:4px;""><xsl:value-of select=""/n1:DespatchAdvice/cbc:DespatchAdviceTypeCode"" /></td>
  </tr>
  <tr>
    <td style=""border:1px solid #999; padding:4px; font-weight:bold;"">Senaryo:</td>
    <td style=""border:1px solid #999; padding:4px;""><xsl:value-of select=""/n1:DespatchAdvice/cbc:ProfileID"" /></td>
  </tr>
  <tr>
    <td style=""border:1px solid #999; padding:4px; font-weight:bold;"">Kalem Sayısı:</td>
    <td style=""border:1px solid #999; padding:4px;""><xsl:value-of select=""/n1:DespatchAdvice/cbc:LineCountNumeric"" /></td>
  </tr>
</table>"),

            ["UBL_IR_ID"] = new SnippetInfo(
                key: "UBL_IR_ID",
                displayName: "İrsaliye No",
                iconText: "🔢",
                category: "UBL-TR e-İrsaliye",
                subCategory: "Başlık",
                description: "Sevk irsaliyesi numarasını tek değer olarak gösterir. 3 hane birim kodu + 4 hane yıl + 9 hane sıra no formatındadır.\nXPath: /n1:DespatchAdvice/cbc:ID",
                xsltCode:
@"<xsl:value-of select=""/n1:DespatchAdvice/cbc:ID"" />"),

            ["UBL_IR_UUID"] = new SnippetInfo(
                key: "UBL_IR_UUID",
                displayName: "İrsaliye ETTN",
                iconText: "🔑",
                category: "UBL-TR e-İrsaliye",
                subCategory: "Başlık",
                description: "Sevk irsaliyesinin Evrensel Tekil Tanımlama Numarasını (ETTN) gösterir. GUID formatındadır.\nXPath: /n1:DespatchAdvice/cbc:UUID",
                xsltCode:
@"<xsl:value-of select=""/n1:DespatchAdvice/cbc:UUID"" />"),

            ["UBL_IR_ISSUEDATE"] = new SnippetInfo(
                key: "UBL_IR_ISSUEDATE",
                displayName: "İrsaliye Tarihi",
                iconText: "📅",
                category: "UBL-TR e-İrsaliye",
                subCategory: "Başlık",
                description: "Sevk irsaliyesinin düzenleme tarihini tek değer olarak gösterir. YYYY-MM-DD formatındadır.\nXPath: /n1:DespatchAdvice/cbc:IssueDate",
                xsltCode:
@"<xsl:value-of select=""/n1:DespatchAdvice/cbc:IssueDate"" />"),

            ["UBL_IR_ISSUETIME"] = new SnippetInfo(
                key: "UBL_IR_ISSUETIME",
                displayName: "İrsaliye Saati",
                iconText: "⏰",
                category: "UBL-TR e-İrsaliye",
                subCategory: "Başlık",
                description: "Sevk irsaliyesinin düzenleme saatini tek değer olarak gösterir. HH:MM:SS formatındadır.\nXPath: /n1:DespatchAdvice/cbc:IssueTime",
                xsltCode:
@"<xsl:value-of select=""/n1:DespatchAdvice/cbc:IssueTime"" />"),

            ["UBL_IR_TYPECODE"] = new SnippetInfo(
                key: "UBL_IR_TYPECODE",
                displayName: "İrsaliye Tip Kodu",
                iconText: "📑",
                category: "UBL-TR e-İrsaliye",
                subCategory: "Başlık",
                description: "Sevk irsaliyesi tip kodunu gösterir. SEVK veya MATBUDAN değerini alır.\nXPath: /n1:DespatchAdvice/cbc:DespatchAdviceTypeCode",
                xsltCode:
@"<xsl:value-of select=""/n1:DespatchAdvice/cbc:DespatchAdviceTypeCode"" />"),

            ["UBL_IR_NOTE"] = new SnippetInfo(
                key: "UBL_IR_NOTE",
                displayName: "İrsaliye Notu",
                iconText: "📝",
                category: "UBL-TR e-İrsaliye",
                subCategory: "Başlık",
                description: "Sevk irsaliyesindeki notları listeler. Birden fazla not olabilir.\nXPath: /n1:DespatchAdvice/cbc:Note",
                xsltCode:
@"<xsl:for-each select=""/n1:DespatchAdvice/cbc:Note"">
  <div style=""padding:2px;""><xsl:value-of select=""."" /></div>
</xsl:for-each>"),

            ["UBL_IR_LINECOUNT"] = new SnippetInfo(
                key: "UBL_IR_LINECOUNT",
                displayName: "İrsaliye Kalem Sayısı",
                iconText: "🔢",
                category: "UBL-TR e-İrsaliye",
                subCategory: "Başlık",
                description: "Sevk irsaliyesindeki kalem sayısını tek değer olarak gösterir.\nXPath: /n1:DespatchAdvice/cbc:LineCountNumeric",
                xsltCode:
@"<xsl:value-of select=""/n1:DespatchAdvice/cbc:LineCountNumeric"" />"),

            // ═══════════════════════════════════════════════════════════════
            // UBL-TR e-İrsaliye — Taraflar
            // ═══════════════════════════════════════════════════════════════

            ["UBL_IR_SUPPLIER"] = new SnippetInfo(
                key: "UBL_IR_SUPPLIER",
                displayName: "Sevk Eden Taraf",
                iconText: "🏭",
                category: "UBL-TR e-İrsaliye",
                subCategory: "Taraflar",
                description: "Sevk irsaliyesindeki malları sevk eden tarafın bilgilerini tablo olarak gösterir: VKN, Unvan, Vergi Dairesi, Adres, İletişim.\nXPath: /n1:DespatchAdvice/cac:DespatchSupplierParty/cac:Party",
                xsltCode:
@"<!-- Sevk Eden Taraf -->
<table style=""width:100%; border-collapse:collapse;"">
  <tr>
    <td style=""border:1px solid #999; padding:4px; font-weight:bold; width:30%;"">VKN/TCKN:</td>
    <td style=""border:1px solid #999; padding:4px;"">
      <xsl:for-each select=""/n1:DespatchAdvice/cac:DespatchSupplierParty/cac:Party/cac:PartyIdentification"">
        <xsl:value-of select=""cbc:ID"" /><xsl:if test=""position()!=last()""><xsl:text> / </xsl:text></xsl:if>
      </xsl:for-each>
    </td>
  </tr>
  <tr>
    <td style=""border:1px solid #999; padding:4px; font-weight:bold;"">Unvan:</td>
    <td style=""border:1px solid #999; padding:4px;""><xsl:value-of select=""/n1:DespatchAdvice/cac:DespatchSupplierParty/cac:Party/cac:PartyName/cbc:Name"" /></td>
  </tr>
  <tr>
    <td style=""border:1px solid #999; padding:4px; font-weight:bold;"">Vergi Dairesi:</td>
    <td style=""border:1px solid #999; padding:4px;""><xsl:value-of select=""/n1:DespatchAdvice/cac:DespatchSupplierParty/cac:Party/cac:PartyTaxScheme/cac:TaxScheme/cbc:Name"" /></td>
  </tr>
  <tr>
    <td style=""border:1px solid #999; padding:4px; font-weight:bold;"">Adres:</td>
    <td style=""border:1px solid #999; padding:4px;"">
      <xsl:value-of select=""/n1:DespatchAdvice/cac:DespatchSupplierParty/cac:Party/cac:PostalAddress/cbc:StreetName"" />
      <xsl:text> </xsl:text><xsl:value-of select=""/n1:DespatchAdvice/cac:DespatchSupplierParty/cac:Party/cac:PostalAddress/cbc:BuildingNumber"" />
      <xsl:text> </xsl:text><xsl:value-of select=""/n1:DespatchAdvice/cac:DespatchSupplierParty/cac:Party/cac:PostalAddress/cbc:CitySubdivisionName"" />
      / <xsl:value-of select=""/n1:DespatchAdvice/cac:DespatchSupplierParty/cac:Party/cac:PostalAddress/cbc:CityName"" />
    </td>
  </tr>
  <tr>
    <td style=""border:1px solid #999; padding:4px; font-weight:bold;"">Telefon:</td>
    <td style=""border:1px solid #999; padding:4px;""><xsl:value-of select=""/n1:DespatchAdvice/cac:DespatchSupplierParty/cac:Party/cac:Contact/cbc:Telephone"" /></td>
  </tr>
</table>"),

            ["UBL_IR_CUSTOMER"] = new SnippetInfo(
                key: "UBL_IR_CUSTOMER",
                displayName: "Teslim Alan Taraf",
                iconText: "👤",
                category: "UBL-TR e-İrsaliye",
                subCategory: "Taraflar",
                description: "Sevk irsaliyesindeki malları teslim alan tarafın bilgilerini tablo olarak gösterir: VKN, Unvan, Vergi Dairesi, Adres, İletişim.\nXPath: /n1:DespatchAdvice/cac:DeliveryCustomerParty/cac:Party",
                xsltCode:
@"<!-- Teslim Alan Taraf -->
<table style=""width:100%; border-collapse:collapse;"">
  <tr>
    <td style=""border:1px solid #999; padding:4px; font-weight:bold; width:30%;"">VKN/TCKN:</td>
    <td style=""border:1px solid #999; padding:4px;"">
      <xsl:for-each select=""/n1:DespatchAdvice/cac:DeliveryCustomerParty/cac:Party/cac:PartyIdentification"">
        <xsl:value-of select=""cbc:ID"" /><xsl:if test=""position()!=last()""><xsl:text> / </xsl:text></xsl:if>
      </xsl:for-each>
    </td>
  </tr>
  <tr>
    <td style=""border:1px solid #999; padding:4px; font-weight:bold;"">Unvan:</td>
    <td style=""border:1px solid #999; padding:4px;""><xsl:value-of select=""/n1:DespatchAdvice/cac:DeliveryCustomerParty/cac:Party/cac:PartyName/cbc:Name"" /></td>
  </tr>
  <tr>
    <td style=""border:1px solid #999; padding:4px; font-weight:bold;"">Vergi Dairesi:</td>
    <td style=""border:1px solid #999; padding:4px;""><xsl:value-of select=""/n1:DespatchAdvice/cac:DeliveryCustomerParty/cac:Party/cac:PartyTaxScheme/cac:TaxScheme/cbc:Name"" /></td>
  </tr>
  <tr>
    <td style=""border:1px solid #999; padding:4px; font-weight:bold;"">Adres:</td>
    <td style=""border:1px solid #999; padding:4px;"">
      <xsl:value-of select=""/n1:DespatchAdvice/cac:DeliveryCustomerParty/cac:Party/cac:PostalAddress/cbc:StreetName"" />
      <xsl:text> </xsl:text><xsl:value-of select=""/n1:DespatchAdvice/cac:DeliveryCustomerParty/cac:Party/cac:PostalAddress/cbc:BuildingNumber"" />
      <xsl:text> </xsl:text><xsl:value-of select=""/n1:DespatchAdvice/cac:DeliveryCustomerParty/cac:Party/cac:PostalAddress/cbc:CitySubdivisionName"" />
      / <xsl:value-of select=""/n1:DespatchAdvice/cac:DeliveryCustomerParty/cac:Party/cac:PostalAddress/cbc:CityName"" />
    </td>
  </tr>
  <tr>
    <td style=""border:1px solid #999; padding:4px; font-weight:bold;"">Telefon:</td>
    <td style=""border:1px solid #999; padding:4px;""><xsl:value-of select=""/n1:DespatchAdvice/cac:DeliveryCustomerParty/cac:Party/cac:Contact/cbc:Telephone"" /></td>
  </tr>
</table>"),

            ["UBL_IR_SUPPLIER_VKN"] = new SnippetInfo(
                key: "UBL_IR_SUPPLIER_VKN",
                displayName: "Sevk Eden VKN",
                iconText: "🆔",
                category: "UBL-TR e-İrsaliye",
                subCategory: "Taraflar",
                description: "Malları sevk eden tarafın VKN/TCKN numarasını tek değer olarak gösterir.\nXPath: /n1:DespatchAdvice/cac:DespatchSupplierParty/cac:Party/cac:PartyIdentification/cbc:ID",
                xsltCode:
@"<xsl:value-of select=""/n1:DespatchAdvice/cac:DespatchSupplierParty/cac:Party/cac:PartyIdentification/cbc:ID"" />"),

            ["UBL_IR_SUPPLIER_NAME"] = new SnippetInfo(
                key: "UBL_IR_SUPPLIER_NAME",
                displayName: "Sevk Eden Unvan",
                iconText: "🏢",
                category: "UBL-TR e-İrsaliye",
                subCategory: "Taraflar",
                description: "Malları sevk eden tarafın ticari unvanını tek değer olarak gösterir.\nXPath: /n1:DespatchAdvice/cac:DespatchSupplierParty/cac:Party/cac:PartyName/cbc:Name",
                xsltCode:
@"<xsl:value-of select=""/n1:DespatchAdvice/cac:DespatchSupplierParty/cac:Party/cac:PartyName/cbc:Name"" />"),

            ["UBL_IR_SUPPLIER_TAXOFFICE"] = new SnippetInfo(
                key: "UBL_IR_SUPPLIER_TAXOFFICE",
                displayName: "Sevk Eden Vergi Dairesi",
                iconText: "🏛",
                category: "UBL-TR e-İrsaliye",
                subCategory: "Taraflar",
                description: "Malları sevk eden tarafın vergi dairesini tek değer olarak gösterir.\nXPath: /n1:DespatchAdvice/cac:DespatchSupplierParty/cac:Party/cac:PartyTaxScheme/cac:TaxScheme/cbc:Name",
                xsltCode:
@"<xsl:value-of select=""/n1:DespatchAdvice/cac:DespatchSupplierParty/cac:Party/cac:PartyTaxScheme/cac:TaxScheme/cbc:Name"" />"),

            ["UBL_IR_CUSTOMER_VKN"] = new SnippetInfo(
                key: "UBL_IR_CUSTOMER_VKN",
                displayName: "Teslim Alan VKN",
                iconText: "🆔",
                category: "UBL-TR e-İrsaliye",
                subCategory: "Taraflar",
                description: "Malları teslim alan tarafın VKN/TCKN numarasını tek değer olarak gösterir.\nXPath: /n1:DespatchAdvice/cac:DeliveryCustomerParty/cac:Party/cac:PartyIdentification/cbc:ID",
                xsltCode:
@"<xsl:value-of select=""/n1:DespatchAdvice/cac:DeliveryCustomerParty/cac:Party/cac:PartyIdentification/cbc:ID"" />"),

            ["UBL_IR_CUSTOMER_NAME"] = new SnippetInfo(
                key: "UBL_IR_CUSTOMER_NAME",
                displayName: "Teslim Alan Unvan",
                iconText: "👤",
                category: "UBL-TR e-İrsaliye",
                subCategory: "Taraflar",
                description: "Malları teslim alan tarafın ticari unvanını tek değer olarak gösterir.\nXPath: /n1:DespatchAdvice/cac:DeliveryCustomerParty/cac:Party/cac:PartyName/cbc:Name",
                xsltCode:
@"<xsl:value-of select=""/n1:DespatchAdvice/cac:DeliveryCustomerParty/cac:Party/cac:PartyName/cbc:Name"" />"),

            ["UBL_IR_CUSTOMER_TAXOFFICE"] = new SnippetInfo(
                key: "UBL_IR_CUSTOMER_TAXOFFICE",
                displayName: "Teslim Alan Vergi Dairesi",
                iconText: "🏛",
                category: "UBL-TR e-İrsaliye",
                subCategory: "Taraflar",
                description: "Malları teslim alan tarafın vergi dairesini tek değer olarak gösterir.\nXPath: /n1:DespatchAdvice/cac:DeliveryCustomerParty/cac:Party/cac:PartyTaxScheme/cac:TaxScheme/cbc:Name",
                xsltCode:
@"<xsl:value-of select=""/n1:DespatchAdvice/cac:DeliveryCustomerParty/cac:Party/cac:PartyTaxScheme/cac:TaxScheme/cbc:Name"" />"),

            // ═══════════════════════════════════════════════════════════════
            // UBL-TR e-İrsaliye — Sevkiyat
            // ═══════════════════════════════════════════════════════════════

            ["UBL_IR_SHIPMENT"] = new SnippetInfo(
                key: "UBL_IR_SHIPMENT",
                displayName: "Sevkiyat Bilgileri",
                iconText: "🚛",
                category: "UBL-TR e-İrsaliye",
                subCategory: "Sevkiyat",
                description: "Sevkiyat bilgilerini tablo olarak gösterir: Araç plakası, dorse plakası, şoför bilgileri, fiili sevk tarihi/saati, taşıyıcı firma.\nXPath: /n1:DespatchAdvice/cac:Shipment",
                xsltCode:
@"<!-- Sevkiyat Bilgileri -->
<xsl:for-each select=""/n1:DespatchAdvice/cac:Shipment"">
  <table style=""width:100%; border-collapse:collapse;"">
    <xsl:for-each select=""cac:ShipmentStage/cac:TransportMeans/cac:RoadTransport"">
      <tr>
        <td style=""border:1px solid #999; padding:4px; font-weight:bold; width:30%;"">Araç Plakası:</td>
        <td style=""border:1px solid #999; padding:4px;""><xsl:value-of select=""cbc:LicensePlateID"" /></td>
      </tr>
    </xsl:for-each>
    <xsl:for-each select=""cac:TransportHandlingUnit/cac:TransportEquipment"">
      <tr>
        <td style=""border:1px solid #999; padding:4px; font-weight:bold;"">Dorse Plakası:</td>
        <td style=""border:1px solid #999; padding:4px;""><xsl:value-of select=""cbc:ID"" /></td>
      </tr>
    </xsl:for-each>
    <xsl:for-each select=""cac:ShipmentStage/cac:DriverPerson"">
      <tr>
        <td style=""border:1px solid #999; padding:4px; font-weight:bold;"">Şoför:</td>
        <td style=""border:1px solid #999; padding:4px;"">
          <xsl:value-of select=""cbc:FirstName"" /><xsl:text> </xsl:text><xsl:value-of select=""cbc:FamilyName"" />
          <xsl:if test=""cbc:NationalityID""> — TCKN: <xsl:value-of select=""cbc:NationalityID"" /></xsl:if>
        </td>
      </tr>
    </xsl:for-each>
    <xsl:if test=""cac:Delivery/cac:Despatch/cbc:ActualDespatchDate"">
      <tr>
        <td style=""border:1px solid #999; padding:4px; font-weight:bold;"">Fiili Sevk Tarihi:</td>
        <td style=""border:1px solid #999; padding:4px;"">
          <xsl:value-of select=""cac:Delivery/cac:Despatch/cbc:ActualDespatchDate"" />
          <xsl:text> </xsl:text><xsl:value-of select=""cac:Delivery/cac:Despatch/cbc:ActualDespatchTime"" />
        </td>
      </tr>
    </xsl:if>
    <xsl:if test=""cac:Delivery/cac:CarrierParty"">
      <tr>
        <td style=""border:1px solid #999; padding:4px; font-weight:bold;"">Taşıyıcı:</td>
        <td style=""border:1px solid #999; padding:4px;"">
          <xsl:value-of select=""cac:Delivery/cac:CarrierParty/cac:PartyName/cbc:Name"" />
          (<xsl:value-of select=""cac:Delivery/cac:CarrierParty/cac:PartyIdentification/cbc:ID"" />)
        </td>
      </tr>
    </xsl:if>
  </table>
</xsl:for-each>"),

            ["UBL_IR_DRIVER"] = new SnippetInfo(
                key: "UBL_IR_DRIVER",
                displayName: "Şoför Bilgileri",
                iconText: "🧑",
                category: "UBL-TR e-İrsaliye",
                subCategory: "Sevkiyat",
                description: "Sevkiyattaki şoför(ler)in ad, soyad, unvan ve TCKN bilgilerini listeler. Birden fazla şoför olabilir.\nXPath: /n1:DespatchAdvice/cac:Shipment/cac:ShipmentStage/cac:DriverPerson",
                xsltCode:
@"<!-- Şoför Bilgileri -->
<xsl:for-each select=""/n1:DespatchAdvice/cac:Shipment/cac:ShipmentStage/cac:DriverPerson"">
  <div style=""padding:2px;"">
    <xsl:value-of select=""cbc:FirstName"" /><xsl:text> </xsl:text><xsl:value-of select=""cbc:FamilyName"" />
    <xsl:if test=""cbc:Title""> — <xsl:value-of select=""cbc:Title"" /></xsl:if>
    <xsl:if test=""cbc:NationalityID""> (TCKN: <xsl:value-of select=""cbc:NationalityID"" />)</xsl:if>
  </div>
</xsl:for-each>"),

            ["UBL_IR_VEHICLE"] = new SnippetInfo(
                key: "UBL_IR_VEHICLE",
                displayName: "Araç Plakası",
                iconText: "🚛",
                category: "UBL-TR e-İrsaliye",
                subCategory: "Sevkiyat",
                description: "Sevkiyat aracının plaka numarasını tek değer olarak gösterir.\nXPath: /n1:DespatchAdvice/cac:Shipment/cac:ShipmentStage/cac:TransportMeans/cac:RoadTransport/cbc:LicensePlateID",
                xsltCode:
@"<xsl:value-of select=""/n1:DespatchAdvice/cac:Shipment/cac:ShipmentStage/cac:TransportMeans/cac:RoadTransport/cbc:LicensePlateID"" />"),

            ["UBL_IR_TRAILER"] = new SnippetInfo(
                key: "UBL_IR_TRAILER",
                displayName: "Dorse Plakası",
                iconText: "🚚",
                category: "UBL-TR e-İrsaliye",
                subCategory: "Sevkiyat",
                description: "Sevkiyat dorsesinin plaka numarasını gösterir. Birden fazla dorse olabilir.\nXPath: /n1:DespatchAdvice/cac:Shipment/cac:TransportHandlingUnit/cac:TransportEquipment/cbc:ID",
                xsltCode:
@"<xsl:for-each select=""/n1:DespatchAdvice/cac:Shipment/cac:TransportHandlingUnit/cac:TransportEquipment"">
  <xsl:value-of select=""cbc:ID"" /><xsl:if test=""position()!=last()""><xsl:text>, </xsl:text></xsl:if>
</xsl:for-each>"),

            ["UBL_IR_CARRIER"] = new SnippetInfo(
                key: "UBL_IR_CARRIER",
                displayName: "Taşıyıcı Firma",
                iconText: "🏗",
                category: "UBL-TR e-İrsaliye",
                subCategory: "Sevkiyat",
                description: "Taşıyıcı (kargo/nakliye) firmasının bilgilerini tablo olarak gösterir: VKN, Unvan, Adres.\nXPath: /n1:DespatchAdvice/cac:Shipment/cac:Delivery/cac:CarrierParty",
                xsltCode:
@"<!-- Taşıyıcı Firma Bilgileri -->
<xsl:for-each select=""/n1:DespatchAdvice/cac:Shipment/cac:Delivery/cac:CarrierParty"">
  <table style=""width:100%; border-collapse:collapse;"">
    <tr>
      <td style=""border:1px solid #999; padding:4px; font-weight:bold; width:30%;"">Taşıyıcı VKN:</td>
      <td style=""border:1px solid #999; padding:4px;""><xsl:value-of select=""cac:PartyIdentification/cbc:ID"" /></td>
    </tr>
    <tr>
      <td style=""border:1px solid #999; padding:4px; font-weight:bold;"">Taşıyıcı Unvan:</td>
      <td style=""border:1px solid #999; padding:4px;""><xsl:value-of select=""cac:PartyName/cbc:Name"" /></td>
    </tr>
    <tr>
      <td style=""border:1px solid #999; padding:4px; font-weight:bold;"">Adres:</td>
      <td style=""border:1px solid #999; padding:4px;"">
        <xsl:value-of select=""cac:PostalAddress/cbc:CitySubdivisionName"" /> / <xsl:value-of select=""cac:PostalAddress/cbc:CityName"" />
      </td>
    </tr>
  </table>
</xsl:for-each>"),

            ["UBL_IR_DESPATCHDATE"] = new SnippetInfo(
                key: "UBL_IR_DESPATCHDATE",
                displayName: "Fiili Sevk Tarihi",
                iconText: "📅",
                category: "UBL-TR e-İrsaliye",
                subCategory: "Sevkiyat",
                description: "Fiili sevk tarihini tek değer olarak gösterir. Malların fiilen yola çıktığı tarihtir.\nXPath: /n1:DespatchAdvice/cac:Shipment/cac:Delivery/cac:Despatch/cbc:ActualDespatchDate",
                xsltCode:
@"<xsl:value-of select=""/n1:DespatchAdvice/cac:Shipment/cac:Delivery/cac:Despatch/cbc:ActualDespatchDate"" />"),

            ["UBL_IR_DESPATCHTIME"] = new SnippetInfo(
                key: "UBL_IR_DESPATCHTIME",
                displayName: "Fiili Sevk Saati",
                iconText: "⏰",
                category: "UBL-TR e-İrsaliye",
                subCategory: "Sevkiyat",
                description: "Fiili sevk saatini tek değer olarak gösterir.\nXPath: /n1:DespatchAdvice/cac:Shipment/cac:Delivery/cac:Despatch/cbc:ActualDespatchTime",
                xsltCode:
@"<xsl:value-of select=""/n1:DespatchAdvice/cac:Shipment/cac:Delivery/cac:Despatch/cbc:ActualDespatchTime"" />"),

            ["UBL_IR_GOODSVALUE"] = new SnippetInfo(
                key: "UBL_IR_GOODSVALUE",
                displayName: "Mal Bedeli",
                iconText: "💰",
                category: "UBL-TR e-İrsaliye",
                subCategory: "Sevkiyat",
                description: "Gönderideki malların toplam bedelini tek değer olarak gösterir.\nXPath: /n1:DespatchAdvice/cac:Shipment/cac:GoodsItem/cbc:ValueAmount",
                xsltCode:
@"<xsl:value-of select=""/n1:DespatchAdvice/cac:Shipment/cac:GoodsItem/cbc:ValueAmount"" />"),

            // ═══════════════════════════════════════════════════════════════
            // UBL-TR e-İrsaliye — Kalemler
            // ═══════════════════════════════════════════════════════════════

            ["UBL_IR_LINES"] = new SnippetInfo(
                key: "UBL_IR_LINES",
                displayName: "İrsaliye Kalem Tablosu",
                iconText: "📋",
                category: "UBL-TR e-İrsaliye",
                subCategory: "Kalemler",
                description: "İrsaliye kalemlerini tablo olarak listeler: Sıra No, Mal/Hizmet Adı, Miktar, Birim, Satıcı Ürün Kodu.\nXPath: /n1:DespatchAdvice/cac:DespatchLine",
                xsltCode:
@"<!-- İrsaliye Kalem Tablosu -->
<table style=""width:100%; border-collapse:collapse;"">
  <tr style=""background-color:#f0f0f0; font-weight:bold;"">
    <td style=""border:1px solid #999; padding:4px; text-align:center; width:8%;"">Sıra</td>
    <td style=""border:1px solid #999; padding:4px;"">Mal/Hizmet</td>
    <td style=""border:1px solid #999; padding:4px; text-align:right; width:12%;"">Miktar</td>
    <td style=""border:1px solid #999; padding:4px; text-align:center; width:10%;"">Birim</td>
    <td style=""border:1px solid #999; padding:4px; width:15%;"">Ürün Kodu</td>
  </tr>
  <xsl:for-each select=""/n1:DespatchAdvice/cac:DespatchLine"">
    <tr>
      <td style=""border:1px solid #999; padding:4px; text-align:center;""><xsl:value-of select=""cbc:ID"" /></td>
      <td style=""border:1px solid #999; padding:4px;""><xsl:value-of select=""cac:Item/cbc:Name"" /></td>
      <td style=""border:1px solid #999; padding:4px; text-align:right;""><xsl:value-of select=""cbc:DeliveredQuantity"" /></td>
      <td style=""border:1px solid #999; padding:4px; text-align:center;""><xsl:value-of select=""cbc:DeliveredQuantity/@unitCode"" /></td>
      <td style=""border:1px solid #999; padding:4px;""><xsl:value-of select=""cac:Item/cac:SellersItemIdentification/cbc:ID"" /></td>
    </tr>
  </xsl:for-each>
</table>"),

            ["UBL_IR_LINE_ID"] = new SnippetInfo(
                key: "UBL_IR_LINE_ID",
                displayName: "Kalem Sıra No (İrs.)",
                iconText: "#️⃣",
                category: "UBL-TR e-İrsaliye",
                subCategory: "Kalemler",
                description: "İrsaliye kaleminin sıra numarasını tek değer olarak gösterir. DespatchLine döngüsü içinde kullanılır.\nXPath: cac:DespatchLine/cbc:ID",
                xsltCode:
@"<xsl:value-of select=""cbc:ID"" />"),

            ["UBL_IR_LINE_QUANTITY"] = new SnippetInfo(
                key: "UBL_IR_LINE_QUANTITY",
                displayName: "Teslim Miktarı",
                iconText: "🔢",
                category: "UBL-TR e-İrsaliye",
                subCategory: "Kalemler",
                description: "İrsaliye kalemindeki teslim edilen miktarı tek değer olarak gösterir. DespatchLine döngüsü içinde kullanılır.\nXPath: cac:DespatchLine/cbc:DeliveredQuantity",
                xsltCode:
@"<xsl:value-of select=""cbc:DeliveredQuantity"" />"),

            ["UBL_IR_LINE_NAME"] = new SnippetInfo(
                key: "UBL_IR_LINE_NAME",
                displayName: "Mal/Hizmet Adı (İrs.)",
                iconText: "📦",
                category: "UBL-TR e-İrsaliye",
                subCategory: "Kalemler",
                description: "İrsaliye kalemindeki mal veya hizmetin adını tek değer olarak gösterir. DespatchLine döngüsü içinde kullanılır.\nXPath: cac:DespatchLine/cac:Item/cbc:Name",
                xsltCode:
@"<xsl:value-of select=""cac:Item/cbc:Name"" />"),

            ["UBL_IR_LINE_UNITCODE"] = new SnippetInfo(
                key: "UBL_IR_LINE_UNITCODE",
                displayName: "Birim Kodu (İrs.)",
                iconText: "📏",
                category: "UBL-TR e-İrsaliye",
                subCategory: "Kalemler",
                description: "İrsaliye kaleminin birim kodunu tek değer olarak gösterir (NIU, KGM, LTR vb.). DespatchLine döngüsü içinde kullanılır.\nXPath: cac:DespatchLine/cbc:DeliveredQuantity/@unitCode",
                xsltCode:
@"<xsl:value-of select=""cbc:DeliveredQuantity/@unitCode"" />"),

            ["UBL_IR_LINE_SELLERID"] = new SnippetInfo(
                key: "UBL_IR_LINE_SELLERID",
                displayName: "Satıcı Ürün Kodu (İrs.)",
                iconText: "🏷",
                category: "UBL-TR e-İrsaliye",
                subCategory: "Kalemler",
                description: "İrsaliye kalemindeki satıcının ürün kodunu tek değer olarak gösterir. DespatchLine döngüsü içinde kullanılır.\nXPath: cac:DespatchLine/cac:Item/cac:SellersItemIdentification/cbc:ID",
                xsltCode:
@"<xsl:value-of select=""cac:Item/cac:SellersItemIdentification/cbc:ID"" />"),

            // ═══════════════════════════════════════════════════════════════
            // UBL-TR e-İrsaliye — Referanslar
            // ═══════════════════════════════════════════════════════════════

            ["UBL_IR_ORDERREF"] = new SnippetInfo(
                key: "UBL_IR_ORDERREF",
                displayName: "Sipariş Referansı (İrs.)",
                iconText: "📎",
                category: "UBL-TR e-İrsaliye",
                subCategory: "Referanslar",
                description: "İrsaliyedeki sipariş referans bilgisini tablo olarak gösterir: Sipariş No ve Tarihi.\nXPath: /n1:DespatchAdvice/cac:OrderReference",
                xsltCode:
@"<xsl:if test=""/n1:DespatchAdvice/cac:OrderReference"">
  <table style=""width:100%; border-collapse:collapse;"">
    <tr>
      <td style=""border:1px solid #999; padding:4px; font-weight:bold; width:30%;"">Sipariş No:</td>
      <td style=""border:1px solid #999; padding:4px;""><xsl:value-of select=""/n1:DespatchAdvice/cac:OrderReference/cbc:ID"" /></td>
    </tr>
    <tr>
      <td style=""border:1px solid #999; padding:4px; font-weight:bold;"">Sipariş Tarihi:</td>
      <td style=""border:1px solid #999; padding:4px;""><xsl:value-of select=""/n1:DespatchAdvice/cac:OrderReference/cbc:IssueDate"" /></td>
    </tr>
  </table>
</xsl:if>"),

            ["UBL_IR_ADDITIONALDOC"] = new SnippetInfo(
                key: "UBL_IR_ADDITIONALDOC",
                displayName: "İlave Doküman (İrs.)",
                iconText: "📄",
                category: "UBL-TR e-İrsaliye",
                subCategory: "Referanslar",
                description: "İrsaliyedeki ilave doküman referanslarını listeler. MATBUDAN tipinde matbu belge seri-sıra no ve tarihi gösterilir.\nXPath: /n1:DespatchAdvice/cac:AdditionalDocumentReference",
                xsltCode:
@"<xsl:for-each select=""/n1:DespatchAdvice/cac:AdditionalDocumentReference"">
  <div style=""padding:2px;"">
    <span style=""font-weight:bold;""><xsl:value-of select=""cbc:DocumentType"" />: </span>
    <xsl:value-of select=""cbc:ID"" />
    <xsl:if test=""cbc:IssueDate""> — <xsl:value-of select=""cbc:IssueDate"" /></xsl:if>
  </div>
</xsl:for-each>"),

            ["UBL_IR_PHYSICALLOCATION"] = new SnippetInfo(
                key: "UBL_IR_PHYSICALLOCATION",
                displayName: "Sevk Adresi (Depo/Şube)",
                iconText: "📍",
                category: "UBL-TR e-İrsaliye",
                subCategory: "Taraflar",
                description: "Sevk eden tarafın fiziksel konum (depo/şube) adresini gösterir. DespatchSupplierParty altındaki PhysicalLocation elemanıdır.\nXPath: /n1:DespatchAdvice/cac:DespatchSupplierParty/cac:Party/cac:PhysicalLocation",
                xsltCode:
@"<xsl:if test=""/n1:DespatchAdvice/cac:DespatchSupplierParty/cac:Party/cac:PhysicalLocation"">
  <table style=""width:100%; border-collapse:collapse;"">
    <tr>
      <td style=""border:1px solid #999; padding:4px; font-weight:bold; width:30%;"">Şube/Depo:</td>
      <td style=""border:1px solid #999; padding:4px;""><xsl:value-of select=""/n1:DespatchAdvice/cac:DespatchSupplierParty/cac:Party/cac:PhysicalLocation/cbc:ID"" /></td>
    </tr>
    <tr>
      <td style=""border:1px solid #999; padding:4px; font-weight:bold;"">Sevk Adresi:</td>
      <td style=""border:1px solid #999; padding:4px;"">
        <xsl:value-of select=""/n1:DespatchAdvice/cac:DespatchSupplierParty/cac:Party/cac:PhysicalLocation/cac:Address/cbc:StreetName"" />
        <xsl:text> </xsl:text><xsl:value-of select=""/n1:DespatchAdvice/cac:DespatchSupplierParty/cac:Party/cac:PhysicalLocation/cac:Address/cbc:BuildingNumber"" />
        <xsl:text> </xsl:text><xsl:value-of select=""/n1:DespatchAdvice/cac:DespatchSupplierParty/cac:Party/cac:PhysicalLocation/cac:Address/cbc:CitySubdivisionName"" />
        / <xsl:value-of select=""/n1:DespatchAdvice/cac:DespatchSupplierParty/cac:Party/cac:PhysicalLocation/cac:Address/cbc:CityName"" />
      </td>
    </tr>
  </table>
</xsl:if>"),
        };
    }
}
