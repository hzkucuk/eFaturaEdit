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

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_opener::init())
        .plugin(tauri_plugin_dialog::init())
        .plugin(tauri_plugin_fs::init())
        .invoke_handler(tauri::generate_handler![
            greet,
            open_devtools,
            ai::ai_chat,
            ai::ai_list_models
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
