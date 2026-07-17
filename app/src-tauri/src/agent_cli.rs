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
use serde::{Deserialize, Serialize};
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
    /// Bu platformda sandbox var mı? **Arayüz bunu tahmin etmemeli** — bilgilendirme
    /// ekranının dürüstlüğü buna bağlı: `false` (Windows) ise "klasör dışına yazamaz"
    /// **denemez**, orada tek koruma onay kapısıdır (CLAUDE.md ders 15).
    pub sandbox: bool,
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
            sandbox: sandbox_destekli(),
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
            sandbox: sandbox_destekli(),
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
            sandbox: sandbox_destekli(),
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

// ── stream-json akışı ─────────────────────────────────────────────────────────

/// Motorun akışından çıkan olay — arayüz bunları görür.
///
/// Alan/varyant adları **ölçülerek** belirlendi (gerçek `claude` 2.1.181 çıktısı,
/// 2026-07-17); belgeden veya hatırlanandan değil (ders 10).
#[derive(Debug, Clone, Serialize, PartialEq)]
#[serde(tag = "tur")]
pub enum ClaudeEvent {
    /// `system`/`init` — koşu başladı.
    Baslangic { model: String, oturum: String },
    /// `thinking` bloğu. **İçeriği taşınmıyor**: kullanıcıya değer katmıyor, günlüğe
    /// düşerse gereksiz veri sızdırır. Yalnızca "çalışıyor" sinyali.
    Dusunuyor,
    /// `assistant` → `text`.
    Metin { metin: String },
    /// `assistant` → `tool_use`. Onay kapısı zaten sorar; bu, akışta göstermek için.
    AracCagrisi { arac: String, girdi: serde_json::Value },
    /// `user` → `tool_result`.
    AracSonucu { hata: bool, icerik: String },
    /// `result` — koşu bitti.
    Bitti {
        /// ⚠️ **`is_error`'a ALDANMA.** Ölçüldü: her araç reddedilse bile `subtype`
        /// `"success"` ve `is_error` `false` gelir. "İş görüldü mü?" sorusunun cevabı
        /// bu değil; [`Self::Bitti::reddedilen`] boş mu, ona bak.
        basarili: bool,
        sure_ms: u64,
        ozet: Option<String>,
        /// Kapının (yani kullanıcının) reddettiği araçlar — koşunun **gerçek** karnesi.
        reddedilen: Vec<Reddedilen>,
        maliyet_usd: Option<f64>,
    },
}

/// `result.permission_denials[]` girdisi — ne engellendi.
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct Reddedilen {
    #[serde(default)]
    pub tool_name: String,
    #[serde(default)]
    pub tool_input: serde_json::Value,
}

/// stream-json'un bir satırı.
///
/// ⚠️ **`Diger` şart.** Ölçüldü: belgelenmemiş `rate_limit_event` satırı geliyor ve
/// gelecekte yenileri eklenebilir. Bilinmeyen tipe hata verirsek motor, Anthropic yeni
/// bir satır tipi ekler eklemez kırılır — üstelik sebebi anlaşılmaz olur.
#[derive(Deserialize)]
#[serde(tag = "type")]
enum Satir {
    #[serde(rename = "system")]
    System {
        #[serde(default)]
        subtype: String,
        #[serde(default)]
        model: String,
        #[serde(default)]
        session_id: String,
    },
    #[serde(rename = "assistant")]
    Assistant { message: Mesaj },
    #[serde(rename = "user")]
    User { message: Mesaj },
    #[serde(rename = "result")]
    Result {
        #[serde(default)]
        is_error: bool,
        #[serde(default)]
        duration_ms: u64,
        #[serde(default)]
        result: Option<String>,
        #[serde(default)]
        total_cost_usd: Option<f64>,
        #[serde(default)]
        permission_denials: Vec<Reddedilen>,
    },
    #[serde(other)]
    Diger,
}

#[derive(Deserialize)]
struct Mesaj {
    #[serde(default)]
    content: Vec<Blok>,
}

#[derive(Deserialize)]
#[serde(tag = "type")]
enum Blok {
    #[serde(rename = "thinking")]
    Thinking,
    #[serde(rename = "text")]
    Text {
        #[serde(default)]
        text: String,
    },
    #[serde(rename = "tool_use")]
    ToolUse {
        #[serde(default)]
        name: String,
        #[serde(default)]
        input: serde_json::Value,
    },
    #[serde(rename = "tool_result")]
    ToolResult {
        /// ⚠️ **`Option` şart** — ölçüldü: başarılı sonuçta `null` geliyor, `false` değil.
        /// Düz `bool` yazılırsa ayrıştırma **çöker** ve tüm akış susar.
        #[serde(default)]
        is_error: Option<bool>,
        #[serde(default)]
        content: serde_json::Value,
    },
    #[serde(other)]
    Diger,
}

/// Bir stream-json satırını olaylara çevir — **saf fonksiyon** (birim testli, gerçek
/// yakalanmış satırlarla). Ayrıştırılamayan satır `Err`; çağıran **sayar ve loglar**,
/// akışı öldürmez (tek bozuk satır yüzünden koşuyu kaybetmek fazla pahalı).
fn parse_line(line: &str) -> Result<Vec<ClaudeEvent>, serde_json::Error> {
    let satir: Satir = serde_json::from_str(line)?;
    Ok(match satir {
        Satir::System { subtype, model, session_id } if subtype == "init" => {
            vec![ClaudeEvent::Baslangic { model, oturum: session_id }]
        }
        Satir::Assistant { message } | Satir::User { message } => message
            .content
            .into_iter()
            .filter_map(|b| match b {
                Blok::Thinking => Some(ClaudeEvent::Dusunuyor),
                Blok::Text { text } => Some(ClaudeEvent::Metin { metin: text }),
                Blok::ToolUse { name, input } => {
                    Some(ClaudeEvent::AracCagrisi { arac: name, girdi: input })
                }
                Blok::ToolResult { is_error, content } => Some(ClaudeEvent::AracSonucu {
                    hata: is_error.unwrap_or(false),
                    // `content` metin de olabilir, blok dizisi de — ikisini de göster.
                    icerik: match content {
                        serde_json::Value::String(s) => s,
                        other => other.to_string(),
                    },
                }),
                Blok::Diger => None,
            })
            .collect(),
        Satir::Result { is_error, duration_ms, result, total_cost_usd, permission_denials } => {
            vec![ClaudeEvent::Bitti {
                basarili: !is_error,
                sure_ms: duration_ms,
                ozet: result,
                reddedilen: permission_denials,
                maliyet_usd: total_cost_usd,
            }]
        }
        // system/<init olmayan> ve rate_limit_event gibi bilgi satırları.
        Satir::System { .. } | Satir::Diger => Vec::new(),
    })
}

/// Motoru sür: `claude`'u onay kapısı bağlıyken çalıştır, akışı **satır satır** yay.
///
/// Kapı **spawn'dan ÖNCE** kurulur (adı önce biz kaparız — bkz. `HookGate::start`).
///
/// `helper_exe` = hook yardımcısı olarak çağrılacak ikili; üretimde
/// [`helper_exe()`] (= bu uygulama). **Parametre olmasının sebebi:** entegrasyon
/// testinde `current_exe()` *test* ikilisini gösterir; testin gerçek uygulamayı
/// çağırabilmesi gerekiyor (ders 13: testin kendi kopyasını ölçme).
///
/// `on_event` **satır geldikçe** çağrılır (`.output()` ile beklenseydi kullanıcı
/// 12+ saniye boş ekrana bakardı — "Düşünüyor…" sendromu, ders 5b).
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
    on_event: &(dyn Fn(ClaudeEvent) + Send + Sync),
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
    let mut child = std::process::Command::new(bin)
        .current_dir(root)
        .arg("-p")
        .arg(prompt)
        .args(["--output-format", "stream-json", "--verbose"])
        .args(["--permission-mode", "default"])
        .arg("--settings")
        .arg(&settings)
        .stdout(std::process::Stdio::piped())
        .stderr(std::process::Stdio::piped())
        .spawn()
        .map_err(|e| format!("Motor çalıştırılamadı: {e}"))?;

    // stderr AYRI thread'de boşaltılır. Tek thread'de stdout okurken stderr borusu
    // dolarsa çocuk süreç yazamaz → **kilitlenir** ve ikimiz de sonsuza dek bekleriz.
    // (Ders 2: dış süreçte önce drain et, sonra konuş.)
    let stderr_pipe = child.stderr.take();
    let stderr_thread = std::thread::spawn(move || {
        let mut s = String::new();
        if let Some(mut e) = stderr_pipe {
            use std::io::Read;
            let _ = e.read_to_string(&mut s);
        }
        s
    });

    let mut satir = 0usize;
    let mut cozulemeyen = 0usize;
    if let Some(stdout) = child.stdout.take() {
        use std::io::BufRead;
        for line in std::io::BufReader::new(stdout).lines() {
            let line = match line {
                Ok(l) => l,
                Err(e) => {
                    log::warn!("[motor] akış satırı okunamadı: {e}");
                    break;
                }
            };
            if line.trim().is_empty() {
                continue;
            }
            satir += 1;
            match parse_line(&line) {
                Ok(olaylar) => olaylar.into_iter().for_each(on_event),
                Err(e) => {
                    cozulemeyen += 1;
                    // Sebebi görünür kıl: sessizce yutulsa "motor bazen boş dönüyor"
                    // diye teşhis edilemez bir şikâyete dönerdi (ders 14).
                    log::warn!("[motor] satır ayrıştırılamadı ({e}) — ilk 200: {:.200}", line);
                }
            }
        }
    }

    let status = child.wait().map_err(|e| format!("Motor beklenirken hata: {e}"))?;
    let sure = t0.elapsed();
    let stderr = stderr_thread.join().unwrap_or_default().trim().to_string();

    // Çıkış kodu tanının yarısıdır (ders 2) — başarıda da yazılır ki "çalıştı ama boş
    // döndü" durumu günlükten görülebilsin.
    log::info!(
        "[motor] koşu bitti — çıkış kodu {:?} · {} ms · {} satır ({} çözülemedi)",
        status.code(),
        sure.as_millis(),
        satir,
        cozulemeyen
    );
    if !stderr.is_empty() {
        log::warn!("[motor] stderr: {stderr}");
    }

    Ok(ClaudeRun { code: status.code(), stderr, ms: sure.as_millis(), satir, cozulemeyen })
}

/// Hook yardımcısı olarak çağrılacak ikili = **bu uygulama** (`--hook-helper` modu).
/// Ayrı bir yardımcı ikili shiplemiyoruz; `lib.rs::run()` argümanı en başta yakalar.
pub fn helper_exe() -> Result<PathBuf, String> {
    std::env::current_exe()
        .map_err(|e| format!("Uygulama yolu bulunamadı (hook yardımcısı çağrılamaz): {e}"))
}

/// Bir motor koşusunun künyesi. İçerik `on_event` ile akmıştır; burada teşhis bilgisi var.
#[derive(Debug, Serialize)]
pub struct ClaudeRun {
    pub code: Option<i32>,
    pub stderr: String,
    pub ms: u128,
    /// Akıştan okunan satır sayısı.
    pub satir: usize,
    /// Ayrıştırılamayan satır sayısı — **0 olmalı**; değilse sözleşme değişmiş demektir.
    pub cozulemeyen: usize,
}

/// Klasör Ajanı'nı **Claude Code motoruyla** sür (arayüzün girişi).
///
/// Her araç çağrısı `claude-hook-request` olayıyla kullanıcıya sorulur; akış
/// `claude-agent-event` ile yayınlanır.
///
/// Bloke eden işi `spawn_blocking`'e alır — Tauri'nin async runtime'ını tutmaz.
#[tauri::command]
pub async fn claude_agent_run(
    app: tauri::AppHandle,
    kok: String,
    gorev: String,
    sistem_ikili: bool,
) -> Result<ClaudeRun, String> {
    // Kök gerçek bir klasör mü? `canonicalize` `..`/symlink'i çözer — sandbox'ın
    // `allowWrite`'ına ham kullanıcı dizesi geçirmiyoruz.
    let kok = std::fs::canonicalize(&kok)
        .map_err(|e| format!("Çalışma klasörü açılamadı ({kok}): {e}"))?;
    if !kok.is_dir() {
        return Err("Çalışma klasörü bir dizin değil.".into());
    }
    if gorev.trim().is_empty() {
        return Err("Görev boş.".into());
    }

    // Motor hazır değilse **sessizce başka bir şeye düşme** (ders 3) — sebebi söyle.
    let durum = claude_engine_status(app.clone(), sistem_ikili);
    if !durum.ready {
        return Err(durum.reason.unwrap_or_else(|| "Claude Code motoru hazır değil.".into()));
    }
    let bin = PathBuf::from(durum.path.ok_or("Motor yolu bilinmiyor.")?);
    let helper = helper_exe()?;

    let app_olay = app.clone();
    let handler = crate::agent_hook::tauri_handler(app.clone());

    tauri::async_runtime::spawn_blocking(move || {
        run_claude(&bin, &kok, &gorev, &helper, handler, &move |olay| {
            use tauri::Emitter;
            if let Err(e) = app_olay.emit("claude-agent-event", &olay) {
                // Akış olayı düşerse arayüz sessizce donuk kalır — görünür kıl.
                log::warn!("[motor] akış olayı yayınlanamadı: {e}");
            }
        })
    })
    .await
    .map_err(|e| format!("Motor görevi çalıştırılamadı: {e}"))?
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

    // ── stream-json ayrıştırma ────────────────────────────────────────────────
    //
    // Fixture'lar **gerçek `claude` 2.1.181 koşusundan kesildi** (2026-07-17), elle
    // yazılmadı: uydurulmuş bir JSON'a karşı yeşil olan parser, gerçek akışta çöker
    // ve testin kendi kopyasını ölçmüş oluruz (ders 13).

    #[test]
    fn init_satiri_baslangic_verir() {
        let l = r#"{"type":"system","subtype":"init","model":"claude-opus-4-8","session_id":"1043f4d5","cwd":"/tmp","tools":["Bash"]}"#;
        assert_eq!(
            parse_line(l).unwrap(),
            vec![ClaudeEvent::Baslangic { model: "claude-opus-4-8".into(), oturum: "1043f4d5".into() }]
        );
    }

    #[test]
    fn tool_use_ve_metin_okunur() {
        let tu = r#"{"type":"assistant","message":{"content":[{"type":"tool_use","name":"Write","input":{"file_path":"/tmp/not.txt","content":"test\n"}}]}}"#;
        match &parse_line(tu).unwrap()[0] {
            ClaudeEvent::AracCagrisi { arac, girdi } => {
                assert_eq!(arac, "Write");
                assert_eq!(girdi["file_path"], "/tmp/not.txt");
            }
            o => panic!("beklenmeyen olay: {o:?}"),
        }

        let tx = r#"{"type":"assistant","message":{"content":[{"type":"text","text":"`not.txt` dosyasını oluşturdum."}]}}"#;
        assert_eq!(
            parse_line(tx).unwrap(),
            vec![ClaudeEvent::Metin { metin: "`not.txt` dosyasını oluşturdum.".into() }]
        );
    }

    /// `thinking` bloğu **ölçüldü** (hafızadaki tip listesinde yoktu). İçeriği
    /// taşımıyoruz ama satır sessizce düşmemeli — "çalışıyor" sinyali.
    #[test]
    fn thinking_blogu_dusunuyor_verir() {
        let l = r#"{"type":"assistant","message":{"content":[{"type":"thinking","thinking":"uzun düşünce","signature":"abc"}]}}"#;
        assert_eq!(parse_line(l).unwrap(), vec![ClaudeEvent::Dusunuyor]);
    }

    /// ⚠️ Ölçülen tuzak: başarılı `tool_result`'ta `is_error` **null** gelir.
    /// Düz `bool` yazılsaydı ayrıştırma çöker, tüm akış susardı.
    #[test]
    fn tool_result_is_error_null_cokmez() {
        let l = r#"{"type":"user","message":{"content":[{"type":"tool_result","content":"Dosya yazıldı","is_error":null,"tool_use_id":"toolu_1"}]}}"#;
        assert_eq!(
            parse_line(l).unwrap(),
            vec![ClaudeEvent::AracSonucu { hata: false, icerik: "Dosya yazıldı".into() }]
        );
    }

    /// Reddettiğimiz sebep modele `is_error:true` ile ulaşıyor (gerçek deny koşusu).
    #[test]
    fn reddedilen_arac_sonucu_hata_olur() {
        let l = r#"{"type":"user","message":{"content":[{"type":"tool_result","content":"Kullanıcı reddetti","is_error":true,"tool_use_id":"toolu_01Xtvik"}]}}"#;
        assert_eq!(
            parse_line(l).unwrap(),
            vec![ClaudeEvent::AracSonucu { hata: true, icerik: "Kullanıcı reddetti".into() }]
        );
    }

    /// ⚠️ **En önemli ölçüm:** her araç reddedilse bile `result` `is_error:false` der.
    /// "İş görüldü mü?" sorusunun cevabı `reddedilen` listesidir — arayüz buna bakmalı.
    #[test]
    fn deny_kosusunda_basarili_ama_reddedilen_dolu() {
        let l = r#"{"type":"result","subtype":"success","is_error":false,"duration_ms":8000,"stop_reason":"end_turn","permission_denials":[{"tool_name":"Write","tool_use_id":"toolu_01Xtvik","tool_input":{"file_path":"/tmp/gizli.txt","content":"x\n"}}],"total_cost_usd":0.05}"#;
        match &parse_line(l).unwrap()[0] {
            ClaudeEvent::Bitti { basarili, reddedilen, sure_ms, .. } => {
                assert!(*basarili, "motor 'success' diyor — is_error'a aldanma tuzağı");
                assert_eq!(*sure_ms, 8000);
                assert_eq!(reddedilen.len(), 1, "asıl karne burada");
                assert_eq!(reddedilen[0].tool_name, "Write");
                assert_eq!(reddedilen[0].tool_input["file_path"], "/tmp/gizli.txt");
            }
            o => panic!("beklenmeyen olay: {o:?}"),
        }
    }

    #[test]
    fn basarili_kosuda_reddedilen_bos() {
        let l = r#"{"type":"result","subtype":"success","is_error":false,"duration_ms":12061,"result":"Oluşturdum.","total_cost_usd":0.159401}"#;
        assert_eq!(
            parse_line(l).unwrap(),
            vec![ClaudeEvent::Bitti {
                basarili: true,
                sure_ms: 12061,
                ozet: Some("Oluşturdum.".into()),
                reddedilen: vec![],
                maliyet_usd: Some(0.159401),
            }]
        );
    }

    /// ⚠️ Ölçüldü: **belgelenmemiş** `rate_limit_event` satırı geliyor. Bilinmeyen tip
    /// hata VERMEMELİ — yoksa Anthropic yeni bir satır ekler eklemez motor kırılır.
    #[test]
    fn bilinmeyen_satir_tipi_kirmaz() {
        let l = r#"{"type":"rate_limit_event","rate_limit_info":{"status":"allowed","rateLimitType":"five_hour"},"uuid":"4aede40d"}"#;
        assert_eq!(parse_line(l).unwrap(), vec![], "bilinmeyen tip olaysız geçmeli");

        // Gelecekte eklenecek varsayımsal bir tip de aynı şekilde geçmeli.
        assert_eq!(parse_line(r#"{"type":"gelecekteki_tip","x":1}"#).unwrap(), vec![]);
    }

    /// Bozuk satır `Err` döner ki çağıran **sayıp loglasın** — sessizce yutulmaz.
    #[test]
    fn bozuk_satir_hata_verir() {
        assert!(parse_line("{bu json degil").is_err());
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
