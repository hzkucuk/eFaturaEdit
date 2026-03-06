using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Gui.CompletionWindow;

namespace eFaturaEdit
{
    /// <summary>
    /// XSLT editörü için otomatik tamamlama sağlayıcısı.
    /// Mevcut snippet tanımlarından ve UBL-TR XPath alanlarından
    /// öneri listesi oluşturur.
    /// </summary>
    internal sealed class XsltCompletionProvider : ICompletionDataProvider
    {
        public ImageList ImageList => null;

        public string PreSelection { get; private set; }

        public int DefaultIndex => -1;

        /// <summary>
        /// Tamamlama verisini üretir. İmleç öncesindeki metne göre
        /// uygun öneri kategorisi seçilir.
        /// </summary>
        public ICompletionData[] GenerateCompletionData(string fileName, TextArea textArea, char charTyped)
        {
            string lineText = GetTextBeforeCaret(textArea);
            PreSelection = ExtractPreSelection(lineText, charTyped);

            var items = new List<ICompletionData>();

            // XSLT tag önerileri ('<' veya 'xsl:' sonrası)
            if (charTyped == '<' || lineText.EndsWith("<") || lineText.EndsWith("<xsl:"))
            {
                items.AddRange(GetXsltTagCompletions());
            }

            // XPath önerileri (select="" veya test="" içinde)
            if (IsInsideXPathAttribute(lineText))
            {
                items.AddRange(GetXPathCompletions());
            }

            // Snippet önerileri (her zaman — Ctrl+Space ile tam liste)
            if (charTyped == '\0' || items.Count == 0)
            {
                items.AddRange(GetSnippetCompletions());
                items.AddRange(GetXsltTagCompletions());
                items.AddRange(GetXPathCompletions());
            }

            // Tekrarları kaldır
            return items
                .GroupBy(c => c.Text)
                .Select(g => g.First())
                .OrderBy(c => c.Text)
                .ToArray();
        }

        public CompletionDataProviderKeyResult ProcessKey(char key)
        {
            if (char.IsLetterOrDigit(key) || key == ':' || key == '/' || key == '_' || key == '-' || key == '.')
                return CompletionDataProviderKeyResult.NormalKey;

            return CompletionDataProviderKeyResult.InsertionKey;
        }

        public bool InsertAction(ICompletionData data, TextArea textArea, int insertionOffset, char key)
        {
            textArea.BeginUpdate();
            try
            {
                // PreSelection kısmını sil (zaten yazılmış olan)
                if (!string.IsNullOrEmpty(PreSelection))
                {
                    int deleteFrom = insertionOffset - PreSelection.Length;
                    if (deleteFrom >= 0)
                    {
                        textArea.Document.Remove(deleteFrom, PreSelection.Length);
                        insertionOffset = deleteFrom;
                    }
                }

                textArea.Document.Insert(insertionOffset, data.Text);
                textArea.Caret.Position = textArea.Document.OffsetToPosition(insertionOffset + data.Text.Length);
            }
            finally
            {
                textArea.EndUpdate();
            }
            return false;
        }

        #region Tamamlama Veri Üreticileri

        /// <summary>
        /// XSLT etiket önerileri: xsl:value-of, xsl:for-each, xsl:if vb.
        /// </summary>
        private static IEnumerable<ICompletionData> GetXsltTagCompletions()
        {
            var tags = new[]
            {
                new { Text = "xsl:value-of select=\"\"", Desc = "XPath değerini çıktıya yazar" },
                new { Text = "xsl:for-each select=\"\"", Desc = "Düğüm kümesi üzerinde döngü" },
                new { Text = "xsl:if test=\"\"", Desc = "Koşullu blok" },
                new { Text = "xsl:choose", Desc = "Çoklu koşul bloğu (switch)" },
                new { Text = "xsl:when test=\"\"", Desc = "choose içinde koşul dalı" },
                new { Text = "xsl:otherwise", Desc = "choose içinde varsayılan dal" },
                new { Text = "xsl:text", Desc = "Sabit metin çıktısı" },
                new { Text = "xsl:variable name=\"\"", Desc = "Değişken tanımı" },
                new { Text = "xsl:template match=\"\"", Desc = "Şablon eşleşme tanımı" },
                new { Text = "xsl:apply-templates", Desc = "Alt şablonları uygula" },
                new { Text = "xsl:attribute name=\"\"", Desc = "Dinamik HTML özniteliği" },
                new { Text = "xsl:element name=\"\"", Desc = "Dinamik HTML elemanı" },
                new { Text = "xsl:call-template name=\"\"", Desc = "İsimle şablon çağır" },
                new { Text = "xsl:sort select=\"\"", Desc = "for-each içinde sıralama" },
                new { Text = "xsl:copy-of select=\"\"", Desc = "Düğümü olduğu gibi kopyala" },
                new { Text = "xsl:number", Desc = "Otomatik numara üretici" },
            };

            return tags.Select(t =>
                new DefaultCompletionData(t.Text, t.Desc, 0));
        }

        /// <summary>
        /// UBL-TR XPath önerileri: /n1:Invoice/..., /n1:DespatchAdvice/..., cbc:*, cac:*
        /// </summary>
        private static IEnumerable<ICompletionData> GetXPathCompletions()
        {
            var paths = new[]
            {
                // ── e-Fatura Başlık ──
                new { Text = "/n1:Invoice/cbc:ID", Desc = "Fatura No" },
                new { Text = "/n1:Invoice/cbc:UUID", Desc = "ETTN" },
                new { Text = "/n1:Invoice/cbc:IssueDate", Desc = "Fatura Tarihi" },
                new { Text = "/n1:Invoice/cbc:IssueTime", Desc = "Düzenleme Saati" },
                new { Text = "/n1:Invoice/cbc:InvoiceTypeCode", Desc = "Fatura Tipi (SATIS, IADE, TEVKİFAT...)" },
                new { Text = "/n1:Invoice/cbc:ProfileID", Desc = "Senaryo (TICARIFATURA, TEMELFATURA...)" },
                new { Text = "/n1:Invoice/cbc:DocumentCurrencyCode", Desc = "Para Birimi (TRY, USD, EUR...)" },
                new { Text = "/n1:Invoice/cbc:Note", Desc = "Fatura Notu" },
                new { Text = "/n1:Invoice/cbc:LineCountNumeric", Desc = "Kalem Sayısı" },
                new { Text = "/n1:Invoice/cbc:CopyIndicator", Desc = "Asıl/Suret" },
                new { Text = "/n1:Invoice/cbc:AccountingCost", Desc = "Muhasebe Maliyet Kodu" },

                // ── Satıcı ──
                new { Text = "/n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PartyIdentification/cbc:ID", Desc = "Satıcı VKN/TCKN" },
                new { Text = "/n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PartyName/cbc:Name", Desc = "Satıcı Unvan" },
                new { Text = "/n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PartyTaxScheme/cac:TaxScheme/cbc:Name", Desc = "Satıcı Vergi Dairesi" },
                new { Text = "/n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:Contact/cbc:Telephone", Desc = "Satıcı Telefon" },
                new { Text = "/n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:Contact/cbc:ElectronicMail", Desc = "Satıcı E-posta" },
                new { Text = "/n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PostalAddress/cbc:CityName", Desc = "Satıcı İl" },
                new { Text = "/n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PostalAddress/cbc:CitySubdivisionName", Desc = "Satıcı İlçe" },
                new { Text = "/n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PostalAddress/cbc:StreetName", Desc = "Satıcı Adres" },

                // ── Alıcı ──
                new { Text = "/n1:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PartyIdentification/cbc:ID", Desc = "Alıcı VKN/TCKN" },
                new { Text = "/n1:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PartyName/cbc:Name", Desc = "Alıcı Unvan" },
                new { Text = "/n1:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PartyTaxScheme/cac:TaxScheme/cbc:Name", Desc = "Alıcı Vergi Dairesi" },
                new { Text = "/n1:Invoice/cac:AccountingCustomerParty/cac:Party/cac:Contact/cbc:Telephone", Desc = "Alıcı Telefon" },
                new { Text = "/n1:Invoice/cac:AccountingCustomerParty/cac:Party/cac:Contact/cbc:ElectronicMail", Desc = "Alıcı E-posta" },
                new { Text = "/n1:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PostalAddress/cbc:CityName", Desc = "Alıcı İl" },
                new { Text = "/n1:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PostalAddress/cbc:CitySubdivisionName", Desc = "Alıcı İlçe" },
                new { Text = "/n1:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PostalAddress/cbc:StreetName", Desc = "Alıcı Adres" },

                // ── Kalem (InvoiceLine döngüsü içinde) ──
                new { Text = "cbc:ID", Desc = "Kalem Sıra No" },
                new { Text = "cac:Item/cbc:Name", Desc = "Mal/Hizmet Adı" },
                new { Text = "cbc:InvoicedQuantity", Desc = "Miktar" },
                new { Text = "cbc:InvoicedQuantity/@unitCode", Desc = "Birim Kodu" },
                new { Text = "cac:Price/cbc:PriceAmount", Desc = "Birim Fiyat" },
                new { Text = "cbc:LineExtensionAmount", Desc = "Kalem Tutarı" },
                new { Text = "cac:TaxTotal/cbc:TaxAmount", Desc = "Kalem KDV Tutarı" },
                new { Text = "cac:TaxTotal/cac:TaxSubtotal/cbc:Percent", Desc = "KDV Oranı (%)" },
                new { Text = "cac:AllowanceCharge[cbc:ChargeIndicator='false']/cbc:Amount", Desc = "İskonto Tutarı" },
                new { Text = "cac:AllowanceCharge[cbc:ChargeIndicator='false']/cbc:MultiplierFactorNumeric", Desc = "İskonto Oranı (%)" },
                new { Text = "cac:Item/cac:SellersItemIdentification/cbc:ID", Desc = "Satıcı Ürün Kodu" },
                new { Text = "cac:Item/cac:BuyersItemIdentification/cbc:ID", Desc = "Alıcı Ürün Kodu" },
                new { Text = "cac:Item/cbc:Description", Desc = "Ürün Açıklaması" },
                new { Text = "cac:Item/cbc:BrandName", Desc = "Marka" },
                new { Text = "cac:Item/cbc:ModelName", Desc = "Model" },

                // ── Toplamlar ──
                new { Text = "/n1:Invoice/cac:LegalMonetaryTotal/cbc:LineExtensionAmount", Desc = "Mal/Hizmet Toplamı" },
                new { Text = "/n1:Invoice/cac:LegalMonetaryTotal/cbc:TaxExclusiveAmount", Desc = "Vergiler Hariç Toplam" },
                new { Text = "/n1:Invoice/cac:LegalMonetaryTotal/cbc:TaxInclusiveAmount", Desc = "Vergiler Dahil Toplam" },
                new { Text = "/n1:Invoice/cac:LegalMonetaryTotal/cbc:AllowanceTotalAmount", Desc = "Toplam İndirim" },
                new { Text = "/n1:Invoice/cac:LegalMonetaryTotal/cbc:PayableAmount", Desc = "Ödenecek Tutar" },
                new { Text = "/n1:Invoice/cac:TaxTotal/cbc:TaxAmount", Desc = "Toplam Vergi" },

                // ── Ödeme ──
                new { Text = "/n1:Invoice/cac:PaymentMeans/cbc:PaymentDueDate", Desc = "Vade Tarihi" },
                new { Text = "/n1:Invoice/cac:PaymentMeans/cac:PayeeFinancialAccount/cbc:ID", Desc = "IBAN/Hesap No" },
                new { Text = "/n1:Invoice/cac:PaymentMeans/cbc:PaymentMeansCode", Desc = "Ödeme Şekli Kodu" },
                new { Text = "/n1:Invoice/cac:PaymentTerms/cbc:Note", Desc = "Ödeme Koşulları Notu" },

                // ── Referanslar ──
                new { Text = "/n1:Invoice/cac:OrderReference/cbc:ID", Desc = "Sipariş Referans No" },
                new { Text = "/n1:Invoice/cac:DespatchDocumentReference/cbc:ID", Desc = "İrsaliye Referans No" },
                new { Text = "/n1:Invoice/cac:ContractDocumentReference/cbc:ID", Desc = "Kontrat Referans No" },

                // ── e-İrsaliye (DespatchAdvice) ──
                new { Text = "/n1:DespatchAdvice/cbc:ID", Desc = "İrsaliye No" },
                new { Text = "/n1:DespatchAdvice/cbc:UUID", Desc = "İrsaliye ETTN" },
                new { Text = "/n1:DespatchAdvice/cbc:IssueDate", Desc = "İrsaliye Tarihi" },
                new { Text = "/n1:DespatchAdvice/cbc:IssueTime", Desc = "İrsaliye Saati" },
                new { Text = "/n1:DespatchAdvice/cbc:DespatchAdviceTypeCode", Desc = "İrsaliye Tip Kodu" },
                new { Text = "/n1:DespatchAdvice/cac:DespatchSupplierParty/cac:Party/cac:PartyName/cbc:Name", Desc = "Sevk Eden Unvan" },
                new { Text = "/n1:DespatchAdvice/cac:DespatchSupplierParty/cac:Party/cac:PartyIdentification/cbc:ID", Desc = "Sevk Eden VKN" },
                new { Text = "/n1:DespatchAdvice/cac:DeliveryCustomerParty/cac:Party/cac:PartyName/cbc:Name", Desc = "Teslim Alan Unvan" },
                new { Text = "/n1:DespatchAdvice/cac:DeliveryCustomerParty/cac:Party/cac:PartyIdentification/cbc:ID", Desc = "Teslim Alan VKN" },
                new { Text = "/n1:DespatchAdvice/cac:Shipment/cac:ShipmentStage/cac:TransportMeans/cac:RoadTransport/cbc:LicensePlateID", Desc = "Araç Plakası" },
                new { Text = "/n1:DespatchAdvice/cac:Shipment/cac:Delivery/cac:Despatch/cbc:ActualDespatchDate", Desc = "Fiili Sevk Tarihi" },
                new { Text = "cbc:DeliveredQuantity", Desc = "Teslim Miktarı (İrsaliye kalem)" },

                // ── XPath fonksiyonları ──
                new { Text = "position()", Desc = "Geçerli düğüm pozisyonu" },
                new { Text = "last()", Desc = "Son düğüm pozisyonu" },
                new { Text = "count()", Desc = "Düğüm sayısı" },
                new { Text = "sum()", Desc = "Sayısal toplam" },
                new { Text = "format-number()", Desc = "Sayı formatlama" },
                new { Text = "translate()", Desc = "Karakter dönüştürme" },
                new { Text = "substring()", Desc = "Alt metin alma" },
                new { Text = "concat()", Desc = "Metin birleştirme" },
                new { Text = "normalize-space()", Desc = "Boşluk temizleme" },
                new { Text = "string-length()", Desc = "Metin uzunluğu" },
            };

            return paths.Select(p =>
                new DefaultCompletionData(p.Text, p.Desc, 1));
        }

        /// <summary>
        /// XsltSnippets sözlüğünden snippet önerileri.
        /// Snippet seçildiğinde tam XSLT kodu eklenir.
        /// </summary>
        private static IEnumerable<ICompletionData> GetSnippetCompletions()
        {
            return XsltSnippets.Elements.Values
                .Select(s => new SnippetCompletionData(s));
        }

        #endregion

        #region Yardımcı Metotlar

        /// <summary>
        /// İmleç pozisyonundan önceki satır metnini alır.
        /// </summary>
        private static string GetTextBeforeCaret(TextArea textArea)
        {
            var caret = textArea.Caret;
            var line = textArea.Document.GetLineSegment(caret.Line);
            int col = Math.Min(caret.Column, line.Length);
            return textArea.Document.GetText(line.Offset, col);
        }

        /// <summary>
        /// İmleç öncesinde zaten yazılmış olan kısmı bulur (filtreleme için).
        /// </summary>
        private static string ExtractPreSelection(string lineText, char charTyped)
        {
            if (charTyped == '<') return "";
            if (string.IsNullOrEmpty(lineText)) return "";

            // Son kelime veya XPath parçasını bul
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
        /// İmlecin bir XPath özniteliği (select="..." veya test="...") içinde olup olmadığını kontrol eder.
        /// </summary>
        private static bool IsInsideXPathAttribute(string lineText)
        {
            // select=" veya test=" sonrası açık tırnak kontrolü
            int lastSelectIdx = lineText.LastIndexOf("select=\"", StringComparison.Ordinal);
            int lastTestIdx = lineText.LastIndexOf("test=\"", StringComparison.Ordinal);
            int attrStart = Math.Max(lastSelectIdx, lastTestIdx);

            if (attrStart < 0) return false;

            // Öznitelik başlangıcından sonra kapanış tırnağı yoksa içindeyiz
            int quoteAfter = lineText.IndexOf("\"", attrStart + (lastSelectIdx >= lastTestIdx ? 8 : 6));
            return quoteAfter < 0 || quoteAfter >= lineText.Length - 1;
        }

        #endregion
    }

    /// <summary>
    /// Snippet tamamlama verisi. Seçildiğinde snippet'in tam XSLT kodunu ekler.
    /// Listede "[Snippet] DisplayName" olarak görünür.
    /// </summary>
    internal sealed class SnippetCompletionData : ICompletionData
    {
        private readonly SnippetInfo _snippet;
        private string _text;

        public SnippetCompletionData(SnippetInfo snippet)
        {
            _snippet = snippet;
            _text = string.Format("★ {0} — {1}", snippet.Key, snippet.DisplayName);
        }

        public int ImageIndex => 2;

        public string Text
        {
            get { return _text; }
            set { _text = value; }
        }

        public string Description => string.Format("[{0}] {1}\n{2}",
            _snippet.SubCategory ?? _snippet.Category,
            _snippet.DisplayName,
            _snippet.Description);

        public double Priority => 0;

        public bool InsertAction(TextArea textArea, char ch)
        {
            textArea.InsertString(_snippet.XsltCode);
            return false;
        }
    }
}
