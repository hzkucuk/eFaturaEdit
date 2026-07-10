namespace eFaturaEdit
{
    /// <summary>
    /// Platform-bağımsız donanım parmak izi sağlayıcısı.
    /// Lisans doğrulama için makineye özgü benzersiz kimlik üretir.
    ///
    /// <para>
    /// Platform implementasyonları:
    /// <list type="bullet">
    /// <item><b>Windows:</b> WMI (Win32_Processor + Win32_BIOS) tabanlı — <c>WmiHardwareIdProvider</c>.</item>
    /// <item><b>macOS:</b> <c>IOPlatformUUID</c> (ioreg) tabanlı — Faz 3'te eklenecek.</item>
    /// <item><b>Linux:</b> <c>/etc/machine-id</c> veya DMI tabanlı — Faz 3'te eklenecek.</item>
    /// </list>
    /// </para>
    /// </summary>
    public interface IHardwareIdProvider
    {
        /// <summary>
        /// Bu makineye özgü benzersiz kimlik değerini üretir.
        /// Format örneği: <c>4876-8DB5-EE85-69D3-FE52-8CF7-395D-2EA9</c> (16 byte MD5).
        /// Aynı makinede tekrar çağrıldığında aynı değeri döndürür (cache önerilir).
        /// </summary>
        /// <returns>Donanım tabanlı benzersiz kimlik metni.</returns>
        string GetHardwareId();
    }
}
