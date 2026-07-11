mod ai;

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

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_opener::init())
        .plugin(tauri_plugin_dialog::init())
        .plugin(tauri_plugin_fs::init())
        .invoke_handler(tauri::generate_handler![
            greet,
            open_devtools,
            secret_set,
            secret_get,
            ai::ai_chat,
            ai::ai_list_models
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
