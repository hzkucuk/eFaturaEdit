namespace eFaturaEdit
{
    /// <summary>
    /// XSLT/HTML snippet meta bilgisi.
    /// UI-bağımsız POCO — hem WinForms hem de gelecek cross-platform UI (Avalonia) kullanır.
    /// </summary>
    public class SnippetInfo
    {
        public string Key { get; }
        public string Category { get; }
        public string SubCategory { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public string IconText { get; }
        public string XsltCode { get; }

        public SnippetInfo(string key, string displayName, string iconText, string xsltCode,
            string category = "HTML", string subCategory = null, string description = null)
        {
            Key = key;
            Category = category;
            SubCategory = subCategory;
            DisplayName = displayName;
            Description = description ?? displayName;
            IconText = iconText;
            XsltCode = xsltCode;
        }

        /// <summary>
        /// Drag-drop veri formatı: "&lt;!-- EFATURA_SNIPPET:KEY --&gt;"
        /// Sayfa syntax'ına uygun HTML yorumu olarak taşınır.
        /// </summary>
        public string DragDataString => XsltSnippets.DragPrefix + Key + XsltSnippets.DragSuffix;
    }
}
