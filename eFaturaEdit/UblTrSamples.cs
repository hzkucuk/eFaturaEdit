using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace eFaturaEdit
{
    /// <summary>
    /// UBL-TR 1.2.1 resmi örnek fatura XML dosyalarının tanımları.
    /// GİB (Gelir İdaresi Başkanlığı) e-Fatura paketinden alınmıştır.
    /// </summary>
    public static class UblTrSamples
    {
        private const string SamplesRelativePath = @"..\..\UBL-TR\Ornekler";

        /// <summary>
        /// Örnek fatura senaryolarının listesi (Kategori → Dosya adı → Açıklama).
        /// </summary>
        public static readonly List<SampleGroup> Groups = new List<SampleGroup>
        {
            new SampleGroup("Temel Fatura", new List<SampleEntry>
            {
                new SampleEntry("1_TEMEL_FATURA.xml", "Temel Fatura"),
                new SampleEntry("6_TEMEL_FATURA_KDV_SIFIR.xml", "Temel Fatura (KDV Sıfır)"),
                new SampleEntry("7_TEMEL_FATURA_IADE.xml", "Temel Fatura (İade)"),
            }),

            new SampleGroup("Ticari Fatura", new List<SampleEntry>
            {
                new SampleEntry("2_TICARI_FATURA.xml", "Ticari Fatura"),
                new SampleEntry("3_TICARI_FATURA.xml", "Ticari Fatura (Red Senaryosu)"),
                new SampleEntry("7_TICARI_FATURA.xml", "Ticari Fatura (İade Senaryosu)"),
            }),

            new SampleGroup("Özel Faturalar", new List<SampleEntry>
            {
                new SampleEntry("TEKNOLOJI_DESTEK.xml", "Teknoloji Desteği"),
            }),

            new SampleGroup("Uygulama Yanıtları", new List<SampleEntry>
            {
                new SampleEntry("2_UYGULAMA_YANITI_KABUL.xml", "Uygulama Yanıtı (Kabul)"),
                new SampleEntry("3_UYGULAMA_YANITI_RED.xml", "Uygulama Yanıtı (Red)"),
                new SampleEntry("7_UYGULAMA_YANITI_IADE.xml", "Uygulama Yanıtı (İade)"),
            }),

            new SampleGroup("Sistem Yanıtları", new List<SampleEntry>
            {
                new SampleEntry("1_SISTEM_YANITI_MERKEZ.xml", "Sistem Yanıtı (Merkez)"),
                new SampleEntry("1_SISTEM_YANITI_POSTA_KUTUSU.xml", "Sistem Yanıtı (Posta Kutusu)"),
                new SampleEntry("1_SISTEM_YANITI_GTB_POSTA_KUTUSU.xml", "Sistem Yanıtı (GTB Posta Kutusu)"),
            }),

            new SampleGroup("Zarf Örnekleri", new List<SampleEntry>
            {
                new SampleEntry("1_TEMEL_FATURA_ZARF.xml", "Temel Fatura Zarfı"),
                new SampleEntry("2_TICARI_FATURA_ZARF.xml", "Ticari Fatura Zarfı"),
                new SampleEntry("3_TICARI_FATURA_ZARF.xml", "Ticari Fatura Zarfı (Red)"),
                new SampleEntry("6_TEMEL_FATURA_KDV_SIFIR_ZARF.xml", "KDV Sıfır Fatura Zarfı"),
                new SampleEntry("7_TEMEL_FATURA_IADE_ZARF.xml", "İade Fatura Zarfı"),
                new SampleEntry("7_TICARI_FATURA_ZARF.xml", "Ticari İade Fatura Zarfı"),
                new SampleEntry("2_UYGULAMA_YANITI_KABUL_ZARF.xml", "Kabul Yanıt Zarfı"),
                new SampleEntry("3_UYGULAMA_YANITI_RED_ZARF.xml", "Red Yanıt Zarfı"),
                new SampleEntry("7_UYGULAMA_YANITI_IADE_ZARF.xml", "İade Yanıt Zarfı"),
            }),

            new SampleGroup("Kullanıcı İşlemleri", new List<SampleEntry>
            {
                new SampleEntry("4_KULLANICI_ACMA.xml", "Kullanıcı Açma"),
                new SampleEntry("4_KULLANICI_ACMA_ZARF.xml", "Kullanıcı Açma Zarfı"),
                new SampleEntry("5_KULLANICI_SILME.xml", "Kullanıcı Silme"),
                new SampleEntry("5_KULLANICI_SILME_ZARF.xml", "Kullanıcı Silme Zarfı"),
                new SampleEntry("8_FATURA_SAKLAMA_KULLANICI_ACMA.xml", "Saklama Kullanıcı Açma"),
                new SampleEntry("8_FATURA_SAKLAMA_KULLANICI_ACMA_ZARF.xml", "Saklama Kullanıcı Açma Zarfı"),
                new SampleEntry("9_FATURA_SAKLAMA_KULLANICI_SILME.xml", "Saklama Kullanıcı Silme"),
                new SampleEntry("9_FATURA_SAKLAMA_KULLANICI_SILME_ZARF.xml", "Saklama Kullanıcı Silme Zarfı"),
            }),
        };

        /// <summary>
        /// Verilen dosya adının tam yolunu döndürür.
        /// </summary>
        public static string GetFullPath(string fileName)
        {
            return Path.Combine(
                Path.GetDirectoryName(Application.ExecutablePath),
                SamplesRelativePath,
                fileName);
        }

        /// <summary>
        /// Örnek XML dosyalarının bulunduğu klasörün var olup olmadığını kontrol eder.
        /// </summary>
        public static bool SamplesDirectoryExists()
        {
            string dir = Path.Combine(
                Path.GetDirectoryName(Application.ExecutablePath),
                SamplesRelativePath);
            return Directory.Exists(dir);
        }
    }

    public class SampleGroup
    {
        public string CategoryName { get; }
        public List<SampleEntry> Entries { get; }

        public SampleGroup(string categoryName, List<SampleEntry> entries)
        {
            CategoryName = categoryName;
            Entries = entries;
        }
    }

    public class SampleEntry
    {
        public string FileName { get; }
        public string DisplayName { get; }

        public SampleEntry(string fileName, string displayName)
        {
            FileName = fileName;
            DisplayName = displayName;
        }
    }
}
