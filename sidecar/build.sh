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

# native-image'ı çöz: Windows'ta komut "native-image.cmd"dir ve Git Bash
# `command -v native-image` ile bulunamaz.
if command -v native-image >/dev/null 2>&1; then
  NATIVE_IMAGE="native-image"
elif command -v native-image.cmd >/dev/null 2>&1; then
  NATIVE_IMAGE="native-image.cmd"
elif [ -n "${JAVA_HOME:-}" ] && [ -x "$JAVA_HOME/bin/native-image" ]; then
  NATIVE_IMAGE="$JAVA_HOME/bin/native-image"
elif [ -n "${JAVA_HOME:-}" ] && [ -f "$JAVA_HOME/bin/native-image.cmd" ]; then
  NATIVE_IMAGE="$JAVA_HOME/bin/native-image.cmd"
else
  echo "HATA: native-image bulunamadı. GraalVM kurup JAVA_HOME/PATH ayarlayın." >&2
  exit 1
fi
echo "==> native-image: $NATIVE_IMAGE"

# Tauri sidecar'ları hedef üçlüsü (target triple) son eki ister.
TRIPLE="$(rustc -vV | awk '/^host:/ {print $2}')"
TARGET="$BIN_DIR/xslt-transform-$TRIPLE"

echo "==> Java derleniyor"
rm -rf "$OUT_DIR" && mkdir -p "$OUT_DIR"
javac -cp "$CP" -d "$OUT_DIR" src/Transform.java

echo "==> native-image ($TRIPLE)"
mkdir -p "$BIN_DIR"

# CPU komut seti: native-image, x64'te varsayılan olarak MODERN komutları
# (AVX2 vb.) hedefler. Bu ikili:
#   - Windows'un ARM üzerindeki x64 emülasyonunda (Prism) ve
#   - eski/kısıtlı CPU'larda
# ilk komutta ANINDA ölür — hata bile veremez. Bizde tam olarak bu oldu:
# ARM64 Windows 11 VM'de süreç başlıyor, biz stdin'e yazamadan gidiyor
# ("Boru sonlandı", os error 109). x64 Windows'ta ise sorunsuz çalışıyor.
#
# `compatibility` = en düşük ortak payda (x86-64 taban). Hız kaybı bu iş yükü
# için önemsiz; çalışmayan bir motorun hızı zaten sıfırdır.
#
# Bayrağı yalnızca DESTEKLENİYORSA ekle: -march AMD64'e özgüdür, ARM64
# (macOS/Linux) derlemelerinde yoktur ve derlemeyi kırardı.
MARCH_FLAG=""
case "$TRIPLE" in
  x86_64-*)
    if "$NATIVE_IMAGE" -march=list >/dev/null 2>&1; then
      MARCH_FLAG="-march=compatibility"
      echo "    (CPU uyumluluk modu: $MARCH_FLAG)"
    else
      echo "    (uyarı: -march desteklenmiyor, varsayılan komut setiyle derleniyor)"
    fi
    ;;
esac

# -J-Duser.language: macOS'ta JVM locale'i sistem tercihlerinden gelir; LANG
# yetmez. Türkçe locale'de "DARWIN".toLowerCase() → "darwın" olur ve
# native-image, include/darwin (jni_md.h) dizinini bulamaz.
# IncludeLocales/AddAllCharsets: format-dateTime gibi XSLT 2.0 fonksiyonları
# locale verisi ister; native-image varsayılan olarak yalnızca en içerir.
# Türkçe belgeler için tr de gerekir.
# IncludeResourceBundles (Xerces msg): XML bozuk olduğunda Xerces, hata metnini
# bir resource bundle'dan okur. Bu paketler ikiliye konmazsa parser hatayı
# BİLDİRİRKEN çöker ve kullanıcı "Could not load any resource bundle by
# ...impl.msg.XMLMessages" gibi SEBEPLE İLGİSİZ bir mesaj görür — gerçek hata
# ("satır 42'de kapanmayan etiket") tamamen kaybolur. Kaldırma.
"$NATIVE_IMAGE" \
  -cp "$CP${SEP}$OUT_DIR" \
  -o "$TARGET" \
  --no-fallback \
  ${MARCH_FLAG} \
  -H:ConfigurationFileDirectories=native-config \
  -H:+ReportExceptionStackTraces \
  -H:IncludeLocales=en,tr \
  -H:+AddAllCharsets \
  -H:IncludeResourceBundles=com.sun.org.apache.xerces.internal.impl.msg.XMLMessages \
  -H:IncludeResourceBundles=com.sun.org.apache.xerces.internal.impl.msg.SAXMessages \
  -H:IncludeResourceBundles=com.sun.org.apache.xerces.internal.impl.msg.DOMMessages \
  -H:IncludeResourceBundles=com.sun.org.apache.xerces.internal.impl.msg.XMLSchemaMessages \
  -H:IncludeResourceBundles=com.sun.org.apache.xerces.internal.impl.msg.DatatypeMessages \
  -J-Duser.language=en \
  -J-Duser.country=US \
  -J-Xmx4g \
  Transform

# native-image Windows'ta çıktıya otomatik .exe ekler.
if [ -f "$TARGET.exe" ]; then TARGET="$TARGET.exe"; fi
echo "==> Hazır: $TARGET ($(du -h "$TARGET" | cut -f1))"
