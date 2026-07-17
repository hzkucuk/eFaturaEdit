# Kurulum Rehberi (Installation)

## Solution Yapısı

```
eFaturaEditSolution.sln
├── src/
│   ├── eFaturaEdit.Core/               ← cross-platform çekirdek
│   │   ├── Snippets/                   UBL-TR snippet'ler + SnippetInfo
│   │   ├── Samples/                    UBL-TR örnek XML kataloğu
│   │   ├── Completion/                 Autocomplete verisi + helpers
│   │   ├── Platform/                   IHardwareIdProvider (interface)
│   │   └── eFaturaEdit.Core.csproj     SDK-style, netstandard2.0;net10.0
│   │
│   └── eFaturaEdit.DataExport/         Core → JSON export tool (net10.0 konsol)
│
└── app/                                 ← Tauri masaüstü uygulaması (macOS/Linux/Windows)
    ├── src/                            SvelteKit + TypeScript frontend
    │   ├── lib/                        CodeEditor, Splitter, HelpModal, drag, settings, ...
    │   ├── lib/data/                   Core'dan üretilen JSON'lar (npm run data:sync)
    │   └── routes/                     Ana sayfa + /settings
    ├── src-tauri/                       Rust backend (Tauri v2)
    └── static/samples/                  Örnek XSLT/XML dosyaları
```

## Gereksinimler

| Bileşen | Minimum Versiyon |
|---|---|
| .NET SDK | 10.0 (Core + DataExport için) |
| Rust | 1.97+ (stable toolchain) |
| Node.js | 20+ (test edilen: 26) |
| npm | 10+ |
| Xcode Command Line Tools | macOS derlemesi için |
| WebKitGTK 4.1 + GTK3 (Linux derlemesi için) | `libwebkit2gtk-4.1-dev libgtk-3-dev librsvg2-dev libayatana-appindicator3-dev` |

> **Çalışma-zamanı, opsiyonel — Claude Code motoru (v2.35.0).** Bu opt-in mod için ek bir
> **derleme** bağımlılığı yoktur (Rust tarafındaki `interprocess`/`sha2`/`flate2`/`tar` saf-Rust'tır,
> otomatik derlenir). Yalnızca **Linux'ta** motorun bash sandbox'ı `bubblewrap` gerektirir; kurulu
> değilse motor sandbox'sız çalışmaz, **görünür bir hata verir** (sessizce korumasız koşmaz). Kur:
> `sudo apt install bubblewrap` (Debian/Ubuntu). macOS'ta yerleşik `sandbox-exec` kullanılır;
> Windows'ta sandbox yoktur (orada bash her seferinde onaya sunulur).

## npm Paketleri (Tauri App)

| Paket | Amaç |
|---|---|
| @tauri-apps/api, @tauri-apps/cli | Tauri v2 JS API + CLI |
| @tauri-apps/plugin-dialog, plugin-fs, plugin-opener | Dosya dialog/okuma-yazma/harici açma |
| @tauri-apps/plugin-clipboard-manager | Editör sağ tık menüsünde Kes/Kopyala/Yapıştır (pano OS üzerinden) |
| @sveltejs/kit, adapter-static, svelte, vite | SvelteKit frontend |
| codemirror + @codemirror/{view,state,commands,language,lang-xml,lang-html,autocomplete,search,theme-one-dark} | Kod editörü |
| thememirror, @lezer/highlight | Editör temaları + syntax highlight |

## Kurulum Adımları

Aşağıdaki adımlar tüm platformlarda ortaktır (macOS örnek alınmıştır).

1. **Rust kurun** (eğer kurulu değilse):
   ```bash
   curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh
   source "$HOME/.cargo/env"
   ```

2. **Node.js 20+ kurun** (nvm, Homebrew veya resmi installer ile).

3. **.NET 10 SDK kurun** (Core + DataExport tool'u için).

4. npm bağımlılıklarını kurun:
   ```bash
   cd app
   npm install
   ```

5. Core verilerini JSON'a senkronlayın (ilk kurulumda ve Core değiştikçe):
   ```bash
   npm run data:sync
   ```

6. Geliştirme modunda çalıştırın:
   ```bash
   npm run tauri dev
   ```

   İlk çalıştırmada Rust bağımlılıkları derlenir (~1 dakika); sonraki
   çalıştırmalar saniyeler içinde açılır.

7. Üretim build'i almak için:
   ```bash
   npm run tauri build
   ```

   Çıktı: `app/src-tauri/target/release/bundle/` altında platforma özgü
   yükleyici (macOS: `.app`/`.dmg`, Windows: `.exe`/NSIS, Linux: `.deb`/`.rpm`/`.AppImage`).

### macOS'ten Windows Cross-Compile

macOS'ten Windows `.exe` + NSIS installer üretmek mümkün (deneysel, Tauri'nin
kendisi de böyle işaretliyor):

```bash
rustup target add x86_64-pc-windows-msvc
cargo install --locked cargo-xwin
brew install llvm makensis
export PATH="/opt/homebrew/opt/llvm/bin:$PATH"
cd app
npx tauri build --target x86_64-pc-windows-msvc --runner cargo-xwin --bundles nsis
```

İmzalanmamış installer üretir (Windows SmartScreen uyarısı gösterir) — dağıtım
öncesi bir code-signing sertifikası düşünülmeli.

### macOS'ten Linux Cross-Compile (Docker ile)

WebKitGTK gerçek native kütüphane gerektirdiği için ham cross-compile yerine
gerçek bir Linux ortamı (Docker) kullanılmalı:

```bash
docker run --rm --platform linux/amd64 -v "$(pwd)":/work ubuntu:24.04 bash -c '
  apt-get update -qq && DEBIAN_FRONTEND=noninteractive apt-get install -y -qq \
    build-essential curl wget file pkg-config libwebkit2gtk-4.1-dev libgtk-3-dev \
    librsvg2-dev libayatana-appindicator3-dev libssl-dev patchelf nodejs npm \
    fuse libfuse2 &&
  curl --proto "=https" --tlsv1.2 -sSf https://sh.rustup.rs | sh -s -- -y &&
  source "$HOME/.cargo/env" && cd /work/app && npm run tauri build
'
```

AppImage adımı Docker'da FUSE eksikliği yüzünden başarısız olabilir (`--device
/dev/fuse --cap-add SYS_ADMIN` ile denenebilir); `.deb`/`.rpm` her durumda
güvenilir şekilde üretilir.

## Yapılandırma (Tauri App)

- **Bundle identifier:** `com.zaferbilgisayar.efaturaedit` — [tauri.conf.json](app/src-tauri/tauri.conf.json)
- **Capability/izinler:** [app/src-tauri/capabilities/default.json](app/src-tauri/capabilities/default.json) — dosya sistemi kapsamı (`$HOME`, `$DOCUMENT`, `$APPDATA` vb.), dialog, pencere kapatma izinleri
- **DevTools:** Release build'de de kasıtlı olarak açık (`"devtools": true` + `tauri` crate'inde `devtools` feature) — önizlemedeki "Stili XSLT'ye Al" özelliği buna dayanıyor.
- **Kullanıcı ayarları:** Tarayıcı `localStorage`'da saklanır (`efaturaEdit.settings.v3`, `efaturaEdit.recentFiles.v1`)
- **Kullanıcı verisi:** `$APPDATA/samples/` (kullanıcı örnekleri), `$APPDATA/user-snippets.json` (kullanıcı snippet'leri)
- **Önizleme geçici dosyaları:** `$APPLOCALDATA/preview/` (yazdırma için tarayıcıya açılan HTML'ler)
