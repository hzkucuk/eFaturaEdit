using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DevExpress.XtraBars;
using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraBars.Helpers;
using System.IO;

using FontAwesome.Sharp;
using DevExpress.XtraTab.Buttons;
using DevExpress.XtraEditors.Controls;
using System.Threading.Tasks;

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
        private static readonly string ResultHtmlPath = Path.Combine(Path.GetTempPath(), "eFaturaEdit_" + Guid.NewGuid().ToString("N") + ".html");

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

                XsltTransformHelper.TransformXslFile(textEditorControlEx1.Tag.ToString(), textEditorControlEx2.Tag.ToString(), ResultHtmlPath);

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

        private void iOpen_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (openFileDialog1.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                textEditorControlEx1.LoadFile(openFileDialog1.FileName);
                textEditorControlEx1.Tag = openFileDialog1.FileName;

                xtraTabControl1.TabPages[0].Text = openFileDialog1.FileName;

                XsltTransformHelper.TransformXslFile(openFileDialog1.FileName, DefaultXmlPath, ResultHtmlPath);

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

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            ShutdownBrowser();

            try { if (File.Exists(ResultHtmlPath)) File.Delete(ResultHtmlPath); }
            catch { /* temp dosya silinememesi kritik değil */ }
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
                        XsltTransformHelper.TransformXslFile(openFileDialog1.FileName, openFileDialog2.FileName, ResultHtmlPath);

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

    }
}
