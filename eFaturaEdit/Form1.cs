using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DevExpress.XtraBars;
using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraBars.Helpers;
using DevExpress.Skins;
using DevExpress.LookAndFeel;
using DevExpress.UserSkins;
using System.Xml.Xsl;
using System.IO;

using CefSharp;
using CefSharp.WinForms;
using System.Xml;
using Saxon.Api;
using DevExpress.XtraTab.Buttons;
using DevExpress.XtraEditors.Controls;
using System.Threading.Tasks;
using System.Collections;
using System.Drawing;

using QLicense;
using eFaturaLicense;
using System.Reflection;

namespace eFaturaEdit
{
    
    public partial class Form1 : RibbonForm

    {
        byte[] _certPubicKeyData;


        private ChromiumWebBrowser brow;
        readonly CefSettings settings;
        private string _activeDragSnippetKey;
        public Form1()
        {
            InitializeComponent();
            InitSkinGallery();
            settings  = new CefSettings();


            Cef.Initialize(settings);
            InitSnippetToolbar();
            InitSampleXmlToolbar();
            InitEditorDragDrop();
        }
        void InitSkinGallery()
        {
            SkinHelper.InitSkinGallery(rgbiSkins, true);
        }
        private void textEditorControlEx1_TextChanged(object sender, EventArgs e)
        {
            UpdateAndCheckFoldings();
        }

        private void UpdateAndCheckFoldings()
        {
            textEditorControlEx1.Document.FoldingManager.UpdateFoldings(null, null);
            textEditorControlEx2.Document.FoldingManager.UpdateFoldings(null, null);
        barStaticItem1.Caption  = string.Join("\r\n", textEditorControlEx1.GetFoldingErrors());
            barStaticItem2.Caption = string.Join("\r\n", textEditorControlEx2.GetFoldingErrors());
            iSave.Enabled = true;

        }

        private void barButtonItem1_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (brow == null) return;
            try
            {
                brow.ShowDevTools();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Geliştirici aracu açılamadı: {ex.Message}",
                    "Hata",
                    MessageBoxButtons.OK);
            }
        }

        private void iSave_ItemClick(object sender, ItemClickEventArgs e)
        {
            try
            {
                textEditorControlEx1.SaveFile(textEditorControlEx1.Tag.ToString());
                textEditorControlEx2.SaveFile(textEditorControlEx2.Tag.ToString());

                iSave.Enabled = false;
            }
            catch (Exception ex)
            {

                MessageBox.Show("Dosya işlenemedi" + ex.Message, "Dosya Açma Hatası", MessageBoxButtons.AbortRetryIgnore);
            }

            try
            {
                myXslTrans.Load(textEditorControlEx1.Tag.ToString());
                myXslTrans.Transform(textEditorControlEx2.Tag.ToString(), @"result.html");
            }
            catch (Exception ex)
            {

                MessageBox.Show("Dosya işlenemedi" + ex.Message, "Dosya Açma Hatası", MessageBoxButtons.AbortRetryIgnore);
            }
            try
            {
                brow?.Reload();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Sayfa yenilenemedi: " + ex.Message, "Yenileme Hatası", MessageBoxButtons.OK);
            }



        }

        private void iExit_ItemClick(object sender, ItemClickEventArgs e)
        {
            Cef.Shutdown();
            Application.Exit();
        }
        readonly XslCompiledTransform myXslTrans = new XslCompiledTransform();
    

        private  void iOpen_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {

                    textEditorControlEx1.LoadFile(openFileDialog1.FileName);
                    textEditorControlEx1.Tag = openFileDialog1.FileName;
                  //  textEditorControlEx2.Tag = openFileDialog1.FileName;

                    xtraTabControl1.TabPages[0].Text = null;
                    xtraTabControl1.TabPages[0].Text = textEditorControlEx1.Tag.ToString();

                    //UpdateAndCheckFoldings();
                    XsltSettings xsettings = new XsltSettings(true, true);
                    myXslTrans.Load(openFileDialog1.FileName, xsettings, new XmlUrlResolver());







                    myXslTrans.Transform(Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "XMLDataFiles", "fatura.xml"), "result.html");








                    brow?.Dispose();
                    brow = new ChromiumWebBrowser(Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "result.html"));
                    SetupBrowserDropEvents();

                    brow.Dock = DockStyle.Fill;

                    xtraTabControl2.TabPages[0].Controls.Clear();
                    xtraTabControl2.TabPages[0].Controls.Add(brow);
                 
                    xtraTabControl2.TabPages[0].Text = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "XMLDataFiles", "fatura.xml");

                    xtraTabControl2.TabPages[1].Text = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "XMLDataFiles", "fatura.xml");
                    textEditorControlEx2.LoadFile(Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "XMLDataFiles", "fatura.xml"), true, true);
                    textEditorControlEx2.Tag = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "XMLDataFiles", "fatura.xml");
   
                    iSave.Enabled = false;
                    iSaveAs.Enabled = true;
                    barButtonItem1.Enabled = true;
                    barButtonItem2.Enabled = true;
                 //   bool x = await brow.PrintToPdfAsync("M:\\chromium_pdf.pdf");


                }
                catch (Exception ex)
                {

                    MessageBox.Show("Dosya işlenemedi" + ex.Message, "Dosya Açma Hatası", MessageBoxButtons.AbortRetryIgnore);
                }


            }

        }

        public static string TransformXml(string xmlData, string xslData)
        {
            var xsltProcessor = new Processor();
            var documentBuilder = xsltProcessor.NewDocumentBuilder();
            documentBuilder.BaseUri = new Uri("file://");
            var xdmNode = documentBuilder.Build(new StringReader(xmlData));

            var xsltCompiler = xsltProcessor.NewXsltCompiler();
            var xsltExecutable = xsltCompiler.Compile(new StringReader(xslData));
            var xsltTransformer = xsltExecutable.Load();
            xsltTransformer.InitialContextNode = xdmNode;

            var results = new XdmDestination();

            xsltTransformer.Run(results);
            return results.XdmNode.OuterXml;
        }


        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (Cef.IsInitialized == true)
                Cef.Shutdown();
        }

        private void barButtonItem2_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (brow == null) return;
            try
            {
                brow.Reload();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Sayfa yenilenemedi: " + ex.Message, "Yenileme Hatası", MessageBoxButtons.OK);
            }
        }

        private void iSaveAs_ItemClick(object sender, ItemClickEventArgs e)
        {
            using (SaveFileDialog saveFileDialog1 = new SaveFileDialog() { Filter = "e-Fatura Dizayn Dosyası XSLT|*.xslt", Title = "Farklı Kaydet" })
            {
                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        textEditorControlEx1.SaveFile(saveFileDialog1.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Dosya kaydedilemedi: " + ex.Message, "Kaydetme Hatası", MessageBoxButtons.OK);
                    }
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            xtraTabControl1.TabPages[0].Text = null;
            xtraTabControl1.CustomHeaderButtons.Add(new CustomHeaderButton(ButtonPredefines.Glyph));
        }

        private void Form1_Shown( object sender, EventArgs e)
        {
            //Initialize variables with default values
            MyLicense _lic = null;
            string _msg = string.Empty;
            LicenseStatus _status = LicenseStatus.UNDEFINED;

            //Read public key from assembly
            Assembly _assembly = Assembly.GetExecutingAssembly();
            using (MemoryStream _mem = new MemoryStream())
            {
                var _stream = _assembly.GetManifestResourceStream("eFaturaEdit.LicenseVerify.cer");
                if (_stream == null)
                    throw new InvalidOperationException("LicenseVerify.cer kayna\u011f\u0131 bulunamad\u0131.");
                _stream.CopyTo(_mem);
                _certPubicKeyData = _mem.ToArray();
            }

            //Check if the XML license file exists
            if (File.Exists("license.lic"))
            {
                _lic = (MyLicense)LicenseHandler.ParseLicenseFromBASE64String(
                    typeof(MyLicense),
                    File.ReadAllText("license.lic"),
                    _certPubicKeyData,
                    out _status,
                    out _msg);
            }
            else
            {
                _status = LicenseStatus.INVALID;
                _msg = "Bu uygulamanın kopyası etkinleştirilmedi";
            }

            switch (_status)
            {
                case LicenseStatus.VALID:

                    //TODO: If license is valid, you can do extra checking here
                    //TODO: E.g., check license expiry date if you have added expiry date property to your license entity
                    //TODO: Also, you can set feature switch here based on the different properties you added to your license entity 

                    //Here for demo, just show the license information and RETURN without additional checking       
                //   AboutBox1.licInfo.ShowLicenseInfo(_lic);

                    return;

                default:
                    //for the other status of license file, show the warning message
                    //and also popup the activation form for user to activate your application
                    MessageBox.Show(_msg, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    using (frmActivation frm = new frmActivation())
                    {
                        frm.CertificatePublicKeyData = _certPubicKeyData;
                        frm.ShowDialog();

                        //Exit the application after activation to reload the license file 
                        //Actually it is not nessessary, you may just call the API to reload the license file
                        //Here just simplied the demo process

                        Application.Exit();
                    }
                    break;
            }
        }

        private void iAbout_ItemClick(object sender, ItemClickEventArgs e)
        {
            AboutBox1 a = new AboutBox1();

          //  a.Text = Tools.Value().GetHashCode().ToString();
            a.Show();
        }

        private  void xtraTabControl2_CustomHeaderButtonClick(object sender, DevExpress.XtraTab.ViewInfo.CustomHeaderButtonEventArgs e)
        {
            if (e.Button.Kind == ButtonPredefines.Glyph)
            {
                if (openFileDialog2.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    try
                    {

                        //textEditorControlEx1.LoadFile(openFileDialog1.FileName);
                     //   textEditorControlEx2.Tag = openFileDialog2.FileName;

                        // xtraTabControl1.TabPages[0].Text = null;
                        // xtraTabControl1.TabPages[0].Text = textEditorControlEx1.Tag.ToString();

                        // UpdateAndCheckFoldings();
                        XsltSettings xsettings = new XsltSettings(true, true);
                        myXslTrans.Load(openFileDialog1.FileName, xsettings, new XmlUrlResolver());

                        myXslTrans.Transform(openFileDialog2.FileName, @"result.html");








                        brow?.Dispose();
                        brow = new ChromiumWebBrowser(Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "result.html"));
                        SetupBrowserDropEvents();

                        brow.Dock = DockStyle.Fill;
                   
                        xtraTabControl2.TabPages[0].Controls.Clear();
                        xtraTabControl2.TabPages[0].Controls.Add(brow);
                        xtraTabControl2.TabPages[0].Text = openFileDialog2.FileName;
                        // xtraTabControl2.TabPages[1].Controls.Clear();
                        xtraTabControl2.TabPages[1].Text = openFileDialog2.FileName;
                        textEditorControlEx2.LoadFile(openFileDialog2.FileName, true, true);
                        textEditorControlEx2.Tag = openFileDialog2.FileName;
                        UpdateAndCheckFoldings();

                        iSave.Enabled = false;
                        iSaveAs.Enabled = true;
                        barButtonItem1.Enabled = true;
                        barButtonItem2.Enabled = true;

                    }
                    catch (Exception ex)
                    {

                        MessageBox.Show("Dosya işlenemedi" + ex.Message, "Dosya Açma Hatası", MessageBoxButtons.AbortRetryIgnore);
                    }

                }
            }
        
        }

        private void textEditorControlEx2_TextChanged(object sender, EventArgs e)
        {
            UpdateAndCheckFoldings();
        }

        private async void barButtonItem3_ItemClick(object sender, ItemClickEventArgs e)
        {
            using (SaveFileDialog saveFileDialog1 = new SaveFileDialog() { Filter = "e-Fatura Dizayn PDF|*.pdf", Title = "Save File" })
            {
                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(saveFileDialog1.FileName))
                        {
                            await GeneratePdf(saveFileDialog1.FileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("PDF oluşturulamadı: " + ex.Message, "PDF Hatası", MessageBoxButtons.OK);
                    }
                }
            }
        }

        private async Task GeneratePdf(string pdff)
        {
            var path = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            var generatedPdfFile = Path.Combine(path, pdff);

            var chromeSettings = new PdfPrintSettings();

            var pdfFileSaved = await brow.PrintToPdfAsync(generatedPdfFile, chromeSettings);
            if (!pdfFileSaved)
            {
                MessageBox.Show("PDF dosyası kaydedilemedi.", "PDF Hatası", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        #region Öğe Ekleme Toolbar — Faz 1 (Editör) + Faz 2 (Önizleme)

        /// <summary>
        /// Ribbon'a "Öğe Ekle" grubu ve her snippet için buton ekler.
        /// Butonlar: tıklama → editöre ekleme, sürükleme → editör veya önizlemeye bırakma.
        /// </summary>
        private void InitSnippetToolbar()
        {
            var snippetPageGroup = new RibbonPageGroup("Öğe Ekle");

            foreach (var kvp in XsltSnippets.Elements)
            {
                var snippet = kvp.Value;
                var btn = new BarButtonItem
                {
                    Caption = snippet.DisplayName,
                    Tag = snippet.Key,
                    Name = "btnSnippet_" + snippet.Key,
                    AllowAllUp = true,
                };
                btn.SuperTip = CreateSnippetTooltip(snippet);
                btn.ItemClick += SnippetButton_ItemClick;

                ribbonControl.Items.Add(btn);
                snippetPageGroup.ItemLinks.Add(btn);
            }

            homeRibbonPage.Groups.Add(snippetPageGroup);

            // Snippet sürükleme için Ribbon MouseDown hook
            ribbonControl.MouseDown += RibbonControl_SnippetMouseDown;
        }

        #endregion

        #region Örnek XML Senaryo Toolbar

        /// <summary>
        /// Ribbon'a "Örnek Faturalar" grubu ve kategorilere göre alt menü butonları ekler.
        /// UBL-TR 1.2.1 resmi örnek XML dosyaları kullanıcıya sunulur.
        /// </summary>
        private void InitSampleXmlToolbar()
        {
            if (!UblTrSamples.SamplesDirectoryExists())
                return;

            var samplePageGroup = new RibbonPageGroup("Örnek Faturalar");

            foreach (var group in UblTrSamples.Groups)
            {
                var subMenu = new BarSubItem
                {
                    Caption = group.CategoryName,
                    Name = "subSample_" + group.CategoryName.Replace(" ", ""),
                };

                foreach (var entry in group.Entries)
                {
                    var item = new BarButtonItem
                    {
                        Caption = entry.DisplayName,
                        Tag = entry.FileName,
                        Name = "btnSample_" + Path.GetFileNameWithoutExtension(entry.FileName),
                    };
                    item.ItemClick += SampleXml_ItemClick;
                    subMenu.AddItem(item);
                    ribbonControl.Items.Add(item);
                }

                ribbonControl.Items.Add(subMenu);
                samplePageGroup.ItemLinks.Add(subMenu);
            }

            homeRibbonPage.Groups.Add(samplePageGroup);
        }

        /// <summary>
        /// Örnek XML seçildiğinde: XML'i editöre yükler, XSLT açıksa dönüşüm yapar ve önizler.
        /// </summary>
        private void SampleXml_ItemClick(object sender, ItemClickEventArgs e)
        {
            string fileName = e.Item.Tag as string;
            if (string.IsNullOrEmpty(fileName))
                return;

            string fullPath = UblTrSamples.GetFullPath(fileName);
            if (!File.Exists(fullPath))
            {
                MessageBox.Show(
                    $"Örnek XML dosyası bulunamadı:\n{fullPath}",
                    "Dosya Bulunamadı",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // XML editörüne yükle
                textEditorControlEx2.LoadFile(fullPath, true, true);
                textEditorControlEx2.Tag = fullPath;
                xtraTabControl2.TabPages[1].Text = fileName;

                // XSLT açıksa dönüşüm yap ve önizle
                if (textEditorControlEx1.Tag != null)
                {
                    XsltSettings xsettings = new XsltSettings(true, true);
                    myXslTrans.Load(textEditorControlEx1.Tag.ToString(), xsettings, new XmlUrlResolver());
                    myXslTrans.Transform(fullPath, @"result.html");

                    brow?.Dispose();
                    brow = new ChromiumWebBrowser(Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "result.html"));
                    SetupBrowserDropEvents();

                    brow.Dock = DockStyle.Fill;
                    xtraTabControl2.TabPages[0].Controls.Clear();
                    xtraTabControl2.TabPages[0].Controls.Add(brow);
                    xtraTabControl2.TabPages[0].Text = fileName;
                }

                UpdateAndCheckFoldings();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Örnek XML yüklenirken hata oluştu:\n{ex.Message}",
                    "Hata",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        #endregion

        private DevExpress.Utils.SuperToolTip CreateSnippetTooltip(SnippetInfo snippet)
        {
            var tip = new DevExpress.Utils.SuperToolTip();
            var titleItem = new DevExpress.Utils.ToolTipTitleItem { Text = snippet.DisplayName };
            var bodyItem = new DevExpress.Utils.ToolTipItem
            {
                Text = snippet.XsltCode.Length > 120
                    ? snippet.XsltCode.Substring(0, 120) + "..."
                    : snippet.XsltCode
            };
            tip.Items.Add(titleItem);
            tip.Items.Add(bodyItem);
            return tip;
        }

        /// <summary>
        /// Ribbon butonu tıklandığında XSLT editöründe imleç pozisyonuna snippet ekler.
        /// </summary>
        private void SnippetButton_ItemClick(object sender, ItemClickEventArgs e)
        {
            string key = e.Item.Tag as string;
            if (key == null || !XsltSnippets.Elements.ContainsKey(key))
                return;

            InsertSnippetAtCursor(XsltSnippets.Elements[key].XsltCode);
        }

        /// <summary>
        /// XSLT editöründe imleç pozisyonuna verilen kodu ekler.
        /// </summary>
        private void InsertSnippetAtCursor(string code)
        {
            var editor = textEditorControlEx1;
            var doc = editor.Document;
            var caret = editor.ActiveTextAreaControl.Caret;

            int offset = doc.PositionToOffset(caret.Position);
            doc.Insert(offset, code);
            editor.Refresh();

            // İmleci eklenen kodun sonuna taşı
            var newPos = doc.OffsetToPosition(offset + code.Length);
            caret.Position = newPos;

            UpdateAndCheckFoldings();
        }

        /// <summary>
        /// Ribbon üzerinde snippet butonuna mouse basılı tutulduğunda drag işlemini başlatır.
        /// </summary>
        private void RibbonControl_SnippetMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            var hitInfo = ribbonControl.CalcHitInfo(e.Location);
            if (hitInfo.Item == null || hitInfo.Item.Item == null) return;

            string key = hitInfo.Item.Item.Tag as string;
            if (key == null || !XsltSnippets.Elements.ContainsKey(key))
                return;

            _activeDragSnippetKey = key;
            var snippet = XsltSnippets.Elements[key];
            ribbonControl.DoDragDrop(snippet.DragDataString, DragDropEffects.Copy);
        }

        #region Faz 1 — XSLT Editörüne Drag-Drop

        /// <summary>
        /// XSLT editörünün drag-drop olaylarını bağlar.
        /// </summary>
        private void InitEditorDragDrop()
        {
            textEditorControlEx1.AllowDrop = true;
            textEditorControlEx1.ActiveTextAreaControl.TextArea.AllowDrop = true;
            textEditorControlEx1.ActiveTextAreaControl.TextArea.DragEnter += EditorTextArea_DragEnter;
            textEditorControlEx1.ActiveTextAreaControl.TextArea.DragDrop += EditorTextArea_DragDrop;
        }

        private void EditorTextArea_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.Text) ||
                e.Data.GetDataPresent(DataFormats.UnicodeText))
            {
                string data = e.Data.GetData(DataFormats.Text) as string ?? string.Empty;
                if (data.StartsWith(XsltSnippets.SnippetPrefix))
                {
                    e.Effect = DragDropEffects.Copy;
                    return;
                }
            }
            e.Effect = DragDropEffects.None;
        }

        private void EditorTextArea_DragDrop(object sender, DragEventArgs e)
        {
            string data = e.Data.GetData(DataFormats.Text) as string;
            if (string.IsNullOrEmpty(data) || !data.StartsWith(XsltSnippets.SnippetPrefix))
                return;

            string key = data.Substring(XsltSnippets.SnippetPrefix.Length);
            if (!XsltSnippets.Elements.ContainsKey(key))
                return;

            // Mouse pozisyonunu editör koordinatına çevir
            var textArea = textEditorControlEx1.ActiveTextAreaControl.TextArea;
            Point clientPoint = textArea.PointToClient(new Point(e.X, e.Y));

            var pos = textArea.TextView.GetLogicalPosition(
                Math.Max(0, clientPoint.X - textArea.TextView.DrawingPosition.X),
                Math.Max(0, clientPoint.Y - textArea.TextView.DrawingPosition.Y));

            var doc = textEditorControlEx1.Document;
            int offset = doc.PositionToOffset(pos);

            string code = XsltSnippets.Elements[key].XsltCode;
            doc.Insert(offset, code);
            textEditorControlEx1.Refresh();

            var newPos = doc.OffsetToPosition(offset + code.Length);
            textEditorControlEx1.ActiveTextAreaControl.Caret.Position = newPos;

            UpdateAndCheckFoldings();
        }

        #endregion

        #region Faz 2 — Önizleme (CefSharp) Üzerine Drag-Drop

        /// <summary>
        /// CefSharp tarayıcısına JavaScript drop handler enjekte eder.
        /// Her sayfa yüklendiğinde otomatik çağrılır.
        /// </summary>
        private void InjectDropHandler()
        {
            if (brow == null) return;

            brow.ExecuteScriptAsync(BrowserDropBridge.DropHandlerScript);
        }

        /// <summary>
        /// CefSharp tarayıcısı kurulduğunda çağrılır — JS message handler'ı bağlar.
        /// </summary>
        private void SetupBrowserDropEvents()
        {
            if (brow == null) return;

            brow.JavascriptMessageReceived -= Brow_JavascriptMessageReceived;
            brow.JavascriptMessageReceived += Brow_JavascriptMessageReceived;

            brow.FrameLoadEnd -= Brow_FrameLoadEnd;
            brow.FrameLoadEnd += Brow_FrameLoadEnd;
        }

        private void Brow_FrameLoadEnd(object sender, CefSharp.FrameLoadEndEventArgs e)
        {
            // Ana frame yüklendiğinde drop handler'ı enjekte et
            if (e.Frame.IsMain)
            {
                InjectDropHandler();
            }
        }

        private void Brow_JavascriptMessageReceived(object sender, CefSharp.JavascriptMessageReceivedEventArgs e)
        {
            // JavaScript'ten gelen drop mesajını işle
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
    }
}