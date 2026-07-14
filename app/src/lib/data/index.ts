/**
 * Core JSON verisi için tipli import merkezi.
 *
 * Vite JSON modüllerini otomatik parse eder ve build zamanında bundle'a dahil eder.
 * Tree-shaking sayesinde kullanılmayan alanlar üretim build'inde çıkarılabilir.
 */
import snippetsJson from './snippets.json';
import samplesJson from './samples.json';
import completionJson from './completion.json';
import skillsJson from './skills.json';
import manifestJson from './manifest.json';

import type { Snippet, Samples, Completion, AiSkill, Manifest } from './types';

export const snippets: Snippet[] = snippetsJson as Snippet[];
export const samples: Samples = samplesJson as Samples;
export const completion: Completion = completionJson as Completion;
export const aiSkills: AiSkill[] = skillsJson as AiSkill[];
export const manifest: Manifest = manifestJson as Manifest;

/**
 * Snippet'leri kategori → alt kategori → snippet olarak grupla.
 */
export function groupSnippetsByCategory(list: Snippet[] = snippets): Map<string, Map<string, Snippet[]>> {
  const grouped = new Map<string, Map<string, Snippet[]>>();

  for (const snippet of list) {
    const category = snippet.category ?? 'Diğer';
    const subCategory = snippet.subCategory ?? '';

    if (!grouped.has(category)) {
      grouped.set(category, new Map());
    }
    const subMap = grouped.get(category)!;

    if (!subMap.has(subCategory)) {
      subMap.set(subCategory, []);
    }
    subMap.get(subCategory)!.push(snippet);
  }

  return grouped;
}

/**
 * Snippet'i key ile bul.
 */
export function findSnippet(key: string): Snippet | undefined {
  return snippets.find((s) => s.key === key);
}
