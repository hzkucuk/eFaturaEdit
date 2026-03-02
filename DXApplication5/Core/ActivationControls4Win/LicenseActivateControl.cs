using System;
using System.Windows.Forms;

namespace QLicense.Windows.Controls
{
    public partial class LicenseActivateControl : UserControl
    {
        public string AppName { get; set; }

        public byte[] CertificatePublicKeyData { private get; set; }

        public bool ShowMessageAfterValidation { get; set; }

        public Type LicenseObjectType { get; set; }

        public string LicenseBASE64String
        {
            get
            {
                return txtLicense.Text.Trim();
            }
        }

        public LicenseActivateControl()
        {
            InitializeComponent();

            ShowMessageAfterValidation = true;
        }

        public void ShowUID()
        {
            txtUID.Text = LicenseHandler.GenerateUID(AppName);// string.Format("{0} / MainBoard ID:{1}", LicenseHandler.GenerateUID(AppName), LicenseHandler.GetMbID()); ;
        }

        public bool ValidateLicense()
        {
            if (string.IsNullOrWhiteSpace(txtLicense.Text))
            {
                MessageBox.Show("Aktivasyon Kodunuzu Giriniz.", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            //Check the activation string
            LicenseStatus _licStatus= LicenseStatus.UNDEFINED;
            string _msg = string.Empty;
            LicenseEntity _lic = LicenseHandler.ParseLicenseFromBASE64String(LicenseObjectType, txtLicense.Text.Trim(), CertificatePublicKeyData, out _licStatus, out _msg);
            switch (_licStatus)
            {
                case LicenseStatus.VALID:                   
                    if (ShowMessageAfterValidation)
                    {
                        MessageBox.Show(_msg, "Lisans Geçerli", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                    return true;
                    
                case LicenseStatus.CRACKED:
                case LicenseStatus.INVALID:
                case LicenseStatus.UNDEFINED:
                    if (ShowMessageAfterValidation)
                    {
                        MessageBox.Show(_msg, "Lisans Geçerli Değil", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    return false;

                default:
                    return false;
            }
        }


        private void lnkCopy_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Clipboard.SetText(txtUID.Text);
        }
    }
}
