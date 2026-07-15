mod agent_tools;
mod ai;
mod xslt;

use std::sync::Mutex;
use tauri::{Emitter, Manager};

// Learn more about Tauri commands at https://tauri.app/develop/calling-rust/
#[tauri::command]
fn greet(name: &str) -> String {
    format!("Hello, {}! You've been greeted from Rust!", name)
}

/// WebView için geliştirici araçlarını aç.
///
/// Release build'de de kasıtlı olarak açık bırakılıyor: önizleme panelindeki
/// "Stili XSLT'ye Al" özelliği (bkz. +page.svelte) kullanıcının DevTools'un
/// Styles panelinden CSS düzenleyip bunu XSLT'ye aktarmasına dayanıyor —
/// bu yalnızca debug build'e kısıtlanırsa dağıtılan uygulamada işe yaramaz.
#[tauri::command]
fn open_devtools(webview_window: tauri::WebviewWindow) {
    webview_window.open_devtools();
}

/// OS anahtar zinciri servis adı — tüm gizli değerler bunun altında saklanır.
const SECRET_SERVICE: &str = "efatura-edit";

/// Gizli bir değeri (ör. AI API anahtarı) OS anahtar zincirinde şifreli sakla.
/// `account` = mantıksal ad (ör. "ai.anthropic"). Boş değer kaydı siler.
#[tauri::command]
fn secret_set(account: String, value: String) -> Result<(), String> {
    let entry = keyring::Entry::new(SECRET_SERVICE, &account)
        .map_err(|e| format!("Anahtar zinciri erişilemedi: {e}"))?;
    if value.is_empty() {
        // Boş değer = kaydı sil (yoksa hata verme).
        return match entry.delete_credential() {
            Ok(()) => Ok(()),
            Err(keyring::Error::NoEntry) => Ok(()),
            Err(e) => Err(format!("Anahtar silinemedi: {e}")),
        };
    }
    entry
        .set_password(&value)
        .map_err(|e| format!("Anahtar kaydedilemedi: {e}"))
}

/// Anahtar zincirinden gizli değeri oku (yoksa `None`).
#[tauri::command]
fn secret_get(account: String) -> Result<Option<String>, String> {
    let entry = keyring::Entry::new(SECRET_SERVICE, &account)
        .map_err(|e| format!("Anahtar zinciri erişilemedi: {e}"))?;
    match entry.get_password() {
        Ok(v) => Ok(Some(v)),
        Err(keyring::Error::NoEntry) => Ok(None),
        Err(e) => Err(format!("Anahtar okunamadı: {e}")),
    }
}

/// "Birlikte Aç" / uygulama ikonuna bırakma ile gelen dosya yolları.
///
/// Bu olay frontend hazır olmadan tetiklenebilir (macOS'ta `Opened`, pencere
/// oluşmadan önce gelebilir), o yüzden yollar burada biriktirilir; arayüz
/// açılışta `take_opened_files` ile kuyruğu boşaltır. Uygulama zaten
/// çalışırken gelen açılışlar ayrıca `files-opened` olayıyla yayınlanır.
#[derive(Default)]
struct PendingOpen(Mutex<Vec<String>>);

/// Bekleyen dosya yollarını al ve kuyruğu boşalt.
#[tauri::command]
fn take_opened_files(state: tauri::State<'_, PendingOpen>) -> Vec<String> {
    match state.0.lock() {
        Ok(mut q) => std::mem::take(&mut *q),
        Err(_) => Vec::new(),
    }
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    let app = tauri::Builder::default()
        // Günlükleme — HER ŞEYDEN ÖNCE kurulur ki sonraki eklentilerin/kurulum
        // adımlarının hataları da dosyaya düşsün.
        //
        // Neden dosyaya: kullanıcı makinesinde (özellikle Windows'ta) sorunlar
        // "sessizce" çıkıyor ve elimizde yalnızca kullanıcının anlattığı semptom
        // oluyordu. Artık gerçek sebep diskte duruyor: Ayarlar → Günlükler.
        .plugin(
            tauri_plugin_log::Builder::new()
                .targets([
                    tauri_plugin_log::Target::new(tauri_plugin_log::TargetKind::LogDir {
                        file_name: Some("efatura-edit".into()),
                    }),
                    tauri_plugin_log::Target::new(tauri_plugin_log::TargetKind::Stdout),
                    // Webview konsoluna da bas — geliştirirken DevTools'ta görünür.
                    tauri_plugin_log::Target::new(tauri_plugin_log::TargetKind::Webview),
                ])
                .level(if cfg!(debug_assertions) {
                    log::LevelFilter::Debug
                } else {
                    log::LevelFilter::Info
                })
                // Dosya büyürse döndür (tek dosya sınırsız büyümesin).
                .max_file_size(2 * 1024 * 1024)
                .rotation_strategy(tauri_plugin_log::RotationStrategy::KeepAll)
                .timezone_strategy(tauri_plugin_log::TimezoneStrategy::UseLocal)
                .build(),
        )
        .plugin(tauri_plugin_opener::init())
        .plugin(tauri_plugin_dialog::init())
        .plugin(tauri_plugin_fs::init())
        .plugin(tauri_plugin_shell::init())
        .plugin(tauri_plugin_updater::Builder::new().build())
        .plugin(tauri_plugin_process::init())
        // Editörün sağ tık menüsündeki Kes/Kopyala/Yapıştır için. Tarayıcının
        // navigator.clipboard.readText()'i masaüstü webview'da izin isteyip
        // sessizce boş dönebiliyor — pano OS üzerinden okunur.
        .plugin(tauri_plugin_clipboard_manager::init())
        .manage(PendingOpen::default())
        .invoke_handler(tauri::generate_handler![
            greet,
            open_devtools,
            secret_set,
            secret_get,
            take_opened_files,
            xslt::xslt_transform,
            xslt::log_dir,
            xslt::open_log_dir,
            ai::ai_chat,
            ai::ai_list_models,
            ai::ai_agent,
            agent_tools::agent_read,
            agent_tools::agent_write,
            agent_tools::agent_edit,
            agent_tools::agent_list,
            agent_tools::agent_bash
        ])
        .setup(|app| {
            // macOS'ta uygulama menüsündeki "e-Fatura Edit → Hakkında" paneli.
            // Tauri'nin varsayılan menüsü bu paneli BOŞ bırakır (yalnızca sürüm
            // görünür); künyeyi burada dolduruyoruz.
            #[cfg(target_os = "macos")]
            {
                use tauri::menu::AboutMetadata;
                let metadata = AboutMetadata {
                    name: Some("e-Fatura Edit".into()),
                    version: Some(app.package_info().version.to_string()),
                    short_version: Some(app.package_info().version.to_string()),
                    authors: Some(vec!["Zafer Bilgisayar".into()]),
                    comments: Some(
                        "Türkiye e-Fatura / e-Arşiv / e-İrsaliye (UBL-TR) belgeleri için \
                         XSLT dizayn editörü. Tam XSLT 1.0/2.0/3.0 desteği (Saxon-HE), \
                         canlı önizleme, AI asistan ve UBL-TR snippet kütüphanesi."
                            .into(),
                    ),
                    copyright: Some("© 2018–2026 Zafer Bilgisayar — MIT Lisansı".into()),
                    license: Some("MIT".into()),
                    website: Some("https://github.com/hzkucuk/eFaturaEdit".into()),
                    website_label: Some("Kaynak kodu (GitHub)".into()),
                    credits: Some("hzkucuk@gmail.com".into()),
                    icon: app.default_window_icon().cloned(),
                    ..Default::default()
                };
                let menu = tauri::menu::Menu::default(app.handle())?;
                // Varsayılan menüdeki "About" öğesi künyesizdir; künyeli olanla
                // değiştirmek için menüyü baştan kurmak yerine, uygulama alt
                // menüsünün ilk öğesini değiştiriyoruz.
                if let Some(tauri::menu::MenuItemKind::Submenu(app_menu)) =
                    menu.items()?.first().map(|i| i.to_owned())
                {
                    let about =
                        tauri::menu::PredefinedMenuItem::about(app.handle(), None, Some(metadata))?;
                    app_menu.remove_at(0)?;
                    app_menu.prepend(&about)?;
                }
                app.set_menu(menu)?;
            }

            // Her oturumun başına künye yaz: sorun bildiren kullanıcıdan günlüğü
            // istediğimizde sürüm/platform tahmin etmek zorunda kalmayalım.
            log::info!(
                "=== e-Fatura Edit {} başladı — {} {} ===",
                app.package_info().version,
                std::env::consts::OS,
                std::env::consts::ARCH
            );

            // Windows/Linux: "Birlikte Aç" dosyayı komut satırı argümanı olarak
            // geçirir. (macOS bunun yerine aşağıdaki `Opened` olayını kullanır.)
            #[cfg(not(target_os = "macos"))]
            {
                let files: Vec<String> = std::env::args()
                    .skip(1)
                    .filter(|a| !a.starts_with('-') && std::path::Path::new(a).is_file())
                    .collect();
                if !files.is_empty() {
                    if let Ok(mut q) = app.state::<PendingOpen>().0.lock() {
                        q.extend(files);
                    }
                }
            }
            let _ = app;
            Ok(())
        })
        .build(tauri::generate_context!())
        .expect("error while running tauri application");

    app.run(|_app, _event| {
        // macOS: Finder → "Birlikte Aç" (uygulama kapalıyken de açıkken de gelir).
        #[cfg(target_os = "macos")]
        if let tauri::RunEvent::Opened { urls } = _event {
            let files: Vec<String> = urls
                .iter()
                .filter_map(|u| u.to_file_path().ok())
                .map(|p| p.to_string_lossy().into_owned())
                .collect();
            if !files.is_empty() {
                if let Ok(mut q) = _app.state::<PendingOpen>().0.lock() {
                    q.extend(files.clone());
                }
                // Arayüz zaten ayaktaysa hemen haber ver; değilse kuyrukta bekler.
                let _ = _app.emit("files-opened", files);
            }
        }
    });
}
