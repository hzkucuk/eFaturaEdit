//! Klasör Ajanı araçları — sandbox-korumalı dosya erişimi.
//!
//! Bu modül, opt-in "Klasör Ajanı" modunun (VSCode-benzeri) araç yüzeyidir:
//! model bir dosyayı okumak/yazmak/düzenlemek istediğinde, çağrı BURADAN geçer
//! ve **her yol çalışma klasörüne kilitlenir.**
//!
//! ## Güvenlik çekirdeği — [`guard`]
//! Direktifin "AI dosya sistemine erişemez" kuralına opt-in istisna açıyoruz;
//! o yüzden koruma frontend'e BIRAKILMAZ, bu Rust sınırında zorlanır. `guard`:
//! 1. Kökü `canonicalize` eder (gerçek yol; macOS'ta `/var`→`/private/var`).
//! 2. Hedefin **var olan en derin atasını** canonicalize eder → symlink ve `..`
//!    orada çözülür; kök dışına çıkıyorsa reddedilir.
//! 3. Olmayan kuyruğu (henüz yaratılmamış dosya/dizin) tekrar ekler; kuyrukta
//!    `..` varsa reddeder (olmayan dizinden kaçışı engeller).
//! 4. Sonuç kökün altında değilse reddeder.
//!
//! ⚠️ Bu koruma **dosya araçları içindir.** `bash` (Faz 2) klasöre gerçekten
//! kilitlenemez (`cwd` sabit olsa da komut `cd /` yapabilir); onun tek koruması
//! kullanıcı onay kapısıdır. Bkz. CLAUDE.md.

use serde::Serialize;
use std::ffi::OsString;
use std::path::{Path, PathBuf};

/// Bir yolu çalışma köküne kilitle. Kök dışına çıkan her yol reddedilir.
///
/// `target` göreli ise köke eklenir; mutlaksa olduğu gibi denenir (ama yine
/// kök denetiminden geçer, yani kök dışı mutlak yol da reddedilir).
pub(crate) fn guard(root: &str, target: &str) -> Result<PathBuf, String> {
    let root = std::fs::canonicalize(root)
        .map_err(|e| format!("Çalışma klasörü çözümlenemedi ({root}): {e}"))?;

    let t = Path::new(target);
    let joined = if t.is_absolute() { t.to_path_buf() } else { root.join(t) };

    // Var olan en derin atayı bul; canonicalize onu (symlink + ..) çözer.
    let mut existing = joined.clone();
    let mut tail: Vec<OsString> = Vec::new();
    while !existing.exists() {
        match existing.file_name() {
            Some(name) => {
                tail.push(name.to_os_string());
                match existing.parent() {
                    Some(p) => existing = p.to_path_buf(),
                    None => break, // dosya sistemi köküne ulaşıldı
                }
            }
            None => break,
        }
    }

    let mut resolved = std::fs::canonicalize(&existing)
        .map_err(|e| format!("Yol çözümlenemedi ({}): {e}", existing.display()))?;

    // Olmayan kuyruğu geri ekle (ters sırada). Kuyrukta `..`/`.` yasak: olmayan
    // bir dizinden kaçışı (symlink'i takip etmeden) engeller.
    for name in tail.iter().rev() {
        if name == ".." || name == "." {
            return Err(format!("Yol geçersiz bileşen içeriyor: {target}"));
        }
        resolved.push(name);
    }

    if !resolved.starts_with(&root) {
        return Err(format!(
            "Yol çalışma klasörünün DIŞINDA, reddedildi: {target}"
        ));
    }
    Ok(resolved)
}

/// `agent_list` için tek girdi.
#[derive(Debug, Serialize)]
pub struct DirEntry {
    pub name: String,
    /// Köke göreli yol (frontend araç sonucunda bunu gösterir).
    pub rel: String,
    pub is_dir: bool,
    /// Dosya boyutu (dizinde 0).
    pub bytes: u64,
}

/// Kök içindeki bir dosyayı oku.
#[tauri::command]
pub fn agent_read(root: String, path: String) -> Result<String, String> {
    let p = guard(&root, &path)?;
    std::fs::read_to_string(&p).map_err(|e| format!("Dosya okunamadı ({path}): {e}"))
}

/// Kök içindeki bir dosyaya yaz (yoksa oluşturur, parent dizinleri kurar).
#[tauri::command]
pub fn agent_write(root: String, path: String, content: String) -> Result<(), String> {
    let p = guard(&root, &path)?;
    if let Some(parent) = p.parent() {
        std::fs::create_dir_all(parent)
            .map_err(|e| format!("Klasör oluşturulamadı ({}): {e}", parent.display()))?;
    }
    std::fs::write(&p, content).map_err(|e| format!("Dosya yazılamadı ({path}): {e}"))
}

/// Kök içindeki bir dosyada tek eşleşmeli string değişimi (text-editor deseni).
/// `old` dosyada tam olarak BİR kez geçmelidir; 0 veya >1 ise hata (belirsizlik).
#[tauri::command]
pub fn agent_edit(root: String, path: String, old: String, new: String) -> Result<(), String> {
    let p = guard(&root, &path)?;
    let content =
        std::fs::read_to_string(&p).map_err(|e| format!("Dosya okunamadı ({path}): {e}"))?;
    let count = content.matches(&old).count();
    if count == 0 {
        return Err(format!("Değiştirilecek metin bulunamadı ({path})."));
    }
    if count > 1 {
        return Err(format!(
            "Metin {count} kez geçiyor ({path}); benzersiz olacak kadar bağlam ekle."
        ));
    }
    let updated = content.replacen(&old, &new, 1);
    std::fs::write(&p, updated).map_err(|e| format!("Dosya yazılamadı ({path}): {e}"))
}

/// Kök içindeki bir alt dizini listele (`sub` boşsa kökün kendisi).
#[tauri::command]
pub fn agent_list(root: String, sub: String) -> Result<Vec<DirEntry>, String> {
    let dir = guard(&root, if sub.is_empty() { "." } else { &sub })?;
    let root_canon = std::fs::canonicalize(&root)
        .map_err(|e| format!("Çalışma klasörü çözümlenemedi: {e}"))?;

    let mut out = Vec::new();
    let rd = std::fs::read_dir(&dir).map_err(|e| format!("Klasör okunamadı ({sub}): {e}"))?;
    for entry in rd.flatten() {
        let path = entry.path();
        let meta = match entry.metadata() {
            Ok(m) => m,
            Err(_) => continue,
        };
        let rel = path
            .strip_prefix(&root_canon)
            .map(|r| r.to_string_lossy().into_owned())
            .unwrap_or_else(|_| path.to_string_lossy().into_owned());
        out.push(DirEntry {
            name: entry.file_name().to_string_lossy().into_owned(),
            rel,
            is_dir: meta.is_dir(),
            bytes: if meta.is_dir() { 0 } else { meta.len() },
        });
    }
    // Dizinler önce, sonra ada göre — deterministik listeleme.
    out.sort_by(|a, b| b.is_dir.cmp(&a.is_dir).then(a.name.cmp(&b.name)));
    Ok(out)
}

/// `agent_bash` sonucu.
#[derive(Debug, Serialize)]
pub struct BashResult {
    pub stdout: String,
    pub stderr: String,
    pub code: i32,
    /// Çıktı kırpıldıysa true (modeli boğmamak için).
    pub truncated: bool,
}

/// Çıktı üst sınırı — büyük çıktı modeli boğar ve token yakar.
const BASH_OUTPUT_CAP: usize = 60_000;
/// Komut zaman aşımı.
const BASH_TIMEOUT: std::time::Duration = std::time::Duration::from_secs(60);

fn cap(s: &[u8]) -> (String, bool) {
    let text = String::from_utf8_lossy(s);
    if text.len() > BASH_OUTPUT_CAP {
        let head: String = text.chars().take(BASH_OUTPUT_CAP).collect();
        (format!("{head}\n…[çıktı kırpıldı]"), true)
    } else {
        (text.into_owned(), false)
    }
}

pub(crate) fn kill_pid(pid: u32) {
    #[cfg(unix)]
    let _ = std::process::Command::new("kill")
        .arg("-9")
        .arg(pid.to_string())
        .status();
    #[cfg(windows)]
    let _ = std::process::Command::new("taskkill")
        .args(["/F", "/PID", &pid.to_string()])
        .status();
}

/// Çalışma klasöründe bir kabuk komutu çalıştır.
///
/// ⚠️ **GÜVENLİK — bu `guard()` KULLANMAZ.** `cwd` köke sabitlenir ama komut
/// `cd /` yapıp klasör dışına çıkabilir; OS-düzeyi sandbox (konteyner/seccomp)
/// Tauri'de yoktur. Bu aracın **tek gerçek koruması, komut çalışmadan önce
/// kullanıcının onaylamasıdır** (frontend onay kapısı). Bu yüzden bash yalnızca
/// kullanıcı Klasör Ajanı'nda açıkça izin verince sunulur ve komut ham gösterilir.
#[tauri::command]
pub fn agent_bash(root: String, command: String) -> Result<BashResult, String> {
    use std::process::Stdio;
    let root_canon = std::fs::canonicalize(&root)
        .map_err(|e| format!("Çalışma klasörü çözümlenemedi: {e}"))?;
    let (shell, flag) = if cfg!(windows) { ("cmd", "/C") } else { ("sh", "-c") };

    let child = std::process::Command::new(shell)
        .arg(flag)
        .arg(&command)
        .current_dir(&root_canon)
        .stdin(Stdio::null())
        .stdout(Stdio::piped())
        .stderr(Stdio::piped())
        .spawn()
        .map_err(|e| format!("Komut başlatılamadı: {e}"))?;

    let pid = child.id();
    let (tx, rx) = std::sync::mpsc::channel();
    std::thread::spawn(move || {
        let _ = tx.send(child.wait_with_output());
    });

    match rx.recv_timeout(BASH_TIMEOUT) {
        Ok(Ok(out)) => {
            let (stdout, t1) = cap(&out.stdout);
            let (stderr, t2) = cap(&out.stderr);
            Ok(BashResult {
                stdout,
                stderr,
                code: out.status.code().unwrap_or(-1),
                truncated: t1 || t2,
            })
        }
        Ok(Err(e)) => Err(format!("Komut çalıştırılamadı: {e}")),
        Err(_) => {
            kill_pid(pid);
            Err(format!(
                "Komut {} saniyede bitmedi, sonlandırıldı: {command}",
                BASH_TIMEOUT.as_secs()
            ))
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use std::fs;

    /// Benzersiz geçici kök klasör kur.
    fn temp_root() -> PathBuf {
        let mut d = std::env::temp_dir();
        d.push(format!("efe-agent-test-{}", uniq()));
        fs::create_dir_all(&d).unwrap();
        // macOS'ta temp_dir /var → /private/var symlink'i; canonicalize'la sabitle.
        fs::canonicalize(&d).unwrap()
    }

    fn uniq() -> u128 {
        std::time::SystemTime::now()
            .duration_since(std::time::UNIX_EPOCH)
            .unwrap()
            .as_nanos()
    }

    #[test]
    fn kok_ici_yol_kabul() {
        let root = temp_root();
        fs::write(root.join("a.txt"), "x").unwrap();
        let r = root.to_string_lossy().into_owned();
        // Var olan dosya
        assert!(guard(&r, "a.txt").is_ok());
        // Henüz olmayan dosya (yazma hedefi) — parent kök, kabul
        assert!(guard(&r, "yeni.txt").is_ok());
        // Alt dizinde olmayan dosya
        assert!(guard(&r, "alt/derin/yeni.txt").is_ok());
    }

    #[test]
    fn ust_dizine_kacis_ret() {
        let root = temp_root();
        let r = root.to_string_lossy().into_owned();
        assert!(guard(&r, "../disari.txt").is_err());
        assert!(guard(&r, "../../etc/passwd").is_err());
        // Ortada .. ile normalize sonrası kök içinde kalan yol KABUL edilmeli
        // (alt/../a.txt == a.txt). Ama alt henüz yoksa canonicalize başarısız
        // olur; var eden:
        fs::create_dir_all(root.join("alt")).unwrap();
        fs::write(root.join("a.txt"), "x").unwrap();
        assert!(guard(&r, "alt/../a.txt").is_ok());
    }

    #[test]
    fn mutlak_kok_disi_yol_ret() {
        let root = temp_root();
        let r = root.to_string_lossy().into_owned();
        assert!(guard(&r, "/etc/hosts").is_err());
        assert!(guard(&r, "/tmp").is_err());
    }

    #[cfg(unix)]
    #[test]
    fn symlink_kacisi_ret() {
        use std::os::unix::fs::symlink;
        let root = temp_root();
        // Kök DIŞINDA bir hedef klasör + kök İÇİNDE ona işaret eden symlink.
        let outside = {
            let mut d = std::env::temp_dir();
            d.push(format!("efe-agent-outside-{}", uniq()));
            fs::create_dir_all(&d).unwrap();
            fs::canonicalize(&d).unwrap()
        };
        fs::write(outside.join("secret.txt"), "gizli").unwrap();
        symlink(&outside, root.join("link")).unwrap();

        let r = root.to_string_lossy().into_owned();
        // link/ kök içinde görünür ama symlink kök DIŞINA çıkıyor → RET.
        assert!(
            guard(&r, "link/secret.txt").is_err(),
            "symlink ile kök dışına erişim reddedilmeliydi"
        );
    }

    #[test]
    fn kok_ici_symlink_ok() {
        // Kök içinde, yine kök içine işaret eden symlink kabul edilmeli
        // (kaçış yok). Unix'te.
        #[cfg(unix)]
        {
            use std::os::unix::fs::symlink;
            let root = temp_root();
            fs::create_dir_all(root.join("real")).unwrap();
            fs::write(root.join("real/data.txt"), "veri").unwrap();
            symlink(root.join("real"), root.join("alias")).unwrap();
            let r = root.to_string_lossy().into_owned();
            assert!(guard(&r, "alias/data.txt").is_ok());
        }
    }
}
