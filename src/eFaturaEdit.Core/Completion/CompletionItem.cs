namespace eFaturaEdit
{
    /// <summary>
    /// Autocomplete kataloğundaki tek bir öneri girdisi.
    /// UI-bağımsız POCO — WinForms <c>DefaultCompletionData</c> veya
    /// Avalonia <c>ICompletionData</c> gibi platform tiplerine adaptör ile bağlanır.
    /// </summary>
    public sealed class CompletionItem
    {
        /// <summary>Editöre eklenecek metin (etiket adı, XPath ifadesi vb.).</summary>
        public string Text { get; }

        /// <summary>Kullanıcıya gösterilecek açıklama.</summary>
        public string Description { get; }

        /// <summary>Öneri grubu (XSLT etiketi = 0, XPath = 1, Snippet = 2).</summary>
        public int ImageIndex { get; }

        public CompletionItem(string text, string description, int imageIndex)
        {
            Text = text;
            Description = description;
            ImageIndex = imageIndex;
        }
    }
}
