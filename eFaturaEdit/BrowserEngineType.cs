namespace eFaturaEdit
{
    /// <summary>
    /// Önizleme için kullanılacak tarayıcı motorunu belirler.
    /// </summary>
    public enum BrowserEngineType
    {
        /// <summary>CefSharp (Chromium Embedded Framework)</summary>
        CefSharp,

        /// <summary>Microsoft WebView2 (Edge tabanlı)</summary>
        WebView2
    }
}
