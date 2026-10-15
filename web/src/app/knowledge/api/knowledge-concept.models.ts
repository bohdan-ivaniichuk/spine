/**
 * Shape of `GET /api/knowledge/concepts/{domain}/{topic}/{concept}` (feature 001), used by the
 * review summary's concept peek and the Atlas concept page (feature 004).
 */
export interface KnowledgeConcept {
  domain: { slug: string; title: string };
  topic: { slug: string; title: string };
  slug: string;
  level: string;
  /** Language actually served (`fallback` = UK requested, EN shown). */
  language: 'en' | 'uk';
  fallback: boolean;
  reviewed: boolean;
  title: string;
  /** Markdown; `null` when the concept has no note yet. */
  note: string | null;
  questionCount: number;
  kataCount: number;
  deprecatedAt: string | null;
}

export function conceptUrl(domain: string, topic: string, concept: string): string {
  return `/api/knowledge/concepts/${encodeURIComponent(domain)}/${encodeURIComponent(topic)}/${encodeURIComponent(concept)}`;
}
