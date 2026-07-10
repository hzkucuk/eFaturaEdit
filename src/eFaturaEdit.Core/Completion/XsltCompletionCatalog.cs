using System;
using System.Collections.Generic;

namespace eFaturaEdit
{
    /// <summary>
    /// XSLT editörü autocomplete katalogu.
    /// XSLT etiket önerileri, UBL-TR XPath önerileri ve XPath fonksiyon
    /// önerilerini içeren UI-bağımsız veri kaynağı.
    ///
    /// Snippet önerileri <see cref="XsltSnippets.Elements"/> üzerinden ayrıca oluşturulur;
    /// bu sınıf snippet listesini duplicate etmez.
    /// </summary>
    public static class XsltCompletionCatalog
    {
        /// <summary>
        /// XSLT etiket önerileri: xsl:value-of, xsl:for-each, xsl:if vb.
        /// </summary>
        public static readonly IReadOnlyList<CompletionItem> XsltTags = new List<CompletionItem>
        {
            new CompletionItem("xsl:value-of select=\"\"", "XPath değerini çıktıya yazar", 0),
            new CompletionItem("xsl:for-each select=\"\"", "Düğüm kümesi üzerinde döngü", 0),
            new CompletionItem("xsl:if test=\"\"", "Koşullu blok", 0),
            new CompletionItem("xsl:choose", "Çoklu koşul bloğu (switch)", 0),
            new CompletionItem("xsl:when test=\"\"", "choose içinde koşul dalı", 0),
            new CompletionItem("xsl:otherwise", "choose içinde varsayılan dal", 0),
            new CompletionItem("xsl:text", "Sabit metin çıktısı", 0),
            new CompletionItem("xsl:variable name=\"\"", "Değişken tanımı", 0),
            new CompletionItem("xsl:template match=\"\"", "Şablon eşleşme tanımı", 0),
            new CompletionItem("xsl:apply-templates", "Alt şablonları uygula", 0),
            new CompletionItem("xsl:attribute name=\"\"", "Dinamik HTML özniteliği", 0),
            new CompletionItem("xsl:element name=\"\"", "Dinamik HTML elemanı", 0),
            new CompletionItem("xsl:call-template name=\"\"", "İsimle şablon çağır", 0),
            new CompletionItem("xsl:sort select=\"\"", "for-each içinde sıralama", 0),
            new CompletionItem("xsl:copy-of select=\"\"", "Düğümü olduğu gibi kopyala", 0),
            new CompletionItem("xsl:number", "Otomatik numara üretici", 0),
        };

        /// <summary>
        /// UBL-TR XPath önerileri: /n1:Invoice/..., /n1:DespatchAdvice/..., cbc:*, cac:*
        /// ve yaygın XPath fonksiyonları.
        /// </summary>
        public static readonly IReadOnlyList<CompletionItem> XPathPaths = new List<CompletionItem>
        {
            // ── e-Fatura Başlık ──
            new CompletionItem("/n1:Invoice/cbc:ID", "Fatura No", 1),
            new CompletionItem("/n1:Invoice/cbc:UUID", "ETTN", 1),
            new CompletionItem("/n1:Invoice/cbc:IssueDate", "Fatura Tarihi", 1),
            new CompletionItem("/n1:Invoice/cbc:IssueTime", "Düzenleme Saati", 1),
            new CompletionItem("/n1:Invoice/cbc:InvoiceTypeCode", "Fatura Tipi (SATIS, IADE, TEVKİFAT...)", 1),
            new CompletionItem("/n1:Invoice/cbc:ProfileID", "Senaryo (TICARIFATURA, TEMELFATURA...)", 1),
            new CompletionItem("/n1:Invoice/cbc:DocumentCurrencyCode", "Para Birimi (TRY, USD, EUR...)", 1),
            new CompletionItem("/n1:Invoice/cbc:Note", "Fatura Notu", 1),
            new CompletionItem("/n1:Invoice/cbc:LineCountNumeric", "Kalem Sayısı", 1),
            new CompletionItem("/n1:Invoice/cbc:CopyIndicator", "Asıl/Suret", 1),
            new CompletionItem("/n1:Invoice/cbc:AccountingCost", "Muhasebe Maliyet Kodu", 1),

            // ── Satıcı ──
            new CompletionItem("/n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PartyIdentification/cbc:ID", "Satıcı VKN/TCKN", 1),
            new CompletionItem("/n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PartyName/cbc:Name", "Satıcı Unvan", 1),
            new CompletionItem("/n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PartyTaxScheme/cac:TaxScheme/cbc:Name", "Satıcı Vergi Dairesi", 1),
            new CompletionItem("/n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:Contact/cbc:Telephone", "Satıcı Telefon", 1),
            new CompletionItem("/n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:Contact/cbc:ElectronicMail", "Satıcı E-posta", 1),
            new CompletionItem("/n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PostalAddress/cbc:CityName", "Satıcı İl", 1),
            new CompletionItem("/n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PostalAddress/cbc:CitySubdivisionName", "Satıcı İlçe", 1),
            new CompletionItem("/n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PostalAddress/cbc:StreetName", "Satıcı Adres", 1),

            // ── Alıcı ──
            new CompletionItem("/n1:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PartyIdentification/cbc:ID", "Alıcı VKN/TCKN", 1),
            new CompletionItem("/n1:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PartyName/cbc:Name", "Alıcı Unvan", 1),
            new CompletionItem("/n1:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PartyTaxScheme/cac:TaxScheme/cbc:Name", "Alıcı Vergi Dairesi", 1),
            new CompletionItem("/n1:Invoice/cac:AccountingCustomerParty/cac:Party/cac:Contact/cbc:Telephone", "Alıcı Telefon", 1),
            new CompletionItem("/n1:Invoice/cac:AccountingCustomerParty/cac:Party/cac:Contact/cbc:ElectronicMail", "Alıcı E-posta", 1),
            new CompletionItem("/n1:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PostalAddress/cbc:CityName", "Alıcı İl", 1),
            new CompletionItem("/n1:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PostalAddress/cbc:CitySubdivisionName", "Alıcı İlçe", 1),
            new CompletionItem("/n1:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PostalAddress/cbc:StreetName", "Alıcı Adres", 1),

            // ── Kalem (InvoiceLine döngüsü içinde) ──
            new CompletionItem("cbc:ID", "Kalem Sıra No", 1),
            new CompletionItem("cac:Item/cbc:Name", "Mal/Hizmet Adı", 1),
            new CompletionItem("cbc:InvoicedQuantity", "Miktar", 1),
            new CompletionItem("cbc:InvoicedQuantity/@unitCode", "Birim Kodu", 1),
            new CompletionItem("cac:Price/cbc:PriceAmount", "Birim Fiyat", 1),
            new CompletionItem("cbc:LineExtensionAmount", "Kalem Tutarı", 1),
            new CompletionItem("cac:TaxTotal/cbc:TaxAmount", "Kalem KDV Tutarı", 1),
            new CompletionItem("cac:TaxTotal/cac:TaxSubtotal/cbc:Percent", "KDV Oranı (%)", 1),
            new CompletionItem("cac:AllowanceCharge[cbc:ChargeIndicator='false']/cbc:Amount", "İskonto Tutarı", 1),
            new CompletionItem("cac:AllowanceCharge[cbc:ChargeIndicator='false']/cbc:MultiplierFactorNumeric", "İskonto Oranı (%)", 1),
            new CompletionItem("cac:Item/cac:SellersItemIdentification/cbc:ID", "Satıcı Ürün Kodu", 1),
            new CompletionItem("cac:Item/cac:BuyersItemIdentification/cbc:ID", "Alıcı Ürün Kodu", 1),
            new CompletionItem("cac:Item/cbc:Description", "Ürün Açıklaması", 1),
            new CompletionItem("cac:Item/cbc:BrandName", "Marka", 1),
            new CompletionItem("cac:Item/cbc:ModelName", "Model", 1),

            // ── Toplamlar ──
            new CompletionItem("/n1:Invoice/cac:LegalMonetaryTotal/cbc:LineExtensionAmount", "Mal/Hizmet Toplamı", 1),
            new CompletionItem("/n1:Invoice/cac:LegalMonetaryTotal/cbc:TaxExclusiveAmount", "Vergiler Hariç Toplam", 1),
            new CompletionItem("/n1:Invoice/cac:LegalMonetaryTotal/cbc:TaxInclusiveAmount", "Vergiler Dahil Toplam", 1),
            new CompletionItem("/n1:Invoice/cac:LegalMonetaryTotal/cbc:AllowanceTotalAmount", "Toplam İndirim", 1),
            new CompletionItem("/n1:Invoice/cac:LegalMonetaryTotal/cbc:PayableAmount", "Ödenecek Tutar", 1),
            new CompletionItem("/n1:Invoice/cac:TaxTotal/cbc:TaxAmount", "Toplam Vergi", 1),

            // ── Ödeme ──
            new CompletionItem("/n1:Invoice/cac:PaymentMeans/cbc:PaymentDueDate", "Vade Tarihi", 1),
            new CompletionItem("/n1:Invoice/cac:PaymentMeans/cac:PayeeFinancialAccount/cbc:ID", "IBAN/Hesap No", 1),
            new CompletionItem("/n1:Invoice/cac:PaymentMeans/cbc:PaymentMeansCode", "Ödeme Şekli Kodu", 1),
            new CompletionItem("/n1:Invoice/cac:PaymentTerms/cbc:Note", "Ödeme Koşulları Notu", 1),

            // ── Referanslar ──
            new CompletionItem("/n1:Invoice/cac:OrderReference/cbc:ID", "Sipariş Referans No", 1),
            new CompletionItem("/n1:Invoice/cac:DespatchDocumentReference/cbc:ID", "İrsaliye Referans No", 1),
            new CompletionItem("/n1:Invoice/cac:ContractDocumentReference/cbc:ID", "Kontrat Referans No", 1),

            // ── e-İrsaliye (DespatchAdvice) ──
            new CompletionItem("/n1:DespatchAdvice/cbc:ID", "İrsaliye No", 1),
            new CompletionItem("/n1:DespatchAdvice/cbc:UUID", "İrsaliye ETTN", 1),
            new CompletionItem("/n1:DespatchAdvice/cbc:IssueDate", "İrsaliye Tarihi", 1),
            new CompletionItem("/n1:DespatchAdvice/cbc:IssueTime", "İrsaliye Saati", 1),
            new CompletionItem("/n1:DespatchAdvice/cbc:DespatchAdviceTypeCode", "İrsaliye Tip Kodu", 1),
            new CompletionItem("/n1:DespatchAdvice/cac:DespatchSupplierParty/cac:Party/cac:PartyName/cbc:Name", "Sevk Eden Unvan", 1),
            new CompletionItem("/n1:DespatchAdvice/cac:DespatchSupplierParty/cac:Party/cac:PartyIdentification/cbc:ID", "Sevk Eden VKN", 1),
            new CompletionItem("/n1:DespatchAdvice/cac:DeliveryCustomerParty/cac:Party/cac:PartyName/cbc:Name", "Teslim Alan Unvan", 1),
            new CompletionItem("/n1:DespatchAdvice/cac:DeliveryCustomerParty/cac:Party/cac:PartyIdentification/cbc:ID", "Teslim Alan VKN", 1),
            new CompletionItem("/n1:DespatchAdvice/cac:Shipment/cac:ShipmentStage/cac:TransportMeans/cac:RoadTransport/cbc:LicensePlateID", "Araç Plakası", 1),
            new CompletionItem("/n1:DespatchAdvice/cac:Shipment/cac:Delivery/cac:Despatch/cbc:ActualDespatchDate", "Fiili Sevk Tarihi", 1),
            new CompletionItem("cbc:DeliveredQuantity", "Teslim Miktarı (İrsaliye kalem)", 1),

            // ── XPath fonksiyonları ──
            new CompletionItem("position()", "Geçerli düğüm pozisyonu", 1),
            new CompletionItem("last()", "Son düğüm pozisyonu", 1),
            new CompletionItem("count()", "Düğüm sayısı", 1),
            new CompletionItem("sum()", "Sayısal toplam", 1),
            new CompletionItem("format-number()", "Sayı formatlama", 1),
            new CompletionItem("translate()", "Karakter dönüştürme", 1),
            new CompletionItem("substring()", "Alt metin alma", 1),
            new CompletionItem("concat()", "Metin birleştirme", 1),
            new CompletionItem("normalize-space()", "Boşluk temizleme", 1),
            new CompletionItem("string-length()", "Metin uzunluğu", 1),
        };

        /// <summary>
        /// İmleç öncesinde zaten yazılmış olan kısmı bulur (filtreleme için).
        /// XPath ve XSLT etiket karakterlerini tanır: harf, rakam, ':', '/', '_', '-', '.', '@', '[', ']', '(', ')'.
        /// </summary>
        public static string ExtractPreSelection(string lineText, char charTyped)
        {
            if (charTyped == '<') return string.Empty;
            if (string.IsNullOrEmpty(lineText)) return string.Empty;

            int i = lineText.Length - 1;
            while (i >= 0)
            {
                char c = lineText[i];
                if (char.IsLetterOrDigit(c) || c == ':' || c == '/' || c == '_' || c == '-' || c == '.' || c == '@' || c == '[' || c == ']' || c == '(' || c == ')')
                    i--;
                else
                    break;
            }

            return lineText.Substring(i + 1);
        }

        /// <summary>
        /// İmlecin bir XPath özniteliği (<c>select="..."</c> veya <c>test="..."</c>) içinde
        /// olup olmadığını kontrol eder. XPath önerilerinin önceliklendirilmesi için kullanılır.
        /// </summary>
        public static bool IsInsideXPathAttribute(string lineText)
        {
            int lastSelectIdx = lineText.LastIndexOf("select=\"", StringComparison.Ordinal);
            int lastTestIdx = lineText.LastIndexOf("test=\"", StringComparison.Ordinal);
            int attrStart = Math.Max(lastSelectIdx, lastTestIdx);

            if (attrStart < 0) return false;

            int quoteAfter = lineText.IndexOf("\"", attrStart + (lastSelectIdx >= lastTestIdx ? 8 : 6), StringComparison.Ordinal);
            return quoteAfter < 0 || quoteAfter >= lineText.Length - 1;
        }
    }
}
