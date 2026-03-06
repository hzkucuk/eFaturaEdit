using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace eFaturaEdit
{
    /// <summary>
    /// Microsoft WebView2 (Edge) tabanlı tarayıcı implementasyonu.
    /// </summary>
    internal sealed class WebView2BrowserHost : IBrowserHost
    {
        private WebView2 _webView;
        private bool _disposed;
        private bool _isReady;
        private string _pendingUrl;

        public BrowserEngineType EngineType => BrowserEngineType.WebView2;
        public Control BrowserControl => _webView;
        public bool IsReady => _isReady;

        public event EventHandler FrameLoadCompleted;
        public event EventHandler<BrowserMessageEventArgs> JavaScriptMessageReceived;

        /// <summary>
        /// Yeni WebView2 tarayıcı oluşturur ve belirtilen URL'ye yönlendirir.
        /// WebView2 Runtime sistemde yüklü olmalıdır.
        /// </summary>
        public WebView2BrowserHost(string url)
        {
            _pendingUrl = url;
            _webView = new WebView2();
            _webView.Dock = DockStyle.Fill;

            _webView.CoreWebView2InitializationCompleted += OnCoreWebView2InitializationCompleted;
            _webView.NavigationCompleted += OnNavigationCompleted;

            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            try
            {
                var userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "eFaturaEdit", "WebView2");

                var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                await _webView.EnsureCoreWebView2Async(env);

                _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
                _isReady = true;

                if (!string.IsNullOrEmpty(_pendingUrl))
                {
                    Navigate(_pendingUrl);
                    _pendingUrl = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"WebView2 başlatılamadı:\n{ex.Message}\n\nMicrosoft Edge WebView2 Runtime yüklü olduğundan emin olun.",
                    "WebView2 Başlatma Hatası",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        public void Navigate(string url)
        {
            if (_webView == null) return;

            if (!_isReady)
            {
                _pendingUrl = url;
                return;
            }

            // file:// şemasına dönüştür
            if (File.Exists(url))
            {
                url = new Uri(url).AbsoluteUri;
            }

            _webView.CoreWebView2.Navigate(url);
        }

        public void Reload()
        {
            if (_isReady)
                _webView?.Reload();
        }

        public void ExecuteScript(string script)
        {
            if (_isReady && _webView?.CoreWebView2 != null)
            {
                _webView.CoreWebView2.ExecuteScriptAsync(script);
            }
        }

        public void ShowDevTools()
        {
            if (_isReady)
                _webView?.CoreWebView2?.OpenDevToolsWindow();
        }

        public async Task<bool> PrintToPdfAsync(string path)
        {
            if (!_isReady || _webView?.CoreWebView2 == null)
                return false;

            try
            {
                await _webView.CoreWebView2.PrintToPdfAsync(path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void OnCoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            if (!e.IsSuccess)
            {
                MessageBox.Show(
                    "WebView2 başlatma başarısız oldu.",
                    "WebView2 Hatası",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void OnNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                FrameLoadCompleted?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                string json = e.WebMessageAsJson;
                var serializer = new JavaScriptSerializer();
                var dict = serializer.Deserialize<Dictionary<string, object>>(json);

                // dynamic erişim için ExpandoObject'e dönüştür
                dynamic expando = new ExpandoObject();
                var expandoDict = (IDictionary<string, object>)expando;
                foreach (var kvp in dict)
                {
                    expandoDict[kvp.Key] = kvp.Value;
                }

                JavaScriptMessageReceived?.Invoke(this, new BrowserMessageEventArgs { Message = expando });
            }
            catch
            {
                // Geçersiz mesajları sessizce yoksay
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_webView != null)
            {
                _webView.CoreWebView2InitializationCompleted -= OnCoreWebView2InitializationCompleted;
                _webView.NavigationCompleted -= OnNavigationCompleted;
                if (_webView.CoreWebView2 != null)
                    _webView.CoreWebView2.WebMessageReceived -= OnWebMessageReceived;
                _webView.Dispose();
                _webView = null;
            }
        }
    }
}
