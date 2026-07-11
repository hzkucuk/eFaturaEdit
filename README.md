# E-FaturaEdit

Türkiye e-Fatura / e-Arşiv / e-İrsaliye (UBL-TR) belgeleri için XSLT tasarım
düzenleyicisi. GİB'in gönderdiği ham XML verisini, kendi HTML/CSS tasarımınızla
görsel bir belgeye dönüştüren XSLT şablonlarını yazmanızı ve canlı önizlemesini
görmenizi sağlar.

## Özellikler

- **CodeMirror 6 tabanlı XSLT/XML editörü** — syntax highlight, 11 tema, autocomplete (242 öneri), Türkçeleştirilmiş arama.
- **149 hazır UBL-TR snippet'i** (e-Fatura, e-Arşiv, e-İrsaliye) + kullanıcı tanımlı snippet ekleme/düzenleme/silme.
- **Sürükle-bırak** snippet ekleme (editöre veya doğrudan önizlemeye).
- **Canlı önizleme** — responsive boyut/zoom, sağ tık menüsü, DevTools ile CSS düzenleyip tek tıkla XSLT'ye aktarma.
- **17 GİB resmi örnek senaryosu** + kullanıcı kendi örnek klasörünü yönetebilir.
- **Otomatik dönüştür, otomatik kaydet, çıkışta kaydetme kontrolü.**
- **Kapsamlı yardım sistemi** (F1) — aranabilir, sidebar navigasyonlu dokümantasyon.
- **Cross-platform:** macOS, Windows, Linux — tek kod tabanı (Tauri v2 + SvelteKit).

- **🤖 AI Asistan (BYOK)** — Claude / ChatGPT / Gemini / Ollama / NVIDIA ile XSLT/XML tasarımına yardım. Hedefli bul/değiştir düzenlemeleri, kendi kendine düzelten ajan modu, onay öncesi canlı önizleme; görsel/PDF/dosya ekleme (vision). Anahtarlar **OS anahtar zincirinde şifreli** saklanır, hiçbir anahtar gömülü/paylaşılı değildir.
- **CSS Stilleri snippet kategorisi** — hazır metin/kutu/yerleşim/tablo/sayfa CSS kuralları.

Detaylı liste için [FEATURES.md](FEATURES.md).

## Ekran Görüntüleri

<!-- Görseller docs/screenshots/ altına eklenince görünür. -->
| Ana pencere | AI Asistan |
| :---: | :---: |
| ![Ana pencere](docs/screenshots/main.png) | ![AI Asistan](docs/screenshots/ai-assistant.png) |
| **AI önerisini uygula (diff + önizleme)** | **Ayarlar** |
| ![AI uygula](docs/screenshots/ai-apply.png) | ![Ayarlar](docs/screenshots/settings.png) |

## Gereksinimler

- Rust (stable), Node.js 20+, .NET 10 SDK (yalnızca veri senkronizasyon aracı için)

Tam liste ve platforma özgü notlar için [INSTALL.md](INSTALL.md).

## Kurulum

```bash
cd app
npm install
npm run data:sync
npm run tauri dev
```

Üretim build'i için `npm run tauri build`. Ayrıntılar ve cross-compile
talimatları (macOS'ten Windows/Linux derleme dahil) için [INSTALL.md](INSTALL.md).

## Kullanım

1. **📂 XSLT** / **📄 XML** ile dosya açın, veya **🎲 Örnek** menüsünden hazır bir senaryo yükleyin.
2. Sol panelden snippet sürükleyip bırakın veya tıklayarak ekleyin.
3. **▶ Dönüştür** ile önizlemeyi güncelleyin (varsayılan olarak otomatik da çalışır).
4. **Cmd/Ctrl+S** ile kaydedin — sözdizimi hatası varsa imleç otomatik olarak hatalı satıra gider.
5. Yardım için **F1**.

## Katkıda Bulunma

Bu proje MIT lisansı ile açık kaynaktır. Hata bildirimi, öneri ve pull request'ler memnuniyetle karşılanır.

## Lisans

[MIT](LICENSE) © 2026 Hüseyin Küçük
