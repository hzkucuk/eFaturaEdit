using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Gui.CompletionWindow;

namespace eFaturaEdit
{
    /// <summary>
    /// XSLT editörü için otomatik tamamlama sağlayıcısı (WinForms adaptörü).
    /// UI-bağımsız veri kaynağı <see cref="XsltCompletionCatalog"/>'dan gelir;
    /// bu sınıf yalnızca ICSharpCode.TextEditor'e bağlı bilgi köprüsüdür.
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
            PreSelection = XsltCompletionCatalog.ExtractPreSelection(lineText, charTyped);

            var items = new List<ICompletionData>();

            // XSLT tag önerileri ('<' veya 'xsl:' sonrası)
            if (charTyped == '<' || lineText.EndsWith("<") || lineText.EndsWith("<xsl:"))
            {
                items.AddRange(ToCompletionData(XsltCompletionCatalog.XsltTags));
            }

            // XPath önerileri (select="" veya test="" içinde)
            if (XsltCompletionCatalog.IsInsideXPathAttribute(lineText))
            {
                items.AddRange(ToCompletionData(XsltCompletionCatalog.XPathPaths));
            }

            // Snippet önerileri (her zaman — Ctrl+Space ile tam liste)
            if (charTyped == '\0' || items.Count == 0)
            {
                items.AddRange(XsltSnippets.Elements.Values.Select(s => (ICompletionData)new SnippetCompletionData(s)));
                items.AddRange(ToCompletionData(XsltCompletionCatalog.XsltTags));
                items.AddRange(ToCompletionData(XsltCompletionCatalog.XPathPaths));
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

        /// <summary>
        /// Core <see cref="CompletionItem"/> listesini ICSharpCode.TextEditor'ün
        /// <see cref="DefaultCompletionData"/> tiplerine dönüştürür.
        /// </summary>
        private static IEnumerable<ICompletionData> ToCompletionData(IEnumerable<CompletionItem> items)
        {
            return items.Select(i => new DefaultCompletionData(i.Text, i.Description, i.ImageIndex));
        }

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
    }

    /// <summary>
    /// Snippet tamamlama verisi. Seçildiğinde snippet'in tam XSLT kodunu ekler.
    /// Listede "★ KEY — DisplayName" olarak görünür.
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
