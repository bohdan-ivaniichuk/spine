/**
 * Review API shapes — one-to-one with `specs/002-review-session/contracts/review-api.md`.
 * camelCase JSON, dates as ISO-8601 strings; `correct` never appears (the server keeps it).
 */

export type ReviewScopeType = 'all' | 'domain' | 'topic';

export interface ReviewScope {
  type: ReviewScopeType;
  domain: string | null;
  topic: string | null;
  domainTitle: string | null;
  topicTitle: string | null;
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
}

export interface SessionAnswer {
  questionId: string;
  selectedIndex: number;
  isCorrect: boolean;
}

export interface Session {
  id: string;
  scope: ReviewScope;
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
}

export interface StartSessionRequest extends ScopeQuery {
  totalLimit?: number;
  newLimit?: number;
}

/** Human-readable scope for the progress chip, the overview banner and the summary. */
export function scopeLabel(scope: ReviewScope): string {
  switch (scope.type) {
    case 'domain':
      return scope.domainTitle ?? scope.domain ?? 'Domain';
    case 'topic':
      return `${scope.domainTitle ?? scope.domain ?? ''} › ${scope.topicTitle ?? scope.topic ?? ''}`;
    default:
      return 'All domains';
  }
}

/** Turns a scope from a session or summary back into a request ("Review again"). */
export function toScopeQuery(scope: ReviewScope): ScopeQuery {
  const query: ScopeQuery = { scope: scope.type };
  if (scope.type !== 'all' && scope.domain) {
    query.domain = scope.domain;
  }
  if (scope.type === 'topic' && scope.topic) {
    query.topic = scope.topic;
  }
  return query;
}
