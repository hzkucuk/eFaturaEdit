# Kurulum Rehberi (Installation)

## Gereksinimler

| Bileşen | Minimum Versiyon |
|---|---|
| .NET Framework | 4.7.2 |
| Visual Studio | 2017+ |
| DevExpress WinForms | v14.2.15 |
| İşletim Sistemi | Windows 7 SP1+ |

## NuGet Paketleri

| Paket | Versiyon | Amaç |
|---|---|---|
| CefSharp.Common | 145.0.260 | Chromium Embedded Framework |
| CefSharp.WinForms | 145.0.260 | WinForms tarayıcı kontrolü |
| chromiumembeddedframework.runtime.win-x64 | 145.0.26 | CEF runtime (x64) |
| chromiumembeddedframework.runtime.win-x86 | 145.0.26 | CEF runtime (x86) |
| ICSharpCode.TextEditorEx | 1.3.0 | Kod editörü kontrolü |
| Saxon-HE | 10.9.0 | XSLT 3.0 dönüşüm motoru |
| Obfuscar | 2.2.38 | Kod obfuskasyon (dev dependency) |

## Kurulum Adımları

1. Depoyu klonlayın:
   ```bash
   git clone https://github.com/hzkucuk/eFaturaEdit.git
   ```

2. Visual Studio ile `E-FaturaEdit.sln` dosyasını açın.

3. NuGet paketlerini geri yükleyin:
   - **Visual Studio:** Solution Explorer → Sağ tık → "Restore NuGet Packages"
   - **Komut satırı:** `nuget restore E-FaturaEdit.sln`

4. **DevExpress v14.2** bileşenlerinin sisteminizde kurulu ve lisanslı olduğundan emin olun.

5. Projeyi derleyin (`Ctrl+Shift+B`).

## Yapılandırma

- Uygulama ayarları: `eFaturaEdit\Properties\Settings.settings`
- Lisans doğrulama sertifikası: `eFaturaEdit\LicenseVerify.cer` (embedded resource)
- Örnek XSLT: `eFaturaEdit\diz.xslt`
- Örnek fatura XML: `eFaturaEdit\fatura.xml`, `eFaturaEdit\XMLDataFiles\`

## Dağıtım (Inno Setup)

Kurulum paketi **Inno Setup** ile oluşturulmaktadır.

- **Yapılandırma dosyası:** `setup.iss`
- **Kültür:** `tr-TR`
- **Versiyon:** `Properties\AssemblyInfo.cs` → `AssemblyVersion` ile senkron tutulmalıdır.
