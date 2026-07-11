/**
 * XSLT kaynak eşleme (WYSIWYG Faz 2a) — salt-okunur.
 *
 * Amaç: Önizlemedeki bir HTML öğesinin ŞABLONDA hangi satırdan geldiğini
 * bilmek. Bunun için XSLT'nin geçici bir kopyasındaki her *literal* öğeye
 * (`<td>`, `<div>` gibi — `xsl:` önekli olmayanlar) `data-xsl-id` enjekte
 * edilir. Bu kopya YALNIZCA önizleme dönüşümünde kullanılır; kullanıcının
 * dosyasına asla yazılmaz.
 *
 * <b>Neden regex değil:</b> 587 KB'lık şablonlarda gömülü CSS, yorumlar,
 * CDATA ve tırnak içinde `>` karakteri var; naif regex kaçınılmaz olarak
 * bozar. XSLT geçerli XML olduğundan küçük ve kesin bir tokenizer yeterlidir.
 *
 * <b>Sınır:</b> `<xsl:element name="div">` gibi DİNAMİK üretilen öğeler
 * literal olmadığından işaretlenemez — bunlar eşlemede görünmez.
 */

export interface XsltElementRef {
  /** Enjekte edilen kimlik (ör. "e42"). */
  id: string;
  /** Öğe adı (ör. "td"). */
  name: string;
  /** Açılış etiketindeki '<' karakterinin ofseti. */
  tagStart: number;
  /** Açılış etiketinin '>' karakterinden hemen SONRAKİ ofset. */
  tagEnd: number;
  /** 1 tabanlı satır numarası. */
  line: number;
}

export interface XsltInstrumentation {
  /** `data-xsl-id` enjekte edilmiş kopya — sadece önizleme için. */
  instrumented: string;
  /** id → kaynak konumu. */
  refs: Map<string, XsltElementRef>;
}

const NAME_START = /[A-Za-z_]/;

function isNameChar(c: string): boolean {
  return /[A-Za-z0-9_.:-]/.test(c);
}

/**
 * XSLT metnini tarayıp literal öğeleri bulur ve her birine `data-xsl-id`
 * enjekte edilmiş bir kopya döndürür.
 */
export function instrumentXslt(source: string): XsltInstrumentation {
  const refs = new Map<string, XsltElementRef>();
  const found: XsltElementRef[] = [];

  // Satır başlangıç ofsetleri — satır numarasını ikili aramayla buluruz.
  // (Her öğe için baştan '\n' saymak 587 KB'da O(n²) olur ve donar.)
  const lineStarts: number[] = [0];
  for (let k = 0; k < source.length; k++) {
    if (source.charCodeAt(k) === 10) lineStarts.push(k + 1);
  }
  const lineOf = (offset: number): number => {
    let lo = 0;
    let hi = lineStarts.length - 1;
    while (lo < hi) {
      const mid = (lo + hi + 1) >> 1;
      if (lineStarts[mid] <= offset) lo = mid;
      else hi = mid - 1;
    }
    return lo + 1; // 1 tabanlı
  };

  let i = 0;
  let counter = 0;
  const n = source.length;

  while (i < n) {
    const lt = source.indexOf('<', i);
    if (lt === -1) break;

    // Yorum / CDATA / işleme yönergesi / DOCTYPE → atla (içleri ayrıştırılmaz).
    if (source.startsWith('<!--', lt)) {
      const end = source.indexOf('-->', lt + 4);
      i = end === -1 ? n : end + 3;
      continue;
    }
    if (source.startsWith('<![CDATA[', lt)) {
      const end = source.indexOf(']]>', lt + 9);
      i = end === -1 ? n : end + 3;
      continue;
    }
    if (source.startsWith('<?', lt)) {
      const end = source.indexOf('?>', lt + 2);
      i = end === -1 ? n : end + 2;
      continue;
    }
    if (source.startsWith('<!', lt)) {
      const end = source.indexOf('>', lt + 2);
      i = end === -1 ? n : end + 1;
      continue;
    }
    // Kapanış etiketi → atla.
    if (source[lt + 1] === '/') {
      const end = source.indexOf('>', lt + 2);
      i = end === -1 ? n : end + 1;
      continue;
    }
    // Öğe adı değilse (ör. metin içinde "<") → ilerle.
    if (!NAME_START.test(source[lt + 1] ?? '')) {
      i = lt + 1;
      continue;
    }

    // Açılış etiketi: adı oku.
    let j = lt + 1;
    while (j < n && isNameChar(source[j])) j++;
    const name = source.slice(lt + 1, j);

    // Öznitelikleri geç — TIRNAKLARA saygı göster (içinde '>' olabilir).
    let quote: string | null = null;
    while (j < n) {
      const c = source[j];
      if (quote) {
        if (c === quote) quote = null;
      } else if (c === '"' || c === "'") {
        quote = c;
      } else if (c === '>') {
        break;
      }
      j++;
    }
    if (j >= n) break;
    const tagEnd = j + 1; // '>' dahil

    // Literal çıktı öğesi = ÖNEKSİZ öğe (`<td>`, `<div>`...). `xsl:*` XSLT
    // komutudur, `ds:*`/`cac:*` gibi önekliler de HTML çıktısı değildir.
    if (!name.includes(':')) {
      const id = 'e' + counter++;
      const ref: XsltElementRef = { id, name, tagStart: lt, tagEnd, line: lineOf(lt) };
      refs.set(id, ref);
      found.push(ref);
    }

    i = tagEnd;
  }

  // Tek geçişte birleştir. (Her enjeksiyonda string'i yeniden kurmak
  // O(n·m) olur ve binlerce öğede donar.) `found` artan ofset sırasındadır.
  const parts: string[] = [];
  let cursor = 0;
  for (const r of found) {
    const insertAt = r.tagStart + 1 + r.name.length; // öğe adından hemen sonra
    parts.push(source.slice(cursor, insertAt), ` data-xsl-id="${r.id}"`);
    cursor = insertAt;
  }
  parts.push(source.slice(cursor));

  return { instrumented: parts.join(''), refs };
}
