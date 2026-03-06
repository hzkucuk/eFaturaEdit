using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace eFaturaEdit
{
    /// <summary>
    /// Önizleme tarayıcısı için motor-bağımsız soyutlama.
    /// CefSharp ve WebView2 implementasyonları bu arayüzü kullanır.
    /// </summary>
    public interface IBrowserHost : IDisposable
    {
        /// <summary>Tarayıcı motorunun türü.</summary>
        BrowserEngineType EngineType { get; }

        /// <summary>Tarayıcının gömülü olduğu WinForms kontrolü.</summary>
        Control BrowserControl { get; }

        /// <summary>Tarayıcı kullanıma hazır mı?</summary>
        bool IsReady { get; }

        /// <summary>Belirtilen URL'ye yönlendirir.</summary>
        void Navigate(string url);

        /// <summary>Sayfayı yeniden yükler.</summary>
        void Reload();

        /// <summary>JavaScript kodunu sayfada çalıştırır.</summary>
        void ExecuteScript(string script);

        /// <summary>Geliştirici araçlarını açar.</summary>
        void ShowDevTools();

        /// <summary>Sayfayı PDF olarak kaydeder.</summary>
        Task<bool> PrintToPdfAsync(string path);

        /// <summary>Ana frame yüklendiğinde tetiklenir.</summary>
        event EventHandler FrameLoadCompleted;

        /// <summary>JavaScript'ten gelen mesaj alındığında tetiklenir.</summary>
        event EventHandler<BrowserMessageEventArgs> JavaScriptMessageReceived;
    }

    /// <summary>
    /// JavaScript'ten gelen mesajları taşıyan olay argümanı.
    /// </summary>
    public class BrowserMessageEventArgs : EventArgs
    {
        /// <summary>JavaScript'ten gelen ham mesaj objesi.</summary>
        public dynamic Message { get; set; }
    }
}
