# Özellikler (Features)

## E-Fatura Dizayn Editörü — v1.7.0

### Ana Özellikler

- **XSLT Editörü:** E-Fatura dizayn (XSLT) dosyalarını düzenleme — `ICSharpCode.TextEditorEx` tabanlı söz dizimi vurgulama.
- **Canlı Önizleme:** XSLT dönüşümünü `CefSharp.WinForms` (Chromium) tabanlı tarayıcıda anlık görselleştirme.
- **XSLT 3.0 Desteği:** `Saxon-HE` motoru ile gelişmiş XSLT dönüşüm desteği.
- **Örnek Fatura Verileri:** Birden fazla örnek XML fatura dosyası ile test imkânı.

### Lisans Sistemi

- **Donanım parmak izi:** CPU + BIOS bilgilerine dayalı benzersiz makine kimliği (`Tools.cs`).
- **Lisans doğrulama:** `QLicense` kütüphanesi ile RSA tabanlı lisans kontrolü.
- **Aktivasyon UI:** `ActivationControls4Win` ile WinForms lisans aktivasyon kontrolleri.
- **Aktivasyon Aracı:** `ActivationTool` ile lisans oluşturma (yönetici tarafı).

### UI Framework

- **DevExpress v14.2:** XtraBars, XtraEditors, XtraLayout, XtraNavBar kontrolleri.
- **Tema:** DevExpress Style skin desteği.

### Güvenlik

- **Obfuscar:** Lisans kütüphaneleri (QLicense, ActivationControls4Win, eFaturaLicense) kod obfuskasyonu ile korunmaktadır.
- **Strong-name signing:** QLicense ve ActivationControls4Win projeleri imzalıdır.
