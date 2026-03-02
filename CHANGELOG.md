# Değişiklik Günlüğü (Changelog)

Tüm önemli değişiklikler bu dosyada belgelenir.
Format [Semantic Versioning](https://semver.org/lang/tr/) kurallarına uygundur.

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
