/**
 * Shape of `GET /api/knowledge/concepts/{domain}/{topic}/{concept}` (feature 001), as far as the
 * review summary needs it. Feature 003 extends it for the concept page.
 */
export interface KnowledgeConcept {
  domain: { slug: string; title: string };
  topic: { slug: string; title: string };
  slug: string;
  level: string;
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
