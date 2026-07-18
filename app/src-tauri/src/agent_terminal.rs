//! Faz D — **"Terminal" sekmesi**: ham interaktif `claude` gerçek bir PTY'de.
//!
//! Ana mod (`agent_cli::run_claude`, `-p`) bizim **fail-closed** hook+sandbox
//! kapımızdan geçer. Bu modül ise `claude`'u **kendi TUI'siyle** çalıştırır:
//! kendi trust prompt'u, kendi araç onayı, kendi çıktısı.
//!
//! ⚠️ **GÜVENLİK (ölçüldü 2026-07-18, CLAUDE.md ders 15/17):** interaktif modda bizim
//! sandbox'ımız klasör kilidini **TUTMUYOR** — claude komutu kendi "sandbox'sız yeniden
//! dene" izin akışıyla kök dışına yazabildi (diskle kanıtlandı). Bu yüzden burada
//! hook/sandbox `--settings`'i **hiç vermiyoruz** (yalan bir güvenlik hissi yaratmasın);
//! sekme arayüzde **açıkça** "ham claude, klasör kilidi garanti değil" diye etiketlenir.
//! Tek koruma = claude'un **kendi** interaktif izin promptları (kullanıcı terminalde görür).

use portable_pty::{CommandBuilder, NativePtySystem, PtySize, PtySystem};
use std::io::{Read, Write};
use std::sync::Mutex;
use tauri::{Emitter, Manager};

/// Uçuştaki tek terminal oturumu (aynı anda tek `claude` TUI yeterli).
#[derive(Default)]
pub struct TerminalState {
    inner: Mutex<Option<Session>>,
}

struct Session {
    writer: Box<dyn Write + Send>,
    master: Box<dyn portable_pty::MasterPty + Send>,
    child: Box<dyn portable_pty::Child + Send + Sync>,
}

/// Terminal oturumunu başlat: `claude`'u PTY'de sür, çıktıyı `claude-terminal-output`
/// olayıyla (base64) yay. Zaten çalışan bir oturum varsa önce kapatılır.
#[tauri::command]
pub fn claude_terminal_start(
    app: tauri::AppHandle,
    kok: String,
    sistem_ikili: bool,
    cols: u16,
    rows: u16,
) -> Result<(), String> {
    let kok = std::fs::canonicalize(&kok)
        .map_err(|e| format!("Çalışma klasörü açılamadı ({kok}): {e}"))?;
    if !kok.is_dir() {
        return Err("Çalışma klasörü bir dizin değil.".into());
    }

    // Motoru çöz (managed/system) — hazır değilse sessizce düşme, sebebi söyle.
    let durum = crate::agent_cli::claude_engine_status(app.clone(), sistem_ikili);
    if !durum.ready {
        return Err(durum.reason.unwrap_or_else(|| "Claude Code motoru hazır değil.".into()));
    }
    let bin = durum.path.ok_or("Motor yolu bilinmiyor.")?;

    // Varsa eski oturumu kapat (tek slot).
    stop_locked(&app);

    let pty_system = NativePtySystem::default();
    let pair = pty_system
        .openpty(PtySize { rows: rows.max(1), cols: cols.max(1), pixel_width: 0, pixel_height: 0 })
        .map_err(|e| format!("PTY açılamadı: {e}"))?;

    let mut cmd = CommandBuilder::new(&bin);
    cmd.cwd(&kok);
    // Renkli TUI için terminal tipi.
    cmd.env("TERM", "xterm-256color");

    let child = pair
        .slave
        .spawn_command(cmd)
        .map_err(|e| format!("Terminal başlatılamadı: {e}"))?;
    // slave artık çocuğa ait; bizde kalması gereksiz FD tutar.
    drop(pair.slave);

    let mut reader = pair
        .master
        .try_clone_reader()
        .map_err(|e| format!("PTY okuyucu alınamadı: {e}"))?;
    let writer = pair
        .master
        .take_writer()
        .map_err(|e| format!("PTY yazıcı alınamadı: {e}"))?;

    log::info!("[terminal] başladı — ikili={} · kök={}", bin, kok.display());

    // Çıktı thread'i: PTY'den oku → base64 → frontend'e yay. EOF/hata'da "exit" olayı.
    let app_out = app.clone();
    std::thread::spawn(move || {
        use base64::Engine as _;
        let mut buf = [0u8; 8192];
        loop {
            match reader.read(&mut buf) {
                Ok(0) => break,
                Ok(n) => {
                    let b64 = base64::engine::general_purpose::STANDARD.encode(&buf[..n]);
                    if app_out.emit("claude-terminal-output", b64).is_err() {
                        break;
                    }
                }
                Err(_) => break,
            }
        }
        let _ = app_out.emit("claude-terminal-exit", ());
        log::info!("[terminal] çıktı akışı bitti");
    });

    let state = app.state::<TerminalState>();
    *state.inner.lock().map_err(|_| "Terminal durumu kilitlenemedi.")? =
        Some(Session { writer, master: pair.master, child });
    Ok(())
}

/// Kullanıcının tuş vuruşlarını (xterm `onData`) PTY'ye yaz.
#[tauri::command]
pub fn claude_terminal_write(app: tauri::AppHandle, data: String) -> Result<(), String> {
    let state = app.state::<TerminalState>();
    let mut guard = state.inner.lock().map_err(|_| "Terminal durumu kilitlenemedi.")?;
    let Some(s) = guard.as_mut() else {
        return Err("Terminal oturumu yok.".into());
    };
    s.writer.write_all(data.as_bytes()).map_err(|e| format!("Terminale yazılamadı: {e}"))?;
    s.writer.flush().map_err(|e| format!("Terminal boşaltılamadı: {e}"))
}

/// Pencere yeniden boyutlandı → PTY boyutunu güncelle (satır sarma doğru olsun).
#[tauri::command]
pub fn claude_terminal_resize(app: tauri::AppHandle, cols: u16, rows: u16) -> Result<(), String> {
    let state = app.state::<TerminalState>();
    let guard = state.inner.lock().map_err(|_| "Terminal durumu kilitlenemedi.")?;
    if let Some(s) = guard.as_ref() {
        s.master
            .resize(PtySize { rows: rows.max(1), cols: cols.max(1), pixel_width: 0, pixel_height: 0 })
            .map_err(|e| format!("Terminal boyutlandırılamadı: {e}"))?;
    }
    Ok(())
}

/// Terminal oturumunu kapat (çocuğu öldür).
#[tauri::command]
pub fn claude_terminal_kill(app: tauri::AppHandle) -> Result<(), String> {
    stop_locked(&app);
    Ok(())
}

/// Çalışan oturumu (varsa) kapat. Kilidi kendi alır — çağıran kilidi tutmamalı.
fn stop_locked(app: &tauri::AppHandle) {
    let state = app.state::<TerminalState>();
    let mut guard = match state.inner.lock() {
        Ok(g) => g,
        Err(_) => return,
    };
    if let Some(mut s) = guard.take() {
        let _ = s.child.kill();
        let _ = s.child.wait();
        log::info!("[terminal] kapatıldı");
    }
}
