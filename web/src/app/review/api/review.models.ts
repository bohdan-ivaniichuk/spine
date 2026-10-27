/**
 * Review API shapes — `specs/002-review-session/contracts/review-api.md` plus the language fields and
 * coverage of `specs/003-atlas-redesign-i18n/contracts/review-api-i18n.md` and the `concept` scope of
 * `specs/004-knowledge-atlas-notes/contracts/review-scope-concept.md` and the session `mode` of
 * `specs/006-weak-spots/contracts/review-mode.md`.
 * camelCase JSON, dates as ISO-8601 strings; `correct` never appears (the server keeps it).
 */

export type ReviewScopeType = 'all' | 'domain' | 'topic' | 'concept';

/**
 * `regular`: due and new questions (the default). `weak`: questions whose most recent answer was
 * wrong, newest miss first, regardless of due dates (`newLimit` is ignored).
 */
export type ReviewMode = 'regular' | 'weak';

export interface ReviewScope {
  type: ReviewScopeType;
  domain: string | null;
  topic: string | null;
  /** Set only for the `concept` scope. */
  concept: string | null;
  domainTitle: string | null;
  topicTitle: string | null;
  conceptTitle: string | null;
}

export interface Ref {
  slug: string;
  title: string;
}

export type QuestionLevel = 'M' | 'S' | 'L';

export interface SessionQuestion {
  id: string;
  level: QuestionLevel;
  reviewed: boolean;
  isNew: boolean;
  /** Markdown subset: fenced code blocks and `inline code`; everything else is literal text. */
  prompt: string;
  /** Option texts in seed order; the correct index is only revealed by the answer feedback. */
  options: string[];
  domain: Ref;
  topic: Ref;
  concept: Ref;
  /** Language actually served for prompt/options; `fallback` = UK requested, EN shown. */
  language?: 'en' | 'uk';
  fallback?: boolean;
}

export interface SessionAnswer {
  questionId: string;
  selectedIndex: number;
  isCorrect: boolean;
}

export interface Session {
  id: string;
  scope: ReviewScope;
  mode: ReviewMode;
  startedAt: string;
  finishedAt: string | null;
  totalLimit: number;
  newLimit: number;
  questions: SessionQuestion[];
  answers: SessionAnswer[];
  /** Questions deprecated after the session started; the client skips them (FR-011). */
  skippedQuestionIds: string[];
}

export interface AnswerFeedback {
  questionId: string;
  selectedIndex: number;
  isCorrect: boolean;
  correctIndex: number;
  explanation: string;
  nextDueAt: string;
  intervalDays: number;
  language?: 'en' | 'uk';
  fallback?: boolean;
}

export interface SessionLimits {
  totalLimit: number;
  newLimit: number;
}

export interface ReviewOverview {
  scope: ReviewScope;
  dueCount: number;
  newCount: number;
  totalEligible: number;
  nextDueAt: string | null;
  /** Questions in scope whose last answer was wrong (independent of mode). */
  weakCount: number;
  defaults: SessionLimits;
  limits: { min: number; max: number };
}

export interface WeakConcept {
  domain: string;
  topic: string;
  concept: string;
  title: string;
  wrong: number;
  total: number;
}

export interface SessionSummary {
  sessionId: string;
  scope: ReviewScope;
  mode: ReviewMode;
  startedAt: string;
  finishedAt: string;
  questionCount: number;
  answered: number;
  correct: number;
  accuracy: number | null;
  weakConcepts: WeakConcept[];
  dueTomorrow: number;
}

/** Query part shared by the overview request and the start request. */
export interface ScopeQuery {
  scope: ReviewScopeType;
  domain?: string;
  topic?: string;
  /** Only with `scope: 'concept'` (the API rejects it for other scopes). */
  concept?: string;
}

export interface StartSessionRequest extends ScopeQuery {
  /** Omitted for `regular` (the server default). */
  mode?: ReviewMode;
  totalLimit?: number;
  newLimit?: number;
}

/** Per-topic counts for the atlas tiles (`GET /api/review/coverage`). */
export interface TopicCoverage {
  domain: string;
  topic: string;
  eligible: number;
  seen: number;
  due: number;
}

export interface Coverage {
  topics: TopicCoverage[];
}

/** Human-readable scope for the progress chip, the overview banner and the summary. */
export function scopeLabel(scope: ReviewScope, allLabel = 'All domains'): string {
  switch (scope.type) {
    case 'domain':
      return scope.domainTitle ?? scope.domain ?? 'Domain';
    case 'topic':
      return `${scope.domainTitle ?? scope.domain ?? ''} › ${scope.topicTitle ?? scope.topic ?? ''}`;
    case 'concept':
      return `${scope.domainTitle ?? scope.domain ?? ''} › ${scope.topicTitle ?? scope.topic ?? ''} › ${scope.conceptTitle ?? scope.concept ?? ''}`;
    default:
      return allLabel;
  }
}

/** Turns a scope from a session or summary back into a request ("Review again"). */
export function toScopeQuery(scope: ReviewScope): ScopeQuery {
  const query: ScopeQuery = { scope: scope.type };
  if (scope.type !== 'all' && scope.domain) {
    query.domain = scope.domain;
  }
  if ((scope.type === 'topic' || scope.type === 'concept') && scope.topic) {
    query.topic = scope.topic;
  }
  if (scope.type === 'concept' && scope.concept) {
    query.concept = scope.concept;
  }
  return query;
}
