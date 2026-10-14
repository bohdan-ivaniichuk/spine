import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ReviewApi, errorMessage } from './api/review-api';
import {
  AnswerFeedback,
  Session,
  SessionAnswer,
  SessionLimits,
  SessionQuestion,
  SessionSummary,
  StartSessionRequest,
  scopeLabel,
  toScopeQuery,
} from './api/review.models';

export type SessionStatus =
  'idle' | 'loading' | 'ready' | 'checking' | 'checked' | 'finishing' | 'finished' | 'error';

export const SESSION_ROUTE = '/review/session';
export const SUMMARY_ROUTE = '/review/summary';
export const OVERVIEW_ROUTE = '/review';

/**
 * State of the running review session. Root-provided so it survives the overview → session →
 * summary navigation; a page reload goes back to the API (`resume`), which is the source of truth.
 */
@Injectable({ providedIn: 'root' })
export class SessionStore {
  private readonly api = inject(ReviewApi);
  private readonly router = inject(Router);

  private readonly sessionState = signal<Session | null>(null);
  private readonly statusState = signal<SessionStatus>('idle');
  private readonly selectedState = signal<number | null>(null);
  private readonly feedbackState = signal<AnswerFeedback | null>(null);
  private readonly answeredState = signal<ReadonlyMap<string, SessionAnswer>>(new Map());
  private readonly summaryState = signal<SessionSummary | null>(null);
  private readonly errorState = signal<string | null>(null);
  private readonly limitsState = signal<SessionLimits | null>(null);

  readonly session = this.sessionState.asReadonly();
  readonly status = this.statusState.asReadonly();
  readonly selectedIndex = this.selectedState.asReadonly();
  readonly feedback = this.feedbackState.asReadonly();
  readonly answered = this.answeredState.asReadonly();
  readonly summary = this.summaryState.asReadonly();
  readonly error = this.errorState.asReadonly();
  /** Limits of the last session, so "Review again" reuses them after the session is gone. */
  readonly limits = this.limitsState.asReadonly();

  /** Session questions minus the ones deprecated after start (FR-011). */
  readonly questions = computed<SessionQuestion[]>(() => {
    const session = this.sessionState();
    if (!session) {
      return [];
    }
    const skipped = new Set(session.skippedQuestionIds);
    return session.questions.filter((q) => !skipped.has(q.id));
  });

  readonly total = computed(() => this.questions().length);

  readonly answeredCount = computed(
    () => this.questions().filter((q) => this.answeredState().has(q.id)).length,
  );

  /** The question on screen: the one just checked, otherwise the first unanswered one. */
  readonly current = computed<SessionQuestion | null>(() => {
    const questions = this.questions();
    const feedback = this.feedbackState();
    if (feedback) {
      return questions.find((q) => q.id === feedback.questionId) ?? null;
    }
    const answered = this.answeredState();
    return questions.find((q) => !answered.has(q.id)) ?? null;
  });

  /** 1-based position of the current question ("7 / 20"); equals `total` once everything is answered. */
  readonly position = computed(() => {
    const current = this.current();
    if (!current) {
      return this.total();
    }
    return this.questions().indexOf(current) + 1;
  });

  /** True when no unanswered question remains besides the current one. */
  readonly isLast = computed(() => {
    const current = this.current();
    const answered = this.answeredState();
    return this.questions().every((q) => q.id === current?.id || answered.has(q.id));
  });

  readonly scopeLabel = computed(() => {
    const session = this.sessionState();
    return session ? scopeLabel(session.scope) : '';
  });

  /** Starts a new session (the server auto-finishes any open one) and opens the card page. */
  async start(request: StartSessionRequest): Promise<void> {
    this.statusState.set('loading');
    this.errorState.set(null);
    try {
      this.load(await this.api.start(request));
      await this.router.navigateByUrl(SESSION_ROUTE);
    } catch (error) {
      this.statusState.set('error');
      this.errorState.set(errorMessage(error));
    }
  }

  /** "Review again" from the summary: same scope, same limits. */
  async reviewAgain(): Promise<void> {
    const summary = this.summaryState();
    if (!summary) {
      return;
    }
    await this.start({ ...toScopeQuery(summary.scope), ...(this.limitsState() ?? {}) });
  }

  /**
   * Loads the open session from the API unless one is already in memory. Returns `null` when the
   * API answers 204 (no open session).
   */
  async resume(): Promise<Session | null> {
    const inMemory = this.sessionState();
    if (inMemory) {
      return inMemory;
    }
    this.statusState.set('loading');
    this.errorState.set(null);
    try {
      const session = await this.api.current();
      if (session) {
        this.load(session);
      } else {
        this.statusState.set('idle');
      }
      return session;
    } catch (error) {
      this.statusState.set('error');
      this.errorState.set(errorMessage(error));
      return null;
    }
  }

  select(index: number): void {
    const question = this.current();
    if (this.statusState() !== 'ready' || !question) {
      return;
    }
    if (index >= 0 && index < question.options.length) {
      this.selectedState.set(index);
    }
  }

  /** Submits the selection. On failure the selection is kept so Check can simply be pressed again (FR-015). */
  async check(): Promise<void> {
    const session = this.sessionState();
    const question = this.current();
    const selectedIndex = this.selectedState();
    if (!session || !question || selectedIndex === null || this.statusState() !== 'ready') {
      return;
    }
    this.statusState.set('checking');
    this.errorState.set(null);
    try {
      const feedback = await this.api.answer(session.id, question.id, selectedIndex);
      this.answeredState.update((map) => {
        const next = new Map(map);
        next.set(question.id, {
          questionId: question.id,
          selectedIndex: feedback.selectedIndex,
          isCorrect: feedback.isCorrect,
        });
        return next;
      });
      this.feedbackState.set(feedback);
      this.statusState.set('checked');
    } catch (error) {
      this.statusState.set('ready');
      this.errorState.set(errorMessage(error));
    }
  }

  /** Moves to the next unanswered question, or finishes when there is none. */
  next(): void {
    if (this.statusState() !== 'checked') {
      return;
    }
    if (this.isLast()) {
      // Keep feedback/selection so a failed finish still renders the card and Finish can be retried;
      // finish() clears them on success.
      void this.finish();
    } else {
      this.feedbackState.set(null);
      this.selectedState.set(null);
      this.statusState.set('ready');
    }
  }

  /** Finishes the session (idempotent on the server) and opens the summary. */
  async finish(): Promise<void> {
    const session = this.sessionState();
    if (!session) {
      return;
    }
    const before = this.statusState();
    if (before === 'finishing' || before === 'loading' || before === 'checking') {
      return;
    }
    this.statusState.set('finishing');
    this.errorState.set(null);
    try {
      const summary = await this.api.finish(session.id);
      this.summaryState.set(summary);
      this.sessionState.set(null);
      this.feedbackState.set(null);
      this.selectedState.set(null);
      this.answeredState.set(new Map());
      this.statusState.set('finished');
      await this.router.navigateByUrl(SUMMARY_ROUTE);
    } catch (error) {
      this.statusState.set(before);
      this.errorState.set(errorMessage(error));
    }
  }

  private load(session: Session): void {
    this.sessionState.set(session);
    this.limitsState.set({ totalLimit: session.totalLimit, newLimit: session.newLimit });
    this.answeredState.set(new Map(session.answers.map((a) => [a.questionId, a])));
    this.selectedState.set(null);
    this.feedbackState.set(null);
    this.summaryState.set(null);
    this.errorState.set(null);
    this.statusState.set('ready');
  }
}
