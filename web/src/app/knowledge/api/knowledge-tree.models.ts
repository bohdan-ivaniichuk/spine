/**
 * Shape of `GET /api/knowledge/tree` (feature 001): used by the spine rail, the review scope pickers
 * and the Atlas (feature 004).
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

/** Live (non-deprecated) topics of a domain in manifest order. */
export function liveTopics(domain: TreeDomain): TreeTopic[] {
  return domain.topics.filter((t) => !t.deprecatedAt).sort((a, b) => a.sortOrder - b.sortOrder);
}

/** Live concepts of a topic in sort order. A topic without any is shown as "soon" in the Atlas. */
export function liveConcepts(topic: TreeTopic): TreeConcept[] {
  return topic.concepts.filter((c) => !c.deprecatedAt).sort((a, b) => a.sortOrder - b.sortOrder);
}
