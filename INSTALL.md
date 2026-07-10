# Kurulum Rehberi (Installation)

## Solution Yapısı

```
eFaturaEditSolution.sln
├── src/
│   ├── eFaturaEdit.Core/               ← v2.12.0 — cross-platform çekirdek
│   │   ├── Snippets/                   UBL-TR snippet'ler + SnippetInfo
│   │   ├── Samples/                    UBL-TR örnek XML kataloğu
│   │   ├── Completion/                 Autocomplete verisi + helpers
│   │   ├── Platform/                   IHardwareIdProvider (interface)
│   │   └── eFaturaEdit.Core.csproj     SDK-style, netstandard2.0;net10.0
│   │
│   └── eFaturaEdit.DataExport/         Core → JSON export tool (net10.0 konsol)
│
├── app/                                 ← v2.12.0 — Tauri masaüstü uygulaması
│   ├── src/                            SvelteKit + TypeScript frontend
│   │   ├── lib/                        CodeEditor, Splitter, HelpModal, drag, settings, ...
│   │   ├── lib/data/                   Core'dan üretilen JSON'lar (npm run data:sync)
│   │   └── routes/                     Ana sayfa + /settings
│   ├── src-tauri/                       Rust backend (Tauri v2)
│   └── static/samples/                  Örnek XSLT/XML dosyaları
│
└── eFaturaEdit/                         Ana WinForms uygulaması (net472)
    └── e-FaturaEdit.csproj              → eFaturaEdit.Core referansı
```

## Gereksinimler

### WinForms Uygulaması (mevcut üretim)

| Bileşen | Minimum Versiyon |
|---|---|
| .NET Framework | 4.7.2 |
| Visual Studio | 2022+ |
| DevExpress WinForms | v14.2.15 |
| İşletim Sistemi | Windows 7 SP1+ |

### Tauri Masaüstü Uygulaması (`app/`) — macOS/Linux/Windows

| Bileşen | Minimum Versiyon |
|---|---|
| .NET SDK | 10.0 (Core + DataExport için) |
| Rust | 1.97+ (stable toolchain) |
| Node.js | 20+ (test edilen: 26) |
| npm | 10+ |
| Xcode Command Line Tools | macOS derlemesi için |

## NuGet Paketleri (WinForms)

| Paket | Versiyon | Amaç |
|---|---|---|
| CefSharp.Common | 145.0.260 | Chromium Embedded Framework |
| CefSharp.WinForms | 145.0.260 | WinForms tarayıcı kontrolü |
| chromiumembeddedframework.runtime.win-x64 | 145.0.26 | CEF runtime (x64) |
| chromiumembeddedframework.runtime.win-x86 | 145.0.26 | CEF runtime (x86) |
| ICSharpCode.TextEditorEx | 1.3.0 | Kod editörü kontrolü |
| Saxon-HE | 10.9.0 | XSLT 3.0 dönüşüm motoru |
| Obfuscar | 2.2.38 | Kod obfuskasyon (dev dependency) |

## npm Paketleri (Tauri App)

| Paket | Amaç |
|---|---|
| @tauri-apps/api, @tauri-apps/cli | Tauri v2 JS API + CLI |
| @tauri-apps/plugin-dialog, plugin-fs, plugin-opener | Dosya dialog/okuma-yazma/harici açma |
| @sveltejs/kit, adapter-static, svelte, vite | SvelteKit frontend |
| codemirror + @codemirror/{view,state,commands,language,lang-xml,lang-html,autocomplete,search,theme-one-dark} | Kod editörü |
| thememirror, @lezer/highlight | Editör temaları + syntax highlight |

## Kurulum Adımları — WinForms

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
- Sertifika şifresi: `eFaturaEdit\Demo\DemoActivationTool\App.config` → `CertificatePassword` (üretim ortamında değiştirin)
- Örnek XSLT: `eFaturaEdit\diz.xslt`
- Örnek fatura XML: `eFaturaEdit\fatura.xml`, `eFaturaEdit\XMLDataFiles\`

## Öğe Ekleme Toolbar

Ribbon üzerindeki "Öğe Ekle" grubu ile 15 hazır XSLT/HTML snippet kullanılabilir:
- **Tıklama:** Butona tıklayarak imleç pozisyonuna snippet ekleme
- **Editöre sürükle-bırak:** Butonu XSLT editörüne sürükleyip bırakma
- **Önizlemeye sürükle-bırak:** Butonu CefSharp önizleme üzerine sürükleyip bırakma (JavaScript interop)

## Dağıtım (Inno Setup)

Kurulum paketi **Inno Setup** ile oluşturulmaktadır.

- **Yapılandırma dosyası:** `setup.iss`
- **Kültür:** `tr-TR`
- **Versiyon:** `Properties\AssemblyInfo.cs` → `AssemblyVersion` ile senkron tutulmalıdır.

## Kurulum Adımları — Tauri Masaüstü Uygulaması (`app/`)

macOS, Linux ve Windows üzerinde çalışan cross-platform uygulama. Aşağıdaki
adımlar tüm platformlarda ortaktır (macOS örnek alınmıştır).

1. **Rust kurun** (eğer kurulu değilse):
   ```bash
   curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh
   source "$HOME/.cargo/env"
   ```

2. **Node.js 20+ kurun** (nvm, Homebrew veya resmi installer ile).

3. **.NET 10 SDK kurun** (Core + DataExport tool'u için).

4. npm bağımlılıklarını kurun:
   ```bash
   cd app
   npm install
   ```

5. Core verilerini JSON'a senkronlayın (ilk kurulumda ve Core değiştikçe):
   ```bash
   npm run data:sync
   ```

6. Geliştirme modunda çalıştırın:
   ```bash
   npm run tauri dev
   ```
   İlk çalıştırmada Rust bağımlılıkları derlenir (~1 dakika); sonraki
   çalıştırmalar saniyeler içinde açılır.

7. Üretim build'i almak için:
   ```bash
   npm run tauri build
   ```
   Çıktı: `app/src-tauri/target/release/bundle/` altında platforma özgü
   yükleyici (`.dmg`, `.msi`, `.deb`/`.AppImage`).

### Yapılandırma (Tauri App)

- **Bundle identifier:** `com.zaferbilgisayar.efaturaedit` — [tauri.conf.json](app/src-tauri/tauri.conf.json)
- **Capability/izinler:** [app/src-tauri/capabilities/default.json](app/src-tauri/capabilities/default.json) — dosya sistemi kapsamı (`$HOME`, `$DOCUMENT`, `$APPDATA` vb.), dialog, pencere kapatma izinleri
- **Kullanıcı ayarları:** Tarayıcı `localStorage`'da saklanır (`efaturaEdit.settings.v3`, `efaturaEdit.recentFiles.v1`)
- **Önizleme geçici dosyaları:** `$APPLOCALDATA/preview/` (yazdırma için tarayıcıya açılan HTML'ler)

