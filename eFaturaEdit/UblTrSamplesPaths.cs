using System.IO;
using System.Windows.Forms;

namespace eFaturaEdit
{
    /// <summary>
    /// WinForms platformuna özgü UBL-TR örnek XML dosya yolu çözümleyicisi.
    /// Çekirdek <see cref="UblTrSamples"/> sınıfı UI-bağımsız kaldığı için
    /// <c>Application.ExecutablePath</c>'e bağımlı yol işlemleri burada tutulur.
    /// </summary>
    internal static class UblTrSamplesPaths
    {
        /// <summary>
        /// Uygulama çalıştırılabilir dizinine göre örnek XML dosyasının tam yolunu döndürür.
        /// </summary>
        public static string GetFullPath(string fileName)
        {
            return Path.Combine(
                Path.GetDirectoryName(Application.ExecutablePath),
                UblTrSamples.SamplesRelativePath,
                fileName);
        }

        /// <summary>
        /// Örnek XML dosyalarının bulunduğu klasörün var olup olmadığını kontrol eder.
        /// </summary>
        public static bool SamplesDirectoryExists()
        {
            string dir = Path.Combine(
                Path.GetDirectoryName(Application.ExecutablePath),
                UblTrSamples.SamplesRelativePath);
            return Directory.Exists(dir);
        }
    }
}
