# Değişiklik Günlüğü (Changelog)

Tüm önemli değişiklikler bu dosyada belgelenir.
Format [Semantic Versioning](https://semver.org/lang/tr/) kurallarına uygundur.

## [2.21.0] — 2026-07-12 — Sürükle-Bırak, "Birlikte Aç" ve Varsayılan Veri Eşlemesi

### Eklenen
- **Finder/Explorer'dan sürükle-bırak:** `.xslt` / `.xsl` / `.xml` dosyalarını doğrudan pencereye
  bırakarak açabilirsin. Sürükleme sırasında tam ekran bir bırakma göstergesi çıkar.
  Tauri'nin **native** sürükle-bırak olayı kullanılır (HTML5 drag-drop WKWebView'de güvenilir değil
  ve webview'de zaten kapalıdır) — `getCurrentWebview().onDragDropEvent`.
- **"Birlikte Aç" (dosya ilişkilendirmesi):** Uygulama artık `.xslt`/`.xsl` ve `.xml` dosya türlerini
  işletim sistemine kaydeder; Finder/Explorer'da sağ tık → Birlikte Aç → e-Fatura Edit çalışır.
  Uygulama kapalıyken açılan dosyalar Rust tarafında kuyruğa alınır (`take_opened_files`), açıkken
  gelenler `files-opened` olayıyla iletilir. macOS `RunEvent::Opened`, Windows/Linux argv kullanır.
- **Varsayılan veri eşlemesi:** XSLT tek başına açıldığında (sürükleme, "Birlikte Aç" veya Aç düğmesi)
  ve elde XML verisi yoksa, paketli varsayılan UBL-TR faturasıyla otomatik eşlenir — önizleme anında
  derlenir. Önceden şablon açılsa bile veri olmadığı için dönüşüm hiç çalışmıyordu.
  Bu veriye disk yolu atanmaz; kullanıcı isterse "Farklı Kaydet" der.

### Güvenlik / Doğruluk
- **Şablon veri alanına, veri şablon alanına düşmez.** Hedef editör **uzantıya değil, içeriğe** göre
  seçilir: belge XSLT ad alanını (`http://www.w3.org/1999/XSL/Transform`) bildiriyorsa şablondur.
  Uzantıya güvenmek yanlış olurdu — XSLT de geçerli bir XML'dir ve `.xml` olarak kaydedilmiş şablonlar
  vardır. Bu koruma sürükle-bırakta, "Birlikte Aç"ta ve **Aç düğmelerinde** de geçerli: "XML Aç" ile bir
  şablon seçersen reddedilir (aksi halde dönüşümün girdisi şablonun kendisi olur ve önizleme sessizce
  anlamsız çıkardı).
- Birden çok dosya bırakılırsa her türden ilki alınır; atlananlar durum çubuğunda **açıkça bildirilir**,
  sessizce yutulmaz.

---

## [2.20.1] — 2026-07-11 — Intel macOS Release Derlemesi Onarıldı — .github/workflows/release.yml

### Düzeltilen
- **Intel macOS yapısı hiç üretilmiyordu.** GitHub `macos-13` runner'ını emekliye ayırdığından
  (dokümanlardan tamamen kaldırılmış) matristeki Intel işine runner atanmıyor, iş **sonsuza dek
  kuyrukta** bekliyordu. v2.17.0 – v2.20.0 arasındaki release'ler bu yüzden Linux, Windows ve
  macOS arm64 varlıklarıyla yayımlandı; **Intel `.dmg` eksik kaldı**.
- Runner `macos-13` → **`macos-15-intel`** olarak değiştirildi (GitHub'ın Intel x86_64 halefi).
  Artık 4 platform da eksiksiz derleniyor.

---

## [2.20.0] — 2026-07-11 — Görsel Düzenleyici Faz 2a: XSLT Kaynak Eşlemesi — app/src/lib/xslt-map.ts

### Eklenen
- **Kaynak eşlemesi (salt-okunur):** Görsel düzenleyicide önizlemedeki bir öğeye tıklayınca panelde
  **"📍 XSLT satır N · `<td>`"** rozeti çıkar; tıklandığında XSLT editörü o satıra atlar. Böylece
  "bu hücre şablonun neresinden geliyor?" sorusu tek tıkla yanıtlanır.
- **`instrumentXslt()`** (`app/src/lib/xslt-map.ts`): Şablonun **bellek içi geçici bir kopyasındaki** her
  literal (öneksiz) öğeye `data-xsl-id` enjekte eder ve `id → {satır, öğe adı, ofset}` eşlemesi üretir.
  Regex değil, küçük bir **XML tokenizer** kullanılır — 587 KB'lık şablonlarda gömülü CSS, yorumlar, CDATA ve
  tırnak içindeki `>` karakteri naif regex'i kaçınılmaz olarak bozar.
- Seçilen öğenin kendisi işaretli değilse **en yakın işaretli atası** kullanılır ve rozette
  *"(en yakın üst öğe)"* olarak belirtilir.

### Güvenlik / Doğruluk
- **Kullanıcının dosyasına asla yazılmaz.** Enstrümante kopya yalnızca önizleme dönüşümünde kullanılır;
  `editorState.xsltText` el değmeden kalır, kaydedilmez, dışa aktarılmaz.
- **Çıktı bozulmuyor — kanıtlandı:** Gerçek 587 KB'lık `default.xslt` üzerinde enstrümante dönüşümün çıktısından
  `data-xsl-id`'ler çıkarıldığında sonuç, temiz dönüşümle **bayt bayt aynı** (542.081 = 542.081 karakter).
- Enstrümantasyon 587 KB'da **6 ms** sürer: satır numaraları önceden hesaplanan `lineStarts[]` üzerinde
  ikili aramayla (O(n²) değil), enjeksiyon ise tek geçişli `join()` ile (O(n·m) değil) yapılır.

### Değişen
- Görsel düzenleyici açılıp kapandığında önizleme yeniden üretilir (işaretli ↔ temiz sürüm arası geçiş).
  Mod, iframe her yeniden yüklendiğinde `onload`'da geri verilir — yarış durumu yok.
- **`FEATURES.md` güncellendi:** v2.12.0'da kalmıştı; Saxon XSLT 2.0/3.0 motoru, AI asistanı, 255 snippet,
  anahtar zinciri, görsel düzenleyici, Hakkında ekranı ve çok-platform release akışı eklendi.

### Bilinen sınırlama
- Yalnızca **literal** öğeler eşlenir; `<xsl:element name="...">` ile dinamik üretilen öğeler eşlemede görünmez.

---

## [2.19.1] — 2026-07-11 — Görsel Düzenleyicide Sabit Metin Düzenleme — app/src/routes/+page.svelte

### Eklenen
- **Sabit metin düzenleme:** Görsel düzenleyicide seçilen öğe yalnızca düz metin içeriyorsa (etiketler: `Fatura Numarası:`, `SAYIN`, `Banka`, `Hesap No`...) panelde bir **Metin** alanı çıkar; yazdıkça önizlemede canlı değişir, "Metni Uygula" ile XSLT'ye yazılır. Metin şablonda birebir geçtiğinden hedefli bul/değiştir yeterlidir.
- **Belirsizlik koruması:** Aynı metin birden çok yerde geçiyorsa önce etiket sınırlarıyla (`>metin<`) daraltılır; yine benzersiz değilse **hiçbir şey değiştirilmez** ve kullanıcı uyarılır (yanlış yeri bozmaktansa dokunmamak). Metin XML verisinden geliyorsa (şablonda bulunamazsa) bu da açıkça bildirilir.
- Karma içerikli öğeler (ör. logo hücresi) sabit metin sayılmaz — alan çıkmaz.

## [2.19.0] — 2026-07-11 — Görsel Düzenleyici (WYSIWYG Faz 1): Tıkla-Seç + Stil Paneli — app/src/routes/+page.svelte

### Eklenen — Görsel Düzenleyici (🎯 Seç & Düzenle)
- **Önizlemede tıkla-seç:** Mod açıkken fare üzerindeki öğe çerçevelenir; tıklanınca öğenin kararlı CSS seçicisi (id → class → en yakın id'li atadan `nth-of-type` yolu) ve hesaplanmış stilleri uygulamaya bildirilir (iframe köprüsü genişletildi).
- **Stil paneli:** Yazı rengi/boyutu/kalınlığı/hizalaması, arka plan, iç boşluk, kenarlık, köşe, genişlik — kontroller öğenin mevcut değerleriyle dolu gelir.
- **Anlık önizleme:** Her değişiklikte iframe'e canlı CSS enjekte edilir — XSLT'ye dokunulmadan, dönüşüm beklenmeden anında görülür.
- **XSLT'ye uygula (deterministik, AI'sız):** Üretilen CSS kuralı XSLT'nin stil bloğuna yazılır; aynı seçici için kural varsa **güncellenir** (yinelenmez). Sonuç mevcut onay modalından geçer → kırmızı/yeşil diff + canlı sonuç önizlemesi + onayda otomatik kaydetme.
- **Şeffaf uyarı:** Bir CSS kuralı seçiciye uyan **tüm** öğeleri etkiler (tek bir fatura satırı değil, hepsi) — panel bunu açıkça belirtir; tek satır için `:nth-child(n)` önerilir. Bu, şablon mantığının doğası olduğundan gizlenmez.

### Düzeltilen (geliştirici notu)
- Kaynak dosyada **yorumların içinde bile** bitişik stil/script etiketi yazmak Svelte'in blok-sınırı ön taramasını bozuyor (bileşenin script bloğunu erken kapatıyor). İki yerde bu tuzağa düşüldü ve giderildi; etiket adları artık `STYLE_TAG`/`SCRIPT_TAG` değişkenlerinden interpolasyonla üretiliyor.

## [2.18.0] — 2026-07-11 — Prompt Caching + Koyu Tema Düzeltmesi + Uygula-Kaydet — app/src-tauri/src/ai.rs, app/src/routes/+layout.svelte

### Eklenen — Prompt Caching (token maliyeti ~%90 düşürüldü)
- **Dosya bağlamı artık promptun KARARLI ÖNEKİNE (system bölümü) konuyor**, sohbet mesajına değil. Önceden son mesaja ekleniyordu; bu, öneki her turda değiştirdiğinden hiçbir sağlayıcının cache'i tutmuyordu.
- **Anthropic:** bağlam bloğu `cache_control: ephemeral` ile işaretlenir → sonraki turlarda cache okuma ≈ girdi fiyatının %10'u.
- **OpenAI (+ NVIDIA/Ollama uyumlu):** bağlam ilk `system` mesajına birleştirilir → otomatik önek cache'i (≥1024 token) devreye girer.
- **Gemini:** bağlam `system_instruction`'a konur → örtük cache önekte tutar.
- Ajan modu turları da aynı mekanizmayı kullanır. Görsel/PDF ekleri (turdan tura değişen) mesajda kalır; metin ekleri önbelleklenebilir bağlama girer.
- Base64 kırpma (v2.14) ile birlikte: 587 KB'lık şablon → ~20K token → **2. turdan itibaren ~2K etkin token**.

### Düzeltilen
- **Koyu tema tüm alanlara uygulanmıyordu.** Tema sınıfı `+page.svelte` içindeki `.app` div'ine veriliyordu; modallar bu div'in DIŞINDA, Ayarlar AYRI bir route, AI paneli kendi stil kapsamındaydı → koyu tema oralara hiç ulaşmıyordu. Tema artık belge köküne (`<html class="dark">`, yeni `+layout.svelte`) uygulanıyor; modallar, AI paneli ve Ayarlar sayfası için koyu tema kuralları eklendi.
- **AI önerisi uygulanınca dosya diske kaydedilmiyordu** — editör güncel, disk bayat kalıyordu. `confirmAiApply` artık `saveOne()` ile kaydediyor (syntax doğrulaması yapar; bozuk çıktı diske yazılmaz, dosya kaydedilmemişse uyarır).

### Eklenen — UI
- AI panel başlığında **kullanılan sağlayıcı/model rozeti** (ör. `Claude (Anthropic) · claude-sonnet-5`).

## [2.17.0] — 2026-07-11 — XSLT 2.0/3.0 Motoru (Saxon-HE) + XSLT Komut Seti + Görsel Gömme — sidecar/, app/src/lib/xslt.ts, app/src/lib/data/xslt-snippets.ts

### Eklenen — XSLT 2.0/3.0 Desteği (regresyon giderildi)
- **Saxon-HE (MPL 2.0) native sidecar** ile tam **XSLT 1.0/2.0/3.0** desteği. Tarayıcının `XSLTProcessor`'ı yalnızca 1.0 destekliyordu (eski WinForms Saxon-HE'ye göre gerilemeydi); `format-dateTime`, `format-date`, `upper-case`, `tokenize`, `replace`, `matches`, `xsl:for-each-group`, `xsl:function`, `xsl:iterate` vb. artık çalışıyor.
- `sidecar/`: Java transformer (stdin uzunluk-önekli XSLT+XML → stdout HTML, BOM/DOCTYPE normalize) + GraalVM `native-image` derleyici (`build.sh`, Windows dahil taşınabilir). Saxon-HE jar'ları + reflection config commit'li; ~40 MB native binary CI'da 3 platformda derlenir (gitignore).
- Rust: `tauri-plugin-shell` + `xslt_transform` komutu (externalBin sidecar). Frontend `xslt.ts` sidecar-öncelikli; sidecar yoksa tarayıcı 1.0 işlemcisine düşer.
- Performans: ~57 ms (587 KB GİB şablonu + fatura XML).

### Eklenen — Snippet Kütüphanesi Genişletildi
- **"XSLT Komutları" kategorisi** üç alt sürümle dolduruldu: **XSLT 1.0** (29 öğe: stylesheet, template, apply/call-templates, choose, key, decimal-format, attribute-set...), **XSLT 2.0** (15: function, for-each-group, analyze-string, format-date/dateTime, tokenize, replace...), **XSLT 3.0** (11: mode, iterate, try/catch, merge, map, where-populated, assert, package...).
- **"XPath Fonksiyonları" kategorisi** (yeni): XPath 1.0 (sum, count, format-number, substring, concat, translate, normalize-space, contains, position...) ve XPath 2.0/3.0 (format-date, string-join, tokenize, replace, distinct-values, upper-case, if/then/else...) — fatura şablonlarına özel örneklerle.

### Eklenen — Hakkında / Lisans
- Ayarlar sayfasına **Hakkında** bölümü: sürüm, MIT lisansı, telif, kaynak kodu bağlantısı ve kullanılan açık kaynak bileşenlerin (Saxon-HE MPL 2.0, Tauri, Svelte, CodeMirror...) atıfları.

### Eklenen — Görsel Gömme
- AI paneline yapıştırılan/eklenen görselde **"⬇ göm"** butonu — base64 data URI olarak XSLT editörüne imleç konumuna `<img>` gömer (AI'a gerek yok).

### Değişen
- AI sistem promptundaki "yalnızca XSLT 1.0" kısıtı kaldırıldı; 2.0/3.0 özellikleri ve Türkçe `format-date`/`format-number` rehberi eklendi.
- Release CI: macOS iki mimariye ayrıldı (native-image universal üretemez) → Apple Silicon + Intel ayrı `.dmg`.

## [2.16.0] — 2026-07-11 — AI Uzmanlaştırma + Oturum Kalıcılığı + Onay Modalı Yeniden Düzenlendi — app/src/lib/AIAssistant.svelte, app/src/routes/+page.svelte

### Değişen — AI Asistan
- **Uzman sistem promptu:** Asistan artık (1) XSLT 1.0/2.0, XPath, XML/XSD, HTML5, CSS3, JavaScript ve baskıya uygun belge tasarımı; (2) UBL-TR 1.2 (e-Fatura, e-Arşiv, e-İrsaliye, e-İrsaliye Yanıtı, e-Müstahsil, e-SMM, e-Uygulama Yanıtı) alanlarında **kıdemli uzman** olarak tanımlandı. Ad alanları (cac/cbc/ext/ubltr), ProfileID/InvoiceTypeCode değerleri, taraf/kalem/vergi/toplam XPath yolları prompta gömüldü.
- **Kritik motor uyarısı prompta eklendi:** Önizleme tarayıcının `XSLTProcessor`'ını kullanır → **yalnızca XSLT 1.0/XPath 1.0** çalışır (dosyada `version="2.0"` yazsa bile). `for-each-group`, `format-dateTime`, `tokenize`, `replace`, `matches` vb. yasaklandı; 1.0 karşılıkları (Muenchian gruplama, `substring`/`concat`, `format-number` + `decimal-format`) belgelendi.
- **Kapsam kilidi sertleştirildi:** Asistan promptta tanımlı iş dışında hiçbir şey yapmaz; rol değiştirme/prompt injection denemelerine karşı korumalı, imza (`ds:Signature`) ve veri anlamı asla değiştirilmez.

### Düzeltilen
- **Ayarlar'a gidip dönünce AI sohbeti sıfırlanıyordu** — bileşen yeniden mount olduğunda aktif oturum kayboluyordu. Aktif sohbet artık modül seviyesinde tutuluyor; sayfa geçişlerinde korunuyor, uygulama yeniden başlatılınca (istendiği gibi) yeni sohbetle açılıyor.

### Değişen — Onay Modalı
- **"AI Önerisini Uygula" modalı iki sütuna ayrıldı:** solda diff/açıklama, **sağda canlı sonuç önizlemesi**. Önizleme artık **% olarak ölçeklenebilir** (−/+/⟲, %25–%200).

## [2.15.0] — 2026-07-11 — Güvenlik: API Anahtarları OS Anahtar Zincirinde — app/src-tauri/src/lib.rs, app/src/lib/settings.svelte.ts

### Değişen (Güvenlik)
- **API anahtarları artık düz metin `localStorage`'da tutulmuyor** — OS anahtar zincirinde (macOS Keychain / Windows Credential Manager / Linux Secret Service) şifreli saklanıyor. Eski sürümden kalan düz-metin anahtarlar açılışta otomatik olarak zincire taşınıp `localStorage`'dan temizlenir (migrasyon).
- Rust: `keyring` crate + `secret_set` / `secret_get` Tauri komutları (`SECRET_SERVICE = "efatura-edit"`). Linux'ta C bağımlılığı olmadan derlensin diye saf-Rust secret-service + RustCrypto backend'i.
- `settings.svelte.ts`: `persist()` apiKey alanını strip eder; `setApiKey()` / `loadApiKeys()` eklendi; `resetSettings()` zincirdeki anahtarları da siler.

### Eklenen (Dokümantasyon)
- `.github/copilot-instructions.md`: her yeni versiyonda `v*` etiketi ile GitHub Release yayınlama akışı direktife eklendi; program-içi versiyon (`manifest.json` / DataExport `Program.cs`) 5. senkron nokta olarak belgelendi.

## [2.14.0] — 2026-07-11 — AI Asistan Entegrasyonu + CSS Snippet'leri + Ek Dosya/Görsel — app/src/lib/AIAssistant.svelte, app/src-tauri/src/ai.rs

### Eklenen — AI Asistan (BYOK)
- **Sohbet paneli** (sol sütun, sürekli açık) — Claude / OpenAI / Gemini / Ollama / NVIDIA; kullanıcının kendi API anahtarı, hiçbir anahtar gömülü değil. Çıktı yalnızca XSLT/XML'e "önerilen değişiklik" olarak sunulur, onaysız uygulanmaz.
- **Hedefli düzenleme (SEARCH/REPLACE):** tüm dosyayı ezmek yerine doğru yere uygular; birebir + boşluk-normalize + girinti-toleranslı eşleştirme. Onay modalında kırmızı/yeşil diff + popup içi canlı sonuç önizlemesi + geçersiz-sonuç uyarısı.
- **Ajan modu:** düzenleme → dönüşüm doğrulama → kendi kendine düzeltme (max 3 tur).
- **Oturumlar** dosya çiftine göre kalıcı, dropdown ile seçim, dosya-uyuşmazlık uyarısı, otomatik scroll.
- **Ek dosya/görsel:** 📎 seçici + clipboard'dan görsel yapıştırma; görsel/PDF multimodal (vision/doküman), metin dosyası bağlam. Rust `ai.rs` 3 sağlayıcı formatında multimodal içerik.
- **Token optimizasyonu:** gömülü base64 görseller bağlamdan kırpılır (~%87), dosya yalnızca son mesaja bir kez eklenir, geçmiş son 12 mesajla sınırlı.
- **Ayarlar:** sağlayıcıdan model listesi getir + önbellek, ölü Gemini modelini otomatik geçerli alias'a taşı.

### Eklenen — Diğer
- **CSS Stilleri snippet kategorisi** (Metin / Kutu & Kenarlık / Yerleşim / Tablo / Sayfa & Baskı).
- **`.github/workflows/release.yml`** — `v*` etiketinde macOS (universal) + Windows + Linux derleyip GitHub Release yayınlayan tauri-action CI.

### Değişen
- Splitter boyut sınırları kaldırıldı (bölmeler serbestçe boyutlandırılır).

## [2.13.0] — 2026-07-10 — Açık Kaynak Geçişi: Eski WinForms Uygulaması Kaldırıldı — eFaturaEdit/, LICENSE, .github/copilot-instructions.md

### Kaldırılan
- **`eFaturaEdit/` klasörünün tamamı** — .NET Framework 4.7.2 + DevExpress v14.2 WinForms uygulaması, yerini tamamen `app/` altındaki Tauri uygulamasına bıraktı.
- **Lisans/aktivasyon sistemi:** `QLicense`, `ActivationControls4Win`, `ActivationControls4Wpf`, `Demo/DemoLicense`, `Demo/DemoActivationTool` projeleri kaldırıldı (RSA lisans doğrulama, donanım parmak izi, aktivasyon UI dahil).
- **DevExpress bağımlılığı** — ticari/lisanslı UI framework, proje genelinden tamamen çıkarıldı.
- **CefSharp + WebView2 dual render engine**, **Saxon-HE 10.9.0** (IKVM üzerinden) — WinForms'a özgüydü.
- Eski installer script'leri: `installforce_setup.ifp`, `setup.iss` (Inno Setup, `tr-TR` kültürü).
- `eFaturaEditSolution.sln` — yalnızca `src/eFaturaEdit.Core` kaldı; `e-FaturaEdit`, `QLicense`, `ActivationControls4Win`, `eFaturaLicense`, `ActivationTool` proje girdileri temizlendi.

### Eklenen
- **`LICENSE`** — MIT lisansı, proje ilk kez açık kaynak olarak yayımlanıyor.
- **`README.md`** — daha önce boş olan dosya, gerçek proje açıklaması/kurulum/kullanım ile dolduruldu.
- **`.github/copilot-instructions.md`** — üç parçalı (WinForms+Core+Tauri) yapıdan iki parçalı (Core+Tauri) yapıya güncellendi; public repo güvenlik notu eklendi.
- **`INSTALL.md`** — macOS'ten Windows (`cargo-xwin` + NSIS) ve Linux (Docker + WebKitGTK) cross-compile talimatları eklendi.

### Doğrulama
- `src/eFaturaEdit.Core` ve `src/eFaturaEdit.DataExport` kaldırma sonrası bağımsız olarak hatasız derleniyor (0 hata).
- Repo görünürlüğü **public** olarak değiştirildi (önceden private).

## [2.12.0] — 2026-07-10 — Faz 3: Tauri Masaüstü Uygulaması (Cross-Platform, macOS/Linux/Windows) — app/, src/eFaturaEdit.DataExport/

### Eklenen — Tauri + SvelteKit Masaüstü Uygulaması
- **`app/` — Tauri v2 + Svelte 5 + TypeScript + SvelteKit adapter-static:** macOS/Linux/Windows üzerinde çalışan yeni masaüstü UI, DevExpress/WinForms bağımlılığı yok. — `app/`
- **[app/src-tauri/](app/src-tauri/):** Rust backend — `tauri v2`, `tauri-plugin-opener`, `tauri-plugin-dialog`, `tauri-plugin-fs`, `serde`/`serde_json`. Bundle identifier: `com.zaferbilgisayar.efaturaedit`. Pencere 1400×900 (min 1000×700), **ilk açılışta maximized**.
- **`open_devtools` Rust komutu:** Sağ tık menüsünden WebView geliştirici araçlarını açar (yalnızca debug build).

### Eklenen — Editör (CodeMirror 6)
- **[app/src/lib/CodeEditor.svelte](app/src/lib/CodeEditor.svelte):** CodeMirror 6 tabanlı XSLT/XML editörü — satır numarası, kod katlama, bracket matching, syntax highlight, undo/redo, arama paneli (**Türkçeleştirilmiş**: Ara, Değiştir, Sonraki, Önceki, BÜYÜK/küçük, kelime, regex).
- **Özel açık tema renklendirmesi:** Tag adı, öznitelik, değer, yorum, namespace prefix için `HighlightStyle` — `light` temada aktif; diğer 10 tema (`thememirror`) kendi paletini kullanır.
- **Autocomplete:** Core kataloğundan 16 XSLT etiketi + 77 XPath önerisi + 149 snippet = 242 öneri. `<` sonrası veya `Ctrl+Space` ile tetiklenir.
- **11 tema:** Açık (varsayılan), One Dark, Dracula, Cobalt, Espresso, Solarized Light, Ayu Light, Noctis Lilac, Rosé Pine Dawn, Clouds, Smoothy — Ayarlar sayfasından seçilir.
- **`insertAtCursor`, `insertAtCoords`, `setValue`, `goToLine`, `undo`, `redo`** — parent component'e expose edilen editör API'si.

### Eklenen — Dosya İşlemleri
- **[app/src/lib/fileio.ts](app/src/lib/fileio.ts):** Tauri dialog + fs API sarmalayıcısı — Aç/Kaydet/Farklı Kaydet, native macOS dialog.
- **`Cmd/Ctrl+S`:** XSLT ve XML dosyalarını **birlikte** kaydeder; sadece değişen (dirty) dosyalar yazılır. Kaydetmeden önce syntax kontrolü yapılır — **hata varsa dosya kaydedilmez ve imleç otomatik olarak hatalı satıra konumlanır** (`XsltError.line/column` → `CodeEditor.goToLine()`).
- **Kaydet butonu görsel indicator:** Turuncu pulse animasyonu + hangi dosyanın (XSLT/XML) değiştiğini gösteren etiket.
- **[app/src/lib/recent-files.svelte.ts](app/src/lib/recent-files.svelte.ts):** Son 10 açılan dosya, localStorage persist, toolbar'da **🕒 Son ▼** dropdown.
- **Otomatik dönüştür:** Dosya açıldığında/kaydedildiğinde (ayarlanabilir), ve yazarken debounce ile (varsayılan 700ms) otomatik XSLT dönüşümü.
- **Auto-save:** Ayarlarda açılabilir — dirty olduktan belirli süre sonra (varsayılan 3sn) sessizce kaydeder (yalnızca zaten bir yolu olan dosyalar için).
- **Çıkışta kaydetme kontrolü:** Tauri `onCloseRequested` olayı dinlenir; kaydedilmemiş değişiklik varsa özel bir onay penceresi (**İptal / Kaydetmeden Çık / Kaydet ve Çık**) gösterilir. "Kaydet ve Çık" syntax hatası durumunda çıkışı iptal eder.

### Eklenen — Snippet Sistemi
- **149 snippet**, kategori sekmeleri + arama kutusu ile filtrelenebilir sol panel.
- **Tıkla-ekle:** Snippet imleç konumuna eklenir.
- **Sürükle-bırak (custom mouse-tracking):** HTML5 native drag-drop API'si WKWebView'de güvenilmez olduğu için [app/src/lib/drag.svelte.ts](app/src/lib/drag.svelte.ts) ile saf mouse event tabanlı sürükleme implementasyonu — 5px eşik, ESC ile iptal, hedef editör üzerinde görsel gösterge (👻 ghost, hedefte mavi 📌).

### Eklenen — Önizleme
- **[app/src/lib/xslt.ts](app/src/lib/xslt.ts):** Native `XSLTProcessor` (XSLT 1.0) sarmalayıcısı. `transformToDocument()` + `outerHTML` serileştirme ile **`<!DOCTYPE html>` ve `<meta charset="utf-8">` garantisi** — bu olmadan tarayıcı Quirks Mode'a düşüp tablo kenarlık/genişlik hesaplamalarını farklı render ediyordu (Standards Mode fix).
- **Responsive boyut seçici:** 📱320 / 📱768 / 🖥️1200 / ⬜Full butonları.
- **Zoom:** −/%/+ butonları + `Cmd/Ctrl +/-/0` kısayolları.
- **Yazdırma/PDF:** Önizleme HTML'i `$APPLOCALDATA/preview/` altına yazılıp sistem tarayıcısında (Safari) açılır; kullanıcı orada `Cmd+P` ile native yazdırma/PDF kaydetme yapar. *(Not: Tauri WKWebView'de uygulama içi `window.print()` native paneli güvenilir açmadığı için — sadece `@media print` stillerini anlık tetikleyip geri dönüyor — bu yol tamamen bırakıldı.)*
- **Sağ tık menüsü:** Yazdır/PDF, HTML kopyala, yeniden dönüştür, Geliştirici Araçları.

### Eklenen — UI/UX
- **[app/src/lib/Splitter.svelte](app/src/lib/Splitter.svelte):** Mouse ile sürüklenebilir panel ayırıcılar (snippet↔editör, editör↔önizleme, XSLT↔XML).
- **Toolbar gruplandırması:** Düzenle / Dosya / İşlemler / Yardım ve Ayarlar — görsel kutular halinde.
- **Hoşgeldin ekranı:** Editörler boşken 3 hızlı başlangıç butonu (Örnek Yükle, XSLT Aç, XML Aç).
- **[app/src/lib/settings.svelte.ts](app/src/lib/settings.svelte.ts) + [/settings](app/src/routes/settings/+page.svelte):** Editör (font, sekme, wrap, satır no), Görünüm (tema), Davranış (autosave, debounce, autocomplete) ayarları — localStorage persist.
- **[app/src/lib/editor-state.svelte.ts](app/src/lib/editor-state.svelte.ts):** Modül-scope global state — Ayarlar sayfasına gidip geri dönüldüğünde editör içeriğinin kaybolmaması için.

### Eklenen — Yardım Sistemi
- **[app/src/lib/HelpModal.svelte](app/src/lib/HelpModal.svelte) + [help-content.ts](app/src/lib/help-content.ts):** Sidebar navigasyonlu, aranabilir, 11 bölümlük tam dokümantasyon (Genel Bakış, Dosya İşlemleri, Örnekler, Snippet'ler, Editör, Önizleme, Klavye Kısayolları, Ayarlar, Panel Boyutları, Sorun Giderme, Hakkında).
- **F1 kısayolu** ve toolbar **❓ Yardım** butonu ile her yerden erişilebilir.
- Tüm buton/kontrollere açıklayıcı `title` (tooltip) eklendi.

### Eklenen — eFaturaEdit.DataExport
- **[src/eFaturaEdit.DataExport/](src/eFaturaEdit.DataExport/eFaturaEdit.DataExport.csproj):** net10.0 konsol tool. Core POCO verilerini camelCase TypeScript-friendly JSON'a dönüştürür. Türkçe karakterler escape edilmez (`JavaScriptEncoder.Create(UnicodeRanges.All)`).
- **Üretilen dosyalar:** `snippets.json` (149 snippet), `samples.json` (6 kategori × 17 örnek — zarf örnekleri kaldırıldı), `completion.json` (16 XSLT tag + 77 XPath), `manifest.json` (versiyon + timestamp).
- **`npm run data:sync`** ile Core → JSON senkronizasyonu tek komutla yenilenir.

### Mimari Kararlar
- **Framework:** Electron değil, **Tauri v2** seçildi. Bundle boyutu ~15 MB (Electron ~150 MB), native WebView (macOS WKWebView + Windows WebView2).
- **UI:** SvelteKit + adapter-static (client-side SPA) — React/Vue yerine Svelte tercih edildi (küçük bundle, az boilerplate).
- **XSLT motoru:** Native `XSLTProcessor` (XSLT 1.0). Saxon-JS 2.x yalnızca Node.js için dağıtılıyor (`saxon-js` npm), tarayıcı runtime'ı (`SaxonJS2.rt.js`) Saxonica'nın kapalı download sayfasından manuel indirilmeli — bu nedenle ertelendi.
- **DevExpress silindi:** Yeni UI'da DevExpress bağımlılığı yok.
- **Namespace stratejisi (Core → Tauri):** Core POCO'lar → JSON export → TypeScript tipleri. Tek gerçek kaynak = C# Core.

### Bilinen Sınırlamalar
- Kullanıcı tanımlı örnek klasörü (`user-samples.ts` hazır) için UI henüz yok.
- Kullanıcı tanımlı/dinamik snippet CRUD (UBL-TR güncellemeleri için) henüz yok.
- XSLT 2.0/3.0 desteği yok (native `XSLTProcessor` yalnızca 1.0).
- Çoklu dosya sekmesi, panel tam ekran (F11), native menü çubuğu yok.
- Uygulama simgesi hâlâ Tauri varsayılanı.
- Lisans/donanım ID sistemi (QLicense port'u) henüz yok.

### Değişen — Versiyon
- **2.11.0 → 2.12.0** — MINOR bump (yeni Tauri app + kapsamlı özellik seti, WinForms projesi davranışsal olarak değişmedi).

### Bağımlılıklar (yeni)
- **Rust 1.97** + Cargo (global), **Node.js 26** + npm 11
- **Tauri v2** + `tauri-plugin-opener`/`dialog`/`fs` v2 (Rust)
- **@sveltejs/kit** v2.9 + `adapter-static` v3, **Svelte** v5, **TypeScript** ~5.6, **Vite** v6
- **@tauri-apps/api** v2 + **@tauri-apps/cli** v2, **@tauri-apps/plugin-dialog/fs** v2
- **CodeMirror 6:** `@codemirror/{view,state,commands,language,lang-xml,lang-html,autocomplete,search,theme-one-dark}`, `thememirror`, `@lezer/highlight`

## [2.11.0] — 2026-07-10 — Faz 2: eFaturaEdit.Core Ayrımı (Cross-Platform Çekirdek) — src/eFaturaEdit.Core/

### Eklenen
- **`eFaturaEdit.Core` projesi:** UI-bağımsız çekirdek. SDK-style csproj, multi-target `netstandard2.0;net10.0`. Sıfır dış NuGet bağımlılığı. — `src/eFaturaEdit.Core/eFaturaEdit.Core.csproj`
- **`Snippets/SnippetInfo.cs`:** POCO snippet meta modeli.
- **`Snippets/XsltSnippets.cs`:** 140 UBL-TR/HTML/XSLT snippet sözlüğü (WinForms'tan taşındı, tam veri).
- **`Samples/SampleGroup.cs`, `Samples/SampleEntry.cs`:** UBL-TR örnek XML katalog POCO'ları.
- **`Samples/UblTrSamples.cs`:** 7 kategori × 30 GİB resmi senaryo XML tanımları (WinForms'tan taşındı, sadece veri kısmı).
- **`Completion/CompletionItem.cs`:** UI-bağımsız autocomplete girdi modeli.
- **`Completion/XsltCompletionCatalog.cs`:** 16 XSLT etiket + 74 UBL-TR XPath önerisi + XPath fonksiyonları + `ExtractPreSelection`/`IsInsideXPathAttribute` yardımcıları. WinForms `XsltCompletionProvider` bu kataloğu tüketir.
- **`Platform/IHardwareIdProvider.cs`:** Donanım parmak izi soyutlaması. Faz 3'te macOS `IOPlatformUUID` implementasyonu eklenecek.
- **`UblTrSamplesPaths.cs` (WinForms):** `Application.ExecutablePath`'e bağımlı yol çözümleyicileri Core'dan ayrıldı, platforma özgü olarak WinForms tarafında tutuldu.

### Değişen
- **WinForms projesi:** `UblTrSamples.cs` ve `XsltSnippets.cs` dosyaları kaldırıldı (Core'a taşındı). Core projesine `ProjectReference` eklendi. — `eFaturaEdit/e-FaturaEdit.csproj`
- **`XsltCompletionProvider.cs` sadeleştirildi:** 337 satır → 143 satır. Tüm öneri verisi ve XPath algılama Core'dan çekiliyor; bu dosya sadece `ICompletionDataProvider` adaptörü ve `SnippetCompletionData` sarmalayıcısı içeriyor.
- **`Form1.Snippets.cs`:** `UblTrSamples.GetFullPath` / `SamplesDirectoryExists` çağrıları `UblTrSamplesPaths`'a yönlendirildi.
- **Namespace stratejisi:** Core tipleri de `eFaturaEdit` root namespace altında — WinForms `using` yönergeleri değiştirilmedi.

### Mimari
- **Cross-platform hazırlık:** Core, gelecek Avalonia (Faz 3) UI'ının veri katmanı olacak. Windows / macOS / Linux üzerinde çalışabilir.
- **Ertelendi (Faz 3):** Saxon-HE 12.x geçişi ertelendi. NuGet'te Saxon 12 için library paketi bulunmuyor (sadece CLI tool paketleri). Faz 3'te `SaxonHE10Net31Api` (Saxon 10.9 API'nin .NET 8/10 portu — aynı `Saxon.Api` namespace, cross-platform) kullanılacak. `XsltTransformHelper.cs` şimdilik WinForms'ta kaldı.

### Solution
- Core projesi `eFaturaEditSolution.sln`'e eklendi. GUID: `{C00E2D88-461F-46C8-BBFD-FED85A8F8C78}`.

## [2.10.0] — 2025-07-14 — Partial Class Refactoring + XsltTransformHelper — Form1*.cs, XsltTransformHelper.cs

### Eklenen
- **Form1 partial class yapısına bölündü:** `Form1.Snippets.cs`, `Form1.Browser.cs`, `Form1.AutoComplete.cs`, `Form1.Wysiwyg.cs` — 1560 satırlık tek dosya ~400 satıra indi.
- **`XsltTransformHelper` sınıfı oluşturuldu:** Saxon XSLT dönüşüm metotları (`TransformXslFile`, `TransformXml`) tek merkezde. — `XsltTransformHelper.cs`

### Değişen
- **`ResultHtmlPath` geçici dosyaya taşındı:** `%TEMP%` altında GUID tabanlı benzersiz dosya adı, form kapanışında temizlenir. — `Form1.cs`
- **Kullanılmayan 7 using yönergesi kaldırıldı.** — `Form1.cs`

### Kaldırılan
- **Geçici dosyalar temizlendi:** `_temp_read_irsaliye3.py`, `nuget.exe`. `.gitignore` güncellendi.

## [2.9.3] — 2025-07-13 — Saxon XSLT 3.0 Geçişi + WebView2Loader Düzeltmesi — Form1.cs, e-FaturaEdit.csproj

### Değişen
- **XSLT dönüşüm motoru Saxon-HE'ye geçirildi:** `XslCompiledTransform` (XSLT 1.0) yerine `Saxon.Api.Processor` (XSLT 1.0/2.0/3.0) kullanılıyor. Tüm 4 dönüşüm noktası güncellendi. — `Form1.cs`
- **`TransformXslFile` yardımcı metodu eklendi:** Dosya tabanlı Saxon dönüşümü tek merkezde. — `Form1.cs`
- **`myXslTrans` alanı ve `System.Xml.Xsl` using kaldırıldı.** — `Form1.cs`

### Düzeltilen
- **WebView2Loader.dll bulunamadı hatası giderildi:** `Microsoft.Web.WebView2.targets` import'u `.csproj`'a eklendi + PostBuildEvent ile DLL çıktı köküne kopyalanıyor. — `e-FaturaEdit.csproj`

## [2.9.2] — 2025-07-13 — Yol Sabitleri + Metod Sadeleştirme — Form1.cs

### Değişen
- **Statik yol sabitleri eklendi:** `AppDir`, `XmlDataDir`, `DefaultXmlPath`, `ResultHtmlPath` — 6× tekrarlanan `Path.Combine` çağrısı kaldırıldı. — `Form1.cs`
- **`iOpen_ItemClick` sadeleştirildi:** Early return pattern, gereksiz `null` atama kaldırıldı, yol sabitleri kullanıldı, tek try-catch bloğu. — `Form1.cs`
- **`iSave_ItemClick` birleştirildi:** 3 ayrı try-catch → tek blok, `ResultHtmlPath` sabiti kullanıldı. — `Form1.cs`

## [2.9.1] — 2025-07-12 — CefSharp Başlatma Hatası Düzeltmesi — Form1.cs

### Düzeltilen
- **CefSharp "Cef.IsInitialized was false!" hatası giderildi:** `Cef.Initialize()` çağrısı `CachePath` olmadan CefSharp v145'te sessizce başarısız oluyordu. — `Form1.cs`
- **`InitializeCef()` metodu eklendi:** `CachePath` (LocalAppData/eFaturaEdit/CefCache), `LogFile` (cef_debug.log), `LogSeverity.Warning` ayarları ve dönüş değeri kontrolü ile güvenli başlatma. — `Form1.cs`
- **`Cef.IsInitialized` koruması eklendi:** XSLT açma, XML dosya açma ve örnek XML yükleme akışlarında `ChromiumWebBrowser` oluşturulmadan önce kontrol eklendi. — `Form1.cs`

## [2.9.0] — 2025-06-24 — Ribbon Sekme Organizasyonu — Form1.cs

### Değişen
- **Ribbon sekmelere ayrıldı:** Tek sekmede (Araçlar) yığılmış 13 grup, 4 mantıksal sekmeye dağıtıldı. — `Form1.cs`
- **Dosya** (Araçlar): Dosya işlemleri (Aç, Kaydet, Farklı Kaydet, DevTools, Yenile, PDF), Örnek Faturalar, Tema, Çıkış.
- **Biçimlendirme** (yeni): Yazı Biçimi, Hizalama, Stil, Ekle — WYSIWYG araçları ayrı sekmeye taşındı.
- **Öğeler** (yeni): HTML Öğeleri, XSLT Komutları, Sayfa Düzeni snippet'leri.
- **UBL-TR** (yeni): e-Fatura, e-Arşiv, e-İrsaliye snippet'leri.
- **Yeni yönlendirme metodu:** `GetRibbonPageForCategory()` — snippet kategorisini doğru sekmeye yönlendirir.

## [2.8.0] — 2025-06-23 — WYSIWYG Biçimlendirme Toolbar — WysiwygHelper.cs, Form1.cs

### Eklenen
- **WYSIWYG Biçimlendirme Toolbar:** Ribbon'a 4 yeni grup halinde görsel biçimlendirme araç çubuğu eklendi. — `WysiwygHelper.cs`, `Form1.cs`
- **Yazı Biçimi (6):** Kalın, İtalik, Altı Çizili, Üstü Çizili, Üst Simge, Alt Simge — mevcut Bold/Italic/Underline butonları aktifleştirildi + 3 yeni buton.
- **Hizalama (3):** Sola, Ortaya, Sağa hizalama — mevcut Align butonları aktifleştirildi, `<div style="text-align:...">` ile sarar.
- **Stil (3):** Yazı Rengi (ColorDialog), Arka Plan Rengi (ColorDialog), Yazı Boyutu (8–48pt alt menü).
- **Ekleme (4):** Yatay Çizgi (`<hr />`), Sıralı Liste (`<ol>`), Madde İşareti (`<ul>`), Kenarlık.
- **Akıllı Sarmalama:** Seçili metin varsa etiketle sarar; yoksa boş etiket çifti ekleyip imleci arasına konumlar.
- **FontAwesome İkonlar:** Tüm WYSIWYG butonlarında FontAwesome.Sharp vektörel ikonlar.
- **Yeni dosya:** `WysiwygHelper.cs` — `WrapSelection`, `WrapWithStyle`, `WrapWithAlignment`, `InsertAtCursor` yardımcı metotları.

## [2.7.0] — 2025-06-23 — XSLT Editör Autocomplete — XsltCompletionProvider.cs, Form1.cs

### Eklenen
- **Otomatik tamamlama (Autocomplete):** XSLT editörüne (textEditorControlEx1) ICSharpCode.TextEditorEx CodeCompletionWindow tabanlı akıllı öneri sistemi eklendi. — `XsltCompletionProvider.cs`, `Form1.cs`
- **Tetikleme:** `<` karakteri yazıldığında etiket önerileri, `Ctrl+Space` ile tam liste (XPath + XSLT + Snippet).
- **XSLT Etiket Önerileri (16):** xsl:value-of, xsl:for-each, xsl:if, xsl:choose, xsl:when, xsl:otherwise, xsl:text, xsl:variable, xsl:template, xsl:apply-templates, xsl:attribute, xsl:element, xsl:call-template, xsl:sort, xsl:copy-of, xsl:number.
- **UBL-TR XPath Önerileri (~75):** e-Fatura (Invoice) ve e-İrsaliye (DespatchAdvice) belge yapısı — Başlık, Satıcı/Alıcı, Kalem, Toplamlar, Ödeme, Referanslar, Sevkiyat alanları + XPath fonksiyonları.
- **Snippet Önerileri (140):** Mevcut tüm snippet tanımları tamamlama listesinde gösterilir; seçildiğinde tam XSLT kodu eklenir.
- **Bağlam Duyarlı Filtreleme:** `select=""` veya `test=""` öznitelikleri içinde XPath önerileri öncelikli gösterilir.
- **Yeni dosya:** `XsltCompletionProvider.cs` — `ICompletionDataProvider` ve `SnippetCompletionData` implementasyonları.

## [2.6.0] — 2025-06-23 — UBL-TR e-İrsaliye Snippet Seti — XsltSnippets.cs

### Eklenen
- **33 yeni e-İrsaliye snippet:** UBL-TR İrsaliye V1.2 kılavuzuna göre DespatchAdvice belge tipi için tamamı yeni kategori oluşturuldu. — `XsltSnippets.cs`
- **Başlık (8):** İrsaliye Başlık Tablosu, İrsaliye No, ETTN, Tarih, Saat, Tip Kodu, Not, Kalem Sayısı.
- **Taraflar (9):** Sevk Eden Taraf (tablo), Teslim Alan Taraf (tablo), Sevk Eden/Teslim Alan VKN, Unvan, Vergi Dairesi (standalone), Sevk Adresi (Depo/Şube).
- **Sevkiyat (8):** Sevkiyat Bilgileri (tablo), Şoför Bilgileri, Araç Plakası, Dorse Plakası, Taşıyıcı Firma (tablo), Fiili Sevk Tarihi, Fiili Sevk Saati, Mal Bedeli.
- **Kalemler (6):** İrsaliye Kalem Tablosu, Kalem Sıra No, Teslim Miktarı, Mal/Hizmet Adı, Birim Kodu, Satıcı Ürün Kodu.
- **Referanslar (2):** Sipariş Referansı, İlave Doküman.
- **Kök Eleman:** `/n1:DespatchAdvice` — e-Fatura (`/n1:Invoice`) ile farklı namespace.

## [2.5.0] — 2025-06-23 — UBL-TR Standalone Snippet Tamamlama — XsltSnippets.cs

### Eklenen
- **29 yeni standalone snippet:** Tablo/döngü içinde gömülü olan tüm UBL-TR alanları bağımsız tekil snippet olarak eklendi. — `XsltSnippets.cs`
- **Başlık (2):** Fatura No (cbc:ID), Fatura Tarihi (cbc:IssueDate).
- **Taraflar (10):** Satıcı VKN/TCKN, Satıcı Unvan, Satıcı Vergi Dairesi, Satıcı Telefon, Satıcı E-posta, Alıcı VKN/TCKN, Alıcı Unvan, Alıcı Vergi Dairesi, Alıcı Telefon, Alıcı E-posta.
- **Kalemler (7):** Kalem Sıra No, Mal/Hizmet Adı, Miktar, Birim Fiyat, Kalem Tutarı, Kalem KDV Tutarı, Kalem İskonto Tutarı.
- **Toplamlar (5):** Mal/Hizmet Toplamı, Vergiler Hariç Toplam, Vergiler Dahil Toplam, İndirim Toplamı, Ödenecek Tutar.
- **Vergi (1):** Toplam Vergi Tutarı.
- **Ödeme (2):** Vade Tarihi, IBAN/Hesap No.
- **Döviz Kurları (2):** Ödeme Döviz Kuru (PaymentExchangeRate), Vergi Döviz Kuru (TaxExchangeRate).

## [2.4.1] — 2025-06-22 — Kalem KDV/İskonto Oranı Standalone Snippet — XsltSnippets.cs

### Eklenen
- **2 yeni standalone snippet:** Kalem KDV Oranı (%) ve Kalem İskonto Oranı (%) bağımsız snippet olarak eklendi. — `XsltSnippets.cs`
- **UBL_LINE_KDVPERCENT:** `cac:TaxTotal/cac:TaxSubtotal/cbc:Percent` — tek değer olarak KDV oranı.
- **UBL_LINE_ISKONTOPERCENT:** `cac:AllowanceCharge/cbc:MultiplierFactorNumeric` — iskonto oranı (% formatında).

## [2.4.0] — 2025-06-22 — UBL-TR Eksik Snippet Tamamlama — XsltSnippets.cs

### Eklenen
- **22 yeni UBL-TR snippet:** UBL-TR 1.2.1 kılavuzlarına göre eksik alanlar tamamlandı. — `XsltSnippets.cs`
- **Kalemler (14):** Satır Açıklaması (Note), Ürün Açıklaması (Description), Birim Kodu (unitCode), Satıcı/Alıcı Ürün Kodu, Marka/Model, Üretici Kodu, Ek Ürün Kimlikleri, Emtia Sınıflandırması, KDV Matrahı, Sipariş/İrsaliye Kalem Ref., Detaylı Kalem Tablosu. — `XsltSnippets.cs`
- **Başlık (3):** Vergi Para Birimi (TaxCurrencyCode), Fatura Dönemi (InvoicePeriod), İlave Fatura Tipi (AccountingCost). — `XsltSnippets.cs`
- **Referanslar (3):** Kontrat Referansı, Alındı Referansı, Başlangıç Dokümanı. — `XsltSnippets.cs`
- **Toplamlar (1):** Yuvarlama Tutarı (PayableRoundingAmount). — `XsltSnippets.cs`
- **Ödeme (2):** Ödeme Koşulları Detay (ceza oranı/tutarı), Banka Hesap Bilgileri (IBAN/para birimi). — `XsltSnippets.cs`
- **Taraflar (1):** Mal Sağlayan Taraf (SellerSupplierParty). — `XsltSnippets.cs`

## [2.3.0] — 2025-06-21 — FontAwesome.Sharp Vektör İkon Entegrasyonu — Form1.cs, e-FaturaEdit.csproj

### Eklenen
- **FontAwesome.Sharp 5.15.4:** Tüm snippet ve kategori butonlarına profesyonel vektör ikonlar. 54 snippet + 5 kategori için renkli FontAwesome ikonları. — `Form1.cs`
- **SnippetIconMap:** 54 snippet → IconChar eşlemesi (FileInvoice, Building, Coins, Calculator vb.). — `Form1.cs`
- **CategoryIconMap:** 5 kategori → IconChar eşlemesi (PuzzlePiece, Code, SolarPanel, FileInvoiceDollar, Archive). — `Form1.cs`
- **Kategori renk sistemi:** Her kategori için ayrı renk (Mavi/Mor/Yeşil/Turuncu/Teal). — `Form1.cs`

### Değişen
- **İkon sistemi yenilendi:** Unicode/emoji simgeler yerine FontAwesome.Sharp vektör bitmap'ler kullanılıyor. — `Form1.cs`
- **Ribbon & Context menü:** Toolbar butonları (Glyph) ve sağ tık menü öğeleri (Image) FontAwesome ikonlarıyla güncellendi. — `Form1.cs`

### Bağımlılıklar
- **FontAwesome.Sharp 5.15.4** NuGet paketi eklendi (.NET Framework 4.7.2 uyumlu). — `packages.config`, `e-FaturaEdit.csproj`

## [2.2.0] — 2025-06-21 — UBL-TR Tam Snippet Seti + Sağ Tık Menü — XsltSnippets.cs, Form1.cs

### Eklenen
- **54 snippet:** 16 genel (HTML/XSLT/Sayfa Düzeni) + 30 UBL-TR e-Fatura + 8 UBL-TR e-Arşiv snippet. Her biri açıklama ve ikonlu. — `XsltSnippets.cs`
- **UBL-TR e-Fatura alt kategorileri:** Başlık (9), Taraflar (12), Kalemler (3), Vergi (4), Toplamlar (4), Ödeme (2), Referanslar (4). — `XsltSnippets.cs`
- **UBL-TR e-Arşiv alt kategorileri:** Teslimat (4), E-Arşiv Özel (4). — `XsltSnippets.cs`
- **Sağ tık context menü:** Editör üzerinde sağ tık ile Category → SubCategory → Snippet ağaç yapısında hızlı erişim menüsü. — `Form1.cs`
- **Ribbon alt menü grupları:** UBL-TR kategorilerinde SubCategory'ye göre BarSubItem alt menüleri. — `Form1.cs`

### Değişen
- **Tooltip açıklamaları:** Her snippet için detaylı Türkçe açıklama ve XPath bilgisi, kısaltılmadan gösterilir (MaxWidth=600). — `Form1.cs`
- **İkon desteği:** Tüm snippet butonlarına Unicode ikonlar eklendi. — `XsltSnippets.cs`, `Form1.cs`
- **SnippetInfo sınıfı genişletildi:** SubCategory ve Description özellikleri eklendi. — `XsltSnippets.cs`

## [2.1.0] — 2025-06-20 — Toolbar Kategori Gruplama — XsltSnippets.cs, Form1.cs

### Değişen
- **Snippet Toolbar kategorilere ayrıldı:** 23 snippet tek "Öğe Ekle" grubu yerine 4 ayrı Ribbon grubuna bölündü: HTML Öğeleri (8), XSLT Komutları (3), Sayfa Düzeni (5), UBL-TR Fatura (8). — `XsltSnippets.cs`, `Form1.cs`
- **SnippetInfo.Category özelliği:** Her snippet'e kategori bilgisi eklendi; `InitSnippetToolbar()` kategoriye göre `RibbonPageGroup` oluşturur. — `XsltSnippets.cs`, `Form1.cs`

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
