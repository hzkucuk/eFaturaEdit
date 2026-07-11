//! XSLT 2.0/3.0 dönüşümü — Saxon-HE sidecar köprüsü.
//!
//! WebView'in yerleşik `XSLTProcessor`'ı yalnızca XSLT 1.0 destekler; GİB ve
//! müşteri şablonları `version="2.0"` bildirip `format-dateTime`, `upper-case`,
//! `tokenize`, `for-each-group`, `xsl:function` gibi 2.0+ özellikleri
//! kullanabilir. Bu modül, Saxon-HE'yi (MPL 2.0) içeren yerel `xslt-transform`
//! sidecar'ını çalıştırır.
//!
//! Protokol (sidecar'ın stdin'i, ikili):
//!   [4 bayt BE XSLT uzunluğu][XSLT UTF-8][4 bayt BE XML uzunluğu][XML UTF-8]
//! Çıktı: stdout'ta HTML; hata durumunda stderr + sıfırdan farklı çıkış kodu.
//!
//! Veri argümanla değil stdin ile aktarılır — şablonlar 600 KB'ı aşabildiğinden
//! komut satırı argüman sınırına takılırdı.

use tauri_plugin_shell::process::CommandEvent;
use tauri_plugin_shell::ShellExt;

/// XSLT (2.0/3.0 dahil) dönüşümü yapar, HTML döndürür.
#[tauri::command]
pub async fn xslt_transform(
    app: tauri::AppHandle,
    xslt: String,
    xml: String,
) -> Result<String, String> {
    let sidecar = app
        .shell()
        .sidecar("xslt-transform")
        .map_err(|e| format!("XSLT motoru bulunamadı: {e}"))?;

    let (mut rx, mut child) = sidecar
        .spawn()
        .map_err(|e| format!("XSLT motoru başlatılamadı: {e}"))?;

    // Uzunluk-önekli girdiyi yaz.
    let mut payload: Vec<u8> = Vec::with_capacity(xslt.len() + xml.len() + 8);
    payload.extend_from_slice(&(xslt.len() as u32).to_be_bytes());
    payload.extend_from_slice(xslt.as_bytes());
    payload.extend_from_slice(&(xml.len() as u32).to_be_bytes());
    payload.extend_from_slice(xml.as_bytes());

    child
        .write(&payload)
        .map_err(|e| format!("XSLT motoruna veri yazılamadı: {e}"))?;
    // stdin'i kapat ki sidecar okumayı bitirsin.
    drop(child);

    let mut stdout: Vec<u8> = Vec::new();
    let mut stderr = String::new();
    let mut code: Option<i32> = None;

    while let Some(event) = rx.recv().await {
        match event {
            CommandEvent::Stdout(chunk) => stdout.extend_from_slice(&chunk),
            CommandEvent::Stderr(chunk) => stderr.push_str(&String::from_utf8_lossy(&chunk)),
            CommandEvent::Terminated(payload) => code = payload.code,
            _ => {}
        }
    }

    if code.unwrap_or(-1) != 0 {
        let message = stderr.trim();
        return Err(if message.is_empty() {
            "XSLT dönüşümü başarısız oldu.".to_string()
        } else {
            message.to_string()
        });
    }

    String::from_utf8(stdout).map_err(|e| format!("XSLT çıktısı okunamadı: {e}"))
}
