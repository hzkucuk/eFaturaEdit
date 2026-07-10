# Özellikler (Features)

## E-Fatura Dizayn Editörü — v2.12.0

### 🖥️ Tauri Masaüstü Uygulaması — Faz 3 (v2.12.0, `app/`)

Cross-platform (macOS/Linux/Windows) yeni nesil masaüstü UI. DevExpress/WinForms
bağımlılığı yok, Tauri v2 + SvelteKit + CodeMirror 6 ile geliştirilmiştir.

- **3 Panel Düzen:** Sol snippet paneli, orta XSLT+XML editörleri, sağ canlı önizleme —
  hepsi mouse ile yeniden boyutlandırılabilir (`Splitter`).
- **CodeMirror 6 Editör:** Syntax highlight (özel açık tema renklendirmesi + 10 hazır tema),
  satır numarası, kod katlama, Türkçeleştirilmiş arama paneli (Cmd/Ctrl+F), undo/redo.
- **Autocomplete:** 242 öneri (16 XSLT etiketi + 77 XPath + 149 snippet) — `Ctrl+Space` veya `<` ile tetiklenir.
- **Snippet Sürükle-Bırak:** Custom mouse-tracking implementasyonu (WKWebView'de HTML5 API güvenilmez) —
  görsel ghost gösterge, hedef editör üzerinde mavi vurgulama.
- **Dosya İşlemleri:** Aç/Kaydet/Farklı Kaydet (native dialog), `Cmd/Ctrl+S` ile XSLT+XML birlikte kaydetme,
  kaydetmeden önce syntax kontrolü (hata varsa imleç otomatik hatalı satıra gider), son 10 dosya listesi.
- **Otomatik Dönüştür:** Yükleme/kaydetme sonrası ve yazarken debounce ile (varsayılan 700ms).
- **Auto-save:** Ayarlardan açılabilir, belirli gecikmeyle sessiz kaydetme.
- **Çıkışta Kaydetme Kontrolü:** Kaydedilmemiş değişiklik varsa çıkışı engelleyip
  "İptal / Kaydetmeden Çık / Kaydet ve Çık" seçenekli onay penceresi gösterir.
- **Önizleme:** Responsive boyut seçici (320/768/1200/Full), zoom (Cmd +/-/0),
  sağ tık menüsü (Yazdır/PDF, HTML kopyala, DevTools). Yazdırma sistem tarayıcısına (Safari) devredilir —
  Tauri WKWebView'de native print paneli güvenilir açılmıyor.
- **Standards Mode Garantisi:** `XSLTProcessor.transformToDocument()` + `<!DOCTYPE html>`/`<meta charset>`
  enjeksiyonu — DOCTYPE eksikliği Quirks Mode'a (farklı tablo render) yol açtığı için eklendi.
- **Ayarlar Sayfası:** Font boyutu, sekme genişliği, kelime kaydırma, 11 tema, autosave/debounce/autocomplete anahtarları.
- **Yardım Sistemi:** F1 kısayolu ile açılan, sidebar navigasyonlu, aranabilir 11 bölümlük tam dokümantasyon
  (`HelpModal`) + tüm kontrollerde açıklayıcı tooltip'ler.
- **Örnek Fatura Kataloğu:** 6 kategori × 17 GİB resmi UBL-TR senaryosu (zarf örnekleri hariç tutuldu).
- **`eFaturaEdit.DataExport` tool'u:** Core POCO'ları TypeScript-friendly JSON'a dönüştürür
  (`npm run data:sync` ile Core → JSON senkronizasyonu).

**Bilinen sınırlamalar:** XSLT 2.0/3.0 desteği yok (native `XSLTProcessor` yalnızca 1.0),
çoklu dosya sekmesi ve native menü çubuğu yok.

### Mimari — Faz 2: Cross-Platform Çekirdek (v2.11.0)

- **`eFaturaEdit.Core` kütüphanesi:** UI-bağımsız veri katmanı. Multi-target `netstandard2.0;net10.0`, sıfır dış NuGet bağımlılığı, Windows/macOS/Linux uyumlu.
- **İçerik:** 149 UBL-TR snippet (`Snippets/`), 17 GİB örnek XML kataloğu (`Samples/`), 16 XSLT etiket + 77 XPath autocomplete verisi (`Completion/`), `IHardwareIdProvider` soyutlaması (`Platform/`).
- **Faz 3 (tamamlandı):** Tauri masaüstü uygulaması (`app/`) Core'u JSON export yoluyla tüketiyor.

---

## Tarihçe: Eski WinForms Uygulaması (v1.0 – v2.12.0, kaldırıldı)

> **v2.13.0'da kaldırıldı.** Aşağıdaki bölümler, projenin öncüsü olan .NET Framework 4.7.2 +
> DevExpress WinForms uygulamasının (`eFaturaEdit/`) özellik geçmişidir. Bu uygulama, lisans/aktivasyon
> sistemi (QLicense, ActivationControls4Win) ve DevExpress bağımlılığıyla birlikte depodan tamamen
> kaldırılmıştır — yerini `app/` altındaki Tauri uygulaması almıştır. Aşağıdaki içerik yalnızca
> tarihsel referans amaçlıdır, aktif kodda karşılığı yoktur.

### Ana Özellikler (WinForms — kaldırıldı)

- **XSLT Editörü:** E-Fatura dizayn (XSLT) dosyalarını düzenleme — `ICSharpCode.TextEditorEx` tabanlı söz dizimi vurgulama.
- **Canlı Önizleme:** XSLT dönüşümünü `CefSharp.WinForms` (Chromium) tabanlı tarayıcıda anlık görselleştirme.
- **XSLT 3.0 Desteği:** `Saxon-HE` motoru ile gelişmiş XSLT dönüşüm desteği.
- **Tek Örnek Çalışma:** Mutex ile uygulama çift açılması engellenir, mevcut pencere öne getirilir.

### WYSIWYG Biçimlendirme Toolbar (v2.8.0)

- **Görsel Biçimlendirme:** Ribbon üzerinde 4 grup halinde WYSIWYG araç çubuğu — seçili metni HTML etiketleriyle sarar veya imleç pozisyonuna ekler.
- **Yazı Biçimi (6):** Kalın (`<b>`), İtalik (`<i>`), Altı Çizili (`<u>`), Üstü Çizili (`<s>`), Üst Simge (`<sup>`), Alt Simge (`<sub>`).
- **Hizalama (3):** Sola, Ortaya, Sağa hizalama (`<div style="text-align:...">`).
- **Stil (3):** Yazı Rengi (ColorDialog → `color:...`), Arka Plan Rengi (`background-color:...`), Yazı Boyutu (8–48pt alt menü → `font-size:...pt`).
- **Ekleme (4):** Yatay Çizgi (`<hr />`), Sıralı Liste (`<ol>`), Madde İşareti (`<ul>`), Kenarlık (`border:1px solid`).
- **Akıllı Sarmalama:** Seçili metin varsa etiketle sarar; yoksa boş etiket çifti ekleyip imleci arasına konumlar.
- **Yeni dosya:** `WysiwygHelper.cs` — `WrapSelection`, `WrapWithStyle`, `WrapWithAlignment`, `InsertAtCursor` yardımcı metotları.

### Otomatik Tamamlama / Autocomplete (v2.7.0)

- **Akıllı Öneri Sistemi:** XSLT editöründe `CodeCompletionWindow` tabanlı otomatik tamamlama.
- **Tetikleme:** `<` karakteri → XSLT etiket önerileri, `Ctrl+Space` → tam liste (XSLT + XPath + Snippet).
- **XSLT Etiket Önerileri (16):** `xsl:value-of`, `xsl:for-each`, `xsl:if`, `xsl:choose` vb.
- **UBL-TR XPath Önerileri (~75):** e-Fatura ve e-İrsaliye belge yapısı + XPath fonksiyonları.
- **Snippet Önerileri (140):** Tüm snippet'ler ★ işaretiyle listelenir; seçildiğinde tam XSLT kodu eklenir.
- **Bağlam Duyarlı:** `select=""` / `test=""` içinde XPath öncelikli gösterilir.
- **Dosya:** `XsltCompletionProvider.cs`

### UBL-TR Tam Snippet Seti (v2.6.0)

- **140 Snippet — 6 Kategori:**
  - **HTML Öğeleri (8):** Resim, Tablo, Metin, Bağlantı, Yatay Çizgi, Kutu, Kalın Metin, Etiket.
  - **XSLT Komutları (3):** XSL Değer, Döngü, Koşul.
  - **Sayfa Düzeni (5):** Barkod, QR Kod, Sayfa Sonu, Üst Bilgi, Alt Bilgi.
  - **UBL-TR e-Fatura (83):** Başlık (14), Taraflar (23), Kalemler (26), Vergi (5), Toplamlar (10), Ödeme (6), Referanslar (7).
  - **UBL-TR e-Arşiv (8):** Teslimat (4), E-Arşiv Özel (4).
  - **UBL-TR e-İrsaliye (33):** Başlık (8), Taraflar (9), Sevkiyat (8), Kalemler (6), Referanslar (2).
- **Standalone Snippet Desteği (v2.5.0):** Tüm UBL-TR alanları hem tablo/döngü snippet'lerinde hem de bağımsız tekil değer snippet'leri olarak mevcuttur.
- **e-İrsaliye Desteği (v2.6.0):** DespatchAdvice belge tipi için tam snippet seti — sevkiyat, şoför, araç/dorse plakası, taşıyıcı firma, depo/şube adresi dahil.
- **Detaylı Tooltip:** Her snippet için Türkçe açıklama, XPath bilgisi ve kullanım notları — kısaltılmadan gösterilir.
- **FontAwesome İkonlar:** Tüm butonlarda FontAwesome.Sharp 5.15.4 vektör ikonlar — 54 snippet ve 5 kategori için renkli profesyonel ikonlar.

### Sağ Tık Context Menü (v2.2.0)

- **Ağaç yapılı menü:** Editör üzerinde sağ tık ile Category → SubCategory → Snippet hiyerarşik menü.
- **Hızlı erişim:** Tıklanan snippet imleç pozisyonuna eklenir.
- **Tooltip desteği:** Her menü öğesinde tam açıklama metni.

### UBL-TR Entegrasyonu (v2.0.0)

- **Örnek Fatura Deposu:** Ribbon'da "Örnek Faturalar" dropdown — 7 kategori, 30 GİB resmi senaryo XML'i.
- **Referans Klasörü:** `UBL-TR/` altında Ornekler, Schematron, XSD dosyaları düzenli yapıda.

### Ribbon Sekme Organizasyonu (v2.9.0)

- **4 Ayrı Sekme:** Tüm araçlar mantıksal sekmelere ayrıldı (tek sekmede 13 grup yerine).
  - **Dosya:** Aç, Kaydet, Farklı Kaydet, DevTools, Yenile, PDF, Örnek Faturalar, Tema, Çıkış.
  - **Biçimlendirme:** Yazı Biçimi, Hizalama, Stil, Ekle — WYSIWYG araçları.
  - **Öğeler:** HTML Öğeleri, XSLT Komutları, Sayfa Düzeni snippet’leri.
  - **UBL-TR:** e-Fatura, e-Arşiv, e-İrsaliye snippet’leri.
- **Otomatik Yönlendirme:** `GetRibbonPageForCategory()` ile snippet kategorileri doğru sekmeye yönlendirilir.

### Öğe Ekleme Toolbar (v1.8.0 → v2.9.0)

- **Kategorili Snippet Toolbar:** Öğeler ve UBL-TR sekmelerinde ayrı gruplar — HTML Öğeleri, XSLT Komutları, Sayfa Düzeni, UBL-TR e-Fatura, UBL-TR e-Arşiv, UBL-TR e-İrsaliye. UBL-TR grupları alt menü (BarSubItem) içerir.
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
