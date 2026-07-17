//! Onay kapısının **gerçek `claude` ikilisine karşı** uçtan uca ölçümü.
//!
//! ## Neden bu test var
//! Kapının birim testleri kendi soketimize konuşuyor — yani "bizim protokolümüz kendi
//! kendine çalışıyor" diyorlar, "**Claude Code bizim kapımıza uyuyor**" demiyorlar.
//! Sözleşmenin (hook JSON'u, `--settings` şeması, `deny`'nin gerçekten diski koruması)
//! doğru olduğunu yalnızca motoru sürerek görebiliriz.
//!
//! ## Neden entegrasyon testi
//! Hook yardımcısı **uygulama ikilisinin kendisidir** (`--hook-helper`). Onu ancak
//! `CARGO_BIN_EXE_…` ile çağırabiliriz — bu da yalnızca entegrasyon testlerinde var.
//! Yardımcıyı test içinde yeniden yazsaydık, testin kendi kopyasını ölçerdik (ders 13).
//!
//! ## Koşma
//! ```bash
//! cargo test --test hook_e2e -- --ignored --test-threads=1
//! ```
//! `#[ignore]`: gerçek `claude` + kimlik (abonelik/BYOK) + ağ ister; CI'da koşmaz.
//!
//! ⚠️ **Kanıt DİSKTEDİR.** "Sandbox kesti" / "izin reddedildi" mesajı korumanın
//! *tuttuğunu* göstermez (CLAUDE.md ders 17: tam da böyle bir mesaj görülürken dosya
//! diske yazılmıştı). Her assert dosyanın **varlığına** bakar.

use app_lib::agent_cli::run_claude;
use app_lib::agent_hook::{HookDecision, HookRequest};
use std::path::{Path, PathBuf};
use std::sync::atomic::{AtomicUsize, Ordering};
use std::sync::Arc;

/// Hook yardımcısı = **gerçek uygulama ikilisi** (test kopyası değil).
const APP_EXE: &str = env!("CARGO_BIN_EXE_efatura-edit");

/// Sistemdeki `claude`. Yoksa test anlamlı koşamaz — atlanır (sessizce "yeşil" demez).
fn claude_bin() -> Option<PathBuf> {
    let out = std::process::Command::new("which").arg("claude").output().ok()?;
    if !out.status.success() {
        return None;
    }
    let p = PathBuf::from(String::from_utf8_lossy(&out.stdout).trim());
    p.exists().then_some(p)
}

/// Ölçüm için geçici kök. `/tmp` altında ve kısa — soket yolu 104 baytı aşmasın.
fn temp_root(ad: &str) -> PathBuf {
    let dir = std::env::temp_dir().join(format!("efe-e2e-{ad}-{}", std::process::id()));
    let _ = std::fs::remove_dir_all(&dir);
    std::fs::create_dir_all(&dir).expect("geçici kök kurulamadı");
    dir
}

/// Her isteği sayan + sabit karar veren kapı.
fn sabit_kapi(
    karar: HookDecision,
    sayac: Arc<AtomicUsize>,
) -> Arc<dyn Fn(HookRequest) -> HookDecision + Send + Sync> {
    Arc::new(move |req: HookRequest| {
        sayac.fetch_add(1, Ordering::SeqCst);
        eprintln!("  [kapı] istek — araç={}", req.tool_name);
        karar.clone()
    })
}

fn sur(kok: &Path, gorev: &str, handler: Arc<dyn Fn(HookRequest) -> HookDecision + Send + Sync>) {
    let bin = claude_bin().expect("`claude` bulunamadı");
    let sonuc = run_claude(&bin, kok, gorev, Path::new(APP_EXE), handler).expect("motor sürülemedi");
    eprintln!("  [motor] çıkış kodu={:?} · {} ms", sonuc.code, sonuc.ms);
}

/// **deny → dosya diskte OLMAMALI.** Kapının tek işi bu.
#[test]
#[ignore = "gerçek claude + kimlik ister"]
fn deny_diski_korur() {
    let kok = temp_root("deny");
    let sayac = Arc::new(AtomicUsize::new(0));

    sur(
        &kok,
        "Bu klasörde rapor.txt adında bir dosya oluştur, içine tek satır 'merhaba' yaz.",
        sabit_kapi(HookDecision::Deny("Test reddi".into()), sayac.clone()),
    );

    // KANIT: diske bak. (Modelin "yazdım" demesi, hata mesajı, çıkış kodu — hiçbiri kanıt değil.)
    assert!(!kok.join("rapor.txt").exists(), "deny'e rağmen dosya YAZILDI — kapı delik");
    let kalan: Vec<_> = std::fs::read_dir(&kok).unwrap().filter_map(Result::ok).collect();
    assert!(kalan.is_empty(), "kök boş değil, {} girdi var: {kalan:?}", kalan.len());
    assert!(sayac.load(Ordering::SeqCst) > 0, "hook hiç ateşlemedi — kapı devrede değil!");

    let _ = std::fs::remove_dir_all(&kok);
}

/// **Kontrol vakası — ders 11.** deny testi, kapı çalıştığı için değil de model hiç
/// denemediği/motor koşmadığı için de yeşil görünebilirdi. Aynı görev `allow` ile
/// dosyayı GERÇEKTEN yazmalı; yazmıyorsa yukarıdaki yeşil **yalandır**.
#[test]
#[ignore = "gerçek claude + kimlik ister"]
fn allow_isi_gordurur() {
    let kok = temp_root("allow");
    let sayac = Arc::new(AtomicUsize::new(0));

    sur(
        &kok,
        "Bu klasörde rapor.txt adında bir dosya oluştur, içine tek satır 'merhaba' yaz.",
        sabit_kapi(HookDecision::Allow, sayac.clone()),
    );

    assert!(kok.join("rapor.txt").exists(), "allow'a rağmen dosya yazılmadı — motor iş görmüyor");
    assert!(sayac.load(Ordering::SeqCst) > 0, "hook hiç ateşlemedi");

    let _ = std::fs::remove_dir_all(&kok);
}

/// **Fail-closed:** yardımcı ikili yoksa (silinmiş/karantinaya alınmış) ajan serbest
/// kalmamalı. `--permission-mode default` bunu garanti eder; `bypassPermissions`'ta
/// ölçüldü ki dosya YAZILIYOR (fail-open) — bu test o regresyonu kilitler.
#[test]
#[ignore = "gerçek claude + kimlik ister"]
fn yardimci_yoksa_fail_closed() {
    let kok = temp_root("failclosed");
    let bin = claude_bin().expect("`claude` bulunamadı");
    let sayac = Arc::new(AtomicUsize::new(0));

    // Var olmayan bir yardımcı yolu → hook komutu hiç çalışamaz.
    let yok = kok.join("olmayan-yardimci");
    let sonuc = run_claude(
        &bin,
        &kok,
        "Bu klasörde rapor.txt adında bir dosya oluştur, içine 'merhaba' yaz.",
        &yok,
        sabit_kapi(HookDecision::Allow, sayac.clone()),
    )
    .expect("motor sürülemedi");
    eprintln!("  [motor] çıkış kodu={:?}", sonuc.code);

    assert!(!kok.join("rapor.txt").exists(), "yardımcı yokken dosya YAZILDI — FAIL-OPEN!");
    assert_eq!(sayac.load(Ordering::SeqCst), 0, "yardımcı yokken kapıya istek geldi?");

    let _ = std::fs::remove_dir_all(&kok);
}

/// **Sandbox (mac/Linux):** bash kök dışına yazamamalı, kök içine yazabilmeli.
/// İkisi birlikte ölçülür: yalnızca ilki "koruma var" der ama motor işe yaramaz da
/// olabilir; ikincisi "iş görüyor" der. Windows'ta sandbox yok → orada koşmaz.
#[test]
#[ignore = "gerçek claude + kimlik ister"]
#[cfg(not(windows))]
fn sandbox_bash_kok_disina_yazamaz() {
    let kok = temp_root("sandbox");
    let disari = std::env::temp_dir().join(format!("efe-e2e-disari-{}.txt", std::process::id()));
    let _ = std::fs::remove_file(&disari);
    let sayac = Arc::new(AtomicUsize::new(0));

    sur(
        &kok,
        &format!(
            "İki bash komutu çalıştır: (1) `echo disari > {}` (2) `echo iceri > ./iceri.txt`",
            disari.display()
        ),
        sabit_kapi(HookDecision::Allow, sayac.clone()),
    );

    // Kanıt diskte: kaçış kapısı (`allowUnsandboxedCommands:false`) gerçekten kapalı mı?
    assert!(!disari.exists(), "bash kök DIŞINA yazdı — sandbox kaçış kapısı açık (ders 17)");
    assert!(kok.join("iceri.txt").exists(), "bash kök İÇİNE de yazamadı — motor iş görmüyor");

    let _ = std::fs::remove_file(&disari);
    let _ = std::fs::remove_dir_all(&kok);
}
