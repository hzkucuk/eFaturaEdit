//! Claude Code motoru — ikili tespiti, indirme ve bütünlük doğrulaması.
//!
//! Klasör Ajanı'nın üçüncü motoru **gerçek Claude Code**'dur: BYOK sağlayıcı
//! yerine `claude` ikilisi sürülür. Bu modül ikiliyi **bulur** (sistemde kurulu
//! olan) veya **kurar** (npm registry'den indirir).
//!
//! ## Neden Node/SDK yok
//! Onay kapısı `--settings` ile tanımlanan **PreToolUse hook**'tur; ölçümle
//! doğrulandı (2026-07-16): hook Bash dahil her araçta ateşler, `deny` dosyayı
//! diske yazdırmaz, onay 75 sn bekletilebilir. Node Agent SDK'nın tek gerekçesi
//! olan `canUseTool` köprüsü gereksiz çıktı → Node runtime, bun/SEA paketleme
//! ve sidecar IPC katmanının tamamı iptal edildi. Bkz. CLAUDE.md ders 16.
//!
//! ## Dağıtım kararı
//! İkili **kuruluma gömülmez** (241 MB ham / 66 MB sıkıştırılmış — .dmg'yi
//! 23→89 MB yapardı ve motoru hiç açmayacak kullanıcılar da öderdi). Motor ilk
//! seçildiğinde indirilir; sistemde kurulu `claude` da kabul edilir (gelişmiş
//! ayar). İndirilen paket **açılmadan ÖNCE** registry'nin `dist.integrity`
//! sha512'siyle doğrulanır.
//!
//! ## Sürüm sürüklenmesi
//! Hook/stream-json sözleşmesi sürümle değişebilir; sessizce farklı davranan bir
//! motor, bu projenin en korktuğu sınıftır. Bu yüzden: indirilen sürüm
//! [`PINNED_VERSION`] ile **sabittir**, sistemdeki ikili ise [`MIN_VERSION`]
//! tabanının altındaysa **açıkça reddedilir** (sessizce kullanılmaz).

use base64::Engine as _;
use serde::Serialize;
use sha2::{Digest, Sha512};
use std::path::{Path, PathBuf};
use std::time::Instant;
use tauri::{Emitter, Manager};

/// Ölçülerek çalıştığı doğrulanan **en düşük** sürüm (2026-07-16: `--settings`
/// PreToolUse sözleşmesi bu sürümde birebir çalıştı — 5/5 araç, `deny` diski
/// korudu). Altındaki sürümler denenmedi; sessizce farklı davranmasındansa
/// kullanıcıya açıkça söylenir.
const MIN_VERSION: (u32, u32, u32) = (2, 1, 181);

/// Uygulamanın indirdiği sürüm — **sabit**. Yükseltmek isteyen: bu sabiti
/// değiştir ve yeni sürümü ÖLÇ (hook ateşliyor mu, deny tutuyor mu, sandbox
/// kaçış kapısı kapalı mı). Ölçmeden yükseltme.
const PINNED_VERSION: &str = "2.1.211";

const REGISTRY: &str = "https://registry.npmjs.org";

/// Ağ zaman aşımları — çıplak `Client::new()` YASAK (CLAUDE.md ders 5b:
/// timeout'suz istek 2 saat "Düşünüyor…" gösterdi).
const CONNECT_TIMEOUT: std::time::Duration = std::time::Duration::from_secs(15);
/// İndirme 67 MB — genel timeout cömert olmalı, yoksa yavaş hatta koparız.
const DOWNLOAD_TIMEOUT: std::time::Duration = std::time::Duration::from_secs(600);

/// Motorun durumu — arayüz buna göre "kur" düğmesi mi, "hazır" rozeti mi gösterir.
#[derive(Debug, Serialize, Clone)]
pub struct EngineStatus {
    /// `"managed"` (uygulamanın indirdiği) · `"system"` (PATH'teki) · `"none"`.
    pub source: String,
    pub path: Option<String>,
    pub version: Option<String>,
    /// Kullanıma hazır mı? `false` ise [`Self::reason`] doldurulur.
    pub ready: bool,
    /// Hazır değilse **Türkçe** sebep (kullanıcıya gösterilir; stack trace yok).
    pub reason: Option<String>,
    /// PATH'te bulunan sürüm — taban altındaysa da bilgi olarak taşınır ki
    /// arayüz "kurulu ama çok eski (X)" diyebilsin.
    pub system_version: Option<String>,
    /// İndirilecek/indirilmiş sabit sürüm (arayüzde gösterilir).
    pub pinned_version: String,
}

impl EngineStatus {
    fn none(reason: impl Into<String>, system_version: Option<String>) -> Self {
        Self {
            source: "none".into(),
            path: None,
            version: None,
            ready: false,
            reason: Some(reason.into()),
            system_version,
            pinned_version: PINNED_VERSION.into(),
        }
    }
}

/// Bu platformun npm paket adı. Desteklenmeyen platformda `Err`.
///
/// Ölçüldü (2026-07-16): npm `os`/`cpu` alanlarıyla optional-deps'i platforma
/// göre süzer; 8 paketin hepsi mevcut ve 5 CI runner'ımızın tamamını kapsar.
fn platform_package() -> Result<&'static str, String> {
    Ok(match (std::env::consts::OS, std::env::consts::ARCH) {
        ("macos", "aarch64") => "claude-code-darwin-arm64",
        ("macos", "x86_64") => "claude-code-darwin-x64",
        ("linux", "x86_64") => "claude-code-linux-x64",
        ("linux", "aarch64") => "claude-code-linux-arm64",
        ("windows", "x86_64") => "claude-code-win32-x64",
        ("windows", "aarch64") => "claude-code-win32-arm64",
        (os, arch) => {
            return Err(format!(
                "Claude Code motoru bu platformda yok: {os}/{arch}. \
                 Klasör Ajanı'nın kendi motorunu (BYOK) kullanabilirsiniz."
            ))
        }
    })
}

/// `"2.1.211 (Claude Code)"` → `(2, 1, 211)`. Beklenmeyen biçimde `None`.
fn parse_version(s: &str) -> Option<(u32, u32, u32)> {
    let first = s.split_whitespace().next()?;
    let mut it = first.split('.');
    let major = it.next()?.parse().ok()?;
    let minor = it.next()?.parse().ok()?;
    // Yama alanı "211-beta" gibi ek taşıyabilir — rakam önekini al.
    let patch_raw = it.next()?;
    let digits: String = patch_raw.chars().take_while(|c| c.is_ascii_digit()).collect();
    let patch = digits.parse().ok()?;
    Some((major, minor, patch))
}

/// İkiliyi `--version` ile yokla. Çalışmıyorsa `None`.
///
/// Dış süreç → çıkış kodu + stderr **her zaman** loglanır (CLAUDE.md ders 2:
/// hata yutmak teşhisi yıllarca geciktirdi).
fn probe(path: &Path) -> Option<String> {
    let t0 = Instant::now();
    let out = std::process::Command::new(path).arg("--version").output();
    match out {
        Ok(o) if o.status.success() => {
            let v = String::from_utf8_lossy(&o.stdout).trim().to_string();
            log::debug!(
                "[motor] yoklama başarılı — {} · sürüm '{}' · {} ms",
                path.display(),
                v,
                t0.elapsed().as_millis()
            );
            Some(v)
        }
        Ok(o) => {
            log::warn!(
                "[motor] yoklama başarısız — {} · çıkış kodu {:?} · stderr: {}",
                path.display(),
                o.status.code(),
                String::from_utf8_lossy(&o.stderr).trim()
            );
            None
        }
        Err(e) => {
            log::debug!("[motor] yoklanamadı — {} · {e}", path.display());
            None
        }
    }
}

/// Uygulamanın indirdiği ikilinin yolu: `<appdata>/claude-engine/<sürüm>/claude[.exe]`.
///
/// Sürüm klasöre girer ki [`PINNED_VERSION`] yükseltilince eski ikili sessizce
/// kullanılmasın (aynı ada yazmak, "yükselttim sandım" hatası üretir).
fn managed_binary(app: &tauri::AppHandle) -> Result<PathBuf, String> {
    let dir = app
        .path()
        .app_data_dir()
        .map_err(|e| format!("Uygulama veri klasörü bulunamadı: {e}"))?;
    let exe = if cfg!(windows) { "claude.exe" } else { "claude" };
    Ok(dir.join("claude-engine").join(PINNED_VERSION).join(exe))
}

/// Sürüm tabanını denetle; geçerse `Ok(sürüm metni)`.
fn check_floor(version_text: &str) -> Result<(), String> {
    match parse_version(version_text) {
        Some(v) if v >= MIN_VERSION => Ok(()),
        Some(v) => Err(format!(
            "Kurulu Claude Code sürümü çok eski: {}.{}.{} \
             (en az {}.{}.{} gerekiyor). Uygulamanın kendi motorunu kurabilir \
             veya 'claude' kurulumunuzu güncelleyebilirsiniz.",
            v.0, v.1, v.2, MIN_VERSION.0, MIN_VERSION.1, MIN_VERSION.2
        )),
        None => Err(format!(
            "Claude Code sürümü anlaşılamadı ('{version_text}'). \
             Uygulamanın kendi motorunu kurmanız önerilir."
        )),
    }
}

/// Motorun durumunu bildir.
///
/// `prefer_system = true` → PATH'teki `claude` kullanılır (gelişmiş ayar).
/// `false` (varsayılan) → uygulamanın indirdiği sabit sürüm kullanılır; yoksa
/// `ready:false` döner ve arayüz "kur" akışını gösterir. Sistemdekine **sessizce**
/// düşülmez: hangi ikilinin sürüldüğü kullanıcı için öngörülebilir olmalı.
#[tauri::command]
pub fn claude_engine_status(app: tauri::AppHandle, prefer_system: bool) -> EngineStatus {
    // PATH'teki ikili — hem gelişmiş ayar için, hem "kurulu ama eski" bilgisi için.
    let system = probe(Path::new("claude"));

    if prefer_system {
        return match &system {
            Some(v) => match check_floor(v) {
                Ok(()) => EngineStatus {
                    source: "system".into(),
                    path: Some("claude".into()),
                    version: Some(v.clone()),
                    ready: true,
                    reason: None,
                    system_version: system.clone(),
                    pinned_version: PINNED_VERSION.into(),
                },
                Err(why) => EngineStatus::none(why, system.clone()),
            },
            None => EngineStatus::none(
                "Sistemde 'claude' bulunamadı (PATH'te yok). Claude Code'u kurun \
                 ya da bu ayarı kapatıp uygulamanın kendi motorunu indirin.",
                None,
            ),
        };
    }

    let managed = match managed_binary(&app) {
        Ok(p) => p,
        Err(e) => return EngineStatus::none(e, system),
    };
    if !managed.exists() {
        return EngineStatus::none(
            format!("Claude Code motoru henüz kurulu değil (sürüm {PINNED_VERSION} indirilecek)."),
            system,
        );
    }
    match probe(&managed) {
        Some(v) => EngineStatus {
            source: "managed".into(),
            path: Some(managed.to_string_lossy().into_owned()),
            version: Some(v),
            ready: true,
            reason: None,
            system_version: system,
            pinned_version: PINNED_VERSION.into(),
        },
        None => EngineStatus::none(
            "Kurulu motor çalıştırılamadı (dosya bozulmuş olabilir). \
             Yeniden kurmayı deneyin.",
            system,
        ),
    }
}

/// İndirme ilerlemesi — arayüz bunu ilerleme çubuğuna bağlar.
#[derive(Serialize, Clone)]
struct Progress {
    /// `"indiriliyor"` · `"dogrulaniyor"` · `"aciliyor"`
    phase: String,
    downloaded: u64,
    total: u64,
}

/// npm registry'den paket meta verisi: (tarball URL, sha512 integrity).
async fn fetch_meta(client: &reqwest::Client, pkg: &str) -> Result<(String, String), String> {
    // Scope'lu ad URL'de kodlanır: @anthropic-ai%2fclaude-code-darwin-arm64
    let url = format!("{REGISTRY}/@anthropic-ai%2f{pkg}/{PINNED_VERSION}");
    log::info!("[motor] meta isteniyor — {pkg} {PINNED_VERSION}");
    let resp = client
        .get(&url)
        .send()
        .await
        .map_err(|e| format!("Paket bilgisi alınamadı: {}", chain(&e)))?;
    let status = resp.status();
    let body = resp
        .text()
        .await
        .map_err(|e| format!("Paket bilgisi okunamadı: {}", chain(&e)))?;
    if !status.is_success() {
        // Ham gövdeyi logla — "bilinmeyen hata" deme (CLAUDE.md ders 14).
        log::error!("[motor] meta HTTP {status} — gövde: {}", body.chars().take(500).collect::<String>());
        return Err(format!("Paket bilgisi alınamadı (HTTP {status})."));
    }
    let json: serde_json::Value =
        serde_json::from_str(&body).map_err(|e| format!("Paket bilgisi çözümlenemedi: {e}"))?;
    let tarball = json["dist"]["tarball"]
        .as_str()
        .ok_or("Paket bilgisinde indirme adresi yok.")?
        .to_string();
    let integrity = json["dist"]["integrity"]
        .as_str()
        .ok_or("Paket bilgisinde bütünlük özeti (sha512) yok — indirme reddedildi.")?
        .to_string();
    Ok((tarball, integrity))
}

/// `reqwest::Error`'un üst mesajı sebebi gizler ("error sending request for
/// url ...") — kaynak zincirini aç (CLAUDE.md ders 5b).
fn chain(e: &reqwest::Error) -> String {
    use std::error::Error;
    let mut s = e.to_string();
    let mut src: Option<&(dyn Error + 'static)> = e.source();
    while let Some(inner) = src {
        s.push_str(" → ");
        s.push_str(&inner.to_string());
        src = inner.source();
    }
    s
}

/// Motoru indir, **açmadan önce** sha512 doğrula, `<appdata>` altına çıkar.
///
/// Başarısızlıkta hata **görünürdür** — sessizce eski/başka bir ikiliye düşülmez
/// (CLAUDE.md ders 3).
#[tauri::command]
pub async fn claude_engine_install(app: tauri::AppHandle) -> Result<EngineStatus, String> {
    let pkg = platform_package()?;
    let target = managed_binary(&app)?;
    if target.exists() {
        log::info!("[motor] zaten kurulu — {}", target.display());
        return Ok(claude_engine_status(app, false));
    }

    let client = http_client()?;
    let app2 = app.clone();
    install_to(&client, pkg, &target, &move |phase, downloaded, total| {
        let _ = app2.emit(
            "claude-engine-progress",
            Progress { phase: phase.into(), downloaded, total },
        );
    })
    .await?;

    // Kurduk demekle kurulduğu aynı şey değil — ÇALIŞTIRIP doğrula (ders 1).
    let status = claude_engine_status(app, false);
    if !status.ready {
        return Err(status
            .reason
            .unwrap_or_else(|| "Motor kuruldu ama çalıştırılamadı.".into()));
    }
    log::info!(
        "[motor] kuruldu ve doğrulandı — {} · {:?}",
        target.display(),
        status.version
    );
    Ok(status)
}

fn http_client() -> Result<reqwest::Client, String> {
    reqwest::Client::builder()
        .connect_timeout(CONNECT_TIMEOUT)
        .timeout(DOWNLOAD_TIMEOUT)
        .build()
        .map_err(|e| format!("Ağ istemcisi kurulamadı: {e}"))
}

/// İndirme → doğrulama → açma çekirdeği. `AppHandle`'dan bağımsızdır ki
/// **gerçek registry'ye karşı test edilebilsin** (bkz. `indirme_ucdan_uca`).
/// İlerleme bir geri çağırma ile bildirilir (Tauri olayı yalnızca sarmalayıcıda).
async fn install_to(
    client: &reqwest::Client,
    pkg: &str,
    target: &Path,
    // `Send + Sync`: Tauri komutunun future'ı Send olmak zorunda ve bu geri
    // çağırma `await` sınırlarını aşıyor.
    progress: &(dyn Fn(&str, u64, u64) + Send + Sync),
) -> Result<(), String> {
    let (tarball, integrity) = fetch_meta(client, pkg).await?;
    log::info!("[motor] indiriliyor — {pkg} {PINNED_VERSION}");
    let t0 = Instant::now();

    let resp = client
        .get(&tarball)
        .send()
        .await
        .map_err(|e| format!("Motor indirilemedi: {}", chain(&e)))?;
    if !resp.status().is_success() {
        return Err(format!("Motor indirilemedi (HTTP {}).", resp.status()));
    }
    let total = resp.content_length().unwrap_or(0);

    // Parça parça oku ki ilerleme gösterilebilsin (67 MB — sessiz bekleme kötü UX).
    let mut buf: Vec<u8> = Vec::with_capacity(total as usize);
    let mut resp = resp;
    while let Some(chunk) = resp
        .chunk()
        .await
        .map_err(|e| format!("İndirme kesildi: {}", chain(&e)))?
    {
        buf.extend_from_slice(&chunk);
        progress("indiriliyor", buf.len() as u64, total);
    }
    log::info!(
        "[motor] indirildi — {} bayt · {} ms",
        buf.len(),
        t0.elapsed().as_millis()
    );

    // --- BÜTÜNLÜK: açmadan ÖNCE. Eşleşmezse dosyaya dokunma.
    progress("dogrulaniyor", buf.len() as u64, total);
    let digest = Sha512::digest(&buf);
    let computed = format!(
        "sha512-{}",
        base64::engine::general_purpose::STANDARD.encode(digest)
    );
    if computed != integrity {
        log::error!(
            "[motor] BÜTÜNLÜK UYUŞMADI — beklenen {} · hesaplanan {}",
            integrity,
            computed
        );
        return Err(
            "İndirilen motor doğrulanamadı (sha512 uyuşmadı). Kurulum iptal edildi.".into(),
        );
    }
    log::info!("[motor] sha512 doğrulandı");

    // --- AÇ: tarball düzeni `package/claude` (+ README/LICENSE). Ölçüldü: 4 dosya.
    progress("aciliyor", buf.len() as u64, total);
    let dir = target
        .parent()
        .ok_or("Hedef klasör belirlenemedi.")?
        .to_path_buf();
    std::fs::create_dir_all(&dir)
        .map_err(|e| format!("Klasör oluşturulamadı ({}): {e}", dir.display()))?;

    let gz = flate2::read::GzDecoder::new(std::io::Cursor::new(&buf));
    let mut archive = tar::Archive::new(gz);
    let mut wrote = false;
    for entry in archive
        .entries()
        .map_err(|e| format!("Paket açılamadı: {e}"))?
    {
        let mut entry = entry.map_err(|e| format!("Paket girdisi okunamadı: {e}"))?;
        let path = entry
            .path()
            .map_err(|e| format!("Paket girdisi çözümlenemedi: {e}"))?
            .to_path_buf();
        // Yalnızca ikiliyi al. (Tar girdilerine körü körüne `unpack` etmek
        // zip-slip sınıfı bir risktir; adı denetleyip tek dosya yazıyoruz.)
        let is_binary = path.file_name().map(|n| n == "claude" || n == "claude.exe") == Some(true);
        if !is_binary {
            continue;
        }
        entry
            .unpack(&target)
            .map_err(|e| format!("Motor yazılamadı ({}): {e}", target.display()))?;
        wrote = true;
        break;
    }
    if !wrote {
        return Err("Pakette çalıştırılabilir motor bulunamadı — kurulum iptal edildi.".into());
    }

    #[cfg(unix)]
    {
        use std::os::unix::fs::PermissionsExt;
        std::fs::set_permissions(&target, std::fs::Permissions::from_mode(0o755))
            .map_err(|e| format!("Çalıştırma izni verilemedi: {e}"))?;
    }

    log::info!("[motor] açıldı — {}", target.display());
    Ok(())
}

// ── Motoru sürme: `--settings` + spawn ────────────────────────────────────────

/// Hook onayı için CLI'a verilen bekleme payı (**saniye**, matcher düzeyinde —
/// `HookCallbackMatcher.timeout`). Yardımcının kendi payı (570 sn) bunun ALTINDA:
/// kararı biz verelim, CLI bizi öldürmesin. 75 sn'lik onay ölçüldü, sorunsuz bekledi.
const HOOK_TIMEOUT_SEC: u32 = 600;

/// `--settings` JSON'unu kur — **saf fonksiyon** (birim testli; bu JSON'daki bir hata
/// sessizce ya kapıyı açar ya motoru işlemez kılar, ikisi de ölçülmeden fark edilmez).
///
/// `sandbox_destekli=false` (Windows) → `sandbox` anahtarı **hiç yazılmaz**.
///
/// ## Neden bu bayraklar (hepsi ölçüldü, 2026-07-16)
/// - `allowUnsandboxedCommands:false` — **şart**. Varsayılan bırakılırsa sandbox komutu
///   keser, Claude Code aynı komutu **sandbox'sız yeniden dener** ve izin ister; otomatik
///   onaylayan bir hook kaçışı geçirir → dosya diske yazılır. "Sandbox kesti" mesajını
///   görüp durmak yanlış sonuç verdirir (CLAUDE.md ders 17).
/// - `failIfUnavailable:true` — Linux'ta `bubblewrap` yoksa **görünür hata**; sessizce
///   sandbox'sız koşmak bu projede yasak (ders 3).
/// - `autoAllowBashIfSandboxed:true` — bash'i yalnızca **gerçekten** sandbox'lıyken
///   otomatik geçir. Windows'ta sandbox olmadığı için bu bayrak da verilmez → bash sorulur.
///
/// ⚠️ **`sandbox` YAZMAYI kilitler, OKUMAYI kilitlemez** (ölçüldü, `allowRead`/
/// `allowManagedReadPathsOnly` denendi — değişmedi). Kullanıcıya "klasör dışına yazamaz"
/// denebilir; **"okuyamaz" DENEMEZ.**
fn build_settings_json(exe: &Path, socket_arg: &str, root: &Path, sandbox_destekli: bool) -> String {
    // ⚠️ Hook komutu **kabuktan** geçer ve `productName` = "e-Fatura Edit" — BOŞLUKLU.
    // Tırnaklanmazsa kabuk yolu boşlukta böler → yardımcı hiç çalışmaz → `default` modu
    // her şeyi reddeder → "motor bozuk" görünür (sessiz-ish başarısızlık, ders 1).
    let command = format!("{} --hook-helper {}", sh_quote(&exe.to_string_lossy()), sh_quote(socket_arg));

    let mut settings = serde_json::json!({
        "hooks": {
            "PreToolUse": [{
                "matcher": "*",
                "timeout": HOOK_TIMEOUT_SEC,
                "hooks": [{ "type": "command", "command": command }]
            }]
        }
    });

    if sandbox_destekli {
        settings["sandbox"] = serde_json::json!({
            "enabled": true,
            "failIfUnavailable": true,
            "allowUnsandboxedCommands": false,
            "autoAllowBashIfSandboxed": true,
            "filesystem": { "allowWrite": [root.to_string_lossy()] }
        });
    }
    settings.to_string()
}

/// POSIX kabuğu için tek tırnakla kaçır. Windows'ta hook komutunu `cmd` çalıştırır;
/// orada tek tırnak işe yaramaz → çift tırnak kullanılır.
fn sh_quote(s: &str) -> String {
    if cfg!(windows) {
        format!("\"{}\"", s.replace('"', "\"\""))
    } else {
        format!("'{}'", s.replace('\'', r"'\''"))
    }
}

/// Bu platformda `claude`'un sandbox'ı var mı?
///
/// **Windows'ta YOK** (ölçüldü/SDK şeması) → orada bash otomatik onaylanamaz, sorulur.
/// Linux'ta `bubblewrap` gerekir; yoksa `failIfUnavailable:true` sayesinde motor
/// **görünür** şekilde hata verir — sessizce korumasız koşmaz.
fn sandbox_destekli() -> bool {
    !cfg!(windows)
}

/// Motoru sür: `claude`'u onay kapısı bağlıyken çalıştır.
///
/// Kapı **spawn'dan ÖNCE** kurulur (adı önce biz kaparız — bkz. `HookGate::start`).
/// Bu turda stdout **ham** toplanır; `stream-json` ayrıştırma bir sonraki adımda.
///
/// `helper_exe` = hook yardımcısı olarak çağrılacak ikili; üretimde
/// [`helper_exe()`] (= bu uygulama). **Parametre olmasının sebebi:** entegrasyon
/// testinde `current_exe()` *test* ikilisini gösterir; testin gerçek uygulamayı
/// çağırabilmesi gerekiyor (ders 13: testin kendi kopyasını ölçme).
///
/// Bloke eder (süreç bitene kadar) — çağıran `spawn_blocking` kullanmalı.
///
/// ⛔ `--permission-mode bypassPermissions` **KULLANILMAZ**: ölçüldü, yardımcı yoksa
/// veya çökerse ajan **serbest kalıyor** (fail-open). `default` = fail-closed taban.
pub fn run_claude(
    bin: &Path,
    root: &Path,
    prompt: &str,
    helper_exe: &Path,
    handler: crate::agent_hook::Handler,
) -> Result<ClaudeRun, String> {
    let gate = crate::agent_hook::HookGate::start(handler)
        .map_err(|e| format!("Onay kapısı açılamadı: {e}"))?;
    let settings = build_settings_json(helper_exe, gate.socket_arg(), root, sandbox_destekli());

    // Dış süreç → giriş boyutu, süre, çıkış kodu, stderr **her zaman** loglanır
    // (ders 5b: log'suz dış çağrı 2 saat "Düşünüyor…" gösterdi). Görev metni ve
    // kullanıcı verisi loglanmaz — yalnızca boyutu.
    log::info!(
        "[motor] koşu başlıyor — ikili={} · kök={} · görev {} bayt · sandbox={} · ayar {} bayt",
        bin.display(),
        root.display(),
        prompt.len(),
        sandbox_destekli(),
        settings.len()
    );

    let t0 = Instant::now();
    let out = std::process::Command::new(bin)
        .current_dir(root)
        .arg("-p")
        .arg(prompt)
        .args(["--output-format", "stream-json", "--verbose"])
        .args(["--permission-mode", "default"])
        .arg("--settings")
        .arg(&settings)
        .output()
        .map_err(|e| format!("Motor çalıştırılamadı: {e}"))?;

    let sure = t0.elapsed();
    let stderr = String::from_utf8_lossy(&out.stderr).trim().to_string();
    let stdout = String::from_utf8_lossy(&out.stdout).to_string();

    // Çıkış kodu tanının yarısıdır (ders 2) — başarıda da yazılır ki "çalıştı ama boş
    // döndü" durumu günlükten görülebilsin.
    log::info!(
        "[motor] koşu bitti — çıkış kodu {:?} · {} ms · stdout {} bayt",
        out.status.code(),
        sure.as_millis(),
        stdout.len()
    );
    if !stderr.is_empty() {
        log::warn!("[motor] stderr: {stderr}");
    }

    Ok(ClaudeRun { code: out.status.code(), stdout, stderr, ms: sure.as_millis() })
}

/// Hook yardımcısı olarak çağrılacak ikili = **bu uygulama** (`--hook-helper` modu).
/// Ayrı bir yardımcı ikili shiplemiyoruz; `lib.rs::run()` argümanı en başta yakalar.
pub fn helper_exe() -> Result<PathBuf, String> {
    std::env::current_exe()
        .map_err(|e| format!("Uygulama yolu bulunamadı (hook yardımcısı çağrılamaz): {e}"))
}

/// Bir motor koşusunun ham sonucu. (`stdout` = stream-json satırları; ayrıştırma sonraki adım.)
#[derive(Debug)]
pub struct ClaudeRun {
    pub code: Option<i32>,
    pub stdout: String,
    pub stderr: String,
    pub ms: u128,
}

#[cfg(test)]
mod tests {
    use super::*;

    /// `--settings`'i gerçekte olduğu gibi kur ve **ayrıştırarak** denetle
    /// (dize içinde `contains` aramak, biçim değişince yalan söyler — ders 11).
    fn ayarlar(sandbox: bool) -> serde_json::Value {
        let json = build_settings_json(
            // Gerçek dert: macOS'ta yol "/Applications/e-Fatura Edit.app/…" — BOŞLUKLU.
            Path::new("/Applications/e-Fatura Edit.app/Contents/MacOS/e-Fatura Edit"),
            "/tmp/efe-abc/h.sock",
            Path::new("/tmp/proje kökü"),
            sandbox,
        );
        serde_json::from_str(&json).expect("settings geçerli JSON değil")
    }

    fn hook_komutu(v: &serde_json::Value) -> String {
        v["hooks"]["PreToolUse"][0]["hooks"][0]["command"].as_str().unwrap_or("").to_owned()
    }

    /// Boşluklu exe yolu tırnaklanmazsa kabuk onu böler → yardımcı hiç çalışmaz →
    /// `default` her şeyi reddeder. Hata vermez, sadece motor işlemez (ders 10).
    #[test]
    fn hook_komutunda_bosluklu_yol_tirnaklanir() {
        let cmd = hook_komutu(&ayarlar(true));
        assert!(cmd.contains("--hook-helper"), "hook komutu eksik: {cmd}");

        // Kabuğun gerçekte kaç parçaya böleceğini SAY: yol tek parça kalmalı.
        // (Tırnaksız olsaydı "/Applications/e-Fatura" ilk parça olurdu.)
        let ilk = cmd.split_whitespace().next().unwrap_or("");
        assert!(
            ilk.starts_with('\'') || ilk.starts_with('"'),
            "exe yolu tırnaksız — boşlukta bölünür: {cmd}"
        );
        assert!(cmd.contains("e-Fatura Edit"), "exe yolu bozulmuş: {cmd}");
    }

    /// ⛔ Ölçüldü: `bypassPermissions` fail-open. Bu dizenin ayarlara/komuta
    /// sızmadığını test **kilitler** — biri "kolaylık olsun" diye eklerse kırılır.
    #[test]
    fn bypass_permissions_asla_gecmez() {
        let json = serde_json::to_string(&ayarlar(true)).unwrap();
        assert!(!json.contains("bypassPermissions"), "fail-open mod ayarlara sızmış: {json}");
    }

    /// Sandbox'ın kaçış kapısı: varsayılan bırakılırsa Claude Code komutu sandbox'sız
    /// YENİDEN dener ve otomatik onay onu geçirir → dosya diske yazılır (ölçüldü).
    #[test]
    fn sandbox_kacis_kapisi_kapali() {
        let s = &ayarlar(true)["sandbox"];
        assert_eq!(s["enabled"], true);
        assert_eq!(s["allowUnsandboxedCommands"], false, "kaçış kapısı açık — koruma delinir");
        assert_eq!(s["failIfUnavailable"], true, "sessiz sandbox'sız koşu yasak (ders 3)");
        assert_eq!(s["autoAllowBashIfSandboxed"], true);
        assert_eq!(s["filesystem"]["allowWrite"][0], "/tmp/proje kökü");
    }

    /// Windows'ta sandbox YOK → anahtar hiç yazılmamalı. Yazılsaydı
    /// `failIfUnavailable:true` motoru **hiç açtırmazdı** (ya da sessizce yanlış
    /// güvence verirdi: "bash kilitli" — değil, orada tek koruma onay kapısı).
    #[test]
    fn sandboxsuz_platformda_anahtar_yazilmaz() {
        let v = ayarlar(false);
        assert!(v.get("sandbox").is_none(), "sandbox'sız platformda anahtar yazılmış: {v}");
        // Kapı yine de kurulu olmalı — onay her platformda şart.
        assert!(hook_komutu(&v).contains("--hook-helper"), "hook kapısı düşmüş: {v}");
    }

    /// Matcher `*` = her araç (5/5 ateşlediği ölçüldü) ve timeout **saniye**.
    #[test]
    fn hook_her_araca_baglanir() {
        let h = &ayarlar(true)["hooks"]["PreToolUse"][0];
        assert_eq!(h["matcher"], "*");
        assert_eq!(h["timeout"], 600);
    }

    #[test]
    fn surum_ayristirma() {
        assert_eq!(parse_version("2.1.211 (Claude Code)"), Some((2, 1, 211)));
        assert_eq!(parse_version("2.1.181"), Some((2, 1, 181)));
        assert_eq!(parse_version("10.20.30 (x)"), Some((10, 20, 30)));
        // Yama eki: rakam öneki alınır.
        assert_eq!(parse_version("2.1.211-beta.1"), Some((2, 1, 211)));
        assert_eq!(parse_version("bilinmiyor"), None);
        assert_eq!(parse_version(""), None);
        assert_eq!(parse_version("2.1"), None);
    }

    #[test]
    fn surum_tabani() {
        // Ölçülen taban tam sınırda kabul edilmeli.
        assert!(check_floor("2.1.181 (Claude Code)").is_ok());
        assert!(check_floor("2.1.211 (Claude Code)").is_ok());
        assert!(check_floor("3.0.0").is_ok());
        // Altındakiler AÇIKÇA reddedilir — sessizce kullanılmaz.
        assert!(check_floor("2.1.180").is_err());
        assert!(check_floor("2.0.999").is_err());
        assert!(check_floor("1.9.9").is_err());
        // Anlaşılmayan sürüm de reddedilir (tahminle çalıştırma).
        assert!(check_floor("bilinmiyor").is_err());
    }

    /// **Uçtan uca, gerçek registry'ye karşı:** indir → sha512 → aç → ÇALIŞTIR.
    ///
    /// Neden `#[ignore]`: ağ gerektirir ve ~67 MB indirir; CI'da her koşuda
    /// çalışması istenmez. Elle:
    /// ```text
    /// cargo test --lib agent_cli::tests::indirme_ucdan_uca -- --ignored --nocapture
    /// ```
    /// Bu test [`install_to`]'nun **gerçek kodunu** sürer (testin kendi kopyasını
    /// değil — CLAUDE.md ders 13) ve sonucu **diskten** doğrular (ders 17:
    /// "hata vermedi" ≠ "iş gördü").
    #[tokio::test]
    #[ignore]
    async fn indirme_ucdan_uca() {
        let pkg = platform_package().expect("platform desteklenmeli");
        let dir = std::env::temp_dir().join(format!("efe-motor-test-{}", std::process::id()));
        let _ = std::fs::remove_dir_all(&dir);
        let target = dir.join(if cfg!(windows) { "claude.exe" } else { "claude" });

        let client = http_client().expect("istemci");
        let phases = std::sync::Mutex::new(Vec::<String>::new());
        install_to(&client, pkg, &target, &|phase, _d, _t| {
            let mut p = phases.lock().unwrap();
            if p.last().map(|x| x != phase).unwrap_or(true) {
                p.push(phase.to_string());
            }
        })
        .await
        .expect("kurulum başarılı olmalı");

        // 1) Diskte gerçekten var mı ve makul boyutta mı? (ölçüldü: ~231 MB)
        assert!(target.exists(), "ikili diske yazılmadı: {}", target.display());
        let size = std::fs::metadata(&target).expect("stat").len();
        assert!(size > 100_000_000, "ikili beklenenden küçük: {size} bayt");

        // 2) ÇALIŞIYOR mu ve sabitlediğimiz sürüm mü? ("kurdum" ≠ "çalışıyor")
        let v = probe(&target).expect("indirilen ikili çalışmalı");
        assert!(v.starts_with(PINNED_VERSION), "sürüm beklenenden farklı: {v}");
        assert!(check_floor(&v).is_ok(), "sabit sürüm tabanı geçmeli: {v}");

        // 3) İlerleme fazları sırayla bildirildi mi? (UI bunlara bağlanacak)
        assert_eq!(
            *phases.lock().unwrap(),
            vec!["indiriliyor", "dogrulaniyor", "aciliyor"]
        );

        let _ = std::fs::remove_dir_all(&dir);
    }

    #[test]
    fn platform_paketi_bu_makinede_cozulur() {
        // Desteklenen bir platformda derleniyorsak ad boş olmamalı.
        if matches!(
            (std::env::consts::OS, std::env::consts::ARCH),
            ("macos", "aarch64") | ("macos", "x86_64") | ("linux", "x86_64")
                | ("linux", "aarch64") | ("windows", "x86_64") | ("windows", "aarch64")
        ) {
            let pkg = platform_package().expect("paket adı çözülmeli");
            assert!(pkg.starts_with("claude-code-"), "beklenmeyen paket adı: {pkg}");
        }
    }
}
