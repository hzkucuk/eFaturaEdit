# Özellikler (Features)

## E-Fatura Dizayn Editörü — v2.0.0

### Ana Özellikler

- **XSLT Editörü:** E-Fatura dizayn (XSLT) dosyalarını düzenleme — `ICSharpCode.TextEditorEx` tabanlı söz dizimi vurgulama.
- **Canlı Önizleme:** XSLT dönüşümünü `CefSharp.WinForms` (Chromium) tabanlı tarayıcıda anlık görselleştirme.
- **XSLT 3.0 Desteği:** `Saxon-HE` motoru ile gelişmiş XSLT dönüşüm desteği.
- **Tek Örnek Çalışma:** Mutex ile uygulama çift açılması engellenir, mevcut pencere öne getirilir.

### UBL-TR Entegrasyonu (v2.0.0)

- **Örnek Fatura Deposu:** Ribbon'da "Örnek Faturalar" dropdown — 7 kategori, 30 GİB resmi senaryo XML'i.
- **UBL-TR Snippet'leri:** Satıcı/Alıcı bilgileri, Fatura kalemleri döngüsü, Vergi toplamları, Genel toplam, Fatura başlığı, Senaryo/tip kontrolü.
- **Referans Klasörü:** `UBL-TR/` altında Ornekler, Schematron, XSD dosyaları düzenli yapıda.

### Öğe Ekleme Toolbar (v1.8.0)

- **Snippet Toolbar:** Ribbon üzerinde "Öğe Ekle" grubu — 23 hazır XSLT/HTML snippet (15 genel + 8 UBL-TR).
- **Tıklama ile Ekleme:** Snippet butonuna tıklayarak XSLT editöründe imleç pozisyonuna kod ekleme.
- **Editöre Sürükle-Bırak:** Ribbon butonunu sürükleyip XSLT editörüne bırakarak hedef pozisyona snippet ekleme.
- **Önizlemeye Sürükle-Bırak:** Ribbon butonunu CefSharp önizleme üzerine sürükleyip bırakarak XSLT kaynağında eşleşen pozisyona snippet ekleme.
- **JavaScript Interop:** HTML5 drag-drop + `CefSharp.PostMessage` ile tarayıcı-C# köprüsü; hover efekti ve görsel drop göstergesi.

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
