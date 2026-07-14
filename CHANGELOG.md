# Değişiklik Günlüğü (Changelog)

Tüm önemli değişiklikler bu dosyada belgelenir.
Format [Semantic Versioning](https://semver.org/lang/tr/) kurallarına uygundur.

## [2.33.1] — 2026-07-14 — Boş AI Yanıtı: Kör Nokta Kapatıldı

### Düzeltilen
- **"Sağlayıcı boş yanıt döndürdü" hatası teşhis edilemiyordu.** `deepseek-v4-flash` üç kez
  `finish_reason: stop` (yani "bitirdim") ile ama **boş `content`** ile döndü — model 8-9 saniye
  çalışıp bir şey üretti, ama bizim okuduğumuz alanda değildi. Ham gövde loglanmadığı için
  sağlayıcının **ne döndürdüğü görülemiyordu**; teşhis kördü.
- Artık `content` boş çıktığında:
  1. **Ham yanıt gövdesi günlüğe yazılır** (`[ai] BOŞ İÇERİK — sağlayıcı · model · bitiş=… · ham yanıt: …`).
     Bu yanıt gövdesidir, istek değil — **API anahtarı içermez.**
  2. Yanıtta `content` dışında **hangi alanların dolu geldiği okunur** ve kullanıcıya söylenir
     (ör. *"yanıtta şu alan(lar) dolu: reasoning_content (2140 karakter) — model cevabı okuduğumuz
     yere koymamış"*). Alan adı **tahmin edilmiyor**, sağlayıcının yanıtından öğreniliyor.
- Aynı kapı üç sağlayıcı yolunda da (Anthropic, Gemini, OpenAI-uyumlu) geçerli. "Metin bloğu yok" ile
  "metin boş" durumları birleştirildi — ikisi de kullanıcı için aynı sonuç (gösterilecek bir şey yok).

### Notlar
- Bu **sebebi bulmadı, sebebi görünür kıldı.** Boş yanıt tekrarlarsa günlükteki `[ai] BOŞ İÇERİK`
  satırı cevabın hangi alanda geldiğini söyleyecek; doğru alanı ondan sonra **ölçerek** okuyacağız.
  (Tahminle `reasoning_content` okumaya kalkmak, uydurulan alan adının sessizce iş görmemesi
  riskini taşırdı.)
- `ai.rs` için ilk birim testleri eklendi (4 test): boş içerikte diğer alanların bildirilmesi, tamamen
  boş yanıt, bozuk/HTML gövdenin teşhis yolunu çökertmemesi, boş alanların ve `role`'ün listelenmemesi.

## [2.33.0] — 2026-07-14 — Önizleme Sağ Tık Menüsü: Arama, Kopyalama, Zoom, Görsel

### Eklenen
- **Önizlemede arama (⌘/Ctrl+F).** Eşleşmeler önizlemede sarı, geçerli eşleşme turuncu vurgulanır;
  `3/17` sayacı, ↑/↓ ile gezinme, Enter/Shift+Enter, Esc ile kapatma. Kısayol **iframe'in içinden**
  köprüyle geliyor — iframe'e odaklıyken basılan tuşlar ana pencereye ulaşmaz (ders 8).
  Vurgular yalnızca önizlemede yaşar: arama kapanınca DOM **birebir eski hâline** döner, XSLT'ye
  veya "HTML'i Kopyala" çıktısına **sızmaz**.
- **Seçimi Kopyala / Metni Kopyala (düz metin) / Tümünü Seç.** Seçim iframe'in içinde yaşadığı için
  postMessage köprüsünden taşınır; seçim yoksa menü maddesi soluk görünür.
- **Yakınlaştır / Uzaklaştır / Sıfırla** — mevcut `previewZoom` ayarına bağlandı (yüzde de gösterilir).
- **Görseli Kopyala / Görseli Kaydet…** — önizlemede bir görsele (logo, QR, imza) sağ tıklayınca çıkar.
  Kopyalama görüntüyü canvas'ta ham RGBA'ya çözüp panoya yazar → **PNG, JPEG, WebP, SVG** hepsi
  çalışır. Kaydetme **orijinal baytları** yazar (yeniden kodlanmaz, kalite düşmez).

### Düzeltilen
- **Yakınlaştırılmış önizlemede sağ tık menüsü yanlış yere açılıyordu.** Iframe `scale()` ile
  ölçekleniyor; içeriden gelen koordinatlar zoom ile çarpılmadan kullanılıyordu.

### Notlar
- `Image.fromBytes()` **kullanılmadı**: yalnızca PNG/ICO destekliyor ve `image-png` Cargo özelliğini
  istiyor (bu projede açık değil) — JPEG logoda sessizce çuvallardı.
- Yeni native yetenekler için capability izinleri eklendi: `clipboard-manager:allow-write-image`
  (görsel panoya) ve `fs:allow-write-file` (ikili dosya yazma). İzin eklenmeden bu işlevler
  sessizce reddedilirdi.
- Editörlerin sağ tık menüsünde kes/kopyala/yapıştır/ara zaten vardı; eksik olan önizleme menüsüydü.
  **Yapıştır önizlemeye eklenmedi** — salt-okunur bir render olduğu için yapıştırılan içerik ilk
  dönüşümde silinirdi (ölü menü maddesi bırakmamak için bilinçli tercih).

## [2.32.1] — 2026-07-14 — Metin-Only Modele Görsel: Sebebi Söyleyen Hata

### Düzeltilen
- **Görsel kabul etmeyen bir modele ekran görüntüsü gönderilince anlaşılmaz bir hata çıkıyordu:**
  `Failed to deserialize the JSON body into the target type: messages[1]: unknown variant
  'image_url', expected 'text'`. Bu **semptomdu, sebep değildi** — kullanıcının bundan "bu model
  görsel okuyamıyor" sonucunu çıkarması imkânsızdı. (Gerçek vaka: DeepSeek `deepseek-v4-pro`.)
  Artık sebep yazılıyor: *"Seçili model görsel eki kabul etmiyor: {model}. Yalnızca metin
  işleyebiliyor. Görseli kaldırıp sorunuzu yazıyla anlatın ya da Ayarlar → AI'dan görsel
  destekleyen bir model seçin."* Sağlayıcının ham yanıtı da mesajın sonunda korunuyor.
- Aynı durum NVIDIA NIM'in metin modelleri için de geçerliydi (ikisi de OpenAI-uyumlu yoldan geçer).
- Günlüğe `[ai] model görsel kabul etmiyor — {sağlayıcı} · {model}` uyarısı düşüyor.

### Notlar
- Ayrım **model adı tahmin edilerek yapılmıyor** — NIM kataloğunda görsel okuyan modeller de var,
  ad kalıbından bilinemez. Yalnızca *gerçekten görsel gönderdiğimiz* ve sağlayıcının `image_url`
  alanından şikâyet ettiği istekte devreye girer; başka bir 400 gelirse ham gövde aynen gösterilir.
- Sohbet geçmişi **zehirlenmiyordu**: ekler geçmişte saklanmaz (yalnızca hafif bir not), görsel
  sadece son mesaja iliştirilir ve başarısız tur geri alınır.

## [2.32.0] — 2026-07-14 — AI Yetenekleri (Skill) — Sağlayıcı Başına Uzmanlık Paketleri

### Eklenen
- **AI yetenekleri (skill):** Asistana, adlandırılmış uzmanlık paketleri eklenebiliyor. Seçilen
  paketlerin talimatı sistem promptunun sonuna `# EK YETENEKLER` başlığı altında eklenir —
  yukarıdaki **kapsam kilidi ve çıktı biçimi kuralları her zaman üstündür**, yetenekler onların
  yerine geçmez, üzerine derinlik ekler.
- **Altı hazır paket** (Ayarlar → AI → Yetenekler):
  - 🎨 **Modern & sanatsal tasarım** — tipografi ölçeği, renk/kontrast, boşluk ritmi, görsel hiyerarşi.
  - 🎨 **A4 / baskı ustalığı** — `@page`, mm ölçüler, sayfa kırılımı, `<thead>` tekrarı, nakli yekûn
    (devir satırının **bir kez** basılması dahil — v2.31.0'da düzeltilen hatanın kuralı pakete yazıldı).
  - ⚙️ **XSLT 2.0/3.0 ileri** — `for-each-group`, `xsl:function`, sequence tipleri, mode, tunnel params.
  - ⚙️ **XPath ileri** — eksenler, predicate, tip dönüşümleri ve **`xpath-default-namespace` tuzağı**
    (UBL kökü varsayılan namespace'te olduğu için `/Invoice/cbc:ID` sessizce hiçbir şey eşleştirmez).
  - ⚙️ **Modern CSS (baskı-güvenli)** — grid/flex'i baskıyı bozmadan kullanma; `print-color-adjust`.
  - ⚙️ **Önizlemede JavaScript** — süsleme/etkileşim için; yapısal düzen ve hesap için **asla**
    (baskıda JS çalışmaz).
- **Kullanıcı yetenekleri:** Kendi paketini yazabilirsin (kimlik, ad, açıklama, talimat).
  `$APPDATA/user-skills.json` içinde **ayrı** tutulur; hazır katalogla birleştirilip **üzerine yazılmaz**.
- **Seçim sağlayıcı başınadır:** Güçlü bir modele altı paketi birden açarken, token bütçesi dar bir
  yerel modele (Ollama) hiçbirini açmayabilirsin. Ayarlarda **seçili yeteneklerin token maliyeti**
  gösterilir (altısı birden ≈ 3.700 token/istek).
- Yetenekler sistem promptunun içinde kaldığı için **prompt önbelleğine dâhildir**; açıp kapatınca
  önek bir kez yeniden yazılır, sonrasında yine cache'ten okunur.

### Değişen
- **AI istek günlüğüne `sistem N bayt` alanı eklendi** (`ai.rs`). "Yeteneği açtım ama işe yaramıyor"
  şikâyeti ancak yeteneğin prompta **gerçekten girip girmediği** ölçülebilirse teşhis edilebilir —
  bu sayı olmadan kör teşhis olurdu.

## [2.31.0] — 2026-07-14 — Nakli Yekûn Ayrı Şablona Taşındı

### Eklenen
- **Nakli yekûnlü şablon artık ayrı bir örnek:** `default-nakli-yekun.xslt`.
  🎲 Örnek menüsünde 🌟 Varsayılan'ın hemen yanında **📄 Nakli Yekûnlü — çok sayfalı** olarak durur.
  Her ikisi de aynı `default.xml`'i (25 kalem) kullanır; aradaki fark doğrudan görünür — klasik
  şablon kalemleri tek uzun tabloda döker, nakli yekûnlü olan sayfalar ve devir satırı basar.
- **`default.xslt` v2.29.0 öncesindeki sayfalamasız haline döndü.** Sayfalama artık varsayılana
  dayatılmıyor; isteyen nakli yekûnlü şablonu seçer.

### Düzeltilen
- **Ara sayfalarda "NAKLİ YEKÛN (sonraki sayfaya devir)" iki kez basılıyordu** — bir kez kalem
  tablosunun son satırı olarak, bir kez de alt toplam kutusunda. Devir satırı artık yalnızca
  kalem tablosunda (matbu fatura geleneğindeki yerinde, tutar sütununda hizalı); alt kutuda
  yalnızca **Sayfa Toplamı** kalır — o zaten farklı bir bilgidir (sayfanın kendi toplamı,
  devreden kümülatif tutar değil).
  Ölçüldü: 25 kalemli `default.xml` → Saxon çıktısında "sonraki sayfaya devir" **2 → 1**.

## [2.30.1] — 2026-07-14 — Toplu Test Kullanım Kılavuzu

### Eklenen
- **Yardım (F1) → 🧪 Toplu Test (Regresyon) bölümü.** Özellik v2.29.0'da **belgesiz** çıkmıştı:
  uygulamada düğme vardı, ama ne işe yaradığını ve nasıl kullanılacağını anlatan tek satır yoktu.
  Yeni bölüm şunları anlatır: hangi derde deva olduğu (tek fatura gösteren editörün kör noktası),
  test klasörünün nasıl kurulacağı (**örnek klasör ağacı + macOS/Windows yolları**), adım adım
  çalıştırma, 📸 anlık görüntünün ne yaptığı, `aynı` / `DEĞİŞTİ` / `yeni` etiketlerinin okunuşu,
  tipik çalışma akışı ve `batch-baseline.json`'ın **üç platformdaki gerçek yolu**.
- Yardım içeriğinde artık çok satırlı blok (`pre`) ve sıralı liste (`ol`) da düzgün görünüyor.

## [2.30.0] — 2026-07-14 — Çoklu Dosya Sekmesi

### Eklenen
- **Sekmeler — aynı anda birden çok fatura/şablon açık tutulabiliyor.**
  Bir sekme = bir **çalışma**: kendi XSLT'si, XML'i, önizlemesi ve kaydedilmemiş-değişiklik durumu.
  Uygulama zaten baştan sona çift üzerine kuruluydu (dönüşümün girdisi şablon + veri), sekme de
  çifti temsil eder — pano başına ayrı sekme değil.
  - **Her panonun kendi şeridi var, ama şeritler evli:** XSLT panosunun şeridi şablon adlarını,
    XML panosununki veri adlarını gösterir; ikisi de **aynı** sekme listesini ve aynı aktif sekmeyi
    işaret eder. XML şeridinden 2. sekmeyi seçmek XSLT şeridini de 2'ye taşır — şablon verisinden
    ayrılmaz. XSLT tek başına açılıp varsayılan UBL-TR verisiyle eşlendiğinde XML sekmesi
    "(varsayılan veri)" yazar; boş sanılmasın.
  - Sekmede dosya adı, kaydedilmemiş göstergesi (●, hangi pano kirliyse orada), kapatma (×).
  - **Kısayollar:** `Cmd/Ctrl+T` yeni sekme · `Cmd/Ctrl+1…9` N'inci sekmeye git ·
    `Ctrl+Tab` / `Ctrl+Shift+Tab` sonraki/önceki sekme. Kapatma: × düğmesi veya **orta tık**.
    (`Cmd+W` bilerek kullanılmadı: macOS'ta Tauri'nin varsayılan menüsündeki "Pencereyi Kapat"a
    ait — webview'e hiç ulaşmaz, yani çalışmayan bir kısayol olurdu.)
  - Sekmeler sürükle-bırak ile yeniden sıralanır.

### Düzeltilen
- **Örnek yüklemek kaydedilmemiş çalışmayı sessizce eziyordu.** Artık bir dosya/örnek yüklenirken
  aktif sekmede **kaydedilmemiş** içerik varsa o içerik ezilmez, **yeni sekmede** açılır.
  Veri yüklenirken yeni sekme, **temiz** şablonu devralır — "aynı şablon, başka fatura" akışı
  bozulmasın diye (kirli şablon devralınmaz: aynı yol için iki farklı sürüm doğar, biri diğerini
  sessizce ezerdi).
- **Çıkışta yalnızca aktif sekmeye bakılıyordu.** Kapatma koruması artık **tüm sekmeleri** sayar;
  "Kaydet ve Çık" kirli sekmelerin hepsini sırayla kaydeder. Arka sekmedeki kaydedilmemiş fatura
  artık sessizce kaybolmuyor.
- Kaydedilen bir dosya başka sekmelerde de açıksa, o sekmelerin **temiz** kopyaları diskteki yeni
  içerikle eşitlenir — yoksa oraya geçip kaydetmek az önceki kaydı geri alırdı.

## [2.29.0] — 2026-07-14 — Nakli Yekûn (Çok Sayfalı Fatura) + Toplu Regresyon Koşusu

### Eklenen
- **Nakli yekûn / sayfalama — örnek şablon artık çok sayfalı fatura basıyor.**
  Şablon şimdiye kadar 20'den az kalemde `InvoiceLine[1]`…`[20]` diye tek tek yazıp boş satırla
  dolduruyor, **20 ve üstü kalemde ise hepsini alt alta döküyordu** — sayfa düzeni taşıyor,
  fatura 2. sayfaya sarkıyordu.

  Artık: her sayfaya `$sayfaSatiri` kalem düşer; sayfanın altında **NAKLİ YEKÛN (sonraki sayfaya
  devir)**, sonraki sayfanın başında **NAKLİ YEKÛN (önceki sayfadan devir)** yazılır. **Logo,
  satıcı/alıcı bilgileri, ETTN — tüm başlık her sayfada tekrar eder.** Gerçek toplamlar
  (`LegalMonetaryTotal`) **yalnızca son sayfada** basılır; ara sayfaların alt kutusunda
  *Sayfa Toplamı* + *Nakli Yekûn* görünür.

  ```xml
  <xsl:param name="sayfaSatiri" select="20"/>   <!-- sabit değil: N satır -->
  ```
  Sayfa sayısı, devir tutarları ve boş satır dolgusu bu tek parametreden türetilir.

- **Toplu regresyon koşusu (🧪 Toplu Test).** Şablonu bir klasördeki **tüm** faturalara karşı
  çalıştırır: hangileri patladı, ne kadar sürdü, çıktı kaç bayt. **📸 Anlık Görüntü** her çıktının
  **sha256**'sını saklar; şablonu değiştirip tekrar koşunca hangi faturaların çıktısının
  **DEĞİŞTİĞİ** satır satır çıkar. Satıra çift tıkla → o fatura editöre yüklenir.

  **Neden:** İskontolu faturada düzelttiğin şey tevkifatlı faturayı bozabilir ve **kimse fark
  etmez** — editör tek seferde tek fatura gösterir. "Bir şeyi düzelttim, başka bir şeyi bozdum mu?"
  sorusunu tahminle değil **ölçümle** yanıtlar.

### Değiştirilen
- **Varsayılan örnek fatura (`default.xml`) artık 25 kalemli, iki sayfalık gerçek bir fatura**
  (lastik/jant/servis kalemleri). Kendi içinde tutarlı: miktar × birim fiyat = satır tutarı,
  KDV %18, `LegalMonetaryTotal` kalemlerin **gerçek** toplamı (136.735,00 + 24.612,30 KDV =
  **161.347,30 TL** ödenecek). Nakli yekûn (134.035,00 TL) yalnızca sunum katmanında hesaplanır —
  UBL'de "ilk 20 kalemin toplamı" diye bir alan yoktur.

### Doğrulama
Sidecar'a doğrudan beslenerek ölçüldü: **29 örnek faturanın (≤20 kalem) çıktısı değişmedi** —
eski ve yeni şablon arasındaki tek fark eklenen sayfa kabı `<div>`'i; beklenmeyen sıfır fark.
25 kalemli fatura 2 sayfaya bölünüyor, devir tutarları toplamı tutuyor
(134.035,00 + 2.700,00 = 136.735,00).

### Düzeltilen
- Devir satırının `colspan`'ı bir eksikti (9): tutar **"Diğer Vergileri"** sütununa düşüyordu.
  Başlık metinlerini saymak yanılttı — `<td>`'ler sayılınca tablonun **11 sütun** olduğu
  (Sıra No dahil) görüldü. `colspan="10"`.

## [2.28.0] — 2026-07-13 — XPath Test Konsolu

### Eklenen
- **XPath test konsolu.** XML panelindeki **ƒx** düğmesi (veya `Cmd/Ctrl+Shift+X`) ile açılır:
  ifadeyi yaz, Enter'a bas, yüklü faturaya karşı **anında** çalışsın — kaç düğüm eşleşti,
  değerleri ne. `↑`/`↓` ile geçmiş, `Esc` ile kapanır.

  **Neden:** Şablon yazarken en çok zaman kaybettiren şey, bir alanın önizlemede boş gelmesi ve
  **sebebinin görünmemesi** — XPath mi yanlış, önek mi kaçtı, veri mi yok? Tek yol "XSLT'yi
  kurcala → dönüştür → bak" döngüsüydü.

- **`xpath-default-namespace` otomatik.** UBL faturalarının kökü varsayılan namespace'tedir; bu
  bildirilmeseydi `/Invoice/cbc:ID` gibi **en doğal görünen** ifade hiçbir şey eşleştirmez ve
  kullanıcı sebebini anlayamazdı. Önekler belgenin **kökünden** okunur (sabit listeden
  uydurulmaz — farklı önek kullanan belgede ifade sessizce boş dönerdi).

### Teknik
- **Sidecar'a dokunulmadı.** Tel protokolü (`[len][XSLT][len][XML]`) değiştirip GraalVM
  native-image'ı 5 platformda yeniden derlemek yerine, XPath minik bir XSLT sarmalayıcısına
  gömülüp **mevcut dönüşüm hattından** geçiriliyor. Saxon'un XPath 2.0/3.0'ı bedavaya geliyor.
- **Geri düşüş görünür.** Saxon yoksa `transformXml` sessizce tarayıcının XSLT 1.0'ına düşer ve
  2.0 sarmalayıcısı orada **hata vermeden yanlış** sonuç verebilirdi. Bu yüzden Saxon doğrudan
  çağrılır; düşülürse sonuçta **`⚠️ XPath 1.0` rozeti** basılır.
- Saxon'un hata mesajı **olduğu gibi** gösterilir ("Expected an expression, but reached the end
  of the input") — "bilinmeyen hata" demek teşhisi kör eder.
- Sidecar'ın çıktıya eklediği `<!DOCTYPE html>` kırpılır (ölçüldü; şu an ayrıştırıcı kabul
  ediyor ama buna bel bağlanmıyor).

**Sidecar'a karşı doğrulandı:** `//cbc:PayableAmount` → 35.40 · `count(//cac:InvoiceLine)` → 1 ·
`/Invoice/cbc:ID` → EFS2016000007422 · eşleşmeyen ifade → 0 · bozuk ifade → Saxon'un gerçek mesajı.

### CI
- **Release workflow artık eksik platformla yayınlarsa koşuyu kırmızıya boyuyor.** v2.27.3'te
  macos-14 işi GitHub'ın geçici altyapı hatasıyla düştü, diğer dördü yeşil geçti, release
  **yayınlandı** ve `latest.json` 15 yerine **13 platformla** çıktı — Apple Silicon kullanıcıları
  güncellemeyi hiç görmedi, hata da almadı. Yeni `verify` işi platformları **sayar**, imzaları
  denetler ve taslak kalmış release'i yakalar.

## [2.27.5] — 2026-07-13 — Editörde Sağ Tık Menüsü (Snippet'ler + Kısayollar)

### Eklenen
- **Editörlerde sağ tık menüsü.** Kes / Kopyala / Yapıştır · Tümünü seç · Ara ve değiştir ·
  Satıra git · Bloğu katla/aç · Tümünü katla/aç · Geri al / Yinele — **her birinin kısayolu
  yanında yazılı** (menünün asıl işi buydu: işlevler zaten vardı ama keşfedilemiyordu).
  Seçim yokken Kes/Kopyala, salt-okunur editörde Yapıştır **kapalı** görünür.
- **Snippet'ler sağ tık menüsünde.** XSLT editöründe, kategorilere ayrılmış alt menüden
  imlecin bulunduğu yere snippet eklenir. (XML veri editöründe **yok** — snippet'ler XSLT
  şablon kodudur.) Sol paneldeki liste ve sürükle-bırak aynen duruyor.
- Seçim dışına sağ tıklayınca imleç oraya taşınır (masaüstü editör davranışı).

### Düzeltilen
- **v2.27.3'te belgelenen katlama kısayolları yanlıştı.** `@codemirror/language` kaynağından
  doğrulandı: `foldAll`/`unfoldAll` için **mac varyantı yoktur** — macOS'ta da `Ctrl+Alt+[` /
  `Ctrl+Alt+]`'dir; tek blok katlama macOS'ta `Cmd+Alt+[`, diğerlerinde `Ctrl+Shift+[`.
  "`+Shift+[` hepsini katlar" diye bir kısayol **hiç yoktu**. CHANGELOG, FEATURES ve 5 dildeki
  düğme tooltip'leri düzeltildi.

### Teknik
- **Yeni bağımlılık:** `@tauri-apps/plugin-clipboard-manager` + `tauri-plugin-clipboard-manager`.
  Pano OS üzerinden okunur/yazılır; tarayıcının `navigator.clipboard.readText()`'i masaüstü
  webview'da izin isteyip **sessizce boş dönebiliyor** — bu projede sessiz geri düşüş yasak.
  `capabilities/default.json`: `clipboard-manager:allow-read-text` + `allow-write-text`.
- Pano hatası yutulmaz; kullanıcıya durum çubuğunda gösterilir.

## [2.27.4] — 2026-07-13 — Örnek Faturadan XAdES İmza Bloğu da Kaldırıldı

### Değiştirilen
- **Örnek fatura (`default.xml`) 14 KB → 8 KB.** `ext:UBLExtensions` altındaki **XAdES dijital
  imza bloğu** (`ds:Signature`, 5,9 KB kriptografik veri) kaldırıldı — tasarım editöründe
  hiçbir işlevi yok. Doğrulandı: Saxon çıktısının **sha256'sı değişmiyor** (542.530 bayt HTML,
  birebir aynı). Örnek fatura başlangıçtaki 172 KB'ın artık **%5'i**.
- `cac:Signature` **korundu** (imzalayan taraf adı/VKN/web sitesi): kriptografik blob değil,
  tasarımda basılabilecek gerçek UBL verisi.

## [2.27.3] — 2026-07-13 — Kod Katlama Kısayolları + Örnek Faturadan 159 KB Ölü Yük Kalktı

### Eklenen
- **Katlama (fold) kısayolları ve düğmeleri.** Fold gutter (satır numarası yanındaki oklar)
  vardı ama `foldKeymap` keymap'e hiç eklenmemişti — klavye kısayolları çalışmıyordu.
  Artık `Ctrl+Alt+[` / `Ctrl+Alt+]` hepsini katlar/açar; tek blok macOS'ta `Cmd+Alt+[`,
  diğer sistemlerde `Ctrl+Shift+[`. (Kısayollar ilk yayında **yanlış belgelenmişti**;
  v2.27.5'te kaynaktan doğrulanıp düzeltildi.)
  Ayrıca her editör başlığında **⊟ / ⊞** düğmeleri (tümünü katla / tümünü aç). 5 dilde.

### Değiştirilen
- **Örnek fatura (`default.xml`) 172 KB → 14 KB (%91 küçüldü).** Dosyanın 159 KB'ı,
  `cac:AdditionalDocumentReference` altında **base64 gömülü bir XSLT tasarımıydı**
  (`DocumentType: XSLT`) — bu editörde tamamen gereksiz. Doğrulandı: kaldırıldıktan sonra
  Saxon çıktısı **bayt bayt aynı** (542.530 bayt HTML), yani faturanın görünümü değişmiyor.
- **AI bağlamı: gömülü belgeler kırpılıyor.** `cbc:EmbeddedDocumentBinaryObject` içindeki ham
  base64 (gerçek UBL-TR faturaları kendi tasarımını böyle taşır) artık modele gönderilmiyor.
  Önceden yalnızca `data:` URI'leri kırpılıyordu; ham base64 her istekte token yakıyordu.

## [2.27.2] — 2026-07-13 — Kesik AI Yanıtı Artık Dosyayı Bozmuyor + Gerçek XML Hataları

### Düzeltilen
- **KRİTİK — kesik AI yanıtı kullanıcının belgesini eziyordu.** "Faturaya kalem ekle" gibi
  bir istekte model tüm XML'i yeniden yazmaya kalkıyor, yanıt `max_tokens` sınırında
  **yarıda kesiliyor**, uygulama bunu başarı sayıp **172 KB'lık faturanın üzerine 17 KB'lık
  yarım belgeyi yazıyor ve otomatik kaydediyordu.** Artık:
  - Rust tarafı `finish_reason=length` (Anthropic `max_tokens`, Gemini `MAX_TOKENS`) durumunu
    yakalar ve **kesik içeriği hiç döndürmez** — ne yapılacağını söyleyen bir hata verir.
  - **Boş yanıt** da başarı sayılmaz (sağlayıcı 0 baytlık yanıt döndürebiliyor).
  - AI önerisi **uygulanmadan önce** iyi-biçimlilik denetiminden geçer; bozuksa onay modalı
    hiç açılmaz. (Uygula → otomatik kaydet zinciri olduğundan bu doğrudan veri kaybıydı.)
  - Sistem promptu artık mevcut dosyanın **tam yeniden yazımını yasaklıyor** (150–600 KB'lık
    belgeler token sınırına sığmaz) ve kod bloğu başında boş satır bırakmayı yasaklıyor.
- **`<?xml` bildiriminden önceki boşluk XML'i geçersiz kılıyordu.** WebKit'in `DOMParser`'ı
  baştaki yeni satırı hoş görür, Xerces (Saxon) ise **ölümcül hata** sayar — AI'ın kod
  bloğundan çıkarılan belgeler tam olarak böyle başlıyordu. Artık Saxon'a gönderilmeden önce
  kırpılıyor (kullanıcının dosyası değişmez); Saxon'un bildirdiği satır numarası da telafi edilir.

### Eklenen
- **AI artık ekrandaki dönüşüm hatasını görüyor — ajan modu KAPALIYKEN de.** Hata,
  `[Uygulamadaki güncel dönüşüm hatası]` başlığıyla mesaja iliştirilir; kullanıcının hata
  metnini elle kopyalamasına gerek kalmaz. Başarılı dönüşümde temizlenir.

### Ayrıca düzeltilen
- **Bozuk XML/XSLT'de sebeple ilgisiz hata.** Geçersiz bir belge Saxon'a ulaştığında
  Xerces, hata metnini bir *resource bundle*'dan okumaya çalışıyor; GraalVM native-image
  bu paketleri ikiliye koymadığı için parser **hatayı bildirirken çöküyordu**. Kullanıcı
  `Could not load any resource bundle by ...impl.msg.XMLMessages` görüyor, gerçek hata
  ("satır 42'de kapanmayan etiket") tamamen kayboluyordu. İki katmanda düzeltildi:
  - **Ön yüz:** `transformXml` artık Saxon'a göndermeden önce XML **ve** XSLT'yi
    iyi-biçimlilik açısından denetler; bozuksa **satır/sütun içeren Türkçe hata** verir ve
    imleç hatalı satıra gider. (BOM'lu dosyalar için `validateXml` baştaki U+FEFF'i temizler —
    aksi halde geçerli belgeler hatalı görünürdü.)
  - **Sidecar:** `-H:IncludeResourceBundles` ile Xerces mesaj paketleri (XMLMessages,
    SAXMessages, DOMMessages, XMLSchemaMessages, DatatypeMessages) native-image'a eklendi;
    ön yüzden sızan durumlarda (DTD/entity hataları) Saxon da gerçek mesajı basabiliyor.

## [2.27.1] — 2026-07-13 — AI: Sonsuz "Düşünüyor…" Düzeltmesi + Durdur Düğmesi + XML Veri Düzenleme

### Düzeltilen
- **Sonsuz "Düşünüyor…" (kritik).** AI çağrılarında **hiçbir timeout yoktu**
  (`reqwest::Client::new()`); sağlayıcı isteği kuyruğa alıp yanıt vermezse uygulama
  **sonsuza kadar** bekliyor, kullanıcı hata bile almıyordu. Artık bağlantı için 15 sn,
  yanıt için 180 sn üst sınır var ve süre dolunca anlamlı Türkçe hata veriliyor.
  (Gerçek vaka: NVIDIA NIM istekleri kabul edip yanıtsız bırakıyordu.)
- **AI çağrıları artık günlüğe yazılıyor** — `ai.rs` daha önce **tek satır bile** log
  yazmıyordu, bu yüzden "yanıt gelmiyor" şikâyeti kör teşhis demekti. Her çağrıda
  sağlayıcı, model, uç nokta (yalnızca şema+host), bağlam boyutu, mesaj/ek sayısı,
  süre ve sonuç loglanır. **API anahtarı loglanmaz** (Gemini anahtarı URL'de taşıdığı
  için tam URL hiç yazılmaz). Model listesi çağrısı da loglanır.
- **Gerçek hata sebebi gösteriliyor.** Hata gövdesi ham metin olarak okunup
  `error.message` / `message` / `detail` alanlarından çözülüyor; hiçbiri yoksa gövdenin
  kendisi yazılıyor — artık "bilinmeyen hata" denmiyor. Ağ hatalarında `reqwest`
  kaynak zinciri (DNS/TLS/kapanan bağlantı) günlüğe düşer. 404'te "bu model bu uç
  noktada servis edilmiyor, başka model seçin" yönlendirmesi verilir.
- **Model listesinde yalnızca tek model görünüyordu.** Seçici `<input list="...">` +
  `<datalist>` idi; native `datalist` önerileri kutudaki metne göre **filtreler**, bu yüzden
  seçili model yazılıyken diğerleri (ör. `deepseek-v4-pro`) hiç görünmüyordu. Liste hep
  doğru geliyordu — arayüz gizliyordu. Artık gerçek bir `<select>`: tüm modeller her zaman
  görünür. **✎** düğmesiyle elle model adı yazılabilir (Ollama/yerel uçlar için).
  Kayıtlı model listede yoksa listenin başına eklenir — seçim sessizce başka modele kaymaz.

### Eklenen
- **"■ Durdur" düğmesi.** Yanıt beklenirken "Gönder"in yerini alır; basınca gösterge
  anında kapanır. `invoke` gerçekten iptal edilemediğinden her isteğe kimlik verilir →
  **geç gelen yanıt sohbete sızmaz, yok sayılır.** Ajan modunda turlar arasında da
  kontrol edilir (durdurulan ajan dosyaya düzenleme uygulayamaz). 5 dilde.
- **AI artık XML belge verisini de düzenleyebiliyor.** Önceden sistem promptu yalnızca
  XSLT tasarımına izin veriyor, "fatura kalemi ekle" gibi istekleri reddediyordu. Artık
  kalem ekleme/çoğaltma, tutar/taraf/tarih değiştirme, test verisi üretme kapsam
  içinde. İki koruma korunur: **XAdES imzasına dokunulmaz** (veri değişince imzanın
  geçersizleştiği kullanıcıya bildirilir) ve **toplam zinciri güncellenir**
  (satır tutarı → `LegalMonetaryTotal` → KDV → `PayableAmount`), UBL öğe sırası korunur.
  Değişiklik yine yalnızca kullanıcı onayıyla uygulanır.

## [2.27.0] — 2026-07-12 — DeepSeek + Dinamik AI Parametreleri + UBL 2.1 Uluslararası Snippet'ler

### Eklenen
- **DeepSeek sağlayıcısı:** AI asistanına 4. sağlayıcı (deepseek-chat / deepseek-reasoner,
  OpenAI-uyumlu uç). DeepSeek'in `max_tokens` üst sınırı 8192 olarak uygulanır.
- **Dinamik AI parametre sistemi (`AI_PARAM_DESCRIPTORS`):** Hangi kontrolün hangi
  sağlayıcı+modelde görüneceği descriptor kayıtlarından belirlenir — yeni bir parametre
  (ör. web araması) eklemek tek kayıt demektir.
- **Derin düşünme (thinking):** Anthropic extended thinking (8192 bütçe), OpenAI
  `reasoning_effort=high` (o-serisi/gpt-5), Gemini `thinkingConfig` (dinamik bütçe).
  Ayarlar → AI'da modele göre görünen kontroller + AI panelinde hızlı 🧠 düğmesi.
  DeepSeek'te düşünme ayrı model (deepseek-reasoner) olduğundan anahtar gösterilmez.
- **Temperature ayarı:** Tüm sağlayıcılarda; boş bırakılırsa sağlayıcı varsayılanı
  kullanılır (parametre gönderilmez). Anthropic'te thinking açıkken temperature
  gönderilmez (API kuralı).
- **UBL 2.1 (Uluslararası) snippet seti:** 39 yeni snippet (`UBL21_*`) — EN 16931 /
  Peppol BIS 3.0 kapsamı, İngilizce çıktı etiketleri. Toplam snippet sayısı 294'e çıktı.
- 6 yeni arayüz metni × 5 dil.

### Düzeltilen
- OpenAI reasoning modelleri (o-serisi/gpt-5) `max_tokens` parametresini reddediyordu —
  OpenAI'da artık `max_completion_tokens` gönderiliyor.

## [2.26.0] — 2026-07-12 — Çoklu Dil (i18n): 5 Dil + Çeviri Düzenleyici

### Eklenen
- **5 arayüz dili:** Türkçe, İngilizce, İspanyolca, Rusça, Lehçe. Ayarlar → Dil'den seçilir;
  tüm arayüz (ana ekran, editör, AI asistanı, ayarlar, yardım kromu) anında değişir.
  332 metin × 5 dil betikle doğrulandı — eksik 0, yer tutucu hatası 0.
- **Çeviri düzenleyici ekranı** (`/translations`): her arayüz metni uygulama içinden düzenlenebilir
  (referans dil Türkçe dahil). Yer tutucu (`{0}` vb.) bozulursa satır kırmızı işaretlenir ve uyarı
  verilir. JSON dışa/içe aktarma ve UI'dan yeni dil ekleme/silme desteklenir.
  Ayarlar → Dil → "✏️ Çevirileri düzenle" ile açılır.
- Açılışta `validateLocales()` çeviri bütünlüğünü denetler; sorunlar konsola ve günlüğe yazılır.

### Değiştirilen
- Yerleşik Türkçe: `common.saveAs` "Farklı" → "Farklı Kaydet".

### Bilinen sınırlamalar
- AI asistanının modele giden uzman promptu **kasten Türkçe** (UBL-TR alan bilgisi Türkçe).
- CodeEditor arama paneli dili editör açılışında seçilir; tr dışındaki dillerde CodeMirror'ın
  İngilizce varsayılanları kullanılır.
- Yardım (HelpModal) içeriği şimdilik yalnızca Türkçe; pencere kromu çevrildi.

---

## [2.25.1] — 2026-07-12 — Linux ARM64 Paketlemesi Onarıldı

### Düzeltilen
- **Linux ARM64 paketi üretilemiyordu:** `failed to bundle project: xdg-open binary not found`.
  ARM64 runner imajında `xdg-utils` kurulu değil (x64'te hazır geliyor) ve AppImage paketlemesi
  `xdg-open`'e ihtiyaç duyuyor. Bağımlılık listesine eklendi.
- GraalVM ve Saxon sidecar ARM64'te sorunsuz derlendi — asıl risk zaten atlatılmıştı.

---

## [2.25.0] — 2026-07-12 — Linux ARM64 Paketleri

### Eklenen
- **Linux ARM64 (aarch64) paketleri** — `.deb` / `.rpm` / `.AppImage`. Release matrisine
  `ubuntu-22.04-arm` runner'ı eklendi (public depolarda ücretsiz).
  - **Neden:** Apple Silicon üzerindeki sanal makineler (VMware Fusion, Parallels, UTM) **yalnızca
    ARM64 misafir** çalıştırır; x86_64 paketlerimiz orada **çalışmaz**. Bu, Windows'ta yediğimiz
    mimari tuzağının aynısıydı — bu kez kurulum yapılmadan önce yakalandı.
  - Ayrıca ARM sunucular, Raspberry Pi ve Asahi Linux kullanıcılarını da kapsar.
  - GraalVM native-image **Linux/AArch64'ü destekler** (Windows/ARM64'ü desteklemiyor), dolayısıyla
    Saxon sidecar'ı bu platformda **yerel** olarak derleniyor — tam XSLT 2.0/3.0.
- `latest.json` artık **15 platform** girdisi taşıyor (11 → 15); otomatik güncelleme ARM64 Linux'ta da
  çalışır (AppImage üzerinden).

### Not
- **Derleme yine `ubuntu-22.04`'te yapılır** (26.04 mevcut olsa da). Eski glibc'de derlenen ikili yeni
  dağıtımlarda çalışır; tersi çalışmaz. Test için istediğin Ubuntu sürümünü kullanabilirsin.

### Geliştirici
- VS Code Java classpath'i (`.vscode/settings.json`): `sidecar/` tek dosyalık bir Java programı,
  Maven/Gradle projesi yok — IDE `net.sf.saxon.*` importlarını çözemiyor ve **13 sahte hata**
  gösteriyordu. Derlemeyi etkilemiyordu; artık gürültü de yok.

### Belgelendirme
- **`LICENSE.tr.md`** — MIT lisansının Türkçe açıklaması + "kısaca ne demek?" özeti + üçüncü taraf
  bileşen lisansları (Saxon-HE **MPL 2.0**). Çeviri **bağlayıcı değildir**; bağlayıcı olan İngilizce
  `LICENSE` dosyasıdır — bu yüzden orijinal metne dokunulmadı.

---

## [2.24.1] — 2026-07-12 — "Günlük Klasörünü Aç" Çalışmıyordu

### Düzeltilen
- **"Günlük klasörünü aç" düğmesi hiçbir şey yapmıyordu.** `opener` eklentisinin **arayüz izni**
  yalnızca `$APPDATA` / `$APPLOCALDATA` altını açmaya yetkiliydi; günlük klasörü ise başka yerde
  (macOS: `~/Library/Logs/<bundle>`). İstek **izinle reddediliyor**, hata da yalnızca günlüğe yazılıp
  kullanıcıya hiçbir şey söylenmiyordu.
  - Klasör artık **Rust tarafından** açılıyor (arayüzün izin kapsamına tabi değil, üç platformda da
    çalışır). Klasör henüz oluşmamışsa yaratılır.
  - **Hata artık sessiz kalmıyor:** açılamazsa düğmenin yanında sebebiyle birlikte gösterilir.
  - **Tam yol her zaman ekranda:** açma çalışmasa bile kullanıcı klasörü elle bulabilir — sorun
    bildiren biri için tek başına yeterli.
  - Bu, kendi yazdığımız 1. dersin ("hata vermiyor ≠ çalışıyor") kendi kodumuzdaki ihlaliydi.
- **Sürüm senkronu:** `eFaturaEdit.Core.csproj` beş sürümdür `2.19.1`'de kalmıştı (direktifte yazılı
  olmasına rağmen atlanmış). Tüm 6 nokta hizalandı; direktife doğrulama adımı eklendi.

---

## [2.24.0] — 2026-07-12 — Alt Bilgi Çubuğu + Kaydetme Onarımları

### Düzeltilen
- **"Farklı Kaydet" yalnızca XSLT'yi kaydediyordu.** XML için farklı-kaydet hiç yoktu; şablonu yeni bir
  klasöre kaydettiğinde XML orada olmuyor ve elinde **eşleşmeyen bir çift** kalıyordu. Artık ikisi de
  kaydedilir — XML penceresi XSLT'nin klasöründe açılır ve mevcut adı önerir. XML atlanırsa
  **sessiz geçilmez**, uyarılır.
- **Kaydet / Cmd+S yalnızca *değişmiş* dosyaları kaydediyordu.** Örnek yükleyip sadece XSLT'yi
  düzenlersen XML "değişmedi" sayılıp **diske hiç yazılmıyordu** → diskte eşi olmayan bir şablon.
  Artık ölçüt "değişmiş **veya** diskte hiç yok". XSLT+XML tek bir çalışma birimidir.
  (Otomatik/sessiz kayıt kullanıcının önüne dialog açmaz; o dosya elle kaydetmeye bırakılır.)
- **Kaydetme penceresi alakasız bir klasörde açılıyordu.** `saveFileAs`'e yalnızca dosya adı
  veriliyor, klasör verilmiyordu; işletim sistemi de **en son kullanılan klasörü** hatırlıyordu.
  Artık üzerinde çalışılan dosyanın yanında (o yoksa çiftinin klasöründe) açılır.
- **Önizlemede sağ tık menüsü kapanmıyordu.** Menü ana pencereye gelen tıklamayla kapanıyor, ama
  önizleme bir **iframe** ve içindeki tıklamalar ana pencereye ulaşmıyor — yani menüyü kapatmak için
  en doğal yere (önizlemenin üstüne) tıklıyordun ve hiçbir şey olmuyordu. Artık iframe içindeki köprü
  tıklamayı ana pencereye bildiriyor.
- **Snippet sayısı tutarsızdı.** Kenar çubuğu 255 derken durum çubuğu ve karşılama ekranı **149**
  diyordu: oralarda yalnızca temel UBL-TR seti sayılıyor, sonradan eklenen **XSLT komutları (56),
  XPath fonksiyonları (26) ve CSS kuralları (24)** hiç görünmüyordu. Dördü de tek kaynağa bağlandı.

### Eklenen
- **Alt bilgi çubuğu (footer):** dosya adları + boyut + kaydedilmemiş değişiklik göstergesi,
  son dönüşümün çıktısı ve **süresi**, AI modeli, snippet sayısı, sürüm ve **saniyeli tarih-saat**.
  - **Motor rozeti** (en kritik alan): `✅ Saxon · XSLT 1.0/2.0/3.0` veya `⚠️ Tarayıcı · yalnızca XSLT 1.0`.
    Yedek motora düşmek sessiz kalırsa 2.0 komutları hata vermeden yok sayılır ve fatura yanlış basılır —
    artık hangi motorla çalıştığın her an ekranda.
- **Editör başlıklarında tam dosya yolu** (uzunsa soldan kısalır ki dosya adı hep görünsün) +
  binlik ayraçlı karakter sayısı.
- **Hakkında → İletişim:** e-posta ve GitHub Issues bağlantısı.
- **macOS yerel "Hakkında" paneli dolduruldu** (sürüm, açıklama, telif, lisans, web sitesi, iletişim).
  Menü hiç kurulmadığı için Tauri varsayılanı kullanılıyordu ve panel künyesizdi.

---

## [2.23.0] — 2026-07-12 — Günlükleme Sistemi + Windows ARM64 XSLT Motoru

### Düzeltilen
- **Windows'ta XSLT motoru çalışmıyordu** (`XSLT motoruna veri yazılamadı: Boru sonlandı. os error 109`).
  Kök neden **mimari**: sidecar x86_64 olarak derleniyor ve GraalVM native-image varsayılan olarak
  **modern CPU komutlarını (AVX2 vb.)** hedefliyor. Windows'un ARM üzerindeki x64 emülasyonunda
  (Prism) bu komutlar desteklenmediğinden süreç **ilk komutta, hata bile veremeden ölüyor**; biz de
  ölmüş bir boruya yazmaya çalışıp anlamsız bir hata gösteriyorduk.
  - Doğrulama: aynı sidecar **x64 Windows'ta 600 KB'lık gerçek yükle sorunsuz** çalışıyor (CI'da
    ölçüldü); yalnızca ARM64 emülasyonunda ölüyor.
  - Çözüm: sidecar artık **`-march=compatibility`** ile, en düşük ortak komut setiyle derleniyor.
    Bu yalnızca ARM64 emülasyonunu değil, **eski CPU'lu amd64 kullanıcılarını** da kurtarır — onlar
    da aynı sebeple patlıyor olabilirdi ve haberimiz olmazdı.
- **Hata gizleniyordu.** stdin yazımı başarısız olunca sidecar'ın `stderr`'i ve çıkış kodu okunmadan
  dönülüyordu — yani asıl sebep çöpe atılıp semptom gösteriliyordu. Artık motorun söyledikleri ve
  çıkış kodu hem hataya hem günlüğe giriyor (çıkış kodu tanı için altın: `-1073741515` → eksik DLL).
- **Uygulama çuvallıyordu.** Geri düşüş koşulu bu hata metniyle eşleşmediğinden, tarayıcı motoruna
  düşmek yerine sert hata veriliyor ve Windows kullanıcısı **hiçbir şey yapamıyordu**. Rust tarafı
  artık "motorun kendisi çalışmıyor" durumunu ayrı işaretliyor (`XSLT_ENGINE_UNAVAILABLE`) ve
  uygulama XSLT 1.0'a düşüyor.
- **Ama sessizce değil:** kalıcı bir uyarı bandı çıkıyor. Sessiz geri düşüş tehlikelidir — tarayıcının
  1.0 işlemcisi `format-dateTime`, `tokenize`, `for-each-group` gibi 2.0 komutlarını **hata vermeden
  yok sayar**; kullanıcı şablonunun çalıştığını sanır, oysa çıktı yanlıştır.

### Eklenen
- **Günlükleme sistemi (`tauri-plugin-log`).** Her şey diske yazılıyor:
  - Oturum künyesi: sürüm, işletim sistemi, **mimari** — bu vakanın ilk bakışta çözülmesini sağlayacak satır.
  - XSLT motoru tam enstrümante: yük boyutları, kaç bayt yazıldı, çıkış kodu, sidecar stderr'i, süre.
  - Yakalanmayan hatalar ve reddedilen promise'ler (eskiden sessizce yutuluyordu).
  - 2 MB'da dönen dosyalar, yerel saat damgası.
- **Ayarlar → Hakkında → "Günlük klasörünü aç"** — sorun bildirirken eklenecek dosya.

### Bilinen sınırlama
- **GraalVM native-image, Windows/ARM64'ü hedef olarak desteklemiyor** — ARM64 Windows için yerel
  Saxon ikilisi üretilemez. `-march=compatibility` ile x64 ikilisinin emülasyon altında çalışması
  hedefleniyor; çalışmazsa uygulama XSLT 1.0'a düşer ve bunu açıkça bildirir.

---

## [2.22.2] — 2026-07-12 — AI Sohbeti Sayfa Geçişinde Kaybolmuyor

### Düzeltilen
- **AI yanıt beklerken Ayarlar'a gidip dönünce sohbet kayboluyordu.** Kök neden: sohbetin canlı
  durumu (açık oturum + "gönderiliyor" + hata) **bileşende** tutuluyordu. Ayarlar'a geçince
  `AIAssistant` unmount oluyor, ama uçuşta olan istek devam ediyordu; yanıt geldiğinde artık yok
  olmuş bileşenin state'ine yazılıyordu. Geri dönüldüğünde yeni bileşen ayrı bir reaktif kopya
  oluşturduğu için cevabı hiç görmüyor, üstelik `sending` sıfırlandığından **"Düşünüyor…" göstergesi
  de kayboluyordu** — kullanıcıya sohbet ölmüş gibi görünüyordu.
- Canlı durum `ai-sessions` modülüne taşındı (`aiRuntime`). Modül uygulama boyunca yaşadığından hem
  istek hem gösterge sayfa geçişinden sağ çıkar. (Önceki oturum kalıcılığı düzeltmesi yalnızca
  **tamamlanmış** mesajları koruyordu; "düşünürken geçiş" durumu açıkta kalmıştı.)
- **Bitişik kusur:** yanıt beklenirken sohbet değiştirilebiliyor, yeni sohbet açılabiliyor veya sohbet
  silinebiliyordu — gelen cevap yanlış oturuma yazılabilirdi (silmede ise oturum yok olup yerine
  yenisi açılmayacaktı). Bu üç kontrol yanıt süresince devre dışı bırakıldı, sebebi ipucu metninde.

### Belgelendirme
- **macOS "hasar görmüş olduğu için açılamıyor" uyarısı** README'ye ve release notlarına eklendi.
  Uygulama bozuk değildir: paketler Apple Developer ID ile imzalanmadığından, macOS tarayıcıyla
  indirilen dosyalara taktığı *karantina* bayrağı yüzünden bu yanıltıcı mesajı gösterir. Çözüm
  (`xattr -dr com.apple.quarantine …`) ve **nedeni** açıkça yazıldı — insanlar "hasar görmüş" deyince
  uygulamayı silip atıyor. Windows SmartScreen uyarısı için de not düşüldü.
- Otomatik güncellemeler bu adımı gerektirmez (karantina bayrağı yalnızca tarayıcı indirmelerine takılır).

---

## [2.22.1] — 2026-07-12 — Güncelleme Penceresinde Gerçek Sürüm Notları

### Düzeltilen
- **Güncelleme penceresi "neyin değiştiğini" göstermiyordu.** Sürüm notları alanına CI'daki sabit
  şablon metni ("otomatik derlenmiş kurulum paketleri… Windows: .msi / .exe…") düşüyordu; yani
  kullanıcı, güncellemede ne olduğunu değil hangi paketlerin bulunduğunu okuyordu. Oysa güncelleme
  bildiriminin bütün anlamı o notlarda.
- Release CI artık sürüm notlarını **`CHANGELOG.md`'nin ilgili bölümünden** üretiyor. Bu metin hem
  GitHub Release'de hem de `latest.json` üzerinden **uygulama içindeki güncelleme penceresinde** çıkar.
  CHANGELOG'da o sürümün girdisi yoksa boş nota düşmez, güvenli bir başlığa geriler.

### Not
- Bu sürüm aynı zamanda otomatik güncellemenin **ilk uçtan uca testidir**: v2.22.0 kurulu bir
  uygulamanın açılışta bu sürümü kendiliğinden bulup sürüm notlarıyla birlikte sunması beklenir.

---

## [2.22.0] — 2026-07-12 — Otomatik Güncelleme

### Eklenen
- **Otomatik güncelleme (Tauri updater).** Uygulama açılışta sessizce yeni sürüm denetler; varsa
  sürüm notlarıyla birlikte bir pencere gösterir. **İndirme/kurma yalnızca kullanıcı onaylarsa**
  başlar; ilerleme çubuğu gösterilir, kurulum bitince uygulama yeniden başlar.
- **Elle denetleme:** Ayarlar → Hakkında → "Güncellemeleri denetle". Güncelse "✓ En güncel sürümü
  kullanıyorsun" der.
- Açılıştaki denetim **3 saniye ertelenir** ve sessizdir — internet yoksa veya geliştirme modundaysak
  kullanıcıya hata gösterilmez (elle denetlemede ise hata görünür).
- Release CI artık her platform için updater paketleri (`.app.tar.gz`, `.msi.zip`, `.AppImage.tar.gz`)
  ve imzalarını üretip release'e `latest.json` manifestini ekliyor.

### Güvenlik
- **İmzalı güncelleme.** Paketler minisign anahtarıyla imzalanır; uygulama, `tauri.conf.json`'a gömülü
  **açık anahtarla doğrulayamadığı hiçbir güncellemeyi kurmaz**. Doğrulama Rust tarafında yapılır,
  arayüz kodu onu atlayamaz. Özel anahtar GitHub Secret'larında tutulur, repoda yoktur.

### Not
- Bu, updater içeren **ilk** sürüm. v2.21.x ve öncesini kullananlar bu sürüme **bir kez elle**
  geçmelidir; sonrasındaki güncellemeler otomatik gelir.
- Linux'ta otomatik güncelleme yalnızca **AppImage** için çalışır (`.deb`/`.rpm` paket yöneticisiyle
  yönetildiğinden updater onlara dokunmaz).

---

## [2.21.1] — 2026-07-12 — Windows MSI Paketlemesi Onarıldı (CP1252 / Türkçe karakter)

### Düzeltilen
- **v2.21.0'ın Windows paketi hiç üretilmedi.** Yeni eklenen dosya ilişkilendirmesinin açıklamaları
  Türkçe `ş` / `İ` / `ı` harfleri içeriyordu; WiX ise MSI dizelerini **code page 1252** (Latin-1) ile
  yazar ve bu harfler o kod sayfasında **yoktur**. Sonuç: `light.exe` **LGHT0311** ile çöktü,
  `.msi` ve `.exe` release'e hiç eklenemedi (macOS ve Linux paketleri etkilenmedi).
- Açıklamalar CP1252 güvenli Türkçe ifadelere çevrildi ("e-Fatura XSLT dizayn belgesi",
  "UBL-TR e-belge XML verisi"). `name` alanı yalnızca macOS `Info.plist`'ine gittiğinden
  (UTF-8) Türkçe kaldı.
- Teşhis notu: Tauri, `light.exe`'nin kendi hata çıktısını yutuyor ("failed to run light.exe" deyip
  susuyor); asıl WiX hatasını görmek için `tauri build --verbose` gerekiyor.

### Eklenen
- **Release CI'da CP1252 ön kontrolü:** `productName` ve dosya ilişkilendirme açıklamaları derleme
  başlamadan doğrulanıyor. Aynı hata bir daha 20 dakikalık derlemenin sonunda kriptik bir WiX
  koduyla değil, saniyesinde anlaşılır bir mesajla yakalanır.

---

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
