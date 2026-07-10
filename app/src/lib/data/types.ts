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

export interface Manifest {
  version: string;
  generatedAt: string;
  source: string;
  files: string[];
}
