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
- Bu Mac'te 4 platform yerel derlenemez (cross-compile yok) — dağıtım **daima** bu CI ile yapılır.
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
