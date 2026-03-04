using System;
using System.Windows.Forms;
using System.IO;
using System.Security;
using System.Configuration;
using System.Reflection;
using QLicense;
using eFaturaLicense;

namespace ActivationTool
{
    public partial class frmMain : Form
    {
        private byte[] _certPubicKeyData;
        private SecureString _certPwd;

        public frmMain()
        {
            InitializeComponent();

            _certPwd = LoadCertificatePassword();
        }

        private static SecureString LoadCertificatePassword()
        {
            var pwd = new SecureString();

            string configValue = ConfigurationManager.AppSettings["CertificatePassword"];
            if (string.IsNullOrEmpty(configValue))
                throw new InvalidOperationException("App.config icinde 'CertificatePassword' ayari bulunamadi.");

            foreach (char c in configValue)
            {
                pwd.AppendChar(c);
            }

            pwd.MakeReadOnly();
            return pwd;
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            //Read public key from assembly
            Assembly _assembly = Assembly.GetExecutingAssembly();
            using (MemoryStream _mem = new MemoryStream())
            {
                _assembly.GetManifestResourceStream("ActivationTool.LicenseSign.pfx").CopyTo(_mem);

                _certPubicKeyData = _mem.ToArray();
            }

            //Initialize the path for the certificate to sign the XML license file
            licSettings.CertificatePrivateKeyData = _certPubicKeyData;
            licSettings.CertificatePassword = _certPwd;

            //Initialize a new license object
            licSettings.License = new MyLicense(); 
        }

        private void licSettings_OnLicenseGenerated(object sender, QLicense.Windows.Controls.LicenseGeneratedEventArgs e)
        {
            //Event raised when license string is generated. Just show it in the text box
            licString.LicenseString = e.LicenseBASE64String;
        }


        private void btnGenSvrMgmLic_Click(object sender, EventArgs e)
        {
            //Event raised when "Generate License" button is clicked. 
            //Call the core library to generate the license
            licString.LicenseString = LicenseHandler.GenerateLicenseBASE64String(
                new MyLicense(),
                _certPubicKeyData,
                _certPwd);
        }

    }
}
