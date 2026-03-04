using System.Collections.Generic;

namespace eFaturaEdit
{
    /// <summary>
    /// E-Fatura XSLT şablonlarına eklenebilecek öğe snippet tanımları.
    /// Her snippet, XSLT kaynak koduna doğrudan eklenebilecek HTML/XSLT bloğudur.
    /// </summary>
    public static class XsltSnippets
    {
        public const string SnippetPrefix = "EFATURA_SNIPPET:";

        public static readonly Dictionary<string, SnippetInfo> Elements = new Dictionary<string, SnippetInfo>
        {
            ["IMAGE"] = new SnippetInfo(
                key: "IMAGE",
                displayName: "Resim",
                iconText: "🖼",
                xsltCode:
@"<img src=""data:image/png;base64,{BASE64_DATA}"" alt=""Resim"" style=""max-width:200px; max-height:100px;"" />"),

            ["TABLE"] = new SnippetInfo(
                key: "TABLE",
                displayName: "Tablo",
                iconText: "📊",
                xsltCode:
@"<table id=""lineTable"" style=""width:100%;"" border=""1"">
  <tr>
    <td id=""lineTableTd"" style=""font-weight:bold;"">Başlık 1</td>
    <td id=""lineTableTd"" style=""font-weight:bold;"">Başlık 2</td>
  </tr>
  <tr>
    <td id=""lineTableTd"">Değer 1</td>
    <td id=""lineTableTd"">Değer 2</td>
  </tr>
</table>"),

            ["TEXT"] = new SnippetInfo(
                key: "TEXT",
                displayName: "Metin",
                iconText: "📝",
                xsltCode:
@"<p style=""font-size:11px; color:#666666;"">Metin buraya yazılır</p>"),

            ["VALUEOF"] = new SnippetInfo(
                key: "VALUEOF",
                displayName: "XSL Değer",
                iconText: "📌",
                xsltCode:
@"<xsl:value-of select=""/n1:Invoice/cbc:ID"" />"),

            ["LINK"] = new SnippetInfo(
                key: "LINK",
                displayName: "Link",
                iconText: "🔗",
                xsltCode:
@"<a href=""http://www.example.com"" target=""_blank"" style=""color:#70A300;"">Link Metni</a>"),

            ["HR"] = new SnippetInfo(
                key: "HR",
                displayName: "Çizgi",
                iconText: "➖",
                xsltCode:
@"<hr style=""height:2px; color:#000000; background-color:#000000;"" />"),

            ["DIV"] = new SnippetInfo(
                key: "DIV",
                displayName: "Kutu",
                iconText: "📦",
                xsltCode:
@"<div style=""border:1px solid #ccc; padding:5px; margin:5px 0;"">
  İçerik buraya eklenir
</div>"),

            ["FOREACH"] = new SnippetInfo(
                key: "FOREACH",
                displayName: "Döngü",
                iconText: "🔄",
                xsltCode:
@"<xsl:for-each select=""/n1:Invoice/cac:InvoiceLine"">
  <tr>
    <td id=""lineTableTd""><xsl:value-of select=""cbc:ID"" /></td>
    <td id=""lineTableTd""><xsl:value-of select=""cac:Item/cbc:Name"" /></td>
  </tr>
</xsl:for-each>"),

            ["IF"] = new SnippetInfo(
                key: "IF",
                displayName: "Koşul",
                iconText: "❓",
                xsltCode:
@"<xsl:if test=""condition"">
  <!-- Koşul doğru olduğunda gösterilecek içerik -->
</xsl:if>"),

            ["BOLD"] = new SnippetInfo(
                key: "BOLD",
                displayName: "Kalın",
                iconText: "B",
                xsltCode:
@"<b>Kalın Metin</b>"),

            ["SPAN"] = new SnippetInfo(
                key: "SPAN",
                displayName: "Etiket",
                iconText: "🏷",
                xsltCode:
@"<span style=""font-size:11px; color:#000000;""><xsl:value-of select=""/n1:Invoice/cbc:ID"" /></span>"),

            ["BARCODE"] = new SnippetInfo(
                key: "BARCODE",
                displayName: "Barkod",
                iconText: "▮",
                xsltCode:
@"<img src=""https://barcode.tec-it.com/barcode.ashx?data={BARCODE_DATA}&amp;code=Code128"" alt=""Barkod"" style=""height:40px;"" />"),

            ["QR"] = new SnippetInfo(
                key: "QR",
                displayName: "QR Kod",
                iconText: "⊞",
                xsltCode:
@"<img src=""https://api.qrserver.com/v1/create-qr-code/?size=100x100&amp;data={QR_DATA}"" alt=""QR Kod"" style=""width:100px; height:100px;"" />"),

            ["PAGEBREAK"] = new SnippetInfo(
                key: "PAGEBREAK",
                displayName: "Sayfa Sonu",
                iconText: "⏎",
                xsltCode:
@"<div style=""page-break-after:always;""></div>"),

            ["HEADER"] = new SnippetInfo(
                key: "HEADER",
                displayName: "Üst Bilgi",
                iconText: "⬆",
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
                xsltCode:
@"<div id=""pageFooter"" style=""width:100%; border-top:1px solid #999; padding-top:5px; margin-top:10px; font-size:9px; color:#999; text-align:center;"">
  Bu belge elektronik olarak oluşturulmuştur.
</div>"),

            // ═══════════════════════════════════════════
            // UBL-TR e-Fatura Snippet'leri
            // ═══════════════════════════════════════════

            ["UBL_SUPPLIER"] = new SnippetInfo(
                key: "UBL_SUPPLIER",
                displayName: "Satıcı Bilgisi",
                iconText: "🏢",
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

            ["UBL_INVOICELINES"] = new SnippetInfo(
                key: "UBL_INVOICELINES",
                displayName: "Fatura Kalemleri",
                iconText: "📋",
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

            ["UBL_TAXTOTAL"] = new SnippetInfo(
                key: "UBL_TAXTOTAL",
                displayName: "Vergi Toplamları",
                iconText: "💰",
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

            ["UBL_TOTALS"] = new SnippetInfo(
                key: "UBL_TOTALS",
                displayName: "Genel Toplam",
                iconText: "Σ",
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

            ["UBL_INVOICEHEADER"] = new SnippetInfo(
                key: "UBL_INVOICEHEADER",
                displayName: "Fatura Başlığı",
                iconText: "📄",
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

            ["UBL_PROFILEID"] = new SnippetInfo(
                key: "UBL_PROFILEID",
                displayName: "Senaryo Kontrolü",
                iconText: "🔀",
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
        };
    }

    public class SnippetInfo
    {
        public string Key { get; }
        public string DisplayName { get; }
        public string IconText { get; }
        public string XsltCode { get; }

        public SnippetInfo(string key, string displayName, string iconText, string xsltCode)
        {
            Key = key;
            DisplayName = displayName;
            IconText = iconText;
            XsltCode = xsltCode;
        }

        /// <summary>
        /// Drag-drop veri formatı: "EFATURA_SNIPPET:KEY"
        /// </summary>
        public string DragDataString => XsltSnippets.SnippetPrefix + Key;
    }
}
