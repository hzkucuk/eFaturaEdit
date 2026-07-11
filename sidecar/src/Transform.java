import net.sf.saxon.s9api.Processor;
import net.sf.saxon.s9api.Serializer;
import net.sf.saxon.s9api.XdmNode;
import net.sf.saxon.s9api.XsltCompiler;
import net.sf.saxon.s9api.XsltExecutable;
import net.sf.saxon.s9api.Xslt30Transformer;

import javax.xml.transform.stream.StreamSource;
import java.io.BufferedInputStream;
import java.io.BufferedOutputStream;
import java.io.DataInputStream;
import java.io.IOException;
import java.io.OutputStream;
import java.io.StringReader;
import java.io.StringWriter;
import java.nio.charset.StandardCharsets;

/**
 * e-Fatura Edit — XSLT 2.0/3.0 dönüşüm sidecar'ı (Saxon-HE, MPL 2.0).
 *
 * Tarayıcının yerleşik XSLTProcessor'ı yalnızca XSLT 1.0 destekler; GİB/müşteri
 * şablonları XSLT 2.0 özellikleri (format-dateTime, upper-case, tokenize,
 * for-each-group, xsl:function ...) kullanabildiğinden tam bir işlemci gerekir.
 * Bu program Tauri tarafından bir sidecar süreç olarak çalıştırılır.
 *
 * Protokol (stdin, ikili):
 *   [4 bayt big-endian XSLT uzunluğu][XSLT UTF-8 baytları]
 *   [4 bayt big-endian XML  uzunluğu][XML  UTF-8 baytları]
 *
 * Çıktı:
 *   Başarı  → stdout'a HTML (UTF-8), çıkış kodu 0
 *   Hata    → stderr'e mesaj, çıkış kodu 1
 *
 * Not: 600 KB'lık şablonlar komut satırı argüman sınırını aşabildiğinden veri
 * argümanla değil stdin üzerinden aktarılır.
 */
public final class Transform {

    public static void main(String[] args) {
        try {
            DataInputStream in = new DataInputStream(new BufferedInputStream(System.in));
            String xsltText = readBlock(in);
            String xmlText = readBlock(in);

            Processor processor = new Processor(false); // false = Saxon-HE (lisanssız)
            XsltCompiler compiler = processor.newXsltCompiler();
            XsltExecutable executable = compiler.compile(new StreamSource(new StringReader(xsltText)));

            XdmNode source = processor.newDocumentBuilder()
                    .build(new StreamSource(new StringReader(xmlText)));

            StringWriter result = new StringWriter();
            Serializer serializer = processor.newSerializer(result);

            Xslt30Transformer transformer = executable.load30();
            // Şablonlardaki global xsl:variable'lar sık sık kökten ("/...") seçim
            // yapar; XSLT 3.0 API'sinde bunun için global bağlam öğesi açıkça
            // verilmelidir, yoksa "context item is absent" (XPDY0002) hatası olur.
            transformer.setGlobalContextItem(source);
            transformer.applyTemplates(source, serializer);

            String html = normalizeDoctype(result.toString());

            OutputStream out = new BufferedOutputStream(System.out);
            out.write(html.getBytes(StandardCharsets.UTF_8));
            out.flush();
            System.exit(0);
        } catch (Throwable e) {
            System.err.println(describe(e));
            if (System.getenv("EFATURA_XSLT_DEBUG") != null) {
                e.printStackTrace();
            }
            System.exit(1);
        }
    }

    /**
     * Hatayı tek satırda, neden zinciriyle birlikte özetle.
     *
     * <p>Bazı hatalar (ör. {@code ExceptionInInitializerError}) kendi başına
     * mesaj taşımaz; asıl bilgi {@code getCause()} zincirindedir. Ayrıntılı
     * yığın izi için {@code EFATURA_XSLT_DEBUG=1} ortam değişkeni kullanılır.
     */
    private static String describe(Throwable e) {
        StringBuilder sb = new StringBuilder();
        Throwable current = e;
        int depth = 0;
        while (current != null && depth < 5) {
            if (depth > 0) {
                sb.append(" ← ");
            }
            String message = current.getMessage();
            sb.append(message == null || message.isEmpty()
                    ? current.getClass().getName()
                    : message);
            current = current.getCause();
            depth++;
        }
        return sb.toString();
    }

    /**
     * Çıktıyı HTML5 DOCTYPE'ına normalize et.
     *
     * <p>Şablonlar genelde {@code xsl:output} ile HTML 4.01 Transitional doctype
     * bildirir; bu, tarayıcıda "almost standards mode"a düşürür ve tablo/kutu
     * modeli hesaplarını değiştirir. Uygulamanın mevcut (tarayıcı tabanlı)
     * önizlemesi her zaman {@code <!DOCTYPE html>} zorluyor — sidecar da aynısını
     * yapmalı ki var olan tasarımlar birebir aynı görünsün.
     */
    private static String normalizeDoctype(String html) {
        String trimmed = html.stripLeading();
        if (trimmed.regionMatches(true, 0, "<!DOCTYPE", 0, 9)) {
            int end = trimmed.indexOf('>');
            if (end >= 0) {
                trimmed = trimmed.substring(end + 1).stripLeading();
            }
        }
        return "<!DOCTYPE html>\n" + trimmed;
    }

    /** 4 bayt big-endian uzunluk + o kadar UTF-8 bayt oku. */
    private static String readBlock(DataInputStream in) throws IOException {
        int length = in.readInt();
        if (length < 0 || length > 64 * 1024 * 1024) {
            throw new IOException("Geçersiz blok uzunluğu: " + length);
        }
        byte[] buffer = new byte[length];
        in.readFully(buffer);
        String text = new String(buffer, StandardCharsets.UTF_8);
        // UTF-8 BOM decode sonrası U+FEFF olarak kalır ve XML ayrıştırıcı
        // "Content is not allowed in prolog" hatası verir — kırp.
        if (!text.isEmpty() && text.charAt(0) == '﻿') {
            text = text.substring(1);
        }
        return text;
    }

    private Transform() {}
}
