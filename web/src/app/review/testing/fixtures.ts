import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { KnowledgeTree } from '../../knowledge/api/knowledge-tree.models';
import {
  AnswerFeedback,
  ReviewOverview,
  ReviewScope,
  Session,
  SessionQuestion,
  SessionSummary,
} from '../api/review.models';

/** Shared fixtures for the review specs — shaped exactly like contracts/review-api.md. */

export const topicScope: ReviewScope = {
  type: 'topic',
  domain: 'dotnet',
  topic: 'clr-memory',
  domainTitle: 'C# & .NET Runtime',
  topicTitle: 'Memory & GC',
};

export const allScope: ReviewScope = {
  type: 'all',
  domain: null,
  topic: null,
  domainTitle: null,
  topicTitle: null,
};

export function question(n: number, overrides: Partial<SessionQuestion> = {}): SessionQuestion {
  return {
    id: `dotnet.clr-memory.stack-vs-heap.mcq.${n}`,
    level: 'M',
    reviewed: true,
    isNew: true,
    prompt: `Question ${n}: what does \`stackalloc\` do?`,
    options: [`Option A${n}`, `Option B${n}`, `Option C${n}`, `Option D${n}`],
    domain: { slug: 'dotnet', title: 'C# & .NET Runtime' },
    topic: { slug: 'clr-memory', title: 'Memory & GC' },
    concept: { slug: 'stack-vs-heap', title: 'Stack vs heap' },
    ...overrides,
  };
}

export function session(count = 3, overrides: Partial<Session> = {}): Session {
  return {
    id: '019200aa-0000-7000-8000-000000000001',
    scope: topicScope,
    startedAt: '2026-09-06T10:00:00+00:00',
    finishedAt: null,
    totalLimit: 20,
    newLimit: 10,
    questions: Array.from({ length: count }, (_, i) => question(i + 1)),
    answers: [],
    skippedQuestionIds: [],
    ...overrides,
  };
}

export function feedback(
  questionId: string,
  selectedIndex: number,
  isCorrect: boolean,
  overrides: Partial<AnswerFeedback> = {},
): AnswerFeedback {
  return {
    questionId,
    selectedIndex,
    isCorrect,
    correctIndex: 0,
    explanation: 'Because `stackalloc` allocates on the stack.',
    nextDueAt: '2026-09-07T10:00:00+00:00',
    intervalDays: 1,
    ...overrides,
  };
}

export function overview(overrides: Partial<ReviewOverview> = {}): ReviewOverview {
  return {
    scope: allScope,
    dueCount: 0,
    newCount: 28,
    totalEligible: 28,
    nextDueAt: null,
    defaults: { totalLimit: 20, newLimit: 10 },
    limits: { min: 1, max: 100 },
    ...overrides,
  };
}

export function summary(overrides: Partial<SessionSummary> = {}): SessionSummary {
  return {
    sessionId: '019200aa-0000-7000-8000-000000000001',
    scope: topicScope,
    startedAt: '2026-09-06T10:00:00+00:00',
    finishedAt: '2026-09-06T10:12:00+00:00',
    questionCount: 20,
    answered: 20,
    correct: 15,
    accuracy: 0.75,
    weakConcepts: [
      {
        domain: 'dotnet',
        topic: 'clr-memory',
        concept: 'gc-modes',
        title: 'GC modes',
        wrong: 2,
        total: 3,
      },
      {
        domain: 'dotnet',
        topic: 'clr-memory',
        concept: 'span-memory',
        title: 'Span<T>, Memory<T>',
        wrong: 1,
        total: 2,
      },
    ],
    dueTomorrow: 5,
    ...overrides,
  };
}

/**
 * Waits one macrotask (so the store's promise continuations after an HTTP flush have run) and then
 * for the zoneless fixture to render. `whenStable()` alone resolves before those continuations.
 */
/** A one-domain tree; the shell's spine and the question card's domain chip read it. */
export const TREE: KnowledgeTree = {
  language: 'en',
  domains: [
    {
      slug: 'dotnet',
      title: 'C# & .NET Runtime',
      titleFallback: false,
      sortOrder: 1,
      deprecatedAt: null,
      topics: [
        {
          slug: 'clr-memory',
          title: 'Memory & GC',
          titleFallback: false,
          sortOrder: 1,
          deprecatedAt: null,
          concepts: [],
        },
      ],
    },
  ],
};

/** Answers any pending `GET /api/knowledge/tree` (shared root resource, requested lazily). */
export function answerTree(http: HttpTestingController, tree: KnowledgeTree = TREE): void {
  for (const req of http.match((r) => r.url.startsWith('/api/knowledge/tree'))) {
    req.flush(tree);
  }
}

/**
 * Lets pending microtasks/effects run, then waits for the fixture to be stable. With `http`, a
 * knowledge-tree request raised meanwhile is answered first (otherwise `whenStable` never resolves).
 */
export async function settle(
  fixture: { whenStable(): Promise<unknown> },
  http?: HttpTestingController,
): Promise<void> {
  await new Promise((resolve) => setTimeout(resolve));
  if (http) {
    TestBed.tick();
    answerTree(http);
  }
  await fixture.whenStable();
}
