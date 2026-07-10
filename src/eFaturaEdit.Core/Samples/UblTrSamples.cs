using System.Collections.Generic;

namespace eFaturaEdit
{
    /// <summary>
    /// UBL-TR 1.2.1 resmi örnek fatura XML dosyalarının tanımları.
    /// GİB (Gelir İdaresi Başkanlığı) e-Fatura paketinden alınmıştır.
    /// UI-bağımsız veri kataloğu. Dosya yolu çözümleme platforma özgüdür
    /// (WinForms: <c>UblTrSamplesPaths</c>).
    /// </summary>
    public static class UblTrSamples
    {
        /// <summary>
        /// Uygulama çalıştırılabilir dizinine göre örnek XML klasörünün göreli yolu.
        /// Cross-platform: forward slash — <c>Path.Combine</c> her OS'ta doğru ayırıcıyı kullanır.
        /// </summary>
        public const string SamplesRelativePath = "../../UBL-TR/Ornekler";

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

            new SampleGroup("Kullanıcı İşlemleri", new List<SampleEntry>
            {
                new SampleEntry("4_KULLANICI_ACMA.xml", "Kullanıcı Açma"),
                new SampleEntry("5_KULLANICI_SILME.xml", "Kullanıcı Silme"),
                new SampleEntry("8_FATURA_SAKLAMA_KULLANICI_ACMA.xml", "Saklama Kullanıcı Açma"),
                new SampleEntry("9_FATURA_SAKLAMA_KULLANICI_SILME.xml", "Saklama Kullanıcı Silme"),
            }),
        };
    }
}
