# Özellikler (Features)

## E-Fatura Dizayn Editörü — v2.27.0

Cross-platform (macOS / Linux / Windows) masaüstü uygulaması.
**Tauri v2 + SvelteKit (Svelte 5) + CodeMirror 6 + Saxon-HE (GraalVM native sidecar).**
DevExpress/WinForms bağımlılığı yoktur; MIT lisanslıdır.

---

### ⚡ XSLT Motoru — Saxon-HE 1.0 / 2.0 / 3.0 (v2.18.0)

Tarayıcının yerleşik `XSLTProcessor`'ı yalnızca **XSLT 1.0** destekler; bu, eski
Saxon-HE tabanlı WinForms uygulamasına göre bir gerilemeydi. Çözüm: Saxon-HE 12.5
(MPL 2.0), **GraalVM native-image** ile ~45 MB'lık bağımsız bir çalıştırılabilire
derlenip Tauri **sidecar**'ı olarak paketlendi — kullanıcıda JRE kurulu olması gerekmez.

- **Tam XSLT 2.0/3.0:** `format-dateTime`, `upper-case`/`lower-case`, `tokenize`,
  `replace` (regex), `for-each-group`, `xsl:function`, `sum()`, `format-number()` vb.
- **İletişim:** stdin üzerinden uzunluk-önekli ikili protokol (`[4-bayt BE uzunluk][XSLT][4-bayt BE uzunluk][XML]`) → stdout'ta HTML.
- **Zarif geri düşüş:** Sidecar bulunamazsa tarayıcı `XSLTProcessor`'ına (XSLT 1.0) otomatik düşer — uygulama çalışmaya devam eder.
- **Uyumluluk:** UTF-8 BOM temizliği, `setGlobalContextItem` (XPDY0002), `<!DOCTYPE html>` normalizasyonu (Standards Mode garantisi).
- **Hata raporlama:** Saxon hata mesajından satır/sütun ayrıştırılır, editörde imleç hatalı satıra gider.
- Kaynak: [`sidecar/`](sidecar/) (Java + `build.sh`), [`app/src-tauri/src/xslt.rs`](app/src-tauri/src/xslt.rs), [`app/src/lib/xslt.ts`](app/src/lib/xslt.ts)

### 🤖 AI Asistanı (v2.14.0 → v2.19.x)

Sağ panelde açılabilen, XSLT dosyasına **cerrahi müdahale** edebilen sohbet asistanı.

- **Uzman rolü:** XSLT/XPath/XML/HTML/CSS/JS **ve** UBL-TR e-belge (e-Fatura, e-Arşiv,
  e-İrsaliye) alanında kıdemli uzman olarak eğitilmiş sistem promptu — `cac:`/`cbc:`/`ext:`/`ubltr:`
  ad alanları, `ProfileID`/`InvoiceTypeCode` değerleri, kanonik XPath yolları. Kapsam kilidi:
  prompt dışına çıkmaz, prompt-injection'a direnir.
- **İki hedef: tasarım + veri (v2.27.1):** XSLT'nin yanı sıra **XML belge verisini** de düzenler —
  kalem ekleme/çoğaltma, tutar/taraf/tarih değiştirme, test verisi üretme. İki koruma zorunlu:
  **XAdES imzasına dokunulmaz** (veri değişince imzanın geçersizleştiği bildirilir) ve **toplam
  zinciri güncellenir** (satır tutarı → `LegalMonetaryTotal` → KDV → `PayableAmount`); UBL öğe
  sırası korunur. Değişiklik yine yalnızca kullanıcı onayıyla uygulanır.
- **Hedefli düzenleme (SEARCH/REPLACE):** Tüm dosyayı yeniden yazmaz; yalnızca değişen blokları
  döndürür. 3 aşamalı eşleştirme (birebir → boşluk-normalize → satır bazlı, girinti toleranslı).
  Eşleşmeyen düzenleme **körlemesine uygulanmaz**, kullanıcıya bildirilir.
- **Ajan döngüsü:** En fazla 3 tur — düzenlemeyi çalışma kopyasına uygular, Saxon ile doğrular,
  hata çıkarsa hatayı modele geri besleyip kendini düzelttirir.
- **Onay modalı:** Sol tarafta diff, sağ tarafta **canlı sonuç önizlemesi** (% olarak ölçeklenebilir).
  "Uygula" dendiğinde dosya **otomatik kaydedilir** (AI'ın kaydedilmemiş kopya üzerinde çalışmasını önler).
- **Bozuk/kesik öneri uygulanmaz (v2.27.2):** Yanıt token sınırında kesilirse (`finish_reason=length`)
  içerik hiç sunulmaz; boş yanıt da başarı sayılmaz. Öneri uygulanmadan önce **iyi-biçimlilik denetimi**
  yapılır — bozuksa onay modalı açılmaz. (Uygula → otomatik kaydet zinciri olduğundan, kesik bir
  "tam dosya" önerisi belgeyi doğrudan bozardı; gerçek vakada 172 KB fatura 17 KB'a düşmüştü.)
- **Hata farkındalığı (v2.27.2):** Ekrandaki dönüşüm hatası mesaja iliştirilir — model hatayı
  **ajan modu kapalıyken de** görür ve doğrudan giderir.
- **Ekler:** Dosya seçici + panodan yapıştırma. Görsel/PDF → çok-kipli (multimodal) gönderim;
  metin dosyaları → önbelleklenen bağlam. Görseller `⬇ göm` ile base64 data-URI olarak editöre gömülebilir.
- **4 sağlayıcı:** Anthropic (Claude), Google (Gemini), OpenAI-uyumlu (OpenAI, NVIDIA, yerel),
  DeepSeek (v2.27.0; deepseek-chat / deepseek-reasoner). Model listesi API'den canlı çekilir;
  sohbete uygun olmayan modeller (görsel üretim vb.) elenir.
- **Model rozeti:** Panel başlığında etkin sağlayıcı ve model adı gösterilir.
- **Dinamik model parametreleri (v2.27.0):** `AI_PARAM_DESCRIPTORS` deseni — hangi kontrolün
  hangi sağlayıcı+modelde görüneceği descriptor kayıtlarından belirlenir. Derin düşünme
  (Anthropic extended thinking, OpenAI `reasoning_effort`, Gemini `thinkingConfig`) ve
  temperature; Ayarlar → AI'daki kontroller + AI panelinde hızlı 🧠 düğmesi.
- **Prompt caching (v2.19.0):** Sistem promptu + dosya bağlamı **kararlı önek** olarak ayrı gönderilir —
  Anthropic'te `cache_control: ephemeral`, OpenAI'de otomatik önek önbelleği, Gemini'de örtük önbellek.
- **Token disiplini:** Dosya yalnızca son mesaja eklenir (her turda değil), geçmiş 12 mesajla sınırlanır,
  base64 data-URI'ler bağlamdan çıkarılır (587 KB → 79 KB, %87 tasarruf).
- **Durdur düğmesi + timeout (v2.27.1):** Yanıt beklenirken "Gönder"in yerini kırmızı **"■ Durdur"**
  alır; basınca gösterge anında kapanır ve **geç gelen yanıt sohbete sızmaz** (her isteğe kimlik
  verilir, iptalde kimlik geçersizleşir). Ajan modunda turlar arasında da kontrol edilir. Ayrıca
  her AI çağrısında bağlantı için 15 sn / yanıt için 180 sn üst sınır vardır — sağlayıcı yanıt
  vermezse uygulama **sonsuza kadar beklemez**, anlamlı hata verir.
- **Ölçülebilir çağrılar (v2.27.1):** Her AI isteği günlüğe yazılır — sağlayıcı, model, uç nokta,
  bağlam boyutu, süre, sonuç ve gerçek hata gövdesi. **API anahtarı loglanmaz.**
  (Ayarlar → Hakkında → "Günlük klasörünü aç")

### 🎨 Görsel Düzenleyici (WYSIWYG) — Faz 1 + 2a (v2.20.0)

Önizlemede bir öğeye tıklayıp doğrudan biçimlendirme.

- **Tıkla-seç:** Fare ile üzerine gelince vurgulama, tıklayınca seçim; kararlı CSS seçici üretilir
  (`#id` → `.class` → `:nth-of-type`).
- **Görsel stil editörü:** Renk, arka plan, yazı boyutu/kalınlığı, hizalama, dolgu, kenarlık,
  köşe yarıçapı, genişlik. Değişiklikler **canlı** olarak önizlemeye enjekte edilir; "Uygula" dendiğinde
  XSLT'nin stil bloğuna **deterministik** CSS kuralı olarak yazılır (AI'sız). Kural zaten varsa güncellenir.
- **Sabit metin düzenleme:** Öğe yalnızca düz metin içeriyorsa metni doğrudan değiştirebilirsin.
  **Belirsizlik koruması:** metin şablonda benzersiz değilse veya XML verisinden geliyorsa
  değişiklik **reddedilir** ve uyarı verilir — yanlış yeri değiştirme riski yok.
- **Kaynak eşleme (Faz 2a, salt-okunur):** Önizlemedeki öğenin **XSLT'de hangi satırdan geldiğini**
  gösterir; tıklayınca editörde o satıra atlar. Seçim modu açıkken şablonun **bellek içi geçici kopyasına**
  `data-xsl-id` enjekte edilir — kullanıcının dosyasına asla yazılmaz. Doğrulandı: işaretler çıkarıldığında
  çıktı, temiz dönüşümle **bayt bayt aynıdır** (render etkilenmez).
  Kaynak: [`app/src/lib/xslt-map.ts`](app/src/lib/xslt-map.ts)

### 📄 Çok Sayfalı Fatura — Nakli Yekûn (v2.29.0)

Varsayılan örnek şablon (`default.xslt`) artık **matbu fatura gibi sayfalanır**:

- Her sayfaya `$sayfaSatiri` kalem düşer — **sabit değil, parametre**:
  ```xml
  <xsl:param name="sayfaSatiri" select="20"/>
  ```
  Sayfa sayısı, devir tutarları ve boş satır dolgusu bu tek değerden türetilir.
- Sayfanın altında **NAKLİ YEKÛN (sonraki sayfaya devir)**, sonraki sayfanın başında
  **NAKLİ YEKÛN (önceki sayfadan devir)**.
- **Logo, satıcı/alıcı, ETTN — tüm başlık her sayfada tekrar eder** (sayfa 2 tek başına da
  okunabilir bir fatura sayfasıdır).
- **Gerçek toplamlar yalnızca son sayfada.** `cac:LegalMonetaryTotal` / `cac:TaxTotal` **belgeden
  okunur, hesaplanmaz** — fatura ne beyan ediyorsa o basılır. Ara sayfaların alt kutusunda
  *Sayfa Toplamı* + *Nakli Yekûn* görünür; bu iki değer sunum katmanında hesaplanır, çünkü UBL'de
  "ilk N kalemin toplamı" diye bir alan yoktur.
- Yazdırmada sayfa sonu doğru yere düşer (`page-break-after`).

**Geriye uyum ölçüldü:** 20 kalemden az faturalarda çıktı eskisiyle **birebir aynı** — 29 örnek
fatura sidecar'a beslenip karşılaştırıldı, tek fark eklenen sayfa kabı `<div>`'i.

### ✂️ Snippet Sistemi — 294 Snippet

| Kategori | Adet | İçerik |
| --- | --- | --- |
| UBL-TR e-Fatura / e-Arşiv / e-İrsaliye + HTML/Sayfa Düzeni | 149 | Başlık, taraflar, kalemler, vergi, toplamlar, ödeme, referanslar, barkod/QR, üst-alt bilgi |
| UBL 2.1 (Uluslararası) (v2.27.0) | 39 | EN 16931 / Peppol BIS 3.0 kapsamı — başlık, taraflar, kalemler, vergi, toplamlar; İngilizce çıktı etiketleri (`UBL21_*`) |
| XSLT Komutları | 56 | **XSLT 1.0 / 2.0 / 3.0** ayrı alt kategoriler — tam komut seti |
| XPath Fonksiyonları | 26 | XPath 1.0 + XPath 2.0/3.0 (dize, sayı, tarih, dizi, düzenli ifade) |
| CSS Kuralları | 24 | Metin, Kutu & Kenarlık, Yerleşim, Tablo, Sayfa & Baskı |

- **Kullanıcı snippet'leri:** Kendi snippet'lerini ekleyip kalıcı saklayabilirsin.
- **Sürükle-bırak:** Özel fare-izleme implementasyonu (WKWebView'de HTML5 drag API güvenilmez) —
  görsel ghost gösterge, hedef editörde mavi vurgulama.
- **Autocomplete:** 387 öneri (16 XSLT etiketi + 77 UBL-TR XPath + 294 snippet) — `Ctrl+Space` veya `<` ile tetiklenir.

### 🖥️ Editör ve Arayüz

- **3 panel düzen:** Sol snippet paneli, orta XSLT+XML editörleri, sağ canlı önizleme —
  hepsi fare ile yeniden boyutlandırılabilir (`Splitter`, sınır kısıtı yok).
- **CodeMirror 6:** Syntax highlight, satır numarası, Türkçe arama paneli (Cmd/Ctrl+F), undo/redo.
- **Kod katlama (v2.27.3):** Satır numarası yanındaki oklarla fare ile; klavyeyle **tümünü**
  katla/aç `Ctrl+Alt+[` / `Ctrl+Alt+]` (macOS'ta da `Ctrl` — `Cmd` değil), **tek blok** macOS'ta
  `Cmd+Alt+[` / `]`, diğer sistemlerde `Ctrl+Shift+[` / `]`. Her editör başlığında **⊟ / ⊞**
  düğmeleri — UBL-TR belgeleri derin iç içe olduğundan "tümünü katla" yapıyı bir bakışta gösterir.
- **Toplu regresyon koşusu (v2.29.0):** Toolbar → **🧪 Toplu Test**. Şablonu bir klasördeki tüm
  faturalara karşı çalıştırır; hangileri patladı, çıktı kaç bayt, ne kadar sürdü. **📸 Anlık
  Görüntü** her çıktının **sha256**'sını saklar → şablonu değiştirip tekrar koşunca hangi
  faturaların çıktısının **DEĞİŞTİĞİ** satır satır çıkar. Satıra çift tık → fatura editöre yüklenir.
  Sıralı çalışır (50 faturaya aynı anda 50 sidecar açmaz).
- **XPath test konsolu (v2.28.0):** XML panelindeki **ƒx** düğmesi veya `Cmd/Ctrl+Shift+X`.
  İfadeyi yaz → Enter → yüklü faturaya karşı anında çalışır; kaç düğüm eşleşti ve değerleri ne
  görünür. `↑`/`↓` geçmiş, `Esc` kapatır. Şablonu kurcalayıp dönüştürmeden "bu alan neden boş
  geliyor?" sorusunu yanıtlar. Önekler belgenin **kökünden** okunur ve `xpath-default-namespace`
  otomatik bildirilir — UBL kökü varsayılan namespace'te olduğundan bu olmadan `/Invoice/cbc:ID`
  hiçbir şey eşleştirmezdi. Saxon yoksa tarayıcının XPath 1.0'ına düşer ve bunu **`⚠️ XPath 1.0`
  rozetiyle söyler** (sessiz geri düşüş yok).
- **Sağ tık menüsü (v2.27.5):** Editörde sağ tık → Kes / Kopyala / Yapıştır · Tümünü seç ·
  Ara ve değiştir · Satıra git · Katla/aç · Geri al / Yinele — **kısayollar menüde yazılı**.
  XSLT editöründe ayrıca **snippet'ler kategori alt menüsünden** imlece eklenir. Pano, Tauri'nin
  `clipboard-manager` eklentisiyle OS üzerinden okunur (webview'ın `readText()`'i sessizce boş
  dönebiliyor); hata olursa durum çubuğunda **görünür** şekilde bildirilir.
- **12 tema:** Açık/koyu varyantlar; tema **belge kökünde** (`html.dark`) uygulanır — modallar,
  ayrı rotalar ve kapsamlı bileşenler dahil **tüm alanlara** yansır.
- **Dosya işlemleri:** Aç / Kaydet / Farklı Kaydet (native dialog), `Cmd/Ctrl+S` ile XSLT+XML birlikte
  kaydetme, kaydetmeden önce syntax kontrolü (hata varsa imleç hatalı satıra gider), son 10 dosya listesi.
- **Çoklu dosya sekmesi (v2.30.0):** Bir sekme = bir **çalışma**, yani bir XSLT+XML **çifti** ve o
  çiftin önizlemesi (pano başına ayrı sekme değil — dönüşümün girdisi zaten şablon + veridir).
  Sekme şeridi editörlerin üstünde: dosya adı, kaydedilmemiş göstergesi (●), kapatma (×),
  sürükle-bırakla yeniden sıralama.
  - **Kısayollar:** `Cmd/Ctrl+T` yeni sekme · `Cmd/Ctrl+1…9` N'inci sekme ·
    `Ctrl+Tab` / `Ctrl+Shift+Tab` sonraki/önceki. Kapatma: × veya **orta tık**.
    `Cmd+W` **bilerek yok** — macOS'ta Tauri'nin varsayılan menüsündeki "Pencereyi Kapat"a aittir,
    webview'e hiç ulaşmaz; sekmeye bağlasaydık üç platformdan birinde sessizce çalışmazdı.
  - **Kaydedilmemiş içerik asla ezilmez:** Dosya/örnek yüklenirken aktif sekmedeki ilgili slot
    kirliyse yeni sekmede açılır. Veri yüklenirken yeni sekme **temiz** şablonu devralır
    ("aynı şablon, başka fatura" akışı sürsün diye); kirli şablon devralınmaz.
  - Çıkış koruması ve "Kaydet ve Çık" **tüm sekmeleri** kapsar, yalnızca aktif olanı değil.
- **Sürükle-bırak:** `.xslt`/`.xsl`/`.xml` dosyalarını Finder/Explorer'dan doğrudan pencereye bırak.
  Tauri'nin native sürükle-bırak olayı kullanılır (HTML5 drag API WKWebView'de güvenilmez).
- **"Birlikte Aç":** Uygulama `.xslt`/`.xsl` ve `.xml` türlerini işletim sistemine kaydeder —
  sağ tık → Birlikte Aç → e-Fatura Edit.
- **İçeriğe göre yönlendirme:** Hedef editör uzantıya değil **içeriğe** göre seçilir; XSLT ad alanını
  bildiren belge şablondur. Böylece `.xml` uzantılı bir şablon veri alanına düşmez (ve tersi).
- **Varsayılan veri eşlemesi:** XSLT tek başına açılırsa paketli varsayılan UBL-TR faturasıyla otomatik
  eşlenir — önizleme anında derlenir, elle XML aramana gerek kalmaz.
- **Otomatik dönüştür:** Yükleme/kaydetme sonrası ve yazarken debounce ile (varsayılan 700 ms).
- **Auto-save:** Ayarlardan açılabilir, belirtilen gecikmeyle sessiz kaydetme.
- **Çıkışta kaydetme kontrolü:** Kaydedilmemiş değişiklik varsa çıkışı engelleyip
  "İptal / Kaydetmeden Çık / Kaydet ve Çık" seçenekli onay penceresi gösterir.
- **Önizleme:** Responsive boyut seçici (320/768/1200/Full), zoom (Cmd +/-/0),
  sağ tık menüsü (Yazdır/PDF, HTML kopyala, DevTools). Yazdırma sistem tarayıcısına devredilir.
- **Ayarlar:** Font boyutu, sekme genişliği, kelime kaydırma, tema, autosave/debounce/autocomplete,
  AI sağlayıcı yapılandırması.
- **Yardım:** F1 ile açılan, sidebar navigasyonlu, aranabilir tam dokümantasyon + tooltip'ler.
- **Hakkında:** Sürüm, lisans (MIT) ve üçüncü taraf lisans bilgileri (Saxon-HE MPL 2.0 dahil).
- **Örnek fatura kataloğu:** 6 kategori × 17 GİB resmi UBL-TR senaryosu.
- **Varsayılan örnek yalın (v2.27.3–v2.27.4):** `default.xml` 172 KB → **8 KB**. Gerçek UBL-TR
  faturaları kendi tasarımını (XSLT) ve XAdES imzasını **base64 gömülü** taşır; bu editörde ikisi
  de gereksizdir. Doğrulandı: kaldırıldıktan sonra Saxon çıktısının **sha256'sı aynı** — görünüm
  değişmiyor. AI'a giden bağlamda da `cbc:EmbeddedDocumentBinaryObject` içeriği kırpılır (gerçek
  müşteri faturalarında da her istekte ~150 KB token yakıyordu).

### 🌍 Çoklu Dil (i18n) — 5 Dil + Çeviri Düzenleyici (v2.26.0)

- **5 arayüz dili:** Türkçe (referans), İngilizce, İspanyolca, Rusça, Lehçe.
  Ayarlar → Dil'den seçilir; tüm arayüz anında değişir. 332 metin × 5 dil betikle
  doğrulanır (eksik anahtar / bozuk yer tutucu = 0).
- **Çeviri düzenleyici** (`/translations`): her metin uygulama içinden düzenlenebilir
  (Türkçe dahil), yer tutucu bozulursa satır kırmızı işaretlenir. JSON dışa/içe aktarma
  ve UI'dan yeni dil ekleme/silme. Kullanıcı çevirileri yerleşiklerle **birleştirilmez**,
  ayrı saklanır.
- Açılışta `validateLocales()` bütünlük denetimi yapar; sorunlar günlüğe yazılır.
- **Bilinen sınırlamalar:** AI uzman promptu kasten Türkçe (UBL-TR alan bilgisi);
  CodeMirror arama paneli dili editör açılışında seçilir; Yardım içeriği şimdilik Türkçe.
- Kaynak: [`app/src/lib/locales/`](app/src/lib/locales/), [`app/src/routes/translations/`](app/src/routes/translations/)

### 🔐 Güvenlik

- **API anahtarları OS anahtar zincirinde:** Rust `keyring` crate'i ile macOS Keychain /
  Windows Credential Manager / Linux Secret Service'te **şifreli** saklanır. `localStorage`'a
  düz metin yazılmaz; eski düz-metin anahtarlar ilk açılışta otomatik olarak anahtar zincirine taşınır.
  Kaynak: [`app/src-tauri/src/lib.rs`](app/src-tauri/src/lib.rs) (`secret_set` / `secret_get`)

### 🔄 Otomatik Güncelleme (v2.22.0)

- Açılışta sessizce yeni sürüm denetlenir; varsa sürüm notlarıyla bir pencere çıkar.
  **İndirme/kurma yalnızca kullanıcı onaylarsa** başlar; ilerleme gösterilir, bitince uygulama yeniden başlar.
- Elle denetleme: **Ayarlar → Hakkında → "Güncellemeleri denetle"**.
- **İmzalı:** Paketler minisign anahtarıyla imzalanır; uygulama, gömülü açık anahtarla doğrulayamadığı
  hiçbir güncellemeyi kurmaz. Doğrulama Rust tarafında yapılır.
- Linux'ta yalnızca **AppImage** için çalışır (`.deb`/`.rpm` paket yöneticisinin sorumluluğundadır).

### 📦 Dağıtım

- **GitHub Releases:** Her sürüm **5 runner** üzerinde otomatik derlenip yayımlanır —
  macOS arm64 (`macos-14`), macOS Intel (`macos-15-intel`), Linux x86_64 (`ubuntu-22.04`),
  Linux arm64 (`ubuntu-22.04-arm`), Windows (`windows-latest`).
  Saxon sidecar her runner'da GraalVM ile o platforma özgü olarak derlenir.
  Kaynak: [`.github/workflows/release.yml`](.github/workflows/release.yml)
- **`eFaturaEdit.DataExport` tool'u:** Core POCO'larını TypeScript-uyumlu JSON'a dönüştürür
  (`npm run data:sync`).

### Mimari — Faz 2: Cross-Platform Çekirdek (v2.11.0)

- **`eFaturaEdit.Core` kütüphanesi:** UI-bağımsız veri katmanı. Multi-target `netstandard2.0;net10.0`,
  sıfır dış NuGet bağımlılığı, Windows/macOS/Linux uyumlu.
- **İçerik:** 149 UBL-TR snippet (`Snippets/`), 17 GİB örnek XML kataloğu (`Samples/`),
  16 XSLT etiket + 77 XPath autocomplete verisi (`Completion/`).
- **Faz 3 (tamamlandı):** Tauri masaüstü uygulaması (`app/`) Core'u JSON export yoluyla tüketir.

### Bilinen sınırlamalar

- Native menü çubuğu yok (macOS'ta yalnızca Tauri'nin varsayılan menüsü).
- Sekmeler oturumla birlikte kapanır — uygulama açılışında **geri yüklenmez**.
- WYSIWYG kaynak eşlemesi yalnızca **literal** öğeleri kapsar; `<xsl:element name="...">` ile
  dinamik üretilen öğeler eşlemede görünmez.
- macOS için evrensel (universal) ikili üretilemez — GraalVM native-image tek mimari derler;
  bu yüzden arm64 ve Intel ayrı yayımlanır.

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
