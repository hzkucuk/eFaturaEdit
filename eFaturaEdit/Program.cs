using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;
using DevExpress.LookAndFeel;

namespace eFaturaEdit
{
    static class Program
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        private const int SW_RESTORE = 9;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Any(a => a.StartsWith("--type=")))
            {
                // CEF sub-process executed via Main Executable
                InitializeCef();
                return;
            }

            bool createdNew;
            using (var mutex = new Mutex(true, "eFaturaEdit_SingleInstance_2E28929B", out createdNew))
            {
                if (!createdNew)
                {
                    ActivateExistingInstance();
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                DevExpress.Skins.SkinManager.EnableFormSkins();
                DevExpress.UserSkins.BonusSkins.Register();
                UserLookAndFeel.Default.SetSkinStyle("DevExpress Style");

                // CefSharp yalnızca seçili motor CefSharp ise başlatılır
                if (GetSelectedBrowserEngine() == BrowserEngineType.CefSharp)
                {
                    InitializeCef();
                }

                Application.Run(new Form1());
            }
        }

        /// <summary>
        /// Kullanıcı ayarlarından seçili tarayıcı motorunu okur.
        /// </summary>
        internal static BrowserEngineType GetSelectedBrowserEngine()
        {
            string setting = Properties.Settings.Default.BrowserEngine;
            if (string.Equals(setting, "WebView2", StringComparison.OrdinalIgnoreCase))
                return BrowserEngineType.WebView2;

            return BrowserEngineType.CefSharp;
        }

        private static void ActivateExistingInstance()
        {
            var current = Process.GetCurrentProcess();
            foreach (var process in Process.GetProcessesByName(current.ProcessName))
            {
                if (process.Id != current.Id && process.MainWindowHandle != IntPtr.Zero)
                {
                    if (IsIconic(process.MainWindowHandle))
                        ShowWindow(process.MainWindowHandle, SW_RESTORE);

                    SetForegroundWindow(process.MainWindowHandle);
                    return;
                }
            }

            //    "e-Fatura Edit zaten çalışıyor.",
            //    "e-Fatura Edit",
            //    MessageBoxButtons.OK,
            //    MessageBoxIcon.Information);
        }

        /// <summary>
        /// CefSharp'ı Application.Run öncesinde başlatır.
        /// </summary>
        private static void InitializeCef()
        {
            if (Cef.IsInitialized == true)
                return;

            try
            {
                var settings = new CefSettings();

                // Let's rely primarily on root cache path and minimal config
                settings.RootCachePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "CefSharp", "Cache");

                settings.LogSeverity = LogSeverity.Error;

                // Fix for CefSharp 138+ admin execution (AutoDeElevate workaround - see CEF #3960 / CefSharp #5135)
                settings.CefCommandLineArgs.Add("do-not-de-elevate", "1");
                settings.CefCommandLineArgs.Add("disable-features", "AutoDeElevate");

                bool result = Cef.Initialize(settings, performDependencyCheck: false, browserProcessHandler: null);

                if (!result) 
                {
                    MessageBox.Show(
                        $"CefSharp başlatılamadı (Cef.Initialize == false).\nGerekli yerel kütüphaneler '{AppDomain.CurrentDomain.BaseDirectory}' dizininde eksik ya da bozuk olabilir.",
                        "CefSharp Başlatma Hatası",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"CefSharp başlatma sırasında hata oluştu:\n{ex.GetType().Name}\n{ex.Message}\n\n{ex.StackTrace}",
                    "CefSharp Başlatma Hatası",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}