# Copilot Direktifi — E-FaturaEdit

**Rol:** Bu depo **iki bağlı parçadan** oluşuyor:

1. **`src/eFaturaEdit.Core/`** + **`src/eFaturaEdit.DataExport/`** — Paylaşılan çekirdek (`netstandard2.0;net10.0` multi-target). Snippet/örnek/tamamlama kataloğu POCO'ları burada yaşar; DataExport bunları `app/src/lib/data/*.json`'a JSON olarak aktarır. UI veya dosya sistemi çağrısı yok, sıfır dış NuGet bağımlılığı.
2. **`app/`** — Cross-platform masaüstü uygulaması: Tauri v2 (Rust, edition 2021) + SvelteKit + Svelte 5 (runes) + TypeScript + CodeMirror 6 + Vite 6. macOS/Windows/Linux hedefliyor.

> Not: Eski bir .NET Framework/WinForms + DevExpress uygulaması (`eFaturaEdit/`) bu projenin öncüsüydü; lisans/aktivasyon sistemi (QLicense, ActivationControls4Win) ve DevExpress bağımlılığıyla birlikte **kaldırıldı** (bkz. CHANGELOG). Geçmişi `git log` ile görülebilir ama aktif kod tabanında yok — yeniden eklenmesi istenmedikçe referans verme.

Konu XSLT/XML/UBL-TR e-fatura tasarımı veya Rust/Tauri/Svelte olabilir — hangi klasörde çalışıyorsan o yığının deyimleriyle düşün.

**Öncelik:** Güvenlik > Mimari bütünlük > Stabilite > Performans

## Temel Kurallar
- Sadece istenen bloğu değiştir; tüm dosyayı yeniden yazma.
- Public API / method imzalarını (C# public method, Rust `#[tauri::command]`, Svelte component `Props`) açık talimat olmadan değiştirme.
- Talep dışı refactor yapma.
- Belirsizlikte işlemi başlatma, soru sor.
- Büyük değişiklikleri parçala, her adımda onay iste.
- Bir parçadaki değişiklik diğerini etkiliyorsa (ör. Core'da alan eklemek → DataExport JSON şeması → `app/src/lib/data/types.ts`) zincirin tamamını güncelle.

## Mimari
- **`src/eFaturaEdit.Core/`**: Saf POCO/mantık katmanı — burada UI veya dosya sistemi çağrısı yok.
- **`app/`**: SvelteKit dosya-tabanlı routing (`src/routes/`), Tauri komutları yalnızca `src-tauri/src/lib.rs`'de. Frontend ⇄ Rust köprüsü sadece `invoke()` / `@tauri-apps/plugin-*` üzerinden — DOM'dan doğrudan dosya sistemine erişim yok.
- Katman ihlali yasak (ör. Svelte component içinden doğrudan Rust mantığı). Yeni pattern eklemeden önce gerekçe sun.
- **AI entegrasyonu (varsa):** Kullanıcının kendi API anahtarı (BYOK) ile çalışır, hiçbir anahtar uygulamaya gömülmez/paylaşılmaz. AI'nin çıktısı yalnızca XSLT/XML editör içeriğine "önerilen değişiklik" olarak sunulur ve kullanıcı onayı olmadan uygulanmaz — dosya sistemi, ayarlar veya başka hiçbir işleve doğrudan erişemez.

## Kod Standartları

### C# (`src/eFaturaEdit.Core/`, `src/eFaturaEdit.DataExport/`)
- `Task.Result` ve `.Wait()` kesinlikle yasak; her zaman `await` kullan.
- `CancellationToken` varsa tüm alt çağrılara ilet.
- Gereksiz `ToList()` / `ToArray()` kullanma.
- Magic number yasak; sabit veya enum kullan.
- `Nullable` bu projede **kapalı** (`<Nullable>disable</Nullable>`) — yeni kod NRT varsaymasın; null kontrolünü klasik `if (x == null) throw` ile yap.

### Rust / TypeScript / Svelte (`app/`)
- Rust: `.unwrap()`/`.expect()` sadece gerçekten panic edilmesi gereken durumda (kurulum/init); kullanıcı girdisi veya IO hatasında `Result` + `?` kullan.
- Yeni bir Tauri komutu native bir capability gerektiriyorsa `src-tauri/capabilities/default.json`'a izni eklemeyi unutma.
- Svelte 5 runes kullan (`$state`, `$derived`, `$effect`) — eski `export let`/`$:` stiline dönme.
- Kaynak dosyada (yorum, regex, string fark etmez) bitişik `<script` veya `<style` metni **yazma** — Svelte derleyicisinin blok-sınırı ön taraması yanlış pozitif verip gerçek `</script>`/`</style>` kapanışını yutabiliyor (bkz. `+page.svelte`'deki `STYLE_TAG` değişken-interpolasyonu deseni).
- Yeni ayar `settings.svelte.ts`'e eklendiğinde Ayarlar sayfasında (`src/routes/settings/+page.svelte`) gerçek bir kontrolle bağlanmadıkça commit'leme (ölü ayar bırakma).

## Veri & Dosya Formatları
- Bu projede veritabanı **yok** — kalıcılık dosya sistemi (XSLT/XML dosyaları, `localStorage`, `$APPDATA/*.json`) üzerinden.
- `app/src/lib/data/*.json` **elle düzenlenmez** — `npm run data:sync` (DataExport) ile Core'dan üretilir. Manuel düzeltme gerekiyorsa kaynağı Core tarafında değiştir.
- Kullanıcı verisi (kullanıcı örnekleri, kullanıcı snippet'leri, AI API anahtarları) ayrı dosyalarda/ayarlarda tutulur (`$APPDATA/samples/`, `$APPDATA/user-snippets.json`, `localStorage`) — bundled veriyle asla birleştirilip üzerine yazılmaz.

## Güvenlik & Hata Yönetimi
- Log'larda şifre/token/API anahtarı/PII maskele.
- Kullanıcıya stack trace gösterme; anlamlı Türkçe hata mesajı döndür (bkz. `status()` deseni, `app/`).
- Exception yutma; handle et veya `throw` ile ilet.
- **Repo public** — commit etmeden önce her zaman gerçek bir sır (API anahtarı, şifre, sertifika private key) olup olmadığını kontrol et; test/placeholder değeri değilse commit etme, sor.

## ⛔ ACI DERSLER — bir daha uğraşma (hepsi bu projede GERÇEKTEN başımıza geldi)

Bu bölüm süs değil. Aşağıdaki her madde saatler kaybettirdi. Yeni bir hatayla karşılaşınca
**önce buraya bak** — muhtemelen aynı sınıftan.

### 1. Sessiz başarısızlıklar — "hata vermiyor" ≠ "çalışıyor"
Bu projedeki en pahalı hataların ortak özelliği: **hiç hata vermiyorlardı, sadece iş görmüyorlardı.**
- `macos-13` emekli olunca Intel işi **sonsuza dek kuyrukta bekledi** → release yayınlandı ama Intel
  `.dmg` yoktu. Dört sürüm boyunca fark edilmedi.
- Windows MSI, açıklamadaki Türkçe `ş` yüzünden çöktü → release çıktı ama **Windows paketi yoktu**.
- Bir CI işi düşerse `latest.json` **eksik platformla** yayınlanır → o platformdaki kullanıcılar
  güncellemeyi **hiç görmez**, hata da almaz.
- AI yanıtı, bileşen unmount olunca **buharlaşıyordu** — ekranda hiçbir hata yoktu.

**Kural:** Bir işin bittiğini varsayma, **çıktısını doğrula.** Release sonrası varlıkları ve
`latest.json`'daki 11 platformu SAY. "Başarılı" gözüken bir koşu eksik ürün çıkarmış olabilir.

### 2. Semptomu değil sebebi göster — hata yutma
Windows'ta `xslt.rs`, stdin yazması başarısız olunca **hemen dönüyor**, sidecar'ın `stderr`'ini ve
**çıkış kodunu okumuyordu**. Sonuç: kullanıcıya "Boru sonlandı (os error 109)" diyorduk — gerçek
sebeple (CPU komut seti) hiç ilgisi olmayan bir mesaj. Saatler buna gitti.

**Kural:** Bir dış süreç ölürse **her zaman** exit code + stderr topla ve raporla. Yazma hatasında
bile önce süreci drain et. Çıkış kodu tanının yarısıdır:
`-1073741515` (0xC0000135) → eksik DLL · `-1073741795` (0xC000001D) → geçersiz komut (CPU/emülasyon)
· `-1073741819` (0xC0000005) → erişim ihlali.

### 3. Sessiz geri düşüş (fallback) YASAK
Saxon yoksa tarayıcının XSLT **1.0** işlemcisine düşüyoruz. 1.0 işlemcisi `format-dateTime`,
`tokenize`, `for-each-group` gibi 2.0 komutlarını **hata vermeden yok sayar** → kullanıcı şablonunun
çalıştığını sanır, **fatura sessizce yanlış basılır.** Bu, hata vermekten beterdir.

**Kural:** Düşüşü her zaman **görünür** kıl (kalıcı uyarı bandı + günlük). Ve "motor çalışmıyor" ile
"şablon hatalı" durumlarını **ayır** (`XSLT_ENGINE_UNAVAILABLE`) — şablon hatasında geri düşmek,
kullanıcının hatasını gizlemek olur. Ayrımı **metin eşleştirmeyle yapma**; ilk sürümde öyleydi ve
Windows'un hata metni hiçbir kalıba uymadığı için uygulama tamamen çuvalladı.

### 4. Kendi test/teşhis aracına da güvenme
Windows teşhis betiğim **iki kez kendi hatamdan** çöktü (PowerShell dizi döndürünce "açar" ve
`Object[]` yapar). İlkinde bunu "sidecar bozuk" diye okusaydım tamamen yanlış yola sapardım.
Küçük yükle (300 bayt) yazdığım duman testi de yanıltıcıydı — gerçek dosya **600 KB**.

**Kural:** Bir test "başarısız" derse önce **testin kendisini** doğrula. Ve testi **gerçek boyut ve
gerçek koşullarla** kur — küçük örnekle geçen test, hiçbir şey kanıtlamaz.

### 5. Türkçe karakterler CI'ı iki kez kırdı
- GraalVM: Türkçe locale'de `"DARWIN".toLowerCase()` → `darwın` (noktasız ı) → `jni_md.h` bulunamadı.
- WiX/MSI: `ş İ ı ğ` **code page 1252'de yok** → `light.exe` LGHT0311 → Windows paketi hiç üretilmedi.

**Kural:** Derleme zincirine giden her dizede Türkçe karakterlere dikkat. Locale'i açıkça sabitle
(`-J-Duser.language=en`). `ç ö ü` sorun değil; `ş Ş ı İ ğ Ğ` tehlikeli.

### 6. Mimari/CPU varsayımları
GraalVM native-image x64'te varsayılan olarak **AVX2** gibi modern komutları hedefler. Böyle bir ikili
Windows-on-ARM emülasyonunda ve eski CPU'larda **ilk komutta ölür**. Semptom tamamen alakasız görünür.

**Kural:** Kullanıcı "çalışmıyor" derse **mimarisini sor** (`uname -m`, VM mi?). Günlükteki oturum
künyesi zaten yazıyor — **önce oraya bak.** Sidecar x64 hedeflerde `-march=compatibility` ile
derlenir; kaldırma.

### 7. Svelte'in `<script>` ön-tarama tuzağı
Kod **yorumunun içine** bile bitişik bir `script`/`style` etiketi yazma — Svelte'in blok-sınırı ön
taraması yanlış pozitif verip bileşenin script bloğunu erken kapatır. `SCRIPT_TAG`/`STYLE_TAG`
değişkeniyle interpolasyon kullan. (Bu direktifte yazılı olmasına rağmen iki kez düştüm.)

### 8. Hata ayıklama disiplini
- **Önce günlüğü iste.** (Ayarlar → Hakkında → "Günlük klasörünü aç".) Oturum künyesinde sürüm + OS +
  **mimari** var.
- **Tahminle üst üste tag atma.** Sebebi bilmeden yayınlanan her sürüm, hem zaman hem de sürüm numarası
  israfıdır. Önce `workflow_dispatch` ile bir teşhis koşusu yaz, ölç, sonra düzelt.
- Tauri, `light.exe` gibi alt araçların stderr'ini **yutar** → `tauri build --verbose` gerekir.

## Otodökümantasyon (otomatik — hatırlatma bekleme)
Her değişiklik sonrası:
- **CHANGELOG.md:** `## [X.Y.Z] — YYYY-MM-DD — [Özet] — [Etkilenen dosya/klasör]` (bkz. mevcut girdiler için stil).
- **FEATURES.md:** Yeni yetenek veya mantık değişikliğinde güncelle.
- **INSTALL.md:** npm / Cargo bağımlılığı veya kurulum adımı değiştiğinde senkronize et.
- Semantic versioning: breaking=MAJOR, yeni özellik=MINOR, düzeltme=PATCH.

## Versiyon Yönetimi (kritik — her release'de uygulanmalı)
Versiyon **4 dosyada** senkron tutulmalı:
1. `app/package.json` → `"version"`
2. `app/src-tauri/Cargo.toml` → `[package] version`
3. `app/src-tauri/tauri.conf.json` → `"version"`
4. `src/eFaturaEdit.Core/eFaturaEdit.Core.csproj` → `<Version>`
- `app/src-tauri/Cargo.lock` cargo tarafından otomatik güncellenir, elle dokunma.
- Program içi görünen versiyon (`app/src/lib/data/manifest.json`) **elle düzenlenmez** —
  kaynağı `src/eFaturaEdit.DataExport/Program.cs` içindeki sabit `Version:` değeridir;
  onu güncelleyip `npm run data:sync` ile yeniden üret. (4 dosya + bu = 5 nokta senkron.)
- `CHANGELOG.md` → `## [X.Y.Z] - YYYY-MM-DD` girdisi.
- Versiyon değişikliğinde ilgili tüm dosyalar **birlikte** güncellenmelidir.

## Release Yayınlama (her yeni versiyonda ZORUNLU — hatırlatma bekleme)
Her versiyon artışı sonrası uygulama otomatik derlenip **GitHub Release** olarak yayınlanır:
1. Yukarıdaki 5 versiyon noktasını güncelle + `CHANGELOG.md` girdisi ekle.
2. Değişiklikleri commit + `git push origin master`.
3. Anlamsal versiyon etiketi oluştur ve push et: `git tag -a vX.Y.Z -m "..."` → `git push origin vX.Y.Z`.
4. `v*` etiketi push'u `.github/workflows/release.yml` (tauri-action) CI'sını tetikler;
   **4 runner** (macOS arm64, macOS Intel, Linux, Windows) derleyip paketleri
   (`.dmg`/`.msi`/`.exe`/`.deb`/`.rpm`/`.AppImage`) tek bir public GitHub Release'e ekler.
5. **Bitince release'i DOĞRULA:** `gh release view vX.Y.Z --json assets` — 4 platformun da
   varlığı listede mi? Bir iş sessizce takılırsa release yine de yayımlanır ama **eksik olur**.
- macOS **universal ikili üretilemez** (GraalVM native-image tek mimari derler) — arm64 ve Intel
  ayrı runner'larda, ayrı `.dmg` olarak çıkar.
- ⚠️ **Runner etiketleri emekliye ayrılır.** `macos-13` kaldırıldığında Intel işine runner
  atanmadı ve **sonsuza dek kuyrukta** bekledi (hata vermez, sadece asılı kalır!) — v2.17.0–v2.20.0
  release'leri bu yüzden Intel `.dmg` olmadan çıktı. Bir iş saatlerce `queued` kalıyorsa
  ilk şüphelenilecek şey runner etiketidir; GitHub'ın güncel listesiyle karşılaştır.
  Geçerli Intel etiketi: **`macos-15-intel`**.
- ⚠️ **MSI'da Türkçe karakter tuzağı (CP1252).** WiX, MSI dizelerini **code page 1252** (Latin-1) ile
  yazar ve Türkçe'nin **`ş Ş ı İ ğ Ğ`** harfleri bu kod sayfasında **yoktur**. `tauri.conf.json`'daki
  `productName` veya `bundle.fileAssociations[].description` bu harfleri içerirse `light.exe`
  **LGHT0311** ile çöker, Windows paketi hiç üretilmez (v2.21.0'da oldu). `candle` geçip `light`
  çökerse ilk şüpheli budur — ayrıca Tauri, `light.exe`'nin hatasını yutar; sebebi görmek için
  `tauri build --verbose` gerekir. Release CI'da artık **saniyesinde yakalayan bir CP1252 kontrolü**
  var. `ç ö ü â é` CP1252'de VARDIR, sorun değildir; `name` alanı yalnızca macOS `Info.plist`'ine
  gider (UTF-8) ve Türkçe kalabilir.
- 🔑 **Updater imza anahtarı (KAYBEDİLEMEZ).** Otomatik güncelleme, paketleri bir minisign anahtarıyla
  imzalar; uygulama, `tauri.conf.json`'a gömülü **açık anahtarla** doğrulayamadığı hiçbir güncellemeyi
  kurmaz. Özel anahtar GitHub Secret'larında (`TAURI_SIGNING_PRIVATE_KEY` + `..._PASSWORD`), yerel
  yedeği `~/.tauri/efaturaedit.key`(+`.password`). **Bu anahtar kaybolursa kurulu uygulamalara bir daha
  güncelleme gönderilemez** — herkes elle yeni sürüm indirmek zorunda kalır. Açık anahtarı değiştirmek
  de aynı sonucu doğurur: eski kurulumlar yeni imzayı reddeder. Anahtarı asla rotate etme.
- Updater paketleri `bundle.createUpdaterArtifacts: true` ile üretilir ve `includeUpdaterJson: true`
  release'e `latest.json` ekler. Uygulama bu dosyayı
  `releases/latest/download/latest.json` adresinden okur → **release taslak (draft) bırakılmamalı**,
  yoksa "latest" onu göstermez ve güncelleme akışı sessizce durur.
- ⚠️ **"Resource not accessible by integration" (403) — release oluşturulamıyor.** v2.22.1'de dört işin
  dördü de bu hatayı verdi. **İzin sorunu DEĞİLDİ:** başarılı koşuyla karşılaştırıldığında token'a
  aynı `Contents: write` verilmişti; kural seti / etiket koruması yoktu, depo aktifti, GitHub'da arıza
  bildirilmemişti. Hata **yalnızca release'i OLUŞTURMA** çağrısında çıktı; release elle oluşturulunca
  aynı token varlıkları sorunsuz yükledi. Çözüm:
  1. `gh release create vX.Y.Z --title ... --notes "<CHANGELOG bölümü>"` ile release'i elle oluştur.
  2. `gh run rerun <id> --failed` ile işleri tekrarla → varlıklar mevcut release'e yüklenir.
  - **Sonra `latest.json`'u MUTLAKA doğrula:** her iş kendi platformunu ekler; bazı işler başarısız
    kalırsa manifest **eksik platformla** yayınlanır ve o platformdaki kullanıcılar güncellemeyi hiç
    görmez (sessiz başarısızlık!). Kontrol:
    `curl -sL .../releases/latest/download/latest.json` → 11 platform girdisi olmalı.
- ⚠️ **Sidecar CPU komut seti (`-march`).** GraalVM native-image x64'te varsayılan olarak **modern
  komutları (AVX2 vb.)** hedefler. Böyle bir ikili, Windows'un ARM üzerindeki x64 emülasyonunda
  (Prism) ve eski CPU'larda **ilk komutta, hata bile veremeden ölür** — semptom "Boru sonlandı
  (os error 109)" gibi alakasız görünür. `sidecar/build.sh` bu yüzden x64 hedeflerde
  **`-march=compatibility`** kullanır; kaldırma.
- ⚠️ **GraalVM native-image, Windows/ARM64'ü desteklemiyor** — o platform için yerel Saxon ikilisi
  üretilemez. Uygulama orada XSLT 1.0'a düşer ve bunu kullanıcıya açıkça söyler (sessiz düşüş
  YASAK: 1.0 işlemcisi 2.0 komutlarını hata vermeden yok sayar, çıktı sessizce yanlış olur).
- Bu Mac'te 4 platform yerel derlenemez (cross-compile yok) — dağıtım **daima** bu CI ile yapılır.

## Günlükleme (v2.23.0+)
- `tauri-plugin-log` → dosyaya yazar; **Ayarlar → Hakkında → "Günlük klasörünü aç"**.
- Kullanıcı bir sorun bildirdiğinde **İLK İSTENECEK ŞEY günlük dosyasıdır.** Oturum künyesinde
  sürüm + işletim sistemi + **mimari** var — bu satır, "Windows'ta çalışmıyor" vakasını saatler
  yerine saniyeler içinde çözerdi.
- Yeni bir dış süreç/ağ çağrısı eklerken **mutlaka logla**: girdiler (boyut), çıkış kodu, stderr, süre.
  Bu projede üç ayrı hata sırf sessiz oldukları için saatler kaybettirdi.
- Etiket zaten varsa: `git tag -d vX.Y.Z && git push origin :vX.Y.Z` ile silip yeniden oluştur.
  **Ancak** o etiketin release'i yayımlanmış/derleniyorsa silme — bir sonraki yamayı yeni sürüm
  (`X.Y.Z+1`) olarak çıkar.

## Git İş Akışı & Commit Kuralları
- **Commit mesajı formatı:** `[tip]: kısa açıklama` (örn: `fix: statik alan sırası düzeltildi`, `feat: AI destekli XSLT önerileri`)
  - Tipler: `feat`, `fix`, `refactor`, `docs`, `chore`, `style`, `test`
- Her değişiklik sonrası **commit öncesi kontrol listesi:**
  1. Dokunulan taraf hatasız derleniyor mu? (`dotnet build` veya `npm run check` + `npm run tauri build`)
  2. CHANGELOG.md güncellendi mi?
  3. Versiyon numarası senkron mu?
- **Branch stratejisi:** `master` → kararlı. Büyük özellikler için `feature/*`, hata düzeltmeleri için `fix/*` düşün.
- **Push öncesi:** `git pull --rebase` ile güncel kalınmalı.
- **Tag:** Her release'de `vX.Y.Z` formatında tag oluşturulmalı: `git tag -a vX.Y.Z -m "Release X.Y.Z"`.
- **Lisans: MIT.** Yeni dosyalara lisans başlığı eklemek zorunlu değil (kök `LICENSE` tüm depoyu kapsar).
- Cross-compile build çıktıları (`target/`, `build/`, `node_modules/`) commit'lenmez, `.gitignore`'da tutulur.

## README.md Güncelleme Kuralları
- **Her önemli değişiklikte** README.md güncellenmeli:
  - Yeni özellik eklendi → "Özellikler" bölümüne ekle.
  - Bağımlılık değişti (npm, Cargo) → "Gereksinimler" bölümünü güncelle.
  - Kurulum/yapılandırma değişti → "Kurulum" bölümünü güncelle.
  - API/kullanım değişti → "Kullanım" bölümünü güncelle.
- README.md yapısı: `Proje Adı` → `Açıklama` → `Özellikler` → `Gereksinimler` → `Kurulum` → `Kullanım` → `Lisans`.
- Ekran görüntüleri `docs/images/` klasöründe tutulmalı.

## Yanıt Formatı
1. Değişiklik özeti (1-2 cümle) — hangi tarafı (Core/Tauri) etkilediğini belirt.
2. Sadece değişen kod bloğu
3. Dokümantasyon güncellemeleri
4. Onay noktası
