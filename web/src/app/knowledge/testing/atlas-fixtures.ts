import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { KnowledgeConcept } from '../api/knowledge-concept.models';
import { KnowledgeTree, TreeConcept } from '../api/knowledge-tree.models';
import { READINESS_URL, Readiness } from '../api/readiness.models';

/** Shared fixtures for the Atlas specs (feature 004), shaped like `GET /api/knowledge/tree`. */

export function treeConcept(slug: string, overrides: Partial<TreeConcept> = {}): TreeConcept {
  return {
    slug,
    title: slug,
    titleFallback: false,
    level: 'M',
    reviewed: true,
    sortOrder: 1,
    questionCount: 2,
    kataCount: 0,
    deprecatedAt: null,
    ...overrides,
  };
}

/**
 * Two live domains (dotnet: clr-memory with three live concepts + one deprecated, async without
 * concepts, a deprecated topic; angular: signals with one concept) and a deprecated domain.
 */
export const ATLAS_TREE: KnowledgeTree = {
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
          concepts: [
            treeConcept('stack-vs-heap', {
              title: 'Stack vs heap',
              sortOrder: 1,
              questionCount: 3,
              kataCount: 1,
            }),
            treeConcept('retired', {
              title: 'Retired concept',
              sortOrder: 2,
              deprecatedAt: '2026-01-01T00:00:00+00:00',
            }),
            treeConcept('gc-modes', {
              title: 'GC modes',
              level: 'S',
              reviewed: false,
              sortOrder: 3,
              questionCount: 2,
            }),
            treeConcept('span-memory', {
              title: 'Span<T>, Memory<T>',
              sortOrder: 4,
              questionCount: 4,
            }),
          ],
        },
        {
          slug: 'async',
          title: 'Async & threading',
          titleFallback: false,
          sortOrder: 2,
          deprecatedAt: null,
          concepts: [],
        },
        {
          slug: 'old-topic',
          title: 'Old topic',
          titleFallback: false,
          sortOrder: 3,
          deprecatedAt: '2026-01-01T00:00:00+00:00',
          concepts: [treeConcept('old', { title: 'Old concept' })],
        },
      ],
    },
    {
      slug: 'old-domain',
      title: 'Old domain',
      titleFallback: false,
      sortOrder: 2,
      deprecatedAt: '2026-01-01T00:00:00+00:00',
      topics: [],
    },
    {
      slug: 'angular',
      title: 'Angular',
      titleFallback: false,
      sortOrder: 3,
      deprecatedAt: null,
      topics: [
        {
          slug: 'signals',
          title: 'Signals',
          titleFallback: false,
          sortOrder: 1,
          deprecatedAt: null,
          concepts: [treeConcept('signals-basics', { title: 'Signal basics', level: 'S' })],
        },
      ],
    },
  ],
};

/**
 * The same tree under `?lang=uk`: dotnet and clr-memory are translated, `gc-modes` is not (EN
 * fallback), angular/signals fall back to English.
 */
export const ATLAS_TREE_UK: KnowledgeTree = {
  ...ATLAS_TREE,
  language: 'uk',
  domains: ATLAS_TREE.domains.map((d) =>
    d.slug === 'dotnet'
      ? {
          ...d,
          title: 'C# та середовище .NET',
          topics: d.topics.map((t) =>
            t.slug === 'clr-memory'
              ? {
                  ...t,
                  title: "Пам'ять і GC",
                  concepts: t.concepts.map((c) =>
                    c.slug === 'gc-modes'
                      ? { ...c, titleFallback: true }
                      : { ...c, title: `${c.title} (uk)` },
                  ),
                }
              : t,
          ),
        }
      : {
          ...d,
          titleFallback: true,
          topics: d.topics.map((t) => ({
            ...t,
            titleFallback: true,
            concepts: t.concepts.map((c) => ({ ...c, titleFallback: true })),
          })),
        },
  ),
};

export function conceptPage(overrides: Partial<KnowledgeConcept> = {}): KnowledgeConcept {
  return {
    domain: { slug: 'dotnet', title: 'C# & .NET Runtime' },
    topic: { slug: 'clr-memory', title: 'Memory & GC' },
    slug: 'gc-modes',
    level: 'S',
    language: 'en',
    fallback: false,
    reviewed: true,
    title: 'GC modes',
    note: '## Workstation vs server\n\nServer GC uses **one heap per core**.\n\n```csharp\nvar x = new List<int>();\n```\n',
    questionCount: 2,
    kataCount: 1,
    deprecatedAt: null,
    ...overrides,
  };
}

/** Answers every pending `GET /api/knowledge/tree` (root resource shared with the rail). */
export function flushTree(http: HttpTestingController, tree: KnowledgeTree = ATLAS_TREE): void {
  TestBed.tick();
  for (const req of http.match((r) => r.url.startsWith('/api/knowledge/tree'))) {
    req.flush(tree);
  }
}

/** Waits a macrotask, then for the zoneless fixture to render. */
export async function settle(fixture: { whenStable(): Promise<unknown> }): Promise<void> {
  await new Promise((resolve) => setTimeout(resolve));
  TestBed.tick();
  await fixture.whenStable();
}

/**
 * Readiness for ATLAS_TREE (hand-made, numbers chosen to read well): clr-memory 1 of 4 ready (25%),
 * async and the angular domain without counted concepts (`percent: null`).
 */
export const READINESS: Readiness = {
  domains: [
    {
      domain: 'dotnet',
      ready: 1,
      counted: 4,
      percent: 25,
      topics: [
        {
          topic: 'clr-memory',
          ready: 1,
          counted: 4,
          percent: 25,
          concepts: [
            { concept: 'stack-vs-heap', state: 'ready', questions: 3, readyQuestions: 3 },
            { concept: 'gc-modes', state: 'in-progress', questions: 2, readyQuestions: 1 },
            { concept: 'span-memory', state: 'not-started', questions: 4, readyQuestions: 0 },
          ],
        },
        { topic: 'async', ready: 0, counted: 0, percent: null, concepts: [] },
      ],
    },
    {
      domain: 'angular',
      ready: 0,
      counted: 0,
      percent: null,
      topics: [{ topic: 'signals', ready: 0, counted: 0, percent: null, concepts: [] }],
    },
  ],
};

/** Answers every pending `GET /api/review/readiness`. */
export function flushReadiness(http: HttpTestingController, body: Readiness = READINESS): void {
  TestBed.tick();
  for (const req of http.match((r) => r.url.startsWith(READINESS_URL))) {
    req.flush(body);
  }
}

/** Tree + readiness: what every Atlas page asks for on entry. */
export function flushAtlas(
  http: HttpTestingController,
  tree: KnowledgeTree = ATLAS_TREE,
  readiness: Readiness = READINESS,
): void {
  flushTree(http, tree);
  flushReadiness(http, readiness);
}
