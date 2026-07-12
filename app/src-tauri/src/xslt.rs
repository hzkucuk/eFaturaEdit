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

/// Motorun KENDİSİ kullanılamıyor (bulunamadı / başlatılamadı / konuşamadan
/// öldü) — şablonun hatalı olmasıyla ilgisi yok.
///
/// Arayüz bu öneki görünce tarayıcının XSLT 1.0 işlemcisine düşer (bkz.
/// xslt.ts). Ayrım şart: gerçek bir XSLT hatasında geri düşmek, kullanıcının
/// hatasını gizleyip yanlış çıktı üretmek olurdu.
pub const ENGINE_UNAVAILABLE: &str = "XSLT_ENGINE_UNAVAILABLE";

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
        .map_err(|e| format!("{ENGINE_UNAVAILABLE}: XSLT motoru bulunamadı: {e}"))?;

    let (mut rx, mut child) = sidecar
        .spawn()
        .map_err(|e| format!("{ENGINE_UNAVAILABLE}: XSLT motoru başlatılamadı: {e}"))?;

    // Uzunluk-önekli girdiyi yaz.
    let mut payload: Vec<u8> = Vec::with_capacity(xslt.len() + xml.len() + 8);
    payload.extend_from_slice(&(xslt.len() as u32).to_be_bytes());
    payload.extend_from_slice(xslt.as_bytes());
    payload.extend_from_slice(&(xml.len() as u32).to_be_bytes());
    payload.extend_from_slice(xml.as_bytes());

    // Parça parça yaz: Windows'ta boru tamponu küçüktür ve 600 KB'lık tek bir
    // yazma, süreç okumaya başlamadan tamponu doldurabilir.
    //
    // Yazma hatası (ör. Windows'ta "Boru sonlandı", os error 109) neredeyse
    // her zaman sürecin çoktan ölmüş olduğu anlamına gelir. Bu durumda HEMEN
    // dönmüyoruz — çünkü asıl sebep sürecin stderr'indedir; hemen dönseydik
    // onu okumadan atmış olurduk ve kullanıcıya sebebi değil semptomu
    // gösterirdik (v2.22.x'te tam olarak bu oldu).
    let mut write_error: Option<String> = None;
    for chunk in payload.chunks(32 * 1024) {
        if let Err(e) = child.write(chunk) {
            write_error = Some(e.to_string());
            break;
        }
    }
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

    let detail = stderr.trim();

    if let Some(e) = write_error {
        let exit = code
            .map(|c| format!(" (çıkış kodu {c})"))
            .unwrap_or_default();
        let said = if detail.is_empty() {
            " Motor hiçbir çıktı vermeden sonlandı.".to_string()
        } else {
            format!(" Motorun bildirdiği: {detail}")
        };
        return Err(format!(
            "{ENGINE_UNAVAILABLE}: XSLT motoruna veri yazılamadı ({e}){exit}.{said}"
        ));
    }

    let exit = code.unwrap_or(-1);
    if exit != 0 {
        // Motor tek kelime etmeden öldüyse bu bir şablon hatası değil, motorun
        // kendisi çalışamıyor demektir → geri düşülebilir olarak işaretle.
        return Err(if detail.is_empty() {
            format!("{ENGINE_UNAVAILABLE}: XSLT motoru çalıştırılamadı (çıkış kodu {exit}).")
        } else {
            detail.to_string()
        });
    }

    String::from_utf8(stdout).map_err(|e| format!("XSLT çıktısı okunamadı: {e}"))
}
