# Değişiklik Günlüğü (Changelog)

Tüm önemli değişiklikler bu dosyada belgelenir.
Format [Semantic Versioning](https://semver.org/lang/tr/) kurallarına uygundur.

## [2.0.0] — 2025-06-20 — UBL-TR Entegrasyonu — UblTrSamples.cs, XsltSnippets.cs, Form1.cs, UBL-TR/

### Eklenen
- **UBL-TR Referans Klasörü:** `UBL-TR/` altında Ornekler (30 XML), Schematron (4 dosya), XSD (16 dosya) düzenli yapı. — `UBL-TR/`
- **Örnek Fatura Deposu:** Ribbon'da "Örnek Faturalar" dropdown — 7 kategori, 30 GİB resmi senaryo XML'i (Temel, Ticari, İade, KDV Sıfır, İhracat, Kullanıcı İşlemleri). — `UblTrSamples.cs`, `Form1.cs`
- **8 UBL-TR Snippet:** Satıcı Bilgisi, Alıcı Bilgisi, Fatura Kalemleri, Vergi Toplamları, Genel Toplam, Fatura Başlığı, Senaryo Kontrolü, Fatura Tipi Kontrolü. Toplam 23 snippet. — `XsltSnippets.cs`

## [1.9.0] — 2025-06-20 — Güvenlik + Yeni Snippet'ler + Dokümantasyon — frmMain.cs, XsltSnippets.cs, App.config, INSTALL.md

### Güvenlik
- **Hardcoded şifre kaldırıldı:** `frmMain.cs` içindeki sabit kodlu sertifika şifresi `App.config` → `CertificatePassword` ayarına taşındı. — `frmMain.cs`, `App.config`, `ActivationTool.csproj`

### Eklenen
- **5 yeni snippet:** Barkod, QR Kod, Sayfa Sonu, Üst Bilgi, Alt Bilgi. Toplam 15 snippet. — `XsltSnippets.cs`

### Dokümantasyon
- **INSTALL.md:** Sertifika şifresi yapılandırma bilgisi ve Öğe Ekleme Toolbar bölümü eklendi. — `INSTALL.md`

## [1.8.1] — 2025-06-20 — Tek Örnek (Single Instance) Kontrolü — Program.cs

### Düzeltme
- **Çift açılma engellendi:** `Mutex` ile uygulama zaten çalışıyorsa ikinci örnek açılmaz, kullanıcıya bilgi mesajı gösterilir. — `Program.cs`

## [1.8.0] — 2025-06-20 — XSLT Öğe Ekleme Toolbar — Form1.cs, XsltSnippets.cs, BrowserDropBridge.cs

### Eklenen
- **Öğe Ekleme Toolbar:** Ribbon üzerinde “Öğe Ekle” grubu ile 10 farklı XSLT/HTML snippet (Resim, Tablo, Metin, XSL Değer, Link, Çizgi, Kutu, Döngü, Koşul, Kalın). — `XsltSnippets.cs`, `Form1.cs`
- **Faz 1 — Editöre Tıklama/Sürükle-Bırak:** Ribbon butonuna tıklayınca XSLT editöründe imleç pozisyonuna snippet eklenir; buton sürüklenip editöre bırakılabilir. — `Form1.cs`
- **Faz 2 — Önizlemeye Sürükle-Bırak:** Ribbon butonunu CefSharp önizleme üzerine sürükleyip bırakıldığında JavaScript drop handler pozisyonu algılar, XSLT kaynağında eşleşen yere snippet eklenir. — `BrowserDropBridge.cs`, `Form1.cs`
- **JavaScript Interop:** CefSharp.PostMessage ile HTML5 drop olayları C#’a aktarılıyor; hover efekti ve görsel drop göstergesi mevcut. — `BrowserDropBridge.cs`

## [1.7.0] — 2025-06-20

### Düzeltme
- **Versiyon senkronizasyonu:** `.csproj` ApplicationVersion `1.2.0` → `1.7.0` olarak AssemblyInfo ile eşitlendi. — `e-FaturaEdit.csproj`
- **Eski CEF paketleri temizlendi:** `cef.redist.x64/x86 v120.2.5`, `cef.redist.x64/x86 v114.2.12`, `CefSharp.Common v114.2.120` referansları kaldırıldı. — `e-FaturaEdit.csproj`, `packages.config`
- **Exception yutma düzeltildi:** `Tools.cs` içindeki boş `catch` bloklarına `Debug.WriteLine` ile hata kaydı eklendi. — `Tools.cs`
- **targetFramework güncellendi:** `packages.config` içinde `ICSharpCode.TextEditorEx` (`net462` → `net472`) ve `Saxon-HE` (`net452` → `net472`) düzeltildi. — `packages.config`

### Eklenen
- `CHANGELOG.md` — Değişiklik günlüğü dosyası oluşturuldu.
- `FEATURES.md` — Özellik listesi dosyası oluşturuldu.
- `INSTALL.md` — Kurulum rehberi dosyası oluşturuldu.
