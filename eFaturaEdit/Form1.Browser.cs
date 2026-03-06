using System;
using System.Windows.Forms;
using DevExpress.XtraBars;
using DevExpress.XtraBars.Ribbon;

namespace eFaturaEdit
{
    public partial class Form1
    {
        #region Faz 2 — Önizleme Üzerine Drag-Drop

        /// <summary>
        /// Tarayıcıya motora uygun JavaScript drop handler enjekte eder.
        /// Her sayfa yüklendiğinde otomatik çağrılır.
        /// </summary>
        private void InjectDropHandler()
        {
            if (_browserHost == null) return;

            _browserHost.ExecuteScript(BrowserDropBridge.GetDropHandlerScript(_currentEngine));
        }

        /// <summary>
        /// Tarayıcı kurulduğunda çağrılır — JS message ve frame load handler'larını bağlar.
        /// </summary>
        private void SetupBrowserDropEvents()
        {
            if (_browserHost == null) return;

            _browserHost.JavaScriptMessageReceived -= BrowserHost_JavaScriptMessageReceived;
            _browserHost.JavaScriptMessageReceived += BrowserHost_JavaScriptMessageReceived;

            _browserHost.FrameLoadCompleted -= BrowserHost_FrameLoadCompleted;
            _browserHost.FrameLoadCompleted += BrowserHost_FrameLoadCompleted;
        }

        private void BrowserHost_FrameLoadCompleted(object sender, EventArgs e)
        {
            InjectDropHandler();
        }

        private void BrowserHost_JavaScriptMessageReceived(object sender, BrowserMessageEventArgs e)
        {
            dynamic message = e.Message;

            string msgType;
            try { msgType = (string)message.type; }
            catch { return; }

            if (msgType != "efatura-drop") return;

            var args = new BrowserDropEventArgs
            {
                SnippetKey = (string)message.snippetKey,
                TargetTagName = (string)message.targetTagName,
                TargetId = (string)message.targetId,
                TargetOuterHtmlPrefix = (string)message.targetOuterHtmlPrefix,
                TargetTextPrefix = (string)message.targetTextPrefix,
            };

            // UI thread'e geç
            this.BeginInvoke(new Action(() => HandlePreviewDrop(args)));
        }

        /// <summary>
        /// Önizlemeye bırakılan snippet'i XSLT kaynağında uygun yere ekler.
        /// </summary>
        private void HandlePreviewDrop(BrowserDropEventArgs args)
        {
            if (!XsltSnippets.Elements.ContainsKey(args.SnippetKey))
                return;

            string code = XsltSnippets.Elements[args.SnippetKey].XsltCode;
            string xsltSource = textEditorControlEx1.Text;

            int insertPos = BrowserDropBridge.FindInsertPosition(xsltSource, args);

            if (insertPos >= 0)
            {
                // XSLT kaynağında bulunan pozisyona ekle
                var doc = textEditorControlEx1.Document;
                doc.Insert(insertPos, "\n" + code + "\n");
                textEditorControlEx1.Refresh();

                // İmleci eklenen kodun sonuna taşı
                var newPos = doc.OffsetToPosition(insertPos + code.Length + 2);
                textEditorControlEx1.ActiveTextAreaControl.Caret.Position = newPos;
            }
            else
            {
                // Pozisyon bulunamadı — imleç pozisyonuna ekle
                InsertSnippetAtCursor(code);
                MessageBox.Show(
                    "Önizlemedeki hedef konum XSLT kaynağında eşleştirilemedi.\nSnippet imleç pozisyonuna eklendi.",
                    "Bilgi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }

            UpdateAndCheckFoldings();
        }

        #endregion

        #region Tarayıcı Motor Yönetimi

        /// <summary>
        /// Seçili motora göre tarayıcı oluşturur, önizleme paneline ekler ve drop olaylarını bağlar.
        /// </summary>
        private void CreateBrowserHost(string url)
        {
            try
            {
                _browserHost?.Dispose();
                _browserHost = null;

                if (_currentEngine == BrowserEngineType.WebView2)
                {
                    _browserHost = new WebView2BrowserHost(url);
                }
                else
                {
                    if (CefSharp.Cef.IsInitialized != true)
                    {
                        MessageBox.Show(
                            "CefSharp başlatılamadı. Önizleme kullanılamıyor.\nCef log dosyasını kontrol edin.",
                            "Önizleme Hatası",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }
                    _browserHost = new CefBrowserHost(url);
                }

                SetupBrowserDropEvents();

                xtraTabControl2.TabPages[0].Controls.Clear();
                xtraTabControl2.TabPages[0].Controls.Add(_browserHost.BrowserControl);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Tarayıcı oluşturulamadı ({_currentEngine}):\n{ex.Message}",
                    "Tarayıcı Hatası",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Tarayıcıyı kapatır ve CefSharp kullanılıyorsa Cef.Shutdown çağırır.
        /// </summary>
        private void ShutdownBrowser()
        {
            _browserHost?.Dispose();
            _browserHost = null;

            if (CefSharp.Cef.IsInitialized == true)
                CefSharp.Cef.Shutdown();
        }

        #endregion

        #region Tarayıcı Motor Seçici UI

        /// <summary>
        /// Ribbon'a tarayıcı motor seçici (CefSharp / WebView2) ekler.
        /// Seçim değiştiğinde ayarlar kaydedilir ve uygulama yeniden başlatma istenir.
        /// </summary>
        private void InitBrowserEngineSelector()
        {
            var engineGroup = new RibbonPageGroup("Tarayıcı Motoru");

            var barEngine = new BarEditItem
            {
                Caption = "Motor",
                Name = "barBrowserEngine",
                Width = 120,
            };

            var cmbEngine = new DevExpress.XtraEditors.Repository.RepositoryItemComboBox();
            cmbEngine.Items.AddRange(new object[] { "CefSharp", "WebView2" });
            cmbEngine.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            ribbonControl.RepositoryItems.Add(cmbEngine);

            barEngine.Edit = cmbEngine;
            barEngine.EditValue = _currentEngine.ToString();

            barEngine.EditValueChanged += BrowserEngineSelector_EditValueChanged;

            ribbonControl.Items.Add(barEngine);
            engineGroup.ItemLinks.Add(barEngine);

            homeRibbonPage.Groups.Add(engineGroup);
        }

        private void BrowserEngineSelector_EditValueChanged(object sender, EventArgs e)
        {
            var barEdit = sender as BarEditItem;
            if (barEdit == null) return;

            string selected = barEdit.EditValue as string;
            if (string.IsNullOrEmpty(selected)) return;

            BrowserEngineType newEngine = string.Equals(selected, "WebView2", StringComparison.OrdinalIgnoreCase)
                ? BrowserEngineType.WebView2
                : BrowserEngineType.CefSharp;

            if (newEngine == _currentEngine) return;

            // Ayarı kaydet
            Properties.Settings.Default.BrowserEngine = selected;
            Properties.Settings.Default.Save();

            MessageBox.Show(
                $"Tarayıcı motoru '{selected}' olarak ayarlandı.\n\nDeğişikliğin uygulanması için programı yeniden başlatın.",
                "Tarayıcı Motoru Değiştirildi",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        #endregion
    }
}
