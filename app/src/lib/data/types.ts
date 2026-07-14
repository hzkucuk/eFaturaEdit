/**
 * eFaturaEdit.Core JSON verilerinin TypeScript tipleri.
 *
 * Kaynak: src/eFaturaEdit.Core (C#) → eFaturaEdit.DataExport → src/lib/data/*.json
 * Yenilemek için: `npm run data:sync`
 */

export interface Snippet {
  key: string;
  category: string;
  subCategory: string | null;
  displayName: string;
  description: string;
  iconText: string;
  xsltCode: string;
  dragDataString: string;
}

export interface SampleEntry {
  fileName: string;
  displayName: string;
}

export interface SampleGroup {
  categoryName: string;
  entries: SampleEntry[];
}

export interface Samples {
  samplesRelativePath: string;
  groups: SampleGroup[];
}

export interface CompletionItem {
  text: string;
  description: string;
  imageIndex: number;
}

export interface Completion {
  xsltTags: CompletionItem[];
  xPathPaths: CompletionItem[];
}

/**
 * AI asistanına eklenebilen adlandırılmış talimat paketi ("yetenek").
 *
 * Seçilen yeteneklerin `prompt` metinleri sistem promptunun sonuna eklenir.
 * Sağlayıcıdan bağımsızdır — tool-calling gerektirmez.
 *
 * Kullanıcının kendi yazdığı yetenekler aynı şekle uyar ama ayrı bir dosyada
 * tutulur (bkz. `$lib/user-skills`), bundled katalogla birleştirilip üzerine
 * yazılmaz.
 */
export interface AiSkill {
  id: string;
  category: string;
  displayName: string;
  description: string;
  prompt: string;
}

export interface Manifest {
  version: string;
  generatedAt: string;
  source: string;
  files: string[];
}
