//! MCP köprüsü — modelin uygulamanın **EDİTÖRÜNÜ** kontrol etmesi.
//!
//! ```text
//!   claude  ──spawn──►  bu ikili `--mcp-server <soket>`  (stdio JSON-RPC)
//!                            │  open_in_editor(path)
//!                            ▼  (yerel soket)
//!                       EditorBridge (uygulama içi dinleyici)
//!                            │  emit("claude-open-in-editor", path)
//!                            ▼
//!                       Frontend → dosyayı editör sekmesinde açar
//! ```
//!
//! **Neden var:** Bu bir XSLT/XML editörü uygulaması; içindeki AI ürettiği dosyayı
//! uygulamanın editöründe **açabilmeli**. Model kendini "terminal aracı" sanıp `open`
//! (GUI/tarayıcı) deniyordu — sandbox'ta engelli. `open_in_editor` MCP aracı bu boşluğu
//! kapatır: dosyayı **uygulamanın kendi sekmesinde** açar.
//!
//! **Ölçüldü (2026-07-17):** `claude -p --mcp-config … --settings <hook+sandbox>` birlikte
//! çalışıyor; MCP aracı da PreToolUse hook'undan geçiyor (kapıda `mcp__…` **otomatik allow**).
//!
//! Soket deseni [`crate::agent_hook`] ile aynı (aynı 104-bayt `sun_path` tuzağı, aynı
//! `--hook-helper`-benzeri ayrı süreç modu). Güvenlik açısından kritik **değil** (dosyayı
//! editörde göstermek okuma sınıfı bir eylem — model zaten `Read` ile okuyabiliyor); yine de
//! yalnız aynı kullanıcının bağlanmasına izin verilir (unix euid).

use interprocess::local_socket::{prelude::*, ListenerOptions, Stream};
use serde::{Deserialize, Serialize};
use std::io::{BufRead, BufReader, Write};
use std::path::PathBuf;
use std::sync::atomic::{AtomicBool, Ordering};
use std::sync::Arc;

use crate::agent_hook::{alloc_socket, build_name};

/// MCP sunucusunun soket üstünden uygulamaya yazdığı istek.
#[derive(Debug, Deserialize, Serialize)]
struct OpenRequest {
    path: String,
}

/// Uygulamanın verdiği cevap.
#[derive(Debug, Deserialize, Serialize)]
struct OpenReply {
    ok: bool,
    #[serde(default)]
    error: String,
}

/// `open_in_editor` çağrısını uygulama tarafında karşılayan işleyici — sekmeyi açar.
/// `Err` mesajı modele araç hatası olarak döner (güvenlik değil, bilgilendirme).
pub type OpenHandler = Arc<dyn Fn(String) -> Result<(), String> + Send + Sync + 'static>;

/// [`crate::agent_cli::run_claude`]'a verilen MCP kurulumu. `None` verilirse motor MCP'siz
/// koşar (e2e testleri böyle sürer — hook/sandbox'a odaklanmak için).
pub struct McpSetup {
    /// `--mcp-server` modunda çalıştırılacak ikili (= bu uygulama).
    pub exe: PathBuf,
    /// `open_in_editor` çağrıldığında sekmeyi açan işleyici.
    pub open_handler: OpenHandler,
}

/// Uygulama içi dinleyici: MCP sunucusundan gelen "şu dosyayı aç" isteklerini karşılar.
pub struct EditorBridge {
    socket_arg: String,
    /// unix'te 0700 izinli geçici dizin — `Drop`'ta silinir. Windows'ta `None`.
    dir: Option<PathBuf>,
    running: Arc<AtomicBool>,
}

impl EditorBridge {
    /// Dinleyiciyi kur ve kabul thread'ini başlat. `claude` spawn edilmeden **önce**
    /// çağrılmalı (adı önce biz kapalım — [`crate::agent_hook::HookGate`] ile aynı gerekçe).
    pub fn start(handler: OpenHandler) -> std::io::Result<Self> {
        let (socket_arg, dir) = alloc_socket()?;
        let listener = ListenerOptions::new().name(build_name(&socket_arg)?).create_sync()?;

        let running = Arc::new(AtomicBool::new(true));
        let running_thread = running.clone();
        log::info!("[mcp] editör köprüsü açıldı — {socket_arg}");

        std::thread::spawn(move || {
            for conn in listener.incoming() {
                if !running_thread.load(Ordering::SeqCst) {
                    break;
                }
                match conn {
                    Ok(stream) => {
                        let h = handler.clone();
                        std::thread::spawn(move || serve_open(stream, &h));
                    }
                    Err(e) => log::warn!("[mcp] bağlantı kabul edilemedi: {e}"),
                }
            }
            log::info!("[mcp] editör köprüsü kapandı");
        });

        Ok(Self { socket_arg, dir, running })
    }

    /// `--mcp-config`'e girecek soket adı (yol / pipe adı).
    pub fn socket_arg(&self) -> &str {
        &self.socket_arg
    }
}

impl Drop for EditorBridge {
    fn drop(&mut self) {
        self.running.store(false, Ordering::SeqCst);
        // `incoming()` accept'te bloke — bir kez bağlanıp thread'i bayrağı görsün diye uyandır.
        if let Ok(name) = build_name(&self.socket_arg) {
            let _ = Stream::connect(name);
        }
        if let Some(dir) = &self.dir {
            let _ = std::fs::remove_dir_all(dir);
        }
    }
}

/// Bir bağlantıyı servis et: `{path}` oku → sekmeyi aç → `{ok|error}` yaz.
fn serve_open(stream: Stream, handler: &OpenHandler) {
    // unix: bağlanan gerçekten biz miyiz? (Kapıdaki `serve` ile aynı koruma.)
    #[cfg(unix)]
    if let Ok(creds) = stream.peer_creds() {
        if let Some(euid) = creds.euid() {
            if euid != unsafe { libc::geteuid() } {
                log::warn!("[mcp] yabancı kullanıcıdan bağlantı reddedildi (euid={euid})");
                return;
            }
        }
    }

    let mut conn = BufReader::new(stream);
    let mut line = String::new();
    if let Err(e) = conn.read_line(&mut line) {
        log::warn!("[mcp] istek okunamadı: {e}");
        return;
    }

    let reply = match serde_json::from_str::<OpenRequest>(&line) {
        Ok(req) => {
            log::info!("[mcp] editörde aç isteği — {} bayt yol", req.path.len());
            match handler(req.path) {
                Ok(()) => OpenReply { ok: true, error: String::new() },
                Err(e) => OpenReply { ok: false, error: e },
            }
        }
        Err(e) => OpenReply { ok: false, error: format!("istek okunamadı: {e}") },
    };

    let body = serde_json::to_string(&reply).unwrap_or_else(|_| "{\"ok\":false}".into());
    let stream = conn.get_mut();
    if let Err(e) = stream.write_all(body.as_bytes()).and_then(|()| stream.write_all(b"\n")) {
        log::warn!("[mcp] cevap yazılamadı: {e}");
    }
    let _ = stream.flush();
}

// ── MCP sunucu tarafı (ayrı süreç: `--mcp-server <soket>`) ──────────────────────

/// `--mcp-server <soket>` modunun gövdesi: stdio üzerinde JSON-RPC (MCP) sunucusu.
///
/// **Tauri başlatılmaz** — bu süreç yalnızca `claude` ile uygulama arasında bir köprü.
/// `lib.rs::run()`'ın ilk satırlarında yakalanır ([`crate::agent_hook::hook_helper_main`] gibi).
pub fn mcp_server_main(socket_arg: &str) {
    log::info!("[mcp] sunucu başladı — soket={socket_arg}");
    let stdin = std::io::stdin();
    let mut stdout = std::io::stdout();
    let mut line = String::new();
    loop {
        line.clear();
        match stdin.lock().read_line(&mut line) {
            Ok(0) => break, // EOF (claude kapandı)
            Ok(_) => {}
            Err(_) => break,
        }
        if line.trim().is_empty() {
            continue;
        }
        if let Some(resp) = mcp_handle_line(&line, &|path| open_exchange(path, socket_arg)) {
            let _ = stdout.write_all(resp.as_bytes());
            let _ = stdout.write_all(b"\n");
            let _ = stdout.flush();
        }
    }
}

/// `open_in_editor` aracının şeması. Model bunu `ToolSearch` ile keşfedip çağırıyor (ölçüldü).
fn tool_schema() -> serde_json::Value {
    serde_json::json!({
        "name": "open_in_editor",
        "description": "e-Fatura Edit uygulamasının editöründe bir .xslt/.xsl/.xml dosyasını açar/gösterir. \
Dosyayı ürettikten veya düzenledikten sonra kullanıcıya göstermek için bunu kullan. \
GUI veya tarayıcı açmaya çalışma (open/xdg-open sandbox'ta engellidir).",
        "inputSchema": {
            "type": "object",
            "properties": {
                "path": { "type": "string", "description": "Açılacak dosyanın mutlak yolu (.xslt/.xsl/.xml)" }
            },
            "required": ["path"]
        }
    })
}

/// Tek bir JSON-RPC satırını işle. `Some(cevap)` = gönderilecek yanıt; `None` = bildirim
/// (yanıtsız) ya da anlaşılamayan satır. `send_open` enjekte edilir → süreç açmadan testlenir.
fn mcp_handle_line(line: &str, send_open: &dyn Fn(&str) -> Result<(), String>) -> Option<String> {
    let msg: serde_json::Value = match serde_json::from_str(line) {
        Ok(v) => v,
        Err(e) => {
            log::warn!("[mcp] JSON-RPC satırı ayrıştırılamadı: {e}");
            return None;
        }
    };
    let method = msg.get("method").and_then(|v| v.as_str()).unwrap_or("");
    let id = msg.get("id").cloned();

    match method {
        "initialize" => {
            // İstemcinin protokol sürümünü echo'la (ölçüldü: claude "2025-11-25" gönderiyor).
            let pv = msg
                .pointer("/params/protocolVersion")
                .and_then(|v| v.as_str())
                .unwrap_or("2025-06-18");
            Some(
                serde_json::json!({
                    "jsonrpc": "2.0", "id": id,
                    "result": {
                        "protocolVersion": pv,
                        "capabilities": { "tools": {} },
                        "serverInfo": { "name": "efe-editor", "version": "1.0.0" }
                    }
                })
                .to_string(),
            )
        }
        "notifications/initialized" => None,
        "tools/list" => Some(
            serde_json::json!({
                "jsonrpc": "2.0", "id": id,
                "result": { "tools": [tool_schema()] }
            })
            .to_string(),
        ),
        "tools/call" => {
            let name = msg.pointer("/params/name").and_then(|v| v.as_str()).unwrap_or("");
            if name != "open_in_editor" {
                return Some(tool_result(id, &format!("Bilinmeyen araç: {name}"), true));
            }
            let path = msg
                .pointer("/params/arguments/path")
                .and_then(|v| v.as_str())
                .unwrap_or("");
            if path.trim().is_empty() {
                return Some(tool_result(id, "Hata: 'path' argümanı boş.", true));
            }
            match send_open(path) {
                Ok(()) => Some(tool_result(id, &format!("Dosya editörde açıldı: {path}"), false)),
                Err(e) => Some(tool_result(id, &format!("Editörde açılamadı: {e}"), true)),
            }
        }
        // Bilinmeyen istek (id'li) → JSON-RPC hata; bildirim (id'siz) → sessiz.
        _ => id.as_ref().map(|_| {
            serde_json::json!({
                "jsonrpc": "2.0", "id": id,
                "error": { "code": -32601, "message": format!("method not found: {method}") }
            })
            .to_string()
        }),
    }
}

/// `tools/call` yanıtı — MCP sözleşmesi: `{content:[{type:text,...}], isError}`.
fn tool_result(id: Option<serde_json::Value>, text: &str, is_error: bool) -> String {
    serde_json::json!({
        "jsonrpc": "2.0", "id": id,
        "result": {
            "content": [{ "type": "text", "text": text }],
            "isError": is_error
        }
    })
    .to_string()
}

/// MCP sunucusundan uygulamaya "şu dosyayı aç" de, cevabı bekle. Güvenlik değil —
/// başarısızlık modele araç hatası olarak döner (sessiz geri düşüş yok, görünür hata).
fn open_exchange(path: &str, socket_arg: &str) -> Result<(), String> {
    let name = build_name(socket_arg).map_err(|e| format!("soket adı geçersiz: {e}"))?;
    let stream = Stream::connect(name).map_err(|e| format!("uygulamaya bağlanılamadı: {e}"))?;
    let mut conn = BufReader::new(stream);

    let req = serde_json::to_string(&OpenRequest { path: path.to_string() })
        .map_err(|e| format!("istek serileştirilemedi: {e}"))?;
    {
        let s = conn.get_mut();
        s.write_all(req.as_bytes())
            .and_then(|()| s.write_all(b"\n"))
            .and_then(|()| s.flush())
            .map_err(|e| format!("istek yazılamadı: {e}"))?;
    }

    let mut line = String::new();
    conn.read_line(&mut line).map_err(|e| format!("cevap okunamadı: {e}"))?;
    if line.trim().is_empty() {
        return Err("uygulama cevap vermeden bağlantıyı kapattı".into());
    }
    let reply: OpenReply =
        serde_json::from_str(&line).map_err(|e| format!("cevap çözülemedi: {e}"))?;
    if reply.ok {
        Ok(())
    } else {
        Err(if reply.error.is_empty() { "bilinmeyen hata".into() } else { reply.error })
    }
}

/// `--mcp-config`'e verilecek JSON. `exe` = bu uygulamanın ikilisi (`--mcp-server` modu);
/// `command` doğrudan çalıştırılır (kabuk yok) → boşluklu yol tırnak gerektirmez (hook'tan farkı).
pub fn build_mcp_config(exe: &std::path::Path, socket_arg: &str) -> String {
    serde_json::json!({
        "mcpServers": {
            "efe-editor": {
                "command": exe.to_string_lossy(),
                "args": ["--mcp-server", socket_arg]
            }
        }
    })
    .to_string()
}

// ── Testler ─────────────────────────────────────────────────────────────────

#[cfg(test)]
mod tests {
    use super::*;

    /// Yanıttan `permissionDecision`/alan çekmeye yardımcı.
    fn parse(s: &str) -> serde_json::Value {
        serde_json::from_str(s).expect("çıktı JSON değil")
    }

    fn hep_basarili(_p: &str) -> Result<(), String> {
        Ok(())
    }

    #[test]
    fn initialize_protokol_surumunu_echolar() {
        let line = r#"{"jsonrpc":"2.0","id":0,"method":"initialize","params":{"protocolVersion":"2025-11-25"}}"#;
        let out = mcp_handle_line(line, &hep_basarili).expect("yanıt bekleniyordu");
        let v = parse(&out);
        assert_eq!(v["result"]["protocolVersion"], "2025-11-25");
        assert_eq!(v["result"]["serverInfo"]["name"], "efe-editor");
    }

    #[test]
    fn initialized_bildirimi_yanitsiz() {
        assert_eq!(
            mcp_handle_line(r#"{"jsonrpc":"2.0","method":"notifications/initialized"}"#, &hep_basarili),
            None
        );
    }

    #[test]
    fn tools_list_open_in_editor_dondurur() {
        let out = mcp_handle_line(r#"{"jsonrpc":"2.0","id":1,"method":"tools/list"}"#, &hep_basarili)
            .expect("yanıt bekleniyordu");
        let v = parse(&out);
        assert_eq!(v["result"]["tools"][0]["name"], "open_in_editor");
    }

    #[test]
    fn tools_call_basarili_yol_dondurur() {
        let line = r#"{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"open_in_editor","arguments":{"path":"/tmp/a.xslt"}}}"#;
        let out = mcp_handle_line(line, &hep_basarili).expect("yanıt bekleniyordu");
        let v = parse(&out);
        assert_eq!(v["result"]["isError"], false);
        assert!(v["result"]["content"][0]["text"].as_str().unwrap().contains("/tmp/a.xslt"));
    }

    #[test]
    fn tools_call_hata_isError_true() {
        let line = r#"{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"open_in_editor","arguments":{"path":"/tmp/a.xslt"}}}"#;
        let out = mcp_handle_line(line, &|_p| Err("sekme açılamadı".into())).expect("yanıt");
        let v = parse(&out);
        assert_eq!(v["result"]["isError"], true);
        assert!(v["result"]["content"][0]["text"].as_str().unwrap().contains("sekme açılamadı"));
    }

    #[test]
    fn bos_yol_reddedilir() {
        let line = r#"{"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"open_in_editor","arguments":{"path":"  "}}}"#;
        let out = mcp_handle_line(line, &hep_basarili).expect("yanıt");
        assert_eq!(parse(&out)["result"]["isError"], true);
    }

    #[test]
    fn bilinmeyen_arac_isError_true() {
        let line = r#"{"jsonrpc":"2.0","id":5,"method":"tools/call","params":{"name":"baska_arac","arguments":{}}}"#;
        let out = mcp_handle_line(line, &hep_basarili).expect("yanıt");
        assert_eq!(parse(&out)["result"]["isError"], true);
    }

    #[test]
    fn bilinmeyen_method_istekte_hata_bildirimde_sessiz() {
        // id'li → JSON-RPC hata
        let out = mcp_handle_line(r#"{"jsonrpc":"2.0","id":9,"method":"foo/bar"}"#, &hep_basarili)
            .expect("id'li bilinmeyen method hata dönmeli");
        assert_eq!(parse(&out)["error"]["code"], -32601);
        // id'siz → sessiz
        assert_eq!(mcp_handle_line(r#"{"jsonrpc":"2.0","method":"foo/bar"}"#, &hep_basarili), None);
    }

    /// EditorBridge uçtan uca: köprüyü aç → soketten `{path}` gönder → handler çağrılıyor mu.
    #[test]
    fn kopru_ucdan_uca_handleri_cagirir() {
        let alinan = Arc::new(std::sync::Mutex::new(String::new()));
        let a = alinan.clone();
        let bridge = EditorBridge::start(Arc::new(move |path: String| {
            *a.lock().unwrap() = path;
            Ok(())
        }))
        .expect("köprü açılamadı");

        open_exchange("/tmp/deneme.xslt", bridge.socket_arg()).expect("aç isteği başarısız");
        assert_eq!(*alinan.lock().unwrap(), "/tmp/deneme.xslt");
    }

    #[test]
    fn kopru_handler_hatasini_iletir() {
        let bridge = EditorBridge::start(Arc::new(|_p: String| Err("desteklenmeyen uzantı".into())))
            .expect("köprü açılamadı");
        let err = open_exchange("/tmp/x.png", bridge.socket_arg()).unwrap_err();
        assert!(err.contains("desteklenmeyen uzantı"), "hata iletilmedi: {err}");
    }

    #[test]
    fn mcp_config_json_dogru() {
        let cfg = build_mcp_config(std::path::Path::new("/Applications/e-Fatura Edit"), "/tmp/efe-1/m.sock");
        let v = parse(&cfg);
        assert_eq!(v["mcpServers"]["efe-editor"]["command"], "/Applications/e-Fatura Edit");
        assert_eq!(v["mcpServers"]["efe-editor"]["args"][0], "--mcp-server");
        assert_eq!(v["mcpServers"]["efe-editor"]["args"][1], "/tmp/efe-1/m.sock");
    }
}
