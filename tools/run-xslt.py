#!/usr/bin/env python3
"""Şablonu Saxon sidecar'ından geçirir — uygulamayı açmadan.

    python3 tools/run-xslt.py <xslt> <xml> > out.html

Uygulamanın kullandığı protokolün AYNISI (bkz. app/src-tauri/src/xslt.rs):
sidecar'ın stdin'ine [4B BE uzunluk][XSLT UTF-8][4B BE uzunluk][XML UTF-8] yazılır,
stdout'tan HTML okunur. Hata durumunda çıkış kodu + stderr basılır — sessizce
"boş çıktı" dönmez.

Ne işe yarar: bir öğenin çıktıda KAÇ KEZ basıldığını saymak (yeşil regresyon, yeni
kod yolunu hiç çalıştırmamış olabilir — bkz. CLAUDE.md ders 12) ve sidecar'ın gerçek
çıkış kodunu görmek.
"""
import platform
import struct
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
BIN_DIR = ROOT / "app" / "src-tauri" / "binaries"

# Tauri sidecar'ları hedef üçlüsüyle adlandırılır; yereldeki tek ikiliyi bulmak yeter.
TRIPLE = {
    ("Darwin", "arm64"): "aarch64-apple-darwin",
    ("Darwin", "x86_64"): "x86_64-apple-darwin",
    ("Linux", "x86_64"): "x86_64-unknown-linux-gnu",
    ("Linux", "aarch64"): "aarch64-unknown-linux-gnu",
    ("Windows", "AMD64"): "x86_64-pc-windows-msvc.exe",
}.get((platform.system(), platform.machine()))

if TRIPLE is None:
    sys.exit(f"Bu platform için sidecar adı bilinmiyor: {platform.system()}/{platform.machine()}")

sidecar = BIN_DIR / f"xslt-transform-{TRIPLE}"
if not sidecar.exists():
    sys.exit(f"Sidecar bulunamadı: {sidecar}\nÖnce derle: sidecar/build.sh")

if len(sys.argv) != 3:
    sys.exit(__doc__)

xslt = Path(sys.argv[1]).read_bytes()
xml = Path(sys.argv[2]).read_bytes()
payload = struct.pack(">I", len(xslt)) + xslt + struct.pack(">I", len(xml)) + xml

proc = subprocess.run([str(sidecar)], input=payload, capture_output=True)
if proc.returncode != 0:
    sys.stderr.write(f"sidecar çıkış kodu {proc.returncode}\n")
    sys.stderr.write(proc.stderr.decode("utf-8", "replace"))
    sys.exit(1)

sys.stdout.buffer.write(proc.stdout)
