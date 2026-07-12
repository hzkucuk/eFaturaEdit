# E-FaturaEdit — Proje Direktifi

Türkiye e-Fatura / e-Arşiv / e-İrsaliye (UBL-TR) XSLT dizayn editörü.
Tauri v2 + SvelteKit (Svelte 5 runes) + CodeMirror 6 + Saxon-HE (GraalVM native sidecar). MIT.

**Öncelik:** Güvenlik > Mimari bütünlük > Stabilite > Performans

---

## ⛔ ÖNCE BUNU OKU — acı dersler

Aşağıdakilerin **hepsi bu projede gerçekten oldu** ve saatler kaybettirdi. Yeni bir hatayla
karşılaşınca **önce buraya bak** — muhtemelen aynı sınıftan.

### 1. Sessiz başarısızlıklar — "hata vermiyor" ≠ "çalışıyor"
En pahalı hataların ortak özelliği: **hiç hata vermiyorlardı, sadece iş görmüyorlardı.**
- `macos-13` emekli olunca Intel işi **sonsuza dek kuyrukta bekledi** → release yayınlandı ama Intel
  `.dmg` yoktu. Dört sürüm boyunca fark edilmedi.
- Windows MSI, açıklamadaki Türkçe `ş` yüzünden çöktü → release çıktı ama **Windows paketi yoktu**.
- Bir CI işi düşerse `latest.json` **eksik platformla** yayınlanır → o platformdaki kullanıcılar
  güncellemeyi **hiç görmez**, hata da almaz.
- AI yanıtı, bileşen unmount olunca **buharlaşıyordu** — ekranda hiçbir hata yoktu.
- `Core.csproj` sürümü 5 sürüm boyunca geride kaldı — direktifte yazılı olmasına rağmen.

**Kural:** Bir işin bittiğini varsayma, **çıktısını doğrula/say.** "Başarılı" gözüken bir koşu eksik
ürün çıkarmış olabilir.

### 2. Semptomu değil sebebi göster — hata yutma
`xslt.rs`, stdin yazması başarısız olunca **hemen dönüyor**, sidecar'ın `stderr`'ini ve **çıkış
kodunu okumuyordu**. Kullanıcıya "Boru sonlandı (os error 109)" diyorduk — gerçek sebeple (CPU komut
seti) hiç ilgisi olmayan bir mesaj. Saatler buna gitti.

**Kural:** Dış süreç ölürse **her zaman** exit code + stderr topla; yazma hatasında bile önce süreci
drain et. Çıkış kodu tanının yarısıdır:
`-1073741515` (0xC0000135) → eksik DLL · `-1073741795` (0xC000001D) → geçersiz komut (CPU/emülasyon)
· `-1073741819` (0xC0000005) → erişim ihlali.

### 3. Sessiz geri düşüş (fallback) YASAK
Saxon yoksa tarayıcının XSLT **1.0** işlemcisine düşülür. 1.0 işlemcisi `format-dateTime`,
`tokenize`, `for-each-group` gibi 2.0 komutlarını **hata vermeden yok sayar** → kullanıcı şablonunun
çalıştığını sanır, **fatura sessizce yanlış basılır.** Bu, hata vermekten beterdir.

**Kural:** Düşüşü daima **görünür** kıl (uyarı bandı + footer rozeti + günlük). "Motor çalışmıyor" ile
"şablon hatalı" durumlarını **ayır** (`XSLT_ENGINE_UNAVAILABLE`) — şablon hatasında geri düşmek,
kullanıcının hatasını gizlemektir. Ayrımı **metin eşleştirmeyle yapma**; ilk sürümde öyleydi ve
Windows'un hata metni hiçbir kalıba uymadığı için uygulama tamamen çuvalladı.

### 4. Kendi test/teşhis aracına da güvenme
Windows teşhis betiği **iki kez kendi hatasından** çöktü (PowerShell dizi döndürünce "açar" ve
`Object[]` yapar). "Sidecar bozuk" diye okunsaydı tamamen yanlış yola sapılırdı. Küçük yükle
(300 bayt) yazılan duman testi de yanıltıcıydı — gerçek dosya **600 KB**.

**Kural:** Bir test "başarısız" derse önce **testin kendisini** doğrula. Testi **gerçek boyut ve
gerçek koşullarla** kur.

### 5. Türkçe karakterler CI'ı iki kez kırdı
- GraalVM: Türkçe locale'de `"DARWIN".toLowerCase()` → `darwın` (noktasız ı) → `jni_md.h` bulunamadı.
- WiX/MSI: `ş İ ı ğ` **code page 1252'de yok** → `light.exe` LGHT0311 → Windows paketi hiç üretilmedi.

**Kural:** Derleme zincirine giden her dizede dikkat. Locale'i sabitle (`-J-Duser.language=en`).
`ç ö ü â é` sorun değil; **`ş Ş ı İ ğ Ğ`** tehlikeli.

### 6. Mimari / CPU varsayımları
GraalVM native-image x64'te varsayılan olarak **AVX2** gibi modern komutları hedefler. Böyle bir ikili
Windows-on-ARM emülasyonunda (Prism) ve eski CPU'larda **ilk komutta, hata bile veremeden ölür**.

**Kural:** Kullanıcı "çalışmıyor" derse **mimarisini sor** (VM mi? ARM mı?). Günlükteki oturum künyesi
zaten yazıyor — **önce oraya bak.** `sidecar/build.sh` x64 hedeflerde `-march=compatibility` kullanır;
kaldırma. GraalVM **Windows/ARM64'ü desteklemiyor** — orada XSLT 1.0'a düşülür ve açıkça bildirilir.

### 7. Svelte `<script>` ön-tarama tuzağı
Kaynak dosyada (yorum, regex, string fark etmez) bitişik `script`/`style` etiketi **yazma** — Svelte'in
blok-sınırı ön taraması yanlış pozitif verip bileşenin script bloğunu erken kapatır.
`SCRIPT_TAG`/`STYLE_TAG` değişken-interpolasyonu kullan. (Direktifte yazılıydı, yine de iki kez düşüldü.)

### 8. Iframe sınırı
Önizleme bir **iframe**; içindeki tıklama/olaylar ana pencereye **ulaşmaz**. Sağ tık menüsü bu yüzden
kapanmıyordu. Iframe ile ana pencere arasındaki her etkileşim **postMessage köprüsünden** geçmeli.

### 9. Hata ayıklama disiplini
- **Önce günlüğü iste** (Ayarlar → Hakkında → "Günlük klasörünü aç"). Künyede sürüm + OS + **mimari** var.
- **Tahminle üst üste tag atma.** Önce `workflow_dispatch` ile teşhis koşusu yaz, **ölç**, sonra düzelt.
- Tauri, `light.exe` gibi alt araçların stderr'ini **yutar** → `tauri build --verbose` gerekir.

---

## Mimari

Depo **iki bağlı parçadan** oluşur:

1. **`src/eFaturaEdit.Core/`** + **`src/eFaturaEdit.DataExport/`** — Paylaşılan çekirdek
   (`netstandard2.0;net10.0`). Snippet/örnek/tamamlama kataloğu POCO'ları burada yaşar; DataExport
   bunları `app/src/lib/data/*.json`'a aktarır. **UI veya dosya sistemi çağrısı yok**, sıfır dış NuGet.
2. **`app/`** — Masaüstü uygulaması: Tauri v2 (Rust) + SvelteKit + Svelte 5 + TypeScript + CodeMirror 6.

- SvelteKit dosya-tabanlı routing (`src/routes/`); Tauri komutları yalnızca `src-tauri/src/`'de.
- Frontend ⇄ Rust köprüsü **sadece** `invoke()` / `@tauri-apps/plugin-*` üzerinden. DOM'dan doğrudan
  dosya sistemine erişim yok.
- Katman ihlali yasak. Yeni pattern eklemeden önce gerekçe sun.
- Bir parçadaki değişiklik diğerini etkiliyorsa (Core'da alan → DataExport JSON → `data/types.ts`)
  **zincirin tamamını** güncelle.

> Eski .NET Framework/WinForms + DevExpress uygulaması (`eFaturaEdit/`) bu projenin öncüsüydü;
> lisans/aktivasyon sistemiyle birlikte **kaldırıldı**. İstenmedikçe referans verme.

**AI entegrasyonu:** Kullanıcının kendi API anahtarıyla (BYOK) çalışır; hiçbir anahtar gömülmez.
AI çıktısı yalnızca **"önerilen değişiklik"** olarak sunulur ve **kullanıcı onayı olmadan uygulanmaz** —
dosya sistemine, ayarlara veya başka bir işleve doğrudan erişemez.

## Temel Kurallar
- Sadece istenen bloğu değiştir; tüm dosyayı yeniden yazma.
- Public API imzalarını (C# public method, `#[tauri::command]`, Svelte `Props`) açık talimat olmadan
  değiştirme.
- Talep dışı refactor yapma. Belirsizlikte sor. Büyük değişiklikleri parçala.

## Kod Standartları

### C# (`src/`)
- `Task.Result` / `.Wait()` **yasak**; her zaman `await`. `CancellationToken` varsa alt çağrılara ilet.
- Gereksiz `ToList()`/`ToArray()` yok. Magic number yok.
- `Nullable` **kapalı** (`<Nullable>disable</Nullable>`) — NRT varsayma; `if (x == null) throw` kullan.

### Rust / TypeScript / Svelte (`app/`)
- Rust: `.unwrap()`/`.expect()` yalnızca gerçekten panic edilmesi gereken yerde (init); kullanıcı
  girdisi/IO hatasında `Result` + `?`.
- Yeni Tauri komutu native capability gerektiriyorsa **`src-tauri/capabilities/default.json`'a izni ekle.**
- Svelte 5 runes (`$state`, `$derived`, `$effect`) — eski `export let` / `$:` stiline dönme.
- **Bileşen unmount'unda ölmemesi gereken durumu (uçuştaki istek, "gönderiliyor" göstergesi) bileşende
  değil MODÜLDE tut** (bkz. `ai-sessions.svelte.ts`) — yoksa sayfa geçişinde yanıt buharlaşır.
- Yeni ayar `settings.svelte.ts`'e eklendiyse Ayarlar sayfasında gerçek bir kontrolle bağlanmadan
  commit'leme (ölü ayar bırakma).

## Veri & Dosya Formatları
- Veritabanı **yok** — kalıcılık dosya sistemi (XSLT/XML, `localStorage`, `$APPDATA/*.json`).
- `app/src/lib/data/*.json` **elle düzenlenmez** — `npm run data:sync` ile Core'dan üretilir.
- Kullanıcı verisi (örnekler, snippet'ler, API anahtarları) ayrı tutulur; bundled veriyle **asla**
  birleştirilip üzerine yazılmaz.
- **API anahtarları OS anahtar zincirinde** (Rust `keyring`), `localStorage`'da düz metin **yok**.

## Güvenlik & Hata Yönetimi
- Günlüklerde şifre/token/API anahtarı/PII **maskele**.
- Kullanıcıya stack trace gösterme; anlamlı **Türkçe** hata mesajı ver (`status()` deseni).
- Exception yutma; handle et veya ilet.
- **Repo public** — commit öncesi gerçek sır (API anahtarı, sertifika) var mı kontrol et.

## Günlükleme (v2.23.0+)
- `tauri-plugin-log` → dosyaya yazar; **Ayarlar → Hakkında → "Günlük klasörünü aç"**.
- Kullanıcı sorun bildirdiğinde **İLK İSTENECEK ŞEY günlük dosyasıdır.**
- Yeni bir **dış süreç / ağ çağrısı** eklerken **mutlaka logla**: girdiler (boyut), çıkış kodu, stderr,
  süre. Bu projede üç ayrı hata sırf sessiz oldukları için saatler kaybettirdi.

---

## Versiyon Yönetimi — **6 nokta senkron olmalı**

| # | Dosya | Alan |
| --- | --- | --- |
| 1 | `app/package.json` | `"version"` |
| 2 | `app/src-tauri/Cargo.toml` | `[package] version` |
| 3 | `app/src-tauri/tauri.conf.json` | `"version"` |
| 4 | `src/eFaturaEdit.Core/eFaturaEdit.Core.csproj` | `<Version>` |
| 5 | `src/eFaturaEdit.DataExport/Program.cs` | `Version:` sabiti |
| 6 | `app/src/lib/data/manifest.json` | `"version"` (5'ten üretilir) |

- `Cargo.lock` cargo tarafından güncellenir, elle dokunma.
- **Doğrula:** hepsinde aynı sürüm var mı? (#4 beş sürüm boyunca geride kaldı — sessizce.)
- `CHANGELOG.md` girdisi: `## [X.Y.Z] — YYYY-MM-DD — Özet`
- SemVer: breaking=MAJOR, özellik=MINOR, düzeltme=PATCH.

## Release Yayınlama (her yeni versiyonda ZORUNLU — hatırlatma bekleme)

1. 6 versiyon noktasını güncelle + `CHANGELOG.md` girdisi ekle.
2. `npm run check` (0 hata) + `cargo build` → commit + `git push origin master`.
3. `git tag vX.Y.Z` → `git push origin vX.Y.Z`.
4. `v*` etiketi `.github/workflows/release.yml`'i tetikler; **4 runner** (macOS arm64, macOS Intel,
   Linux, Windows) derler ve tek bir public Release'e ekler.
5. **BİTİNCE DOĞRULA:**
   - `gh release view vX.Y.Z --json assets` → 4 platformun paketleri var mı?
   - `curl -sL .../releases/latest/download/latest.json` → **11 platform girdisi** var mı?
     Eksikse o platformdaki kullanıcılar güncellemeyi **hiç görmez** (sessiz başarısızlık).

### Release tuzakları
- **macOS universal ikili üretilemez** (native-image tek mimari) — arm64 ve Intel ayrı `.dmg`.
- ⚠️ **Runner etiketleri emekliye ayrılır.** `macos-13` kaldırılınca Intel işine runner atanmadı ve
  **sonsuza dek kuyrukta** bekledi (hata vermez, asılı kalır!). Bir iş saatlerce `queued` kalıyorsa
  ilk şüpheli budur. Geçerli Intel etiketi: **`macos-15-intel`**.
- ⚠️ **MSI'da CP1252 tuzağı.** `productName` veya `fileAssociations[].description` içinde `ş Ş ı İ ğ Ğ`
  varsa `light.exe` **LGHT0311** ile çöker ve Windows paketi hiç üretilmez. `candle` geçip `light`
  çökerse ilk şüpheli budur. CI'da artık saniyesinde yakalayan bir kontrol var. `name` alanı yalnızca
  macOS `Info.plist`'ine gider (UTF-8), Türkçe kalabilir.
- ⚠️ **403 "Resource not accessible by integration"** — release *oluşturma* çağrısında çıkabilir.
  **İzin sorunu değildir** (token'da `Contents: write` vardır). Çözüm:
  1. `gh release create vX.Y.Z --title ... --notes "<CHANGELOG bölümü>"` ile elle oluştur.
  2. `gh run rerun <id> --failed` → varlıklar mevcut release'e yüklenir.
  3. `latest.json`'daki 11 platformu **doğrula**.
- 🔑 **Updater imza anahtarı — KAYBEDİLEMEZ.** Özel anahtar GitHub Secret'larında
  (`TAURI_SIGNING_PRIVATE_KEY` + `..._PASSWORD`), yedeği `~/.tauri/efaturaedit.key`(+`.password`).
  **Kaybolursa kurulu uygulamalara bir daha güncelleme gönderilemez.** Açık anahtarı değiştirmek de
  aynı sonucu doğurur — eski kurulumlar yeni imzayı reddeder. **Asla rotate etme.**
- Release **taslak (draft) bırakılmamalı** — `latest` onu göstermez, güncelleme akışı sessizce durur.
- Etiket zaten varsa `git tag -d` + `git push origin :vX.Y.Z` ile silinebilir — **ama** o etiketin
  release'i yayımlanmış/derleniyorsa **silme**, bir sonraki yamayı yeni sürüm olarak çıkar.
- Bu Mac'te 4 platform yerel derlenemez (cross-compile yok) — dağıtım **daima** CI ile.

## Git & Commit
- Format: `[tip]: kısa açıklama` — `feat`, `fix`, `refactor`, `docs`, `chore`, `style`, `test`.
- Commit öncesi: derleniyor mu? CHANGELOG güncel mi? Versiyon senkron mu?
- `master` → kararlı. Build çıktıları (`target/`, `build/`, `node_modules/`) commit'lenmez.
- **Lisans: MIT** (kök `LICENSE` tüm depoyu kapsar; dosya başlığı gerekmez).

## Dokümantasyon (otomatik — hatırlatma bekleme)
- **CHANGELOG.md** — her değişiklikte.
- **FEATURES.md** — yeni yetenek veya mantık değişikliğinde.
- **README.md** — özellik/bağımlılık/kurulum/kullanım değiştiğinde. Ekran görüntüleri `docs/images/`.
- **INSTALL.md** — npm/Cargo bağımlılığı veya kurulum adımı değiştiğinde.

## Sık kullanılan

```bash
cd app && npm run tauri dev      # geliştirme
cd app && npm run check          # svelte-check (0 hata olmalı)
cd app/src-tauri && cargo build  # Rust
cd app && npm run data:sync      # Core → JSON (manifest.json dahil)
```
