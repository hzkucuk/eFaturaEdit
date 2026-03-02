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

using QLicense;
using eFaturaLicense;
using System.Reflection;

namespace eFaturaEdit
{
    
    public partial class Form1 : RibbonForm

    {
        byte[] _certPubicKeyData;


        private static  ChromiumWebBrowser brow;
        readonly CefSettings settings;
        public Form1()
        {
            InitializeComponent();
            InitSkinGallery();
            settings  = new CefSettings();
            

            Cef.Initialize(settings);
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
            try
            {
                brow.ShowDevTools();
            }
            catch (Exception ex)
            {

                MessageBox.Show($@"Dosya işlenemedi{ex.Message}", 
                    @"Dosya Açma Hatası", 
                    MessageBoxButtons.AbortRetryIgnore);
            }
           
           
            
        }

        private void iSave_ItemClick(object sender, ItemClickEventArgs e)
        {
            try
            {
                textEditorControlEx1.SaveFile(textEditorControlEx1.Tag.ToString());
                textEditorControlEx2.SaveFile(textEditorControlEx2.Tag.ToString());

                textEditorControlEx2.SaveFile(xtraTabControl2.TabPages[1].Text);
                      
                    

                
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
                brow.Reload();
            }
            catch (Exception ex)
            {

                MessageBox.Show("Dosya işlenemedi" + ex.Message, "Dosya Açma Hatası", MessageBoxButtons.AbortRetryIgnore);
            }



        }

        private void iExit_ItemClick(object sender, ItemClickEventArgs e)
        {
            try
            {
            Cef.Shutdown();
            Application.Exit();
            }
            finally
            {
                Cef.Shutdown();
                Application.Exit();
            }
           
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







                    myXslTrans.Transform(Path.GetDirectoryName(Application.ExecutablePath) + @"\XMLDataFiles\fatura.xml", @"result.html");








                    brow = null;
                    brow = new ChromiumWebBrowser(Path.GetDirectoryName(Application.ExecutablePath) + @"\result.html");

                    brow.Dock = DockStyle.Fill;

                    xtraTabControl2.TabPages[0].Controls.Clear();
                    xtraTabControl2.TabPages[0].Controls.Add(brow);
                 
                    xtraTabControl2.TabPages[0].Text = Path.GetDirectoryName(Application.ExecutablePath) + @"\XMLDataFiles\fatura.xml";

                    xtraTabControl2.TabPages[1].Text = Path.GetDirectoryName(Application.ExecutablePath) + @"\XMLDataFiles\fatura.xml";
                    textEditorControlEx2.LoadFile(Path.GetDirectoryName(Application.ExecutablePath) + @"\XMLDataFiles\fatura.xml", true, true);
                    textEditorControlEx2.Tag = Path.GetDirectoryName(Application.ExecutablePath) + @"\XMLDataFiles\fatura.xml";
   
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
            try
            {
                Cef.Shutdown();
                Application.Exit();
            }
            finally
            {
                Cef.Shutdown();
                Application.Exit();
            }
          
        }

        private void barButtonItem2_ItemClick(object sender, ItemClickEventArgs e)
        {
            try
            {
                brow.Reload();
            }
            catch (Exception ex)
            {

                MessageBox.Show("Dosya işlenemedi" + ex.Message, "Dosya Açma Hatası", MessageBoxButtons.AbortRetryIgnore);
            }
            
        }

        private void iSaveAs_ItemClick(object sender, ItemClickEventArgs e)
        {
            using (SaveFileDialog saveFileDialog1 = new SaveFileDialog() { Filter = "(e-Fatura Dizayn Dosyası Xslt  |*.Xslt", Title = "Save  File" })
            {
                saveFileDialog1.ShowDialog();
                try
                {
                    if (saveFileDialog1.FileName != "")
                    {
                        textEditorControlEx1.SaveFile(saveFileDialog1.FileName);
                    }
                }
                catch (Exception ex)
                {

                    MessageBox.Show("Dosya işlenemedi" + ex.Message, "Dosya Açma Hatası", MessageBoxButtons.AbortRetryIgnore);
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
                _assembly.GetManifestResourceStream("eFaturaEdit.LicenseVerify.cer").CopyTo(_mem);

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








                        brow = null;
                        brow = new ChromiumWebBrowser(Path.GetDirectoryName(Application.ExecutablePath) + @"\result.html");

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

        private  void barButtonItem3_ItemClick(object sender, ItemClickEventArgs e)
        {
            using (SaveFileDialog saveFileDialog1 = new SaveFileDialog() { Filter = "(e-Fatura Dizayn pdf  |*.pdf", Title = "Save  File" })
            {
                saveFileDialog1.ShowDialog();
                try
                {
                    if (saveFileDialog1.FileName != "")
                    {
                        GeneratePdf(saveFileDialog1.FileName);
                       // textEditorControlEx1.SaveFile(saveFileDialog1.FileName);
                    }
                }
                catch (Exception ex)
                {

                    MessageBox.Show("Dosya işlenemedi" + ex.Message, "Dosya Açma Hatası", MessageBoxButtons.AbortRetryIgnore);
                }
            }
            

        }

        private static async void GeneratePdf(string pdff)
        {
            var path = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            var generatedPdfFile = Path.Combine(path, pdff);

            var chromeSettings = new PdfPrintSettings();
            var htmlFile = Path.Combine(path, @"\result.html");
           
                var pdfFileSaved = await brow.PrintToPdfAsync(generatedPdfFile, chromeSettings);
                if (pdfFileSaved)
                {
                    //Thread.Sleep(10);  // <-- uncomment this line and no exception happens
                    using (var testStream = new FileStream(generatedPdfFile, FileMode.Open))
                    {
                        // access the file for read --> exception is thrown. PDF file is still in use by Chromium.
                    }
                }
            
        }
    }
}