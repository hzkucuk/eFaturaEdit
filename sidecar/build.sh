#!/usr/bin/env bash
#
# e-Fatura Edit — XSLT 2.0/3.0 sidecar derleyicisi (Saxon-HE + GraalVM native-image)
#
# Tarayıcının XSLTProcessor'ı yalnızca XSLT 1.0 destekler. Bu script, Saxon-HE'yi
# (MPL 2.0) tek bir yerel çalıştırılabilir dosyaya derler; Tauri bunu "sidecar"
# olarak paketler ve Rust tarafından çağırır.
#
# Gereksinim: JAVA_HOME → native-image içeren bir GraalVM.
# Kullanım:   ./build.sh            (çıktı: bin/xslt-transform-<target-triple>)
#
set -euo pipefail
cd "$(dirname "$0")"

# Türkçe locale'de "DARWIN".toLowerCase() → "darwın" (noktasız ı) olur ve
# native-image, JNI başlık dizinini (include/darwin) bulamaz. Locale'i nötrle.
export LANG=en_US.UTF-8
export LC_ALL=en_US.UTF-8

OUT_DIR="out"
BIN_DIR="../app/src-tauri/binaries"

# Classpath ayıracı platforma göre değişir (Windows ';', diğerleri ':').
case "${OSTYPE:-}" in
  msys*|cygwin*|win*) SEP=';' ;;
  *) SEP=':' ;;
esac
CP="lib/Saxon-HE.jar${SEP}lib/xmlresolver.jar${SEP}lib/xmlresolver-data.jar"

if ! command -v native-image >/dev/null 2>&1; then
  echo "HATA: native-image bulunamadı. GraalVM kurup JAVA_HOME/PATH ayarlayın." >&2
  exit 1
fi

# Tauri sidecar'ları hedef üçlüsü (target triple) son eki ister.
TRIPLE="$(rustc -vV | awk '/^host:/ {print $2}')"
TARGET="$BIN_DIR/xslt-transform-$TRIPLE"

echo "==> Java derleniyor"
rm -rf "$OUT_DIR" && mkdir -p "$OUT_DIR"
javac -cp "$CP" -d "$OUT_DIR" src/Transform.java

echo "==> native-image ($TRIPLE)"
mkdir -p "$BIN_DIR"
# -J-Duser.language: macOS'ta JVM locale'i sistem tercihlerinden gelir; LANG
# yetmez. Türkçe locale'de "DARWIN".toLowerCase() → "darwın" olur ve
# native-image, include/darwin (jni_md.h) dizinini bulamaz.
# IncludeLocales/AddAllCharsets: format-dateTime gibi XSLT 2.0 fonksiyonları
# locale verisi ister; native-image varsayılan olarak yalnızca en içerir.
# Türkçe belgeler için tr de gerekir.
native-image \
  -cp "$CP:$OUT_DIR" \
  -o "$TARGET" \
  --no-fallback \
  -H:ConfigurationFileDirectories=native-config \
  -H:+ReportExceptionStackTraces \
  -H:IncludeLocales=en,tr \
  -H:+AddAllCharsets \
  -J-Duser.language=en \
  -J-Duser.country=US \
  -J-Xmx4g \
  Transform

# native-image Windows'ta çıktıya otomatik .exe ekler.
[ -f "$TARGET.exe" ] && TARGET="$TARGET.exe"
echo "==> Hazır: $TARGET ($(du -h "$TARGET" | cut -f1))"
