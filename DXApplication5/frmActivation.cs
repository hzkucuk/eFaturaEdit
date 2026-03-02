using System;
using System.Windows.Forms;
using System.IO;

namespace eFaturaEdit
{
    public partial class frmActivation : Form
    {
        public byte[] CertificatePublicKeyData { private get; set; }

        public frmActivation()
        {
            InitializeComponent();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Çıkmak istediğinizden Eminmisiniz?", string.Empty, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                this.Close();
            }
        }

        private void frmActivation_Load(object sender, EventArgs e)
        {

            //Assign the application information values to the license control
            licActCtrl.AppName = "eFaturaEdit";
            licActCtrl.LicenseObjectType = typeof(eFaturaLicense.MyLicense);
            licActCtrl.CertificatePublicKeyData = this.CertificatePublicKeyData;
            //Display the device unique ID
            licActCtrl.ShowUID();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            //Call license control to validate the license string
            if (licActCtrl.ValidateLicense())
            {
                //If license if valid, save the license string into a local file
                File.WriteAllText(Path.Combine(Application.StartupPath, "license.lic"), licActCtrl.LicenseBASE64String);

                MessageBox.Show("Lisans Kabul edildi, Uygulama kaptılacaktır. Lütfen uygulamayı yeniden çalıştırınız.", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Information);

                this.Close();
            }
        }
    }
}
