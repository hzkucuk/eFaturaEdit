# E-FaturaEdit — Proje Direktifi

> **Tam direktif:** [.github/copilot-instructions.md](.github/copilot-instructions.md) — mimari, kod
> standartları, versiyon/release süreci, günlükleme. **Çalışmaya başlamadan önce oku.**
>
> @.github/copilot-instructions.md

Türkiye e-Fatura / e-Arşiv / e-İrsaliye (UBL-TR) XSLT dizayn editörü.
Tauri v2 + SvelteKit (Svelte 5) + CodeMirror 6 + Saxon-HE (GraalVM native sidecar). MIT.

---

## ⛔ ÖNCE BUNU OKU — tekrar eden hatalar

Aşağıdakilerin **hepsi bu projede gerçekten oldu** ve saatler kaybettirdi. Bir sorunla
karşılaştığında önce bu listeye bak; büyük ihtimalle aynı sınıftan.

1. **"Hata vermiyor" ≠ "çalışıyor".** En pahalı hatalar sessizdi: Intel yapısı hiç üretilmiyordu
   (iş sonsuza dek kuyrukta), Windows paketi hiç çıkmıyordu, AI yanıtı buharlaşıyordu — hiçbirinde
   ekranda hata yoktu. **Bir işin bittiğini varsayma, çıktısını say/doğrula.**

2. **Semptomu değil sebebi göster.** Bir dış süreç ölürse **her zaman** exit code + stderr topla.
   Windows'ta bunu yapmadığımız için gerçek sebep (CPU komut seti) yerine alakasız bir mesaj
   ("Boru sonlandı") gösterdik. Çıkış kodu tanının yarısıdır.

3. **Sessiz geri düşüş YASAK.** Saxon yoksa XSLT 1.0'a düşüyoruz; 1.0 işlemcisi 2.0 komutlarını
   **hata vermeden yok sayar** → fatura sessizce yanlış basılır. Düşüşü daima görünür kıl.

4. **Kendi teşhis aracına da güvenme.** Teşhis betiğim iki kez kendi hatasından çöktü; "sidecar bozuk"
   diye okusaydım yanlış yola sapardım. Ayrıca **gerçek boyutla** test et (şablonlar ~600 KB).

5. **Türkçe karakterler CI'ı iki kez kırdı.** `ş İ ı ğ` → GraalVM locale hatası, WiX/MSI code page
   1252 hatası. `ç ö ü` sorun değil; `ş Ş ı İ ğ Ğ` tehlikeli.

6. **Mimariyi sor.** GraalVM x64 ikilisi varsayılan olarak AVX2 hedefler → Windows-on-ARM
   emülasyonunda ve eski CPU'larda ilk komutta ölür. `-march=compatibility` şart.

7. **Svelte `<script>` ön-tarama tuzağı:** kod **yorumunun içinde** bile bitişik `script`/`style`
   etiketi yazma — bileşenin script bloğunu erken kapatır.

8. **Sorun bildirilince İLK İSTENECEK ŞEY günlüktür** (Ayarlar → Hakkında → "Günlük klasörünü aç").
   Oturum künyesinde sürüm + OS + **mimari** var. Tahminle üst üste tag atma; önce ölç.

---

## Sık kullanılan

```bash
cd app && npm run tauri dev     # geliştirme
cd app && npm run check         # svelte-check (0 hata olmalı)
cd app/src-tauri && cargo build # Rust
```

**Release:** 5 versiyon noktasını güncelle + CHANGELOG → commit → `git tag vX.Y.Z` → push.
CI 4 platformda derler. **Bitince `latest.json`'da 11 platform girdisini DOĞRULA** — eksikse o
platformdaki kullanıcılar güncellemeyi hiç görmez. Ayrıntı: `.github/copilot-instructions.md`.
