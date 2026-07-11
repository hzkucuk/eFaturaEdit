/**
 * AI yanıtından uygulanabilir öneri çıkarımı ve uygulanması.
 *
 * İki tür öneri desteklenir:
 *  - `edits`: hedefli "bul/değiştir" düzenlemeleri (mevcut dosyayı bozmadan,
 *    doğru yere uygular). En yaygın durum.
 *  - `full`: sıfırdan tam belge (yalnızca yeni dosya oluştururken).
 *
 * Kapsam kilidi: burada hiçbir şey otomatik uygulanmaz — çağıran taraf
 * (+page.svelte) sonucu bir onay modalında gösterir, kullanıcı onaylayınca
 * editöre yazar.
 */
export type AiTarget = 'xslt' | 'xml';

export interface AiEdit {
  search: string;
  replace: string;
}

export type AiSuggestion =
  | { kind: 'edits'; target: AiTarget; edits: AiEdit[] }
  | { kind: 'full'; target: AiTarget; code: string };

function normalizeLang(raw: string | undefined): AiTarget {
  return (raw ?? '').toLowerCase() === 'xml' ? 'xml' : 'xslt';
}

/** Kapanmış ya da (yanıt kesildiyse) kapanmamış tek bir tam kod bloğunu ayıklar. */
export function extractCodeBlock(content: string): { lang: AiTarget; code: string } | null {
  const paired = content.match(/```(xslt|xsl|xml)?[ \t]*\n([\s\S]*?)```/i);
  if (paired) {
    return { lang: normalizeLang(paired[1]), code: paired[2].trim() };
  }
  // Açılış var ama kapanış ``` yok — açılıştan sonrasını al, sondaki düz metni
  // son XML etiketine kadar kırparak kod dışı "prose"yi ayıkla.
  const open = content.match(/```(xslt|xsl|xml)?[ \t]*\n([\s\S]*)$/i);
  if (open) {
    let code = open[2];
    const lastTag = code.lastIndexOf('>');
    if (lastTag !== -1) code = code.slice(0, lastTag + 1);
    code = code.trim();
    if (code) return { lang: normalizeLang(open[1]), code };
  }
  return null;
}

const EDIT_RE =
  /<<<<<<< SEARCH\r?\n([\s\S]*?)\r?\n=======\r?\n([\s\S]*?)\r?\n>>>>>>> REPLACE/g;

/** Metindeki tüm SEARCH/REPLACE bloklarını ayıklar. */
export function parseEdits(content: string): AiEdit[] {
  const edits: AiEdit[] = [];
  let m: RegExpExecArray | null;
  EDIT_RE.lastIndex = 0;
  while ((m = EDIT_RE.exec(content)) !== null) {
    edits.push({ search: m[1], replace: m[2] });
  }
  return edits;
}

/** Asistan mesajından uygulanabilir öneriyi çıkarır (yoksa null). */
export function extractSuggestion(content: string): AiSuggestion | null {
  // Hedef dili ilk kod bloğu etiketinden çıkar (varsa).
  const fence = content.match(/```(xslt|xsl|xml)/i);
  const target = normalizeLang(fence?.[1]);

  const edits = parseEdits(content);
  if (edits.length > 0) return { kind: 'edits', target, edits };

  const block = extractCodeBlock(content);
  // SEARCH/REPLACE yoksa ve tam blok varsa: yeni-dosya (full) önerisi.
  if (block) return { kind: 'full', target: block.lang, code: block.code };

  return null;
}

/** Kod dışı açıklama metnini (blok/düzenleme bölgelerinden önceki kısım) döndürür. */
export function suggestionProse(content: string): string {
  const idx = content.search(/```|<<<<<<< SEARCH/);
  return (idx === -1 ? content : content.slice(0, idx)).trim();
}

function normalizeWhitespace(s: string): string {
  return s.replace(/\r\n/g, '\n').replace(/[ \t]+$/gm, '');
}

/**
 * Satır bazlı, girinti/boşluğa toleranslı eşleştirme. Model SEARCH bloğunu
 * dosyadakinden farklı girintiyle (tab↔boşluk, farklı derinlik) yazsa bile
 * her satırın "trim" edilmiş hali karşılaştırılarak konum bulunur; bulunan
 * ÖZGÜN satır aralığı `replace` ile değiştirilir. Bulunamazsa null.
 */
function lineFuzzyReplace(text: string, search: string, replace: string): string | null {
  const textLines = text.replace(/\r\n/g, '\n').split('\n');
  let searchLines = search.replace(/\r\n/g, '\n').split('\n');
  // Baştaki/sondaki boş satırları at.
  while (searchLines.length && searchLines[0].trim() === '') searchLines = searchLines.slice(1);
  while (searchLines.length && searchLines[searchLines.length - 1].trim() === '')
    searchLines = searchLines.slice(0, -1);
  if (searchLines.length === 0) return null;

  const needle = searchLines.map((l) => l.trim());
  for (let i = 0; i + needle.length <= textLines.length; i++) {
    let ok = true;
    for (let k = 0; k < needle.length; k++) {
      if (textLines[i + k].trim() !== needle[k]) {
        ok = false;
        break;
      }
    }
    if (ok) {
      const before = textLines.slice(0, i);
      const after = textLines.slice(i + needle.length);
      return [...before, ...replace.replace(/\r\n/g, '\n').split('\n'), ...after].join('\n');
    }
  }
  return null;
}

export interface ApplyResult {
  result: string;
  applied: number;
  unmatched: AiEdit[];
}

/**
 * Düzenlemeleri sırayla metne uygular. Bir SEARCH bulunamazsa o düzenleme
 * atlanır ve `unmatched`'e eklenir (dosya asla kör biçimde bozulmaz).
 * Sırayla: (1) birebir, (2) satır-sonu/boşluk normalize, (3) satır bazlı
 * girinti-toleranslı eşleştirme.
 */
export function applyEdits(text: string, edits: AiEdit[]): ApplyResult {
  let result = text;
  let applied = 0;
  const unmatched: AiEdit[] = [];

  for (const edit of edits) {
    if (result.includes(edit.search)) {
      result = result.replace(edit.search, edit.replace);
      applied++;
      continue;
    }
    // Normalize edilmiş eşleşme (CRLF ve satır sonu boşlukları).
    const normResult = normalizeWhitespace(result);
    const normSearch = normalizeWhitespace(edit.search);
    if (normSearch && normResult.includes(normSearch)) {
      result = normResult.replace(normSearch, edit.replace);
      applied++;
      continue;
    }
    // Girinti/boşluğa toleranslı satır bazlı eşleştirme.
    const fuzzy = lineFuzzyReplace(result, edit.search, edit.replace);
    if (fuzzy !== null) {
      result = fuzzy;
      applied++;
      continue;
    }
    unmatched.push(edit);
  }

  return { result, applied, unmatched };
}
