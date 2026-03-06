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
using System.IO;

using CefSharp;
using CefSharp.WinForms;
using System.Xml;
using FontAwesome.Sharp;
using Saxon.Api;
using DevExpress.XtraTab.Buttons;
using DevExpress.XtraEditors.Controls;
using System.Threading.Tasks;
using System.Collections;

using QLicense;
using eFaturaLicense;
using System.Reflection;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Gui.CompletionWindow;

namespace eFaturaEdit
{

    public partial class Form1 : RibbonForm

    {
        byte[] _certPubicKeyData;


        private static readonly string AppDir = Path.GetDirectoryName(Application.ExecutablePath);
        private static readonly string XmlDataDir = Path.Combine(AppDir, "XMLDataFiles");
        private static readonly string DefaultXmlPath = Path.Combine(XmlDataDir, "fatura.xml");
        private static readonly string ResultHtmlPath = Path.Combine(AppDir, "result.html");

        private IBrowserHost _browserHost;
        private BrowserEngineType _currentEngine;
        private string _activeDragSnippetKey;
        private CodeCompletionWindow _completionWindow;
        private RibbonPage _snippetRibbonPage;
        private RibbonPage _ublRibbonPage;
        private RibbonPage _formatRibbonPage;
        public Form1()
        {
            InitializeComponent();
            InitSkinGallery();

            _currentEngine = Program.GetSelectedBrowserEngine();

            InitSnippetToolbar();
            // InitSampleXmlToolbar();
            InitEditorDragDrop();
            InitAutoComplete();
            InitWysiwygToolbar();
            InitBrowserEngineSelector();
        }

        /// <summary>
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
            if (_browserHost == null) return;
            try
            {
                _browserHost.ShowDevTools();
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

                TransformXslFile(textEditorControlEx1.Tag.ToString(), textEditorControlEx2.Tag.ToString(), ResultHtmlPath);

                _browserHost?.Reload();
            }
            catch (Exception ex)
            {
                MessageBox.Show("İşlem sırasında hata oluştu: " + ex.Message,
                    "Hata",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void iExit_ItemClick(object sender, ItemClickEventArgs e)
        {
            ShutdownBrowser();
            Application.Exit();
        }
        private static readonly Processor _saxonProcessor = new Processor();

        /// <summary>
        /// Saxon-HE ile XSLT dosyasını XML dosyasına uygulayıp sonucu dosyaya yazar (XSLT 1.0/2.0/3.0 destekler).
        /// </summary>
        private static void TransformXslFile(string xsltPath, string xmlPath, string outputPath)
        {
            var compiler = _saxonProcessor.NewXsltCompiler();
            var executable = compiler.Compile(new Uri(Path.GetFullPath(xsltPath)));

            var docBuilder = _saxonProcessor.NewDocumentBuilder();
            var input = docBuilder.Build(new Uri(Path.GetFullPath(xmlPath)));

            var transformer = executable.Load();
            transformer.InitialContextNode = input;

            var serializer = _saxonProcessor.NewSerializer();
            serializer.SetOutputFile(outputPath);
            transformer.Run(serializer);
        }
    

        private void iOpen_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (openFileDialog1.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                textEditorControlEx1.LoadFile(openFileDialog1.FileName);
                textEditorControlEx1.Tag = openFileDialog1.FileName;

                xtraTabControl1.TabPages[0].Text = openFileDialog1.FileName;

                TransformXslFile(openFileDialog1.FileName, DefaultXmlPath, ResultHtmlPath);

                CreateBrowserHost(ResultHtmlPath);

                xtraTabControl2.TabPages[0].Text = DefaultXmlPath;
                xtraTabControl2.TabPages[1].Text = DefaultXmlPath;

                textEditorControlEx2.LoadFile(DefaultXmlPath, true, true);
                textEditorControlEx2.Tag = DefaultXmlPath;

                iSave.Enabled = false;
                iSaveAs.Enabled = true;
                barButtonItem1.Enabled = true;
                barButtonItem2.Enabled = true;
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                MessageBox.Show(
                    "XSLT dosyası işlenemedi.\n\n" +
                    "Olası nedenler:\n" +
                    "• XSLT dosyasında XML söz dizimi hatası olabilir.\n" +
                    "• Dosya bozuk veya eksik indirilebilmiş olabilir.\n\n" +
                    "Teknik detay:\n" + innerMsg,
                    "Dosya Açma Hatası",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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
            ShutdownBrowser();
        }

        private void barButtonItem2_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (_browserHost == null) return;
            try
            {
                _browserHost.Reload();
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
                        TransformXslFile(openFileDialog1.FileName, openFileDialog2.FileName, ResultHtmlPath);

                        CreateBrowserHost(ResultHtmlPath);

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
            if (_browserHost == null) return;

            var path = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            var generatedPdfFile = Path.Combine(path, pdff);

            var pdfFileSaved = await _browserHost.PrintToPdfAsync(generatedPdfFile);
            if (!pdfFileSaved)
            {
                MessageBox.Show("PDF dosyası kaydedilemedi.", "PDF Hatası", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        #region Öğe Ekleme Toolbar — Faz 1 (Editör) + Faz 2 (Önizleme)

        /// <summary>
        /// Ribbon'a snippet'leri kategorilerine göre ayrı sekmelere ekler.
        /// "Öğeler" sekmesi: HTML Öğeleri, XSLT Komutları, Sayfa Düzeni.
        /// "UBL-TR" sekmesi: e-Fatura, e-Arşiv, e-İrsaliye.
        /// SubCategory varsa alt menü (BarSubItem) oluşturur.
        /// </summary>
        private void InitSnippetToolbar()
        {
            // Yeni sekmeleri oluştur
            _snippetRibbonPage = new RibbonPage { Text = "Öğeler" };
            _ublRibbonPage = new RibbonPage { Text = "UBL-TR" };
            ribbonControl.Pages.Add(_snippetRibbonPage);
            ribbonControl.Pages.Add(_ublRibbonPage);

            var grouped = XsltSnippets.Elements.Values
                .GroupBy(s => s.Category)
                .OrderBy(g => GetCategoryOrder(g.Key));

            foreach (var group in grouped)
            {
                var pageGroup = new RibbonPageGroup(group.Key);

                // SubCategory'ye göre alt gruplama
                var subGrouped = group
                    .GroupBy(s => s.SubCategory ?? string.Empty)
                    .OrderBy(sg => sg.Key);

                foreach (var subGroup in subGrouped)
                {
                    var catColor = GetCategoryColor(group.Key);

                    if (string.IsNullOrEmpty(subGroup.Key))
                    {
                        // SubCategory yok — doğrudan buton ekle
                        foreach (var snippet in subGroup)
                        {
                            var btn = new BarButtonItem
                            {
                                Caption = snippet.DisplayName,
                                Tag = snippet.Key,
                                Name = "btnSnippet_" + snippet.Key,
                                AllowAllUp = true,
                                Glyph = CreateSnippetIcon(snippet.Key, catColor),
                            };
                            btn.SuperTip = CreateSnippetTooltip(snippet);
                            btn.ItemClick += SnippetButton_ItemClick;

                            ribbonControl.Items.Add(btn);
                            pageGroup.ItemLinks.Add(btn);
                        }
                    }
                    else
                    {
                        // SubCategory var — alt menü oluştur
                        var subMenu = new BarSubItem
                        {
                            Caption = subGroup.Key,
                            Name = "subSnippet_" + group.Key.Replace(" ", "") + "_" + subGroup.Key.Replace(" ", ""),
                            Glyph = CreateCategoryIcon(group.Key, catColor),
                        };

                        foreach (var snippet in subGroup)
                        {
                            var btn = new BarButtonItem
                            {
                                Caption = snippet.DisplayName,
                                Tag = snippet.Key,
                                Name = "btnSnippet_" + snippet.Key,
                                AllowAllUp = true,
                                Glyph = CreateSnippetIcon(snippet.Key, catColor),
                            };
                            btn.SuperTip = CreateSnippetTooltip(snippet);
                            btn.ItemClick += SnippetButton_ItemClick;

                            subMenu.AddItem(btn);
                            ribbonControl.Items.Add(btn);
                        }

                        ribbonControl.Items.Add(subMenu);
                        pageGroup.ItemLinks.Add(subMenu);
                    }
                }

                // Kategoriyi doğru sekmeye yönlendir
                GetRibbonPageForCategory(group.Key).Groups.Add(pageGroup);
            }

            // Snippet sürükleme için Ribbon MouseDown hook
            ribbonControl.MouseDown += RibbonControl_SnippetMouseDown;

            // Editör sağ tık menüsünü oluştur
            InitEditorContextMenu();
        }

        /// <summary>
        /// Snippet kategorisini doğru Ribbon sekmesine yönlendirir.
        /// </summary>
        private RibbonPage GetRibbonPageForCategory(string category)
        {
            switch (category)
            {
                case "UBL-TR e-Fatura":
                case "UBL-TR e-Arşiv":
                case "UBL-TR e-İrsaliye":
                    return _ublRibbonPage;
                default:
                    return _snippetRibbonPage;
            }
        }

        /// <summary>
        /// Kategori sıralama önceliği (Ribbon'da soldan sağa).
        /// </summary>
        private static int GetCategoryOrder(string category)
        {
            switch (category)
            {
                case "HTML Öğeleri": return 0;
                case "XSLT Komutları": return 1;
                case "Sayfa Düzeni": return 2;
                case "UBL-TR e-Fatura": return 3;
                case "UBL-TR e-Arşiv": return 4;
                default: return 99;
            }
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
                    TransformXslFile(textEditorControlEx1.Tag.ToString(), fullPath, ResultHtmlPath);

                    CreateBrowserHost(ResultHtmlPath);
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

        #region Editör Sağ Tık Menüsü

        /// <summary>
        /// XSLT editörüne sağ tık context menüsü ekler.
        /// Snippet'ler Category → SubCategory → Snippet şeklinde ağaç yapısında gösterilir.
        /// </summary>
        private void InitEditorContextMenu()
        {
            var ctxMenu = new ContextMenuStrip();
            ctxMenu.ShowImageMargin = true;
            ctxMenu.RenderMode = ToolStripRenderMode.Professional;

            // Snippet'leri kategoriye göre grupla
            var grouped = XsltSnippets.Elements.Values
                .GroupBy(s => s.Category)
                .OrderBy(g => GetCategoryOrder(g.Key));

            foreach (var group in grouped)
            {
                var catColor = GetCategoryColor(group.Key);
                var catItem = new ToolStripMenuItem(group.Key);
                catItem.Font = new Font(catItem.Font, FontStyle.Bold);
                catItem.Image = CreateCategoryIcon(group.Key, catColor);

                // SubCategory'ye göre alt gruplama
                var subGrouped = group
                    .GroupBy(s => s.SubCategory ?? string.Empty)
                    .OrderBy(sg => sg.Key);

                foreach (var subGroup in subGrouped)
                {
                    if (string.IsNullOrEmpty(subGroup.Key))
                    {
                        // SubCategory yok — doğrudan snippet ekle
                        foreach (var snippet in subGroup)
                        {
                            var item = CreateContextMenuItem(snippet, catColor);
                            catItem.DropDownItems.Add(item);
                        }
                    }
                    else
                    {
                        // SubCategory var — ara menü oluştur
                        var subCatItem = new ToolStripMenuItem(subGroup.Key);
                        subCatItem.Image = CreateCategoryIcon(group.Key, catColor);

                        foreach (var snippet in subGroup)
                        {
                            var item = CreateContextMenuItem(snippet, catColor);
                            subCatItem.DropDownItems.Add(item);
                        }

                        catItem.DropDownItems.Add(subCatItem);
                    }
                }

                ctxMenu.Items.Add(catItem);
            }

            // Editör TextArea'ya bağla
            textEditorControlEx1.ActiveTextAreaControl.TextArea.ContextMenuStrip = ctxMenu;
        }

        /// <summary>
        /// Sağ tık menüsü için snippet menü öğesi oluşturur.
        /// Renkli ikon + metin; ToolTipText tam açıklama metnini içerir (kısaltılmaz).
        /// </summary>
        private ToolStripMenuItem CreateContextMenuItem(SnippetInfo snippet, Color catColor)
        {
            var item = new ToolStripMenuItem(snippet.DisplayName);
            item.Image = CreateSnippetIcon(snippet.Key, catColor);
            item.ToolTipText = snippet.Description;
            item.Tag = snippet.Key;
            item.Click += ContextMenuSnippet_Click;
            return item;
        }

        /// <summary>
        /// Sağ tık menüsünden snippet seçildiğinde editöre ekler.
        /// </summary>
        private void ContextMenuSnippet_Click(object sender, EventArgs e)
        {
            var menuItem = sender as ToolStripMenuItem;
            if (menuItem == null) return;

            string key = menuItem.Tag as string;
            if (key == null || !XsltSnippets.Elements.ContainsKey(key))
                return;

            InsertSnippetAtCursor(XsltSnippets.Elements[key].XsltCode);
        }

        #endregion

        #region Snippet İkon Oluşturma (FontAwesome.Sharp)

        /// <summary>
        /// Her snippet anahtarına karşılık gelen FontAwesome ikonu.
        /// </summary>
        private static readonly Dictionary<string, FontAwesome.Sharp.IconChar> SnippetIconMap =
            new Dictionary<string, FontAwesome.Sharp.IconChar>
        {
            // ── HTML Öğeleri ──
            ["IMAGE"]   = FontAwesome.Sharp.IconChar.Image,
            ["TABLE"]   = FontAwesome.Sharp.IconChar.Table,
            ["TEXT"]    = FontAwesome.Sharp.IconChar.Paragraph,
            ["LINK"]    = FontAwesome.Sharp.IconChar.Link,
            ["HR"]      = FontAwesome.Sharp.IconChar.GripLines,
            ["DIV"]     = FontAwesome.Sharp.IconChar.BorderAll,
            ["BOLD"]    = FontAwesome.Sharp.IconChar.Bold,
            ["SPAN"]    = FontAwesome.Sharp.IconChar.Tag,

            // ── XSLT Komutları ──
            ["VALUEOF"]  = FontAwesome.Sharp.IconChar.Code,
            ["FOREACH"]  = FontAwesome.Sharp.IconChar.Redo,
            ["IF"]       = FontAwesome.Sharp.IconChar.QuestionCircle,

            // ── Sayfa Düzeni ──
            ["BARCODE"]   = FontAwesome.Sharp.IconChar.Barcode,
            ["QR"]        = FontAwesome.Sharp.IconChar.Qrcode,
            ["PAGEBREAK"] = FontAwesome.Sharp.IconChar.FileExport,
            ["HEADER"]    = FontAwesome.Sharp.IconChar.ArrowUp,
            ["FOOTER"]    = FontAwesome.Sharp.IconChar.ArrowDown,

            // ── UBL-TR e-Fatura — Başlık ──
            ["UBL_INVOICEHEADER"] = FontAwesome.Sharp.IconChar.FileInvoice,
            ["UBL_UUID"]          = FontAwesome.Sharp.IconChar.Key,
            ["UBL_ISSUETIME"]     = FontAwesome.Sharp.IconChar.Clock,
            ["UBL_NOTES"]         = FontAwesome.Sharp.IconChar.StickyNote,
            ["UBL_LINECOUNT"]     = FontAwesome.Sharp.IconChar.ListOl,
            ["UBL_COPYINDICATOR"] = FontAwesome.Sharp.IconChar.Copy,
            ["UBL_PROFILEID"]     = FontAwesome.Sharp.IconChar.Random,
            ["UBL_TYPECODE"]      = FontAwesome.Sharp.IconChar.Tags,
            ["UBL_CURRENCYID"]    = FontAwesome.Sharp.IconChar.MoneyBillAlt,

            // ── UBL-TR e-Fatura — Taraflar ──
            ["UBL_SUPPLIER"]          = FontAwesome.Sharp.IconChar.Building,
            ["UBL_CUSTOMER"]          = FontAwesome.Sharp.IconChar.User,
            ["UBL_SUPPLIER_CONTACT"]  = FontAwesome.Sharp.IconChar.Phone,
            ["UBL_CUSTOMER_CONTACT"]  = FontAwesome.Sharp.IconChar.MobileAlt,
            ["UBL_SUPPLIER_ADDRESS"]  = FontAwesome.Sharp.IconChar.MapMarkerAlt,
            ["UBL_CUSTOMER_ADDRESS"]  = FontAwesome.Sharp.IconChar.MapPin,
            ["UBL_PERSON"]            = FontAwesome.Sharp.IconChar.IdCard,
            ["UBL_PARTYIDS"]          = FontAwesome.Sharp.IconChar.IdBadge,
            ["UBL_WEBSITEURI"]        = FontAwesome.Sharp.IconChar.Globe,
            ["UBL_IDENTITYDOC"]       = FontAwesome.Sharp.IconChar.AddressCard,
            ["UBL_BUYERCUSTOMER"]     = FontAwesome.Sharp.IconChar.ShoppingCart,
            ["UBL_TAXREPRESENTATIVE"] = FontAwesome.Sharp.IconChar.Landmark,

            // ── UBL-TR e-Fatura — Kalemler ──
            ["UBL_INVOICELINES"]    = FontAwesome.Sharp.IconChar.ClipboardList,
            ["UBL_LINE_ALLOWANCE"]  = FontAwesome.Sharp.IconChar.Percent,
            ["UBL_LINE_WITHHOLDING"] = FontAwesome.Sharp.IconChar.Lock,

            // ── UBL-TR e-Fatura — Vergi ──
            ["UBL_TAXTOTAL"]      = FontAwesome.Sharp.IconChar.Coins,
            ["UBL_WITHHOLDING"]   = FontAwesome.Sharp.IconChar.BalanceScale,
            ["UBL_TAXEXEMPTION"]  = FontAwesome.Sharp.IconChar.Ban,
            ["UBL_TAXTYPEFILTER"] = FontAwesome.Sharp.IconChar.Filter,

            // ── UBL-TR e-Fatura — Toplamlar ──
            ["UBL_TOTALS"]          = FontAwesome.Sharp.IconChar.Calculator,
            ["UBL_CHARGETOTAL"]     = FontAwesome.Sharp.IconChar.PlusCircle,
            ["UBL_ALLOWANCECHARGE"] = FontAwesome.Sharp.IconChar.MinusCircle,
            ["UBL_EXCHANGERATE"]    = FontAwesome.Sharp.IconChar.ExchangeAlt,

            // ── UBL-TR e-Fatura — Ödeme ──
            ["UBL_PAYMENTMEANS"] = FontAwesome.Sharp.IconChar.CreditCard,
            ["UBL_PAYMENTTERMS"] = FontAwesome.Sharp.IconChar.CalendarAlt,

            // ── UBL-TR e-Fatura — Referanslar ──
            ["UBL_ORDERREF"]      = FontAwesome.Sharp.IconChar.Box,
            ["UBL_DESPATCHREF"]   = FontAwesome.Sharp.IconChar.Truck,
            ["UBL_BILLINGREF"]    = FontAwesome.Sharp.IconChar.FileInvoiceDollar,
            ["UBL_ADDITIONALDOC"] = FontAwesome.Sharp.IconChar.Paperclip,

            // ── UBL-TR e-Arşiv — Teslimat ──
            ["UBL_EA_DELIVERY"]     = FontAwesome.Sharp.IconChar.TruckLoading,
            ["UBL_EA_DELIVERYDATE"] = FontAwesome.Sharp.IconChar.CalendarCheck,
            ["UBL_EA_SHIPMENT"]     = FontAwesome.Sharp.IconChar.Ship,
            ["UBL_EA_CARRIER"]      = FontAwesome.Sharp.IconChar.TruckMoving,

            // ── UBL-TR e-Arşiv — E-Arşiv Özel ──
            ["UBL_EA_INTERNETSALES"]  = FontAwesome.Sharp.IconChar.Store,
            ["UBL_EA_SENDINGTYPE"]    = FontAwesome.Sharp.IconChar.PaperPlane,
            ["UBL_EA_PAYMENTCHANNEL"] = FontAwesome.Sharp.IconChar.University,
            ["UBL_EA_PAYMENTCODE"]    = FontAwesome.Sharp.IconChar.Receipt,
        };

        /// <summary>
        /// Kategori başlığına karşılık gelen FontAwesome ikonu.
        /// </summary>
        private static readonly Dictionary<string, FontAwesome.Sharp.IconChar> CategoryIconMap =
            new Dictionary<string, FontAwesome.Sharp.IconChar>
        {
            ["HTML Öğeleri"]    = FontAwesome.Sharp.IconChar.Html5,
            ["XSLT Komutları"]  = FontAwesome.Sharp.IconChar.Code,
            ["Sayfa Düzeni"]    = FontAwesome.Sharp.IconChar.Columns,
            ["UBL-TR e-Fatura"] = FontAwesome.Sharp.IconChar.FileInvoice,
            ["UBL-TR e-Arşiv"]  = FontAwesome.Sharp.IconChar.Archive,
        };

        /// <summary>
        /// Kategori adına göre renkli ikon rengini döndürür.
        /// </summary>
        private static Color GetCategoryColor(string category)
        {
            switch (category)
            {
                case "HTML Öğeleri":    return Color.FromArgb(33, 150, 243);   // Mavi
                case "XSLT Komutları":  return Color.FromArgb(156, 39, 176);   // Mor
                case "Sayfa Düzeni":    return Color.FromArgb(76, 175, 80);    // Yeşil
                case "UBL-TR e-Fatura": return Color.FromArgb(255, 87, 34);    // Turuncu
                case "UBL-TR e-Arşiv":  return Color.FromArgb(0, 137, 123);    // Deniz Yeşili
                default:                return Color.FromArgb(96, 125, 139);    // Gri-Mavi
            }
        }

        /// <summary>
        /// Snippet anahtarına göre FontAwesome vektörel renkli ikon oluşturur (16x16).
        /// </summary>
        private static Image CreateSnippetIcon(string snippetKey, Color color)
        {
            FontAwesome.Sharp.IconChar icon;
            if (!SnippetIconMap.TryGetValue(snippetKey, out icon))
                icon = FontAwesome.Sharp.IconChar.Circle;

            return icon.ToBitmap(color, 16);
        }

        /// <summary>
        /// Kategori / alt kategori başlığı için FontAwesome vektörel renkli ikon oluşturur (16x16).
        /// </summary>
        private static Image CreateCategoryIcon(string category, Color color)
        {
            FontAwesome.Sharp.IconChar icon;
            if (!CategoryIconMap.TryGetValue(category, out icon))
                icon = FontAwesome.Sharp.IconChar.FolderOpen;

            return icon.ToBitmap(color, 16);
        }

        #endregion

        private DevExpress.Utils.SuperToolTip CreateSnippetTooltip(SnippetInfo snippet)
        {
            var tip = new DevExpress.Utils.SuperToolTip();
            tip.MaxWidth = 600;
            tip.AllowHtmlText = DevExpress.Utils.DefaultBoolean.True;
            var titleItem = new DevExpress.Utils.ToolTipTitleItem { Text = snippet.DisplayName };
            var bodyItem = new DevExpress.Utils.ToolTipItem
            {
                Text = snippet.Description
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
                if (data.StartsWith(XsltSnippets.DragPrefix))
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
            if (string.IsNullOrEmpty(data) || !data.StartsWith(XsltSnippets.DragPrefix))
                return;

            string key = data.Substring(XsltSnippets.DragPrefix.Length);
            if (key.EndsWith(XsltSnippets.DragSuffix))
                key = key.Substring(0, key.Length - XsltSnippets.DragSuffix.Length);

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

        #region Autocomplete (Ctrl+Space / '<' tetiklemeli)

        /// <summary>
        /// XSLT editörüne otomatik tamamlama desteği ekler.
        /// Ctrl+Space ile tam listeyi, '&lt;' karakteriyle etiket önerilerini tetikler.
        /// </summary>
        private void InitAutoComplete()
        {
            textEditorControlEx1.ActiveTextAreaControl.TextArea.KeyEventHandler += TextArea_KeyEventHandler;
            textEditorControlEx1.ActiveTextAreaControl.TextArea.KeyDown += TextArea_AutoCompleteKeyDown;
        }

        /// <summary>
        /// Karakter yazıldığında (&lt;) tamamlama penceresini açar.
        /// </summary>
        private bool TextArea_KeyEventHandler(char ch)
        {
            if (ch == '<')
            {
                ShowCompletionWindow(ch);
            }
            return false;
        }

        /// <summary>
        /// Ctrl+Space ile tamamlama penceresini açar.
        /// </summary>
        private void TextArea_AutoCompleteKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.Space)
            {
                e.SuppressKeyPress = true;
                ShowCompletionWindow('\0');
            }
        }

        /// <summary>
        /// CodeCompletionWindow'u gösterir. Mevcut pencere açıksa yenisini açmaz.
        /// </summary>
        private void ShowCompletionWindow(char ch)
        {
            if (_completionWindow != null && !_completionWindow.IsDisposed)
                return;

            var provider = new XsltCompletionProvider();
            string fileName = textEditorControlEx1.Tag as string ?? "xslt";

            _completionWindow = CodeCompletionWindow.ShowCompletionWindow(
                this,
                textEditorControlEx1,
                fileName,
                provider,
                ch);

            if (_completionWindow != null)
            {
                _completionWindow.Closed += delegate
                {
                    _completionWindow.Dispose();
                    _completionWindow = null;
                };
            }
        }

        #endregion

        #region WYSIWYG Biçimlendirme Toolbar

        /// <summary>
        /// Ribbon'a WYSIWYG biçimlendirme araç çubuğunu ekler.
        /// Mevcut Bold/Italic/Underline/Align butonlarını aktifleştirir
        /// ve yeni biçimlendirme butonları ekler.
        /// </summary>
        private void InitWysiwygToolbar()
        {
            // Biçimlendirme sekmesini oluştur
            _formatRibbonPage = new RibbonPage { Text = "Biçimlendirme" };
            ribbonControl.Pages.Insert(1, _formatRibbonPage);

            var formatColor = Color.FromArgb(33, 33, 33);

            // ── Yazı Biçimi grubu ──
            var fontGroup = new RibbonPageGroup("Yazı Biçimi");

            // Mevcut butonlara handler bağla ve gruba ekle
            iBoldFontStyle.Glyph = FontAwesome.Sharp.IconChar.Bold.ToBitmap(formatColor, 16);
            iBoldFontStyle.ItemClick += WysiwygBold_ItemClick;
            fontGroup.ItemLinks.Add(iBoldFontStyle);

            iItalicFontStyle.Glyph = FontAwesome.Sharp.IconChar.Italic.ToBitmap(formatColor, 16);
            iItalicFontStyle.ItemClick += WysiwygItalic_ItemClick;
            fontGroup.ItemLinks.Add(iItalicFontStyle);

            iUnderlinedFontStyle.Glyph = FontAwesome.Sharp.IconChar.Underline.ToBitmap(formatColor, 16);
            iUnderlinedFontStyle.ItemClick += WysiwygUnderline_ItemClick;
            fontGroup.ItemLinks.Add(iUnderlinedFontStyle);

            // Üstü çizili
            var btnStrike = new BarButtonItem
            {
                Caption = "Üstü Çizili",
                Name = "iStrikethrough",
                Glyph = FontAwesome.Sharp.IconChar.Strikethrough.ToBitmap(formatColor, 16),
            };
            btnStrike.ItemClick += WysiwygStrikethrough_ItemClick;
            ribbonControl.Items.Add(btnStrike);
            fontGroup.ItemLinks.Add(btnStrike);

            // Üst simge
            var btnSuperscript = new BarButtonItem
            {
                Caption = "Üst Simge",
                Name = "iSuperscript",
                Glyph = FontAwesome.Sharp.IconChar.Superscript.ToBitmap(formatColor, 16),
            };
            btnSuperscript.ItemClick += WysiwygSuperscript_ItemClick;
            ribbonControl.Items.Add(btnSuperscript);
            fontGroup.ItemLinks.Add(btnSuperscript);

            // Alt simge
            var btnSubscript = new BarButtonItem
            {
                Caption = "Alt Simge",
                Name = "iSubscript",
                Glyph = FontAwesome.Sharp.IconChar.Subscript.ToBitmap(formatColor, 16),
            };
            btnSubscript.ItemClick += WysiwygSubscript_ItemClick;
            ribbonControl.Items.Add(btnSubscript);
            fontGroup.ItemLinks.Add(btnSubscript);

            _formatRibbonPage.Groups.Add(fontGroup);

            // ── Hizalama grubu ──
            var alignGroup = new RibbonPageGroup("Hizalama");

            iLeftTextAlign.Glyph = FontAwesome.Sharp.IconChar.AlignLeft.ToBitmap(formatColor, 16);
            iLeftTextAlign.ItemClick += WysiwygAlignLeft_ItemClick;
            alignGroup.ItemLinks.Add(iLeftTextAlign);

            iCenterTextAlign.Glyph = FontAwesome.Sharp.IconChar.AlignCenter.ToBitmap(formatColor, 16);
            iCenterTextAlign.ItemClick += WysiwygAlignCenter_ItemClick;
            alignGroup.ItemLinks.Add(iCenterTextAlign);

            iRightTextAlign.Glyph = FontAwesome.Sharp.IconChar.AlignRight.ToBitmap(formatColor, 16);
            iRightTextAlign.ItemClick += WysiwygAlignRight_ItemClick;
            alignGroup.ItemLinks.Add(iRightTextAlign);

            _formatRibbonPage.Groups.Add(alignGroup);

            // ── Renk & Boyut grubu ──
            var styleGroup = new RibbonPageGroup("Stil");

            // Yazı rengi
            var btnFontColor = new BarButtonItem
            {
                Caption = "Yazı Rengi",
                Name = "iFontColor",
                Glyph = FontAwesome.Sharp.IconChar.Palette.ToBitmap(Color.FromArgb(229, 57, 53), 16),
            };
            btnFontColor.ItemClick += WysiwygFontColor_ItemClick;
            ribbonControl.Items.Add(btnFontColor);
            styleGroup.ItemLinks.Add(btnFontColor);

            // Arka plan rengi
            var btnBgColor = new BarButtonItem
            {
                Caption = "Arka Plan Rengi",
                Name = "iBgColor",
                Glyph = FontAwesome.Sharp.IconChar.FillDrip.ToBitmap(Color.FromArgb(255, 193, 7), 16),
            };
            btnBgColor.ItemClick += WysiwygBgColor_ItemClick;
            ribbonControl.Items.Add(btnBgColor);
            styleGroup.ItemLinks.Add(btnBgColor);

            // Yazı boyutu — alt menü
            var subFontSize = new BarSubItem
            {
                Caption = "Yazı Boyutu",
                Name = "iFontSize",
                Glyph = FontAwesome.Sharp.IconChar.TextHeight.ToBitmap(formatColor, 16),
            };
            string[] fontSizes = { "8", "9", "10", "11", "12", "14", "16", "18", "20", "24", "28", "32", "36", "48" };
            foreach (string size in fontSizes)
            {
                var sizeBtn = new BarButtonItem { Caption = size + "pt", Tag = size, Name = "iFontSize_" + size };
                sizeBtn.ItemClick += WysiwygFontSize_ItemClick;
                subFontSize.AddItem(sizeBtn);
                ribbonControl.Items.Add(sizeBtn);
            }
            ribbonControl.Items.Add(subFontSize);
            styleGroup.ItemLinks.Add(subFontSize);

            _formatRibbonPage.Groups.Add(styleGroup);

            // ── Ekleme grubu ──
            var insertGroup = new RibbonPageGroup("Ekle");

            // Yatay çizgi
            var btnHr = new BarButtonItem
            {
                Caption = "Yatay Çizgi",
                Name = "iWysiwygHr",
                Glyph = FontAwesome.Sharp.IconChar.GripLines.ToBitmap(formatColor, 16),
            };
            btnHr.ItemClick += WysiwygHr_ItemClick;
            ribbonControl.Items.Add(btnHr);
            insertGroup.ItemLinks.Add(btnHr);

            // Sıralı liste
            var btnOl = new BarButtonItem
            {
                Caption = "Sıralı Liste",
                Name = "iOrderedList",
                Glyph = FontAwesome.Sharp.IconChar.ListOl.ToBitmap(formatColor, 16),
            };
            btnOl.ItemClick += WysiwygOrderedList_ItemClick;
            ribbonControl.Items.Add(btnOl);
            insertGroup.ItemLinks.Add(btnOl);

            // Sırasız liste
            var btnUl = new BarButtonItem
            {
                Caption = "Madde İşareti",
                Name = "iUnorderedList",
                Glyph = FontAwesome.Sharp.IconChar.ListUl.ToBitmap(formatColor, 16),
            };
            btnUl.ItemClick += WysiwygUnorderedList_ItemClick;
            ribbonControl.Items.Add(btnUl);
            insertGroup.ItemLinks.Add(btnUl);

            // Kenarlık / border
            var btnBorder = new BarButtonItem
            {
                Caption = "Kenarlık",
                Name = "iWysiwygBorder",
                Glyph = FontAwesome.Sharp.IconChar.BorderAll.ToBitmap(formatColor, 16),
            };
            btnBorder.ItemClick += WysiwygBorder_ItemClick;
            ribbonControl.Items.Add(btnBorder);
            insertGroup.ItemLinks.Add(btnBorder);

            _formatRibbonPage.Groups.Add(insertGroup);
        }

        // ── Event Handler'lar ──

        private void WysiwygBold_ItemClick(object sender, ItemClickEventArgs e)
        {
            WysiwygHelper.WrapSelection(textEditorControlEx1, "b");
        }

        private void WysiwygItalic_ItemClick(object sender, ItemClickEventArgs e)
        {
            WysiwygHelper.WrapSelection(textEditorControlEx1, "i");
        }

        private void WysiwygUnderline_ItemClick(object sender, ItemClickEventArgs e)
        {
            WysiwygHelper.WrapSelection(textEditorControlEx1, "u");
        }

        private void WysiwygStrikethrough_ItemClick(object sender, ItemClickEventArgs e)
        {
            WysiwygHelper.WrapSelection(textEditorControlEx1, "s");
        }

        private void WysiwygSuperscript_ItemClick(object sender, ItemClickEventArgs e)
        {
            WysiwygHelper.WrapSelection(textEditorControlEx1, "sup");
        }

        private void WysiwygSubscript_ItemClick(object sender, ItemClickEventArgs e)
        {
            WysiwygHelper.WrapSelection(textEditorControlEx1, "sub");
        }

        private void WysiwygAlignLeft_ItemClick(object sender, ItemClickEventArgs e)
        {
            WysiwygHelper.WrapWithAlignment(textEditorControlEx1, "left");
        }

        private void WysiwygAlignCenter_ItemClick(object sender, ItemClickEventArgs e)
        {
            WysiwygHelper.WrapWithAlignment(textEditorControlEx1, "center");
        }

        private void WysiwygAlignRight_ItemClick(object sender, ItemClickEventArgs e)
        {
            WysiwygHelper.WrapWithAlignment(textEditorControlEx1, "right");
        }

        private void WysiwygFontColor_ItemClick(object sender, ItemClickEventArgs e)
        {
            using (var dlg = new ColorDialog())
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    string hex = ColorTranslator.ToHtml(dlg.Color);
                    WysiwygHelper.WrapWithStyle(textEditorControlEx1, "color:" + hex);
                }
            }
        }

        private void WysiwygBgColor_ItemClick(object sender, ItemClickEventArgs e)
        {
            using (var dlg = new ColorDialog())
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    string hex = ColorTranslator.ToHtml(dlg.Color);
                    WysiwygHelper.WrapWithStyle(textEditorControlEx1, "background-color:" + hex);
                }
            }
        }

        private void WysiwygFontSize_ItemClick(object sender, ItemClickEventArgs e)
        {
            string size = e.Item.Tag as string;
            if (string.IsNullOrEmpty(size)) return;
            WysiwygHelper.WrapWithStyle(textEditorControlEx1, "font-size:" + size + "pt");
        }

        private void WysiwygHr_ItemClick(object sender, ItemClickEventArgs e)
        {
            WysiwygHelper.InsertAtCursor(textEditorControlEx1, "<hr />");
        }

        private void WysiwygOrderedList_ItemClick(object sender, ItemClickEventArgs e)
        {
            WysiwygHelper.InsertAtCursor(textEditorControlEx1,
                "<ol>\n  <li></li>\n  <li></li>\n  <li></li>\n</ol>");
        }

        private void WysiwygUnorderedList_ItemClick(object sender, ItemClickEventArgs e)
        {
            WysiwygHelper.InsertAtCursor(textEditorControlEx1,
                "<ul>\n  <li></li>\n  <li></li>\n  <li></li>\n</ul>");
        }

        private void WysiwygBorder_ItemClick(object sender, ItemClickEventArgs e)
        {
            WysiwygHelper.WrapWithStyle(textEditorControlEx1, "border:1px solid #000;padding:4px");
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
