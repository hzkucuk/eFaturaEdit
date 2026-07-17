//! Onay kapısı — Claude Code motorunda araç çağrılarının **tek karar noktası**.
//!
//! ## Neden ayrı bir süreç ve soket var
//! `claude`'a "her araç çağrısını bana sor" demenin ölçülmüş tek yolu, `--settings`
//! ile tanımlanan **PreToolUse hook**'udur. Hook bir **komut**tur: CLI onu ayrı bir
//! süreç olarak çalıştırır, isteği stdin'den verir, kararı stdout'tan okur. O süreç
//! bizim GUI'mizin belleğine erişemez → araya bir **yerel soket** koyuyoruz:
//!
//! ```text
//! claude → (stdin JSON) → efatura-edit --hook-helper <ad> → soket → uygulama → kullanıcı
//!                          ^                                                      |
//!                          +---- (stdout: permissionDecision) <-- karar ----------+
//! ```
//!
//! ## Ölçülmüş olgular (2026-07-16 spike'ları — tahmin değil)
//! - Hook **5/5 araçta** ateşler (Bash dahil); `deny` dosyayı **diske yazdırmaz**.
//! - **Aynı anda 3 onay uçuşta olabilir** (model salt-okunur araçları paralelleştirir)
//!   → dinleyici bağlantı başına ayrı thread açar, tek modal varsayımı YANLIŞ olurdu.
//! - Onay **75 sn** bekletildiğinde CLI sorunsuz bekledi (matcher `timeout` = saniye).
//! - `--permission-mode default` **fail-closed**; `bypassPermissions` **fail-open** (⛔).
//!
//! ## Fail-closed duruşu — üç katman
//! 1. **CLI:** `default` modu → yardımcı hiç çalışmazsa CLI `-p` modunda soramaz, reddeder.
//! 2. **Yardımcı:** sokete ulaşamaz / bozuk yanıt / zaman aşımı → **`deny`** basar (panik yok).
//! 3. **Kapı:** stdin JSON'u ayrıştırılamazsa → **`deny`**.
//!
//! ## Dürüst sınırlar (bkz. CLAUDE.md ders 15/17 — zorlanamayan korumayı zorlanıyormuş gibi sunma)
//! Bu kapı **onay** kapısıdır, sandbox değil. Bash'in klasöre kilitlenmesi sandbox'a
//! bağlıdır (mac/Linux), okuma **hiçbir motorda** kilitlenmez. Kapının garantisi şudur:
//! *kullanıcı görmeden ve onaylamadan hiçbir araç çalışmaz.*

use interprocess::local_socket::{prelude::*, ListenerOptions, Name, Stream};
use serde::{Deserialize, Serialize};
use std::io::{BufRead, BufReader, Write};
use std::path::PathBuf;
use std::sync::atomic::{AtomicBool, Ordering};
use std::sync::mpsc;
use std::sync::Arc;
use std::time::Duration;

/// Yardımcının **kendi** zaman aşımı. `--settings` matcher'ındaki 600 sn'nin ALTINDA
/// olmalı: CLI bizi öldürmeden önce biz `deny` diyebilelim. CLI hook'u kendi timeout'unda
/// öldürürse ne yaptığı **ölçülmedi** — o belirsizliğe hiç girmiyoruz (ders 1: bittiğini
/// varsayma). Kullanıcı 9.5 dakikadır cevap vermediyse zaten "hayır" demektir.
pub const HELPER_TIMEOUT: Duration = Duration::from_secs(570);

/// PreToolUse hook'un stdin'inden gelen istek.
///
/// Anahtarlar **ölçülerek** alındı (2026-07-16 canlı koşu), belgeden/hatırlanandan değil
/// (ders 10: uydurulan tanımlayıcı hata vermez, sadece iş görmez). CLI yeni alanlar
/// eklerse ayrıştırma bozulmasın diye bilinmeyen alanlar yok sayılır ve hepsi
/// `#[serde(default)]` — eksik alan yüzünden `deny` fırlatmak kullanıcıyı boşuna durdurur.
#[derive(Debug, Clone, Default, Deserialize, Serialize)]
pub struct HookRequest {
    /// `"Read"` · `"Write"` · `"Edit"` · `"Bash"` · `"Grep"` · `"Glob"` …
    #[serde(default)]
    pub tool_name: String,
    /// Araca özel girdi. Bash'te `{command, description}` — **dosya yolu yoktur**, bu yüzden
    /// bash'e kök denetimi uygulanamaz (CLAUDE.md ders 15). Write/Edit'te `{file_path, ...}`.
    #[serde(default)]
    pub tool_input: serde_json::Value,
    #[serde(default)]
    pub cwd: String,
    #[serde(default)]
    pub tool_use_id: String,
    #[serde(default)]
    pub session_id: String,
}

/// Kapının kararı.
///
/// Sözleşmede `ask` ve `defer` de var ama **bize yaramaz**: `-p` (etkileşimsiz) modda CLI
/// kendi sorusunu soramaz — `ask` demek, kullanıcının göremeyeceği bir soruya havale etmektir.
/// Karar ikili: ya kullanıcı onayladı, ya onaylamadı.
#[derive(Debug, Clone, PartialEq, Eq)]
pub enum HookDecision {
    Allow,
    /// Kullanıcıya **Türkçe** gösterilecek sebep (stack trace değil).
    Deny(String),
}

/// İstek geldiğinde çağrılan karar verici. Her istek için **ayrı thread**'de koşar.
pub type Handler = Arc<dyn Fn(HookRequest) -> HookDecision + Send + Sync + 'static>;

/// Uygulama ⇄ yardımcı arasındaki **kendi** tel protokolümüz (CLI'ın sözleşmesi değil).
/// CLI'ın JSON biçimini tek yerde — yardımcıda — tutuyoruz ki sözleşme değişirse
/// tek dosyada değişsin.
#[derive(Debug, Serialize, Deserialize)]
struct WireDecision {
    /// `"allow"` | `"deny"`
    decision: String,
    #[serde(default)]
    reason: String,
}

impl From<HookDecision> for WireDecision {
    fn from(d: HookDecision) -> Self {
        match d {
            HookDecision::Allow => Self { decision: "allow".into(), reason: String::new() },
            HookDecision::Deny(r) => Self { decision: "deny".into(), reason: r },
        }
    }
}

// ── CLI sözleşmesi ────────────────────────────────────────────────────────────

/// Hook'un stdout'una basılacak JSON — **ölçülmüş** anahtarlar.
///
/// `{"hookSpecificOutput":{"hookEventName":"PreToolUse","permissionDecision":…,
///   "permissionDecisionReason":…}}`
fn cli_json(decision: &str, reason: &str) -> String {
    serde_json::json!({
        "hookSpecificOutput": {
            "hookEventName": "PreToolUse",
            "permissionDecision": decision,
            "permissionDecisionReason": reason,
        }
    })
    .to_string()
}

/// Fail-closed cevap. Yardımcıda **her** hata yolu buraya çıkar.
fn cli_deny(reason: impl AsRef<str>) -> String {
    cli_json("deny", reason.as_ref())
}

// ── Soket adı ─────────────────────────────────────────────────────────────────

/// Soket adını platforma göre kur.
///
/// - **unix:** dosya yolu → UDS. ⚠️ macOS'ta `sun_path` **104 bayt**; aşarsa dinleyici
///   *hiç* ayağa kalkmaz (ölçüldü). Bu yüzden yol `/tmp` altında ve kısa tutulur.
/// - **Windows:** ad → named pipe (`\\.\pipe\…`). Dosya yolu yok.
fn build_name(arg: &str) -> std::io::Result<Name<'_>> {
    #[cfg(windows)]
    {
        use interprocess::local_socket::{GenericNamespaced, ToNsName};
        arg.to_ns_name::<GenericNamespaced>()
    }
    #[cfg(not(windows))]
    {
        use interprocess::local_socket::{GenericFilePath, ToFsName};
        arg.to_fs_name::<GenericFilePath>()
    }
}

/// Çakışmayan bir ad üret. Gizli olması gerekmiyor (unix'te dizin izni, Windows'ta
/// `FILE_FLAG_FIRST_PIPE_INSTANCE` koruyor) — yalnızca **benzersiz** olmalı.
///
/// Üç parça, çünkü ikisi yetmedi (**ölçüldü**: yalnız pid+zaman ile testler paralel
/// koşarken `AlreadyExists` verdi — saat çözünürlüğü aynı süreçteki iki kapıyı ayırmaya
/// yetmiyor). Üretimde karşılığı: kullanıcı iki oturumu aynı anda başlatınca kapı
/// kurulamaz. Sayaç **süreç içini**, pid+zaman **süreçler arasını** ayırır.
fn uniq() -> String {
    static SAYAC: std::sync::atomic::AtomicU64 = std::sync::atomic::AtomicU64::new(0);
    let n = SAYAC.fetch_add(1, Ordering::SeqCst);
    let nanos = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .map(|d| u64::from(d.subsec_nanos()) ^ d.as_secs())
        .unwrap_or(0);
    format!("{:x}{:x}{:x}", std::process::id(), nanos, n)
}

// ── Yardımcı tarafı (ayrı süreç: `--hook-helper <ad>`) ────────────────────────

/// Yardımcının **saf çekirdeği** — stdin/stdout enjekte edilir, bu yüzden gerçek
/// süreç açmadan birim testlenebilir (`install_to()` ile aynı gerekçe).
///
/// **ASLA `Err` dönmez, ASLA panik etmez:** her hata yolu `deny`'dir. Yardımcı çökerse
/// stdout'a bir şey basamaz ve kapı sessizce açılabilirdi — bu yüzden panik yok.
pub fn hook_exchange(input: &[u8], socket_arg: &str, timeout: Duration) -> String {
    let (tx, rx) = mpsc::channel();
    let arg = socket_arg.to_owned();
    let payload = input.to_vec();

    // Zaman aşımını taşınabilir kurmanın tek yolu bu: `interprocess`'in genel `Stream`'i
    // `set_read_timeout`'u YALNIZCA unix'te açıyor (kaynaktan doğrulandı), Windows'ta yok.
    std::thread::spawn(move || {
        let _ = tx.send(exchange_inner(&payload, &arg));
    });

    match rx.recv_timeout(timeout) {
        Ok(Ok(json)) => json,
        Ok(Err(e)) => cli_deny(format!("Onay köprüsü kurulamadı: {e}")),
        Err(_) => cli_deny(format!(
            "Onay için {} sn beklendi, cevap gelmedi — güvenli tarafta kalındı.",
            timeout.as_secs()
        )),
    }
}

/// Tek gidiş-dönüş: bağlan → isteği yaz → kararı oku → CLI JSON'una çevir.
fn exchange_inner(payload: &[u8], socket_arg: &str) -> std::io::Result<String> {
    let name = build_name(socket_arg)?;
    let stream = Stream::connect(name)?;
    let mut writer = BufReader::new(stream);

    // İstek tek satır JSON — satır sonu, karşı tarafın `read_line`'ının bitiş işareti.
    writer.get_mut().write_all(payload.trim_ascii_end())?;
    writer.get_mut().write_all(b"\n")?;
    writer.get_mut().flush()?;

    let mut line = String::new();
    writer.read_line(&mut line)?;

    // Boş cevap = uygulama bağlantıyı kapattı (çöktü/kapandı) → fail-closed.
    if line.trim().is_empty() {
        return Ok(cli_deny("Uygulama onay vermeden bağlantıyı kapattı."));
    }

    match serde_json::from_str::<WireDecision>(&line) {
        Ok(w) if w.decision == "allow" => Ok(cli_json("allow", &w.reason)),
        Ok(w) => Ok(cli_json("deny", &w.reason)),
        // Anlamadığımız cevabı "evet" saymak, kapıyı sessizce açmaktır.
        Err(e) => Ok(cli_deny(format!("Onay cevabı okunamadı: {e}"))),
    }
}

/// `--hook-helper <ad>` modunun gövdesi: gerçek stdin/stdout'u [`hook_exchange`]'e bağlar.
///
/// **Tauri hiç başlatılmaz** — bu süreç yalnızca bir boru. `lib.rs::run()`'ın ilk
/// satırlarında yakalanır.
pub fn hook_helper_main(socket_arg: &str) {
    use std::io::Read;
    let mut input = Vec::new();
    // stdin okunamazsa bile cevap basmalıyız (sessiz kalmak = CLI'ı belirsizlikte bırakmak).
    let _ = std::io::stdin().read_to_end(&mut input);

    let out = hook_exchange(&input, socket_arg, HELPER_TIMEOUT);

    let mut stdout = std::io::stdout();
    let _ = stdout.write_all(out.as_bytes());
    let _ = stdout.write_all(b"\n");
    let _ = stdout.flush();
}

// ── Kapı tarafı (uygulama içi dinleyici) ──────────────────────────────────────

/// Yerel soket dinleyicisi — yardımcı süreçlerin bağlandığı uç.
///
/// **AppHandle almaz.** `install_to()` ile aynı gerekçe: Tauri'ye bağlanmayan bir çekirdek
/// gerçek `claude`'a karşı testten sürülebilir. Tauri sargısı `handler` kapanışında yaşar.
pub struct HookGate {
    socket_arg: String,
    /// unix'te 0700 izinli geçici dizin — `Drop`'ta silinir. Windows'ta `None`.
    dir: Option<PathBuf>,
    running: Arc<AtomicBool>,
}

impl HookGate {
    /// Dinleyiciyi kur ve kabul thread'ini başlat.
    ///
    /// **`claude` spawn edilmeden ÖNCE** çağrılmalı: adı önce biz kaparsak, aynı ada
    /// dinleyici kuran başka bir süreç (Windows'ta `FILE_FLAG_FIRST_PIPE_INSTANCE`
    /// sayesinde) hata alır. Ters sırada çalışırsak "her şeye allow" diyen bir taklitçi
    /// araya girebilirdi.
    pub fn start(handler: Handler) -> std::io::Result<Self> {
        let (socket_arg, dir) = alloc_socket()?;
        let listener = ListenerOptions::new().name(build_name(&socket_arg)?).create_sync()?;

        let running = Arc::new(AtomicBool::new(true));
        let running_thread = running.clone();
        log::info!("[kapı] dinleyici açıldı — {socket_arg}");

        std::thread::spawn(move || {
            for conn in listener.incoming() {
                if !running_thread.load(Ordering::SeqCst) {
                    break;
                }
                match conn {
                    Ok(stream) => {
                        // Bağlantı başına thread: aynı anda 3 onay uçuşta ölçüldü —
                        // seri işlersek ikinci istek birincinin onayını beklerdi.
                        let h = handler.clone();
                        std::thread::spawn(move || serve(stream, &h));
                    }
                    Err(e) => log::warn!("[kapı] bağlantı kabul edilemedi: {e}"),
                }
            }
            log::info!("[kapı] dinleyici kapandı");
        });

        Ok(Self { socket_arg, dir, running })
    }

    /// `--settings`'teki hook komutuna girecek ad (soket yolu / pipe adı).
    pub fn socket_arg(&self) -> &str {
        &self.socket_arg
    }
}

impl Drop for HookGate {
    fn drop(&mut self) {
        self.running.store(false, Ordering::SeqCst);
        // `incoming()` accept'te bloke — bir kez bağlanıp thread'i uyandır ki bayrağı görsün.
        if let Ok(name) = build_name(&self.socket_arg) {
            let _ = Stream::connect(name);
        }
        if let Some(dir) = &self.dir {
            let _ = std::fs::remove_dir_all(dir);
        }
    }
}

/// Bir bağlantıyı sonuna kadar servis et: isteği oku → karar al → cevabı yaz.
fn serve(stream: Stream, handler: &Handler) {
    // unix: bağlanan süreç gerçekten biz miyiz? Değilse konuşmayız.
    // (Windows'ta euid yok — orada koruma pipe'ın kendi ACL'i + FIRST_PIPE_INSTANCE.)
    #[cfg(unix)]
    if let Ok(creds) = stream.peer_creds() {
        if let Some(euid) = creds.euid() {
            if euid != unsafe { libc::geteuid() } {
                log::warn!("[kapı] yabancı kullanıcıdan bağlantı reddedildi (euid={euid})");
                return;
            }
        }
    }

    let mut conn = BufReader::new(stream);
    let mut line = String::new();
    if let Err(e) = conn.read_line(&mut line) {
        log::warn!("[kapı] istek okunamadı: {e}");
        return;
    }

    let decision = match serde_json::from_str::<HookRequest>(&line) {
        Ok(req) => {
            log::info!(
                "[kapı] istek — araç={} tool_use_id={} girdi={} bayt",
                req.tool_name,
                req.tool_use_id,
                line.len()
            );
            handler(req)
        }
        // Anlamadığımız isteği onaylamak, imzalamadan imzalamaktır.
        Err(e) => {
            log::warn!("[kapı] istek JSON'u ayrıştırılamadı: {e}");
            HookDecision::Deny("İstek okunamadı (bozuk JSON) — güvenli tarafta kalındı.".into())
        }
    };

    let wire = WireDecision::from(decision);
    log::info!("[kapı] karar — {}", wire.decision);
    let body = match serde_json::to_string(&wire) {
        Ok(b) => b,
        Err(e) => {
            log::error!("[kapı] karar serileştirilemedi: {e}");
            return; // Boş cevap → yardımcı fail-closed deny basar.
        }
    };

    let stream = conn.get_mut();
    if let Err(e) = stream.write_all(body.as_bytes()).and_then(|()| stream.write_all(b"\n")) {
        log::warn!("[kapı] karar yazılamadı: {e}");
    }
    let _ = stream.flush();
}

/// Soket adını (ve unix'te 0700 dizinini) ayır.
#[cfg(not(windows))]
fn alloc_socket() -> std::io::Result<(String, Option<PathBuf>)> {
    use std::os::unix::fs::DirBuilderExt;

    // ⚠️ `std::env::temp_dir()` KULLANILMAZ: macOS'ta `/var/folders/xx/…/T/` döner ve
    // AF_UNIX'in 104 baytlık `sun_path` sınırını aşabilir → dinleyici hiç açılmaz (ölçüldü).
    // `/tmp` her iki platformda da var ve kısa: `/tmp/efe-<id>/h.sock` ≈ 30 bayt.
    let dir = PathBuf::from("/tmp").join(format!("efe-{}", uniq()));

    // 0700: soket **dosyasının** izni bazı BSD'lerde yok sayılır; dizin izni her yerde
    // zorlanır → başka kullanıcı sokete hiç ulaşamaz.
    std::fs::DirBuilder::new().mode(0o700).create(&dir)?;

    let path = dir.join("h.sock");
    Ok((path.to_string_lossy().into_owned(), Some(dir)))
}

#[cfg(windows)]
fn alloc_socket() -> std::io::Result<(String, Option<PathBuf>)> {
    // Named pipe — dosya sistemi yolu yok, temizlenecek dizin de yok.
    Ok((format!("efe-{}.sock", uniq()), None))
}

// ── Tauri sargısı: kararı KULLANICIYA sor ─────────────────────────────────────

/// Uygulamanın kullanıcıya sorarken beklediği süre. Yardımcının kendi payından
/// (570 sn) **kısa**: kararı biz verelim, yardımcı bizi beklemekten vazgeçmesin.
const UI_TIMEOUT: Duration = Duration::from_secs(540);

/// Bekleyen onaylar: `id` → kararı iletecek kanal.
///
/// **Neden harita (tek slot değil):** aynı anda **3 onay uçuşta** olduğu ölçüldü —
/// model salt-okunur araçları paralelleştiriyor. Tek slot olsaydı ikinci istek
/// birincinin üzerine yazar ve bir araç **sessizce cevapsız** kalırdı.
#[derive(Default)]
pub struct GateState {
    bekleyen: std::sync::Mutex<std::collections::HashMap<String, mpsc::Sender<HookDecision>>>,
}

/// Kapı isteğini **arayüze** taşıyan handler: olayı yay, kullanıcının kararını bekle.
pub fn tauri_handler(app: tauri::AppHandle) -> Handler {
    use tauri::{Emitter, Manager};
    Arc::new(move |req: HookRequest| {
        let id = uniq();
        let (tx, rx) = mpsc::channel();

        let state = app.state::<GateState>();
        match state.bekleyen.lock() {
            Ok(mut m) => {
                m.insert(id.clone(), tx);
            }
            // Kilit zehirlenmişse soramayız → soramadığımızı onay sayamayız.
            Err(e) => {
                log::error!("[kapı] bekleyen listesi kilitlenemedi: {e}");
                return HookDecision::Deny("Onay kuyruğuna erişilemedi.".into());
            }
        }

        let yayin = app.emit(
            "claude-hook-request",
            serde_json::json!({
                "id": id,
                "arac": req.tool_name,
                "girdi": req.tool_input,
                "cwd": req.cwd,
            }),
        );
        if let Err(e) = yayin {
            log::error!("[kapı] onay isteği arayüze yayınlanamadı: {e}");
            if let Ok(mut m) = state.bekleyen.lock() {
                m.remove(&id);
            }
            // Kullanıcı soruyu GÖRMEDİ → "evet" sayılamaz.
            return HookDecision::Deny("Onay penceresi açılamadı.".into());
        }

        let karar = match rx.recv_timeout(UI_TIMEOUT) {
            Ok(k) => k,
            Err(_) => HookDecision::Deny(format!(
                "{} dakika içinde onaylanmadı — güvenli tarafta kalındı.",
                UI_TIMEOUT.as_secs() / 60
            )),
        };
        if let Ok(mut m) = state.bekleyen.lock() {
            m.remove(&id);
        }
        karar
    })
}

/// Arayüzün kararı: `izin=false` ise `sebep` kullanıcıya/modele gider.
///
/// Bilinmeyen `id` (koşu bitmiş, kullanıcı geç tıklamış) **hata değildir** — sessizce
/// yok sayılır; kullanıcıya anlamsız bir hata göstermenin faydası yok.
#[tauri::command]
pub fn claude_hook_decide(
    state: tauri::State<'_, GateState>,
    id: String,
    izin: bool,
    sebep: String,
) -> Result<(), String> {
    let gonderici = state
        .bekleyen
        .lock()
        .map_err(|e| format!("Onay kuyruğuna erişilemedi: {e}"))?
        .get(&id)
        .cloned();

    let Some(tx) = gonderici else {
        log::debug!("[kapı] karar geldi ama istek yok (id={id}) — koşu bitmiş olabilir");
        return Ok(());
    };

    let karar = if izin {
        HookDecision::Allow
    } else {
        HookDecision::Deny(if sebep.trim().is_empty() {
            "Kullanıcı reddetti.".into()
        } else {
            sebep
        })
    };
    // Alıcı gitmişse (zaman aşımı) sorun değil — yardımcı zaten deny aldı.
    let _ = tx.send(karar);
    Ok(())
}

// ── Testler ───────────────────────────────────────────────────────────────────

#[cfg(test)]
mod tests {
    use super::*;
    use std::sync::atomic::AtomicUsize;

    fn istek(arac: &str) -> Vec<u8> {
        serde_json::json!({
            "session_id": "s1",
            "cwd": "/tmp",
            "hook_event_name": "PreToolUse",
            "tool_name": arac,
            "tool_input": {"file_path": "/tmp/x.txt"},
            "tool_use_id": "tu_1",
            "permission_mode": "default",
        })
        .to_string()
        .into_bytes()
    }

    fn karar(json: &str) -> String {
        let v: serde_json::Value = serde_json::from_str(json).expect("çıktı JSON değil");
        v["hookSpecificOutput"]["permissionDecision"].as_str().unwrap_or("").to_owned()
    }

    /// Kapı yoksa yardımcı **açılmaz**. Bu, üç fail-closed katmanının ikincisi.
    #[test]
    fn soket_yoksa_reddeder() {
        let out = hook_exchange(&istek("Write"), "/tmp/efe-olmayan-soket/h.sock", Duration::from_secs(5));
        assert_eq!(karar(&out), "deny", "soket yokken deny bekleniyordu: {out}");
    }

    #[test]
    fn kapi_allow_gecirir() {
        let gate = HookGate::start(Arc::new(|_req| HookDecision::Allow)).unwrap();
        let out = hook_exchange(&istek("Read"), gate.socket_arg(), Duration::from_secs(5));
        assert_eq!(karar(&out), "allow", "{out}");
    }

    #[test]
    fn kapi_deny_sebebiyle_reddeder() {
        let gate =
            HookGate::start(Arc::new(|_r| HookDecision::Deny("Kullanıcı reddetti".into()))).unwrap();
        let out = hook_exchange(&istek("Bash"), gate.socket_arg(), Duration::from_secs(5));
        assert_eq!(karar(&out), "deny", "{out}");
        assert!(out.contains("Kullanıcı reddetti"), "sebep iletilmedi: {out}");
    }

    /// Handler gerçekten isteğin **içeriğini** görüyor mu? (Boş bir istek de "allow"
    /// alsaydı test yeşil olurdu ama kapı kör olurdu — ders 11.)
    #[test]
    fn handler_istegi_gorur() {
        let gate = HookGate::start(Arc::new(|req: HookRequest| {
            if req.tool_name == "Bash" && req.tool_use_id == "tu_1" {
                HookDecision::Allow
            } else {
                HookDecision::Deny(format!("beklenmeyen istek: {}", req.tool_name))
            }
        }))
        .unwrap();
        assert_eq!(karar(&hook_exchange(&istek("Bash"), gate.socket_arg(), Duration::from_secs(5))), "allow");
        assert_eq!(karar(&hook_exchange(&istek("Read"), gate.socket_arg(), Duration::from_secs(5))), "deny");
    }

    #[test]
    fn bozuk_json_reddedilir() {
        let gate = HookGate::start(Arc::new(|_r| HookDecision::Allow)).unwrap();
        let out = hook_exchange(b"{bu json degil", gate.socket_arg(), Duration::from_secs(5));
        assert_eq!(karar(&out), "deny", "bozuk istek allow aldı: {out}");
    }

    /// **Ölçülen gerçek:** aynı anda 3 onay uçuşta olabilir. Handler'ı bekleterek
    /// örtüşmeyi zorluyoruz — seri işlense üçü de sırayla beklerdi ve sayaç 1'i geçemezdi.
    #[test]
    fn uc_eszamanli_onay() {
        static ANLIK: AtomicUsize = AtomicUsize::new(0);
        static ZIRVE: AtomicUsize = AtomicUsize::new(0);

        let gate = HookGate::start(Arc::new(|_r| {
            let n = ANLIK.fetch_add(1, Ordering::SeqCst) + 1;
            ZIRVE.fetch_max(n, Ordering::SeqCst);
            std::thread::sleep(Duration::from_millis(300));
            ANLIK.fetch_sub(1, Ordering::SeqCst);
            HookDecision::Allow
        }))
        .unwrap();

        let arg = gate.socket_arg().to_owned();
        let elciler: Vec<_> = (0..3)
            .map(|_| {
                let a = arg.clone();
                std::thread::spawn(move || hook_exchange(&istek("Read"), &a, Duration::from_secs(10)))
            })
            .collect();

        for e in elciler {
            assert_eq!(karar(&e.join().unwrap()), "allow");
        }
        assert_eq!(ZIRVE.load(Ordering::SeqCst), 3, "üç onay örtüşmedi — kapı seri işliyor");
    }

    /// Kullanıcı cevap vermezse yardımcı **kendi** timeout'unda deny basar; CLI'ın
    /// bizi öldürmesini beklemeyiz (o durumun ne yaptığı ölçülmedi).
    #[test]
    fn zaman_asiminda_reddeder() {
        let gate = HookGate::start(Arc::new(|_r| {
            std::thread::sleep(Duration::from_secs(3));
            HookDecision::Allow
        }))
        .unwrap();
        let out = hook_exchange(&istek("Write"), gate.socket_arg(), Duration::from_millis(200));
        assert_eq!(karar(&out), "deny", "zaman aşımında deny bekleniyordu: {out}");
    }

    /// Kapı düşerse (oturum bitti) soket de gitmeli — artık bağlanan yardımcı deny alır.
    #[test]
    fn kapi_dusunce_soket_temizlenir() {
        let arg = {
            let gate = HookGate::start(Arc::new(|_r| HookDecision::Allow)).unwrap();
            gate.socket_arg().to_owned()
        }; // gate burada Drop

        #[cfg(not(windows))]
        assert!(!std::path::Path::new(&arg).exists(), "soket dosyası silinmedi: {arg}");

        let out = hook_exchange(&istek("Read"), &arg, Duration::from_secs(5));
        assert_eq!(karar(&out), "deny", "kapı yokken allow alındı: {out}");
    }
}
