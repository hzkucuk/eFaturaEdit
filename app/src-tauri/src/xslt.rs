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
    let started = std::time::Instant::now();
    log::info!(
        "[xslt] dönüşüm başlıyor — XSLT {} bayt, XML {} bayt",
        xslt.len(),
        xml.len()
    );

    let sidecar = app.shell().sidecar("xslt-transform").map_err(|e| {
        log::error!("[xslt] sidecar çözümlenemedi: {e}");
        format!("{ENGINE_UNAVAILABLE}: XSLT motoru bulunamadı: {e}")
    })?;

    let (mut rx, mut child) = sidecar.spawn().map_err(|e| {
        log::error!("[xslt] sidecar başlatılamadı: {e}");
        format!("{ENGINE_UNAVAILABLE}: XSLT motoru başlatılamadı: {e}")
    })?;
    log::debug!("[xslt] sidecar süreci başladı");

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
    let total = payload.len();
    let mut written = 0usize;
    let mut write_error: Option<String> = None;
    for chunk in payload.chunks(32 * 1024) {
        if let Err(e) = child.write(chunk) {
            log::error!(
                "[xslt] stdin yazma hatası — {written}/{total} bayt yazılmıştı: {e}"
            );
            write_error = Some(e.to_string());
            break;
        }
        written += chunk.len();
    }
    // stdin'i kapat ki sidecar okumayı bitirsin.
    drop(child);
    if write_error.is_none() {
        log::debug!("[xslt] stdin tamamlandı ({total} bayt)");
    }

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
        // Çıkış kodu tanı için altın değerinde:
        //   -1073741515 (0xC0000135) → gerekli bir DLL yok (ör. VCRUNTIME140)
        //   -1073741819 (0xC0000005) → erişim ihlali / süreç öldürüldü
        log::error!(
            "[xslt] motor veri yazılırken öldü — yazma hatası: {e}{exit}. stderr: {}",
            if detail.is_empty() { "(boş)" } else { detail }
        );
        return Err(format!(
            "{ENGINE_UNAVAILABLE}: XSLT motoruna veri yazılamadı ({e}){exit}.{said}"
        ));
    }

    let exit = code.unwrap_or(-1);
    if exit != 0 {
        log::error!(
            "[xslt] motor sıfırdan farklı kodla çıktı: {exit}. stderr: {}",
            if detail.is_empty() { "(boş)" } else { detail }
        );
        // Motor tek kelime etmeden öldüyse bu bir şablon hatası değil, motorun
        // kendisi çalışamıyor demektir → geri düşülebilir olarak işaretle.
        return Err(if detail.is_empty() {
            format!("{ENGINE_UNAVAILABLE}: XSLT motoru çalıştırılamadı (çıkış kodu {exit}).")
        } else {
            detail.to_string()
        });
    }

    log::info!(
        "[xslt] Saxon dönüşümü tamam — {} bayt HTML, {} ms",
        stdout.len(),
        started.elapsed().as_millis()
    );
    String::from_utf8(stdout).map_err(|e| {
        log::error!("[xslt] çıktı UTF-8 değil: {e}");
        format!("XSLT çıktısı okunamadı: {e}")
    })
}

/// Günlük dosyalarının bulunduğu klasörün yolu (Ayarlar'da gösterilir).
#[tauri::command]
pub fn log_dir(app: tauri::AppHandle) -> Result<String, String> {
    use tauri::Manager;
    app.path()
        .app_log_dir()
        .map(|p| p.to_string_lossy().into_owned())
        .map_err(|e| format!("Günlük klasörü bulunamadı: {e}"))
}

/// Günlük klasörünü sistem dosya yöneticisinde aç.
///
/// Neden arayüzden değil de Rust'tan: `opener` eklentisinin arayüz izni yalnızca
/// `$APPDATA`/`$APPLOCALDATA` altını açabiliyor; günlük klasörü ise başka yerde
/// (macOS: `~/Library/Logs/<bundle>`). Arayüzden çağrılınca istek **izinle
/// reddediliyor** ve hiçbir şey olmuyordu. Rust tarafı bu kapsama tabi değil.
#[tauri::command]
pub fn open_log_dir(app: tauri::AppHandle) -> Result<String, String> {
    use tauri::Manager;
    use tauri_plugin_opener::OpenerExt;

    let dir = app
        .path()
        .app_log_dir()
        .map_err(|e| format!("Günlük klasörü bulunamadı: {e}"))?;

    // İlk günlük yazılmadan önce klasör henüz olmayabilir.
    std::fs::create_dir_all(&dir)
        .map_err(|e| format!("Günlük klasörü oluşturulamadı ({}): {e}", dir.display()))?;

    app.opener()
        .open_path(dir.to_string_lossy(), None::<&str>)
        .map_err(|e| format!("Günlük klasörü açılamadı ({}): {e}", dir.display()))?;

    log::info!("[günlük] klasör açıldı: {}", dir.display());
    Ok(dir.to_string_lossy().into_owned())
}
