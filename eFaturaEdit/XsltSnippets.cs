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
