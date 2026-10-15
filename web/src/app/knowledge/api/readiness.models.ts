/**
 * `GET /api/review/readiness` (feature 004, contracts/readiness-api.md): the local learner's readiness
 * per domain, topic and concept. Slugs only — the client joins titles from the knowledge tree.
 */

export type ReadinessState = 'not-started' | 'in-progress' | 'ready';

export interface ConceptReadiness {
  concept: string;
  state: ReadinessState;
  /** Schedulable questions of the concept. */
  questions: number;
  readyQuestions: number;
}

/** Shared by topics and domains: ready concepts of those counted; `percent` is `null` when none count. */
export interface ReadinessSummary {
  ready: number;
  counted: number;
  percent: number | null;
}

export interface TopicReadiness extends ReadinessSummary {
  topic: string;
  /** Only concepts with at least one schedulable question. */
  concepts: ConceptReadiness[];
}

export interface DomainReadiness extends ReadinessSummary {
  domain: string;
  topics: TopicReadiness[];
}

export interface Readiness {
  domains: DomainReadiness[];
}

/** Language-independent (FR-010), so no `?lang=`. */
export const READINESS_URL = '/api/review/readiness';
