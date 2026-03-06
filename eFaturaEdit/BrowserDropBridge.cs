using System;

namespace eFaturaEdit
{
    /// <summary>
    /// CefSharp tarayıcısında JavaScript drop olaylarından C#'a bildirim taşıyan veri sınıfı.
    /// JavaScript tarafından gönderilen mesaj bu sınıfla parse edilir.
    /// </summary>
    public class BrowserDropEventArgs : EventArgs
    {
        /// <summary>Bırakılan snippet anahtarı (IMAGE, TABLE vb.)</summary>
        public string SnippetKey { get; set; }

        /// <summary>Drop hedefindeki HTML elemanının tag adı (div, td, p vb.)</summary>
        public string TargetTagName { get; set; }

        /// <summary>Drop hedefindeki HTML elemanının id attribute değeri</summary>
        public string TargetId { get; set; }

        /// <summary>Drop hedefindeki HTML elemanının dış HTML içeriğinin ilk N karakteri</summary>
        public string TargetOuterHtmlPrefix { get; set; }

        /// <summary>Drop hedefindeki HTML elemanının metin içeriğinin ilk N karakteri</summary>
        public string TargetTextPrefix { get; set; }
    }

    /// <summary>
    /// CefSharp ve WebView2 önizleme sayfasına enjekte edilecek JavaScript kodu.
    /// Sayfa üzerine bırakılan öğelerin pozisyon bilgisini C#'a bildirir.
    /// </summary>
    public static class BrowserDropBridge
    {
        /// <summary>
        /// Belirtilen tarayıcı motoruna uygun drop handler JavaScript kodunu döndürür.
        /// CefSharp: CefSharp.PostMessage(info)
        /// WebView2: window.chrome.webview.postMessage(info)
        /// </summary>
        public static string GetDropHandlerScript(BrowserEngineType engine)
        {
            string postMessageCall = engine == BrowserEngineType.WebView2
                ? "window.chrome.webview.postMessage(info);"
                : "CefSharp.PostMessage(info);";

            return DropHandlerTemplate.Replace("/*__POST_MESSAGE__*/", postMessageCall);
        }

        private static readonly string DropHandlerTemplate = @"
(function() {
    if (window.__efaturaDropReady) return;
    window.__efaturaDropReady = true;

    // Tüm öğelerin drop kabul etmesini sağla
    document.addEventListener('dragover', function(e) {
        e.preventDefault();
        e.dataTransfer.dropEffect = 'copy';

        // Hover efekti
        if (window.__lastHover) {
            window.__lastHover.style.outline = '';
        }
        var target = e.target;
        if (target && target !== document.body && target !== document.documentElement) {
            target.style.outline = '2px dashed #0078D4';
            window.__lastHover = target;
        }
    }, true);

    document.addEventListener('dragleave', function(e) {
        if (window.__lastHover) {
            window.__lastHover.style.outline = '';
            window.__lastHover = null;
        }
    }, true);

    document.addEventListener('drop', function(e) {
        e.preventDefault();

        // Hover efektini temizle
        if (window.__lastHover) {
            window.__lastHover.style.outline = '';
            window.__lastHover = null;
        }

        var data = e.dataTransfer.getData('text');
        if (!data) return;

        var prefix = '<!-- EFATURA_SNIPPET:';
        var suffix = ' -->';
        if (data.indexOf(prefix) !== 0) return;

        var snippetKey = data.substring(prefix.length);
        if (snippetKey.indexOf(suffix) === snippetKey.length - suffix.length) {
            snippetKey = snippetKey.substring(0, snippetKey.length - suffix.length);
        }
        var target = e.target;

        // Hedef elemanın bilgilerini topla
        var info = {
            type: 'efatura-drop',
            snippetKey: snippetKey,
            targetTagName: (target.tagName || '').toLowerCase(),
            targetId: target.id || '',
            targetOuterHtmlPrefix: (target.outerHTML || '').substring(0, 200),
            targetTextPrefix: (target.textContent || '').trim().substring(0, 100)
        };

        // Drop noktasına görsel gösterge ekle
        var marker = document.createElement('div');
        marker.style.cssText = 'background:#0078D4; color:#fff; padding:2px 8px; display:inline-block; font-size:10px; border-radius:3px; margin:2px;';
        marker.textContent = '[' + snippetKey + ' ekleniyor...]';
        target.appendChild(marker);
        setTimeout(function() { if (marker.parentNode) marker.parentNode.removeChild(marker); }, 2000);

        // C#'a bildir
        /*__POST_MESSAGE__*/
    }, true);
})();
";

        /// <summary>
        /// CefSharp tarayıcısı için drop handler JavaScript kodu (geriye uyumluluk).
        /// </summary>
        public static readonly string DropHandlerScript = GetDropHandlerScript(BrowserEngineType.CefSharp);

        /// <summary>
        /// Gelen JavaScript mesajından SnippetKey'e göre drop pozisyonunu XSLT kaynağında bulur.
        /// </summary>
        /// <param name="xsltSource">Mevcut XSLT kaynak kodu</param>
        /// <param name="args">JavaScript'ten gelen drop bilgisi</param>
        /// <returns>XSLT kaynağında snippet'in eklenmesi gereken karakter indeksi; bulunamazsa -1</returns>
        public static int FindInsertPosition(string xsltSource, BrowserDropEventArgs args)
        {
            if (string.IsNullOrEmpty(xsltSource) || args == null)
                return -1;

            // Strateji 1: Hedef elemanın id'sine göre XSLT'de ara
            if (!string.IsNullOrEmpty(args.TargetId))
            {
                string idPattern = "id=\"" + args.TargetId + "\"";
                int idPos = xsltSource.IndexOf(idPattern, StringComparison.OrdinalIgnoreCase);
                if (idPos >= 0)
                {
                    // Bu id'yi içeren tag'ın kapanışını bul ve sonrasına ekle
                    return FindClosingTagEnd(xsltSource, idPos);
                }
            }

            // Strateji 2: Hedef elemanın metin içeriğine göre XSLT'de ara
            if (!string.IsNullOrEmpty(args.TargetTextPrefix) && args.TargetTextPrefix.Length > 3)
            {
                int textPos = xsltSource.IndexOf(args.TargetTextPrefix, StringComparison.OrdinalIgnoreCase);
                if (textPos >= 0)
                {
                    return FindClosingTagEnd(xsltSource, textPos);
                }
            }

            // Strateji 3: Tag adına göre son geçen yeri bul
            if (!string.IsNullOrEmpty(args.TargetTagName))
            {
                string closingTag = "</" + args.TargetTagName + ">";
                int lastTagPos = xsltSource.LastIndexOf(closingTag, StringComparison.OrdinalIgnoreCase);
                if (lastTagPos >= 0)
                {
                    return lastTagPos + closingTag.Length;
                }
            }

            return -1;
        }

        private static int FindClosingTagEnd(string source, int fromPos)
        {
            // fromPos'tan itibaren ilk '>' karakterini bul
            int gtPos = source.IndexOf('>', fromPos);
            if (gtPos < 0) return -1;

            // Self-closing tag kontrolü (<img ... />)
            if (source[gtPos - 1] == '/')
                return gtPos + 1;

            // Normal tag: kapanış tag'ını bul
            // Önce tag adını çıkar
            int ltPos = source.LastIndexOf('<', fromPos);
            if (ltPos < 0) return gtPos + 1;

            string tagContent = source.Substring(ltPos + 1, fromPos - ltPos - 1).Trim();
            string tagName = tagContent.Split(' ', '\t', '\r', '\n')[0];

            if (string.IsNullOrEmpty(tagName))
                return gtPos + 1;

            string closingTag = "</" + tagName + ">";
            int closePos = source.IndexOf(closingTag, gtPos, StringComparison.OrdinalIgnoreCase);
            if (closePos >= 0)
                return closePos + closingTag.Length;

            return gtPos + 1;
        }
    }
}
