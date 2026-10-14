/**
 * Minimal shape of `GET /api/knowledge/tree` (feature 001), enough for the review scope pickers.
 * Feature 003 extends these types for the knowledge tree UI.
 */

export interface KnowledgeTree {
  language: string;
  domains: TreeDomain[];
}

export interface TreeDomain {
  slug: string;
  /** In the requested language when the manifest has it (`titles.uk`), else English. */
  title: string;
  /** True when a non-English language was requested and the English title is served. */
  titleFallback: boolean;
  sortOrder: number;
  deprecatedAt: string | null;
  topics: TreeTopic[];
}

export interface TreeTopic {
  slug: string;
  /** In the requested language when the manifest has it (`titles.uk`), else English. */
  title: string;
  /** True when a non-English language was requested and the English title is served. */
  titleFallback: boolean;
  sortOrder: number;
  deprecatedAt: string | null;
  concepts: TreeConcept[];
}

export interface TreeConcept {
  slug: string;
  title: string;
  titleFallback: boolean;
  level: string;
  reviewed: boolean;
  sortOrder: number;
  questionCount: number;
  kataCount: number;
  deprecatedAt: string | null;
}
