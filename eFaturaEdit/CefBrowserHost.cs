using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;

namespace eFaturaEdit
{
    /// <summary>
    /// CefSharp tabanlı tarayıcı implementasyonu.
    /// </summary>
    internal sealed class CefBrowserHost : IBrowserHost
    {
        private ChromiumWebBrowser _browser;
        private bool _disposed;

        public BrowserEngineType EngineType => BrowserEngineType.CefSharp;
        public Control BrowserControl => _browser;
        public bool IsReady => _browser != null && Cef.IsInitialized == true;

        public event EventHandler FrameLoadCompleted;
        public event EventHandler<BrowserMessageEventArgs> JavaScriptMessageReceived;

        /// <summary>
        /// Yeni CefSharp tarayıcı oluşturur ve belirtilen URL'ye yönlendirir.
        /// Cef.Initialize önceden çağrılmış olmalıdır.
        /// </summary>
        public CefBrowserHost(string url)
        {
            if (Cef.IsInitialized != true)
                throw new InvalidOperationException("CefSharp başlatılmamış. Cef.Initialize önce çağrılmalıdır.");

            _browser = new ChromiumWebBrowser(url);
            _browser.Dock = DockStyle.Fill;

            _browser.FrameLoadEnd += OnFrameLoadEnd;
            _browser.JavascriptMessageReceived += OnJavascriptMessageReceived;
        }

        public void Navigate(string url)
        {
            if (_browser == null) return;
            _browser.LoadUrl(url);
        }

        public void Reload()
        {
            _browser?.Reload();
        }

        public void ExecuteScript(string script)
        {
            _browser?.ExecuteScriptAsync(script);
        }

        public void ShowDevTools()
        {
            _browser?.ShowDevTools();
        }

        public async Task<bool> PrintToPdfAsync(string path)
        {
            if (_browser == null) return false;
            return await _browser.PrintToPdfAsync(path, new PdfPrintSettings());
        }

        private void OnFrameLoadEnd(object sender, FrameLoadEndEventArgs e)
        {
            if (e.Frame.IsMain)
            {
                FrameLoadCompleted?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnJavascriptMessageReceived(object sender, JavascriptMessageReceivedEventArgs e)
        {
            JavaScriptMessageReceived?.Invoke(this, new BrowserMessageEventArgs { Message = e.Message });
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_browser != null)
            {
                _browser.FrameLoadEnd -= OnFrameLoadEnd;
                _browser.JavascriptMessageReceived -= OnJavascriptMessageReceived;
                _browser.Dispose();
                _browser = null;
            }
        }
    }
}
