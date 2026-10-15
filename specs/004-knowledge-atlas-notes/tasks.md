# Tasks: Knowledge atlas and concept notes

**Input**: `specs/004-knowledge-atlas-notes/` — spec.md, plan.md, research.md, data-model.md,
contracts/readiness-api.md, contracts/review-scope-concept.md, quickstart.md.

**Tests**: mandatory (Lockflight). Within each story: stub declarations first (bodies
`throw new NotImplementedException()` in .NET, empty/throwing exports in web), then the tests (.NET tests
tagged `[Trait("Requirement", "FR-xxx")]`, integration tests also `[Trait("Category", "Integration")]`),
then the implementation. Web stories are verified with Vitest specs; .NET stories go through
`lockflight redgreen red` / `green`.

**Organization**: one phase per user story in spec priority order; every task cites its requirements.

## Phase 1: Setup

- [X] T001 Add `markdown-it` 14.x, `highlight.js` 11.x and dev `@types/markdown-it` 14.x to `web/package.json` and `web/package-lock.json` (`npx npm@11 install`, Node 22 via the `web-toolchain` skill)
- [X] T002 [P] Write ADR `docs/architecture/decisions/0007-concept-scope-readiness-and-notes.md` (concept scope column, readiness rule interval ≥ 7 over schedulable questions, separate readiness endpoint, markdown-it + highlight.js)

## Phase 2: Foundational

- [X] T003 Add `/atlas` lazy route to `web/src/app/app.routes.ts` loading an empty `KNOWLEDGE_ROUTES` from `web/src/app/knowledge/knowledge.routes.ts`, and enable `withComponentInputBinding()` in `web/src/app/app.config.ts`

---

## Phase 3: User Story 1 - Browse the atlas down to a concept (P1)

**Goal**: rail "Atlas" → domains/topics in manifest order → topic concepts → concept URL; reload-safe
URLs, breadcrumbs, "soon" topics, not-found.

**Independent Test**: open `/atlas`, drill to a concept, reload on each level, use Back; open an unknown
slug.

- [X] T004 [US1] (FR-002) Declare stub components `AtlasLayout`, `AtlasHome`, `AtlasDomain`, `AtlasTopic`, `AtlasNotFound` (inputs `domain`/`topic`/`concept`, empty templates) in `web/src/app/knowledge/{atlas-layout,atlas-home,atlas-domain,atlas-topic,not-found}/` and register the four child routes in `web/src/app/knowledge/knowledge.routes.ts`

### Tests

- [X] T005 [P] [US1] (FR-001) Spec `web/src/app/knowledge/atlas-home/atlas-home.spec.ts`: domains and topics in manifest order with number, colour, title and concept count; deprecated hidden; a topic with no concepts shows "soon" and is not a link
- [X] T006 [P] [US1] (FR-002) Spec `web/src/app/knowledge/atlas-layout/atlas-layout.spec.ts`: breadcrumbs Atlas › domain › topic › concept at each level from the route; tree reloaded on entry
- [X] T007 [P] [US1] (FR-001, FR-002, FR-015) Specs `web/src/app/knowledge/atlas-domain/atlas-domain.spec.ts` and `web/src/app/knowledge/atlas-topic/atlas-topic.spec.ts`: domain lists its topics, topic lists concepts with title, level and question count linking to the concept URL; unknown slug → not-found with a link to `/atlas`
- [X] T008 [P] [US1] (FR-013) Spec in `web/src/app/app.spec.ts`: rail "Atlas" is a link to `/atlas` without "soon" and is active inside the Atlas

### Implementation

- [X] T009 [US1] (FR-002) Implement `AtlasLayout` (breadcrumbs from `KnowledgeTreeSource`, `<router-outlet>`, tree reload on entry) in `web/src/app/knowledge/atlas-layout/`
- [X] T010 [US1] (FR-001, FR-015) Implement `AtlasHome`, `AtlasDomain`, `AtlasTopic`, `AtlasNotFound` with Atlas tokens and `domain-1…5` colours in `web/src/app/knowledge/`
- [X] T011 [US1] (FR-013) Turn the rail "Atlas" item into a `routerLink` with `routerLinkActive` in `web/src/app/app.html`
- [X] T012 [US1] (FR-014) Add `titles.atlas` and `atlas.*` keys for US1 (title, lead, breadcrumb, counts, soon, not-found, back) to `web/src/app/shared/i18n/messages.en.ts` and `messages.uk.ts`

**Checkpoint**: Atlas browsable to the concept URL (concept page placeholder until US2).

---

## Phase 4: User Story 2 - Read a concept note (P1)

**Goal**: concept page with safe, highlighted Markdown, meta and prev/next.

**Independent Test**: open a concept with a heading, a list and a C# block in both themes; follow "next".

- [X] T013 [US2] (FR-003) Declare `renderNote(markdown: string): string` throwing in `web/src/app/knowledge/concept-note/note-markdown.ts`, stub `ConceptNote` and `ConceptPage` components, and the concept child route (lazy `loadComponent`) in `web/src/app/knowledge/knowledge.routes.ts`

### Tests

- [X] T014 [P] [US2] (FR-003) Spec `web/src/app/knowledge/concept-note/note-markdown.spec.ts`: headings, lists, emphasis, table wrapped in a scroll container, links, inline code; `csharp`/`typescript`/`javascript`/`sql`/`json`/`html`/`bash` fences carry `hljs-*` spans; unknown language renders plain and escaped
- [X] T015 [P] [US2] (FR-004) Spec in `web/src/app/knowledge/concept-note/note-markdown.spec.ts`: `<script>`, `<img onerror>`, inline HTML and `[x](javascript:…)` are neutralised
- [X] T016 [P] [US2] (FR-005, FR-015) Spec `web/src/app/knowledge/concept-page/concept-page.spec.ts`: level, question and kata counts; previous/next within the topic; no "next" on the last concept; 404 → not-found; empty note → "no note yet"

### Implementation

- [X] T017 [US2] (FR-003, FR-004) Implement `renderNote` with markdown-it (`html: false`) and `highlight.js/lib/core` + 7 languages and aliases in `web/src/app/knowledge/concept-note/note-markdown.ts`
- [X] T018 [US2] (FR-003, FR-005) Implement `ConceptNote` (`[innerHTML]`, no bypass) and `ConceptPage` (`httpResource` on the concept URL with `withLang`, `linkedSignal`, prev/next from the tree) in `web/src/app/knowledge/`
- [X] T019 [US2] (FR-003, FR-014) Add code highlight tokens for light and dark Atlas (≥ 4.5:1 on `--color-code-bg`) and `.note` prose styles to `web/src/styles.scss`; add `atlas.*` keys for US2 (previous, next, neighbours, noNote, katas, codeLanguage) to both dictionaries

**Checkpoint**: notes readable and navigable.

---

## Phase 5: User Story 3 - Ukrainian, fallback and review status (P1)

**Goal**: EN badge for every fallback title/note under УК, unreviewed badge everywhere, in-place
language switch.

**Independent Test**: switch to УК on a translated and an untranslated concept; check badges; switch back.

- [X] T020 [US3] (FR-006, FR-007) Declare stub `EnBadge` and `UnreviewedBadge` components in `web/src/app/knowledge/badges/`

### Tests

- [X] T021 [P] [US3] (FR-006, FR-007) Spec `web/src/app/knowledge/badges/badges.spec.ts`: `EnBadge` renders only when language is uk and the item is a fallback; `UnreviewedBadge` renders in both languages
- [X] T022 [P] [US3] (FR-006, FR-007) Extend `atlas-home.spec.ts`, `atlas-topic.spec.ts` and `concept-page.spec.ts`: fallback domain/topic/concept titles and notes carry the EN badge and `lang="en"` under uk; unreviewed concepts carry the badge in lists and on the page; switching language keeps the URL and requests `?lang=uk`
- [X] T023 [P] [US3] (FR-006) Extend `web/src/app/review/review-overview/review-overview.spec.ts`: atlas tiles show the EN badge and `lang="en"` for fallback domain/topic titles

### Implementation

- [X] T024 [US3] (FR-006, FR-007) Implement the badges and wire them into `AtlasHome`, `AtlasDomain`, `AtlasTopic`, `ConceptPage`, breadcrumbs
- [X] T025 [US3] (FR-006) Carry `titleFallback` through `AtlasDomain`/`AtlasTopic` view models in `web/src/app/review/review-overview/review-overview.{ts,html}` with the EN badge
- [X] T026 [US3] (FR-014) Add a shared `badges.*` section (unreviewed, unreviewedHint, fallback, fallbackHint) to both dictionaries and reuse it from the session views

**Checkpoint**: P1 stories complete — MVP.

---

## Phase 6: User Story 4 - See readiness per domain, topic and concept (P2)

**Goal**: `GET /api/review/readiness` and readiness bars/chips in the Atlas.

**Independent Test**: seed review states for one topic; check percentages and per-concept states.

- [X] T027 [US4] (FR-008) Declare stubs: `ConceptReadiness` and `ReadinessRule.IsReady(int intervalDays)` in `src/Domain/Progress/Readiness.cs`; readiness DTOs, `ConceptCounts`, `IReadinessQueries` and `ReadinessCalculator.Build` in `src/Application/Review/`; `ReadinessQueries` in `src/Infrastructure/Review/ReadinessQueries.cs` and `GetReadinessEndpoint` in `src/Api/Endpoints/Review/GetReadinessEndpoint.cs` — all bodies `throw new NotImplementedException()`; register in `src/Infrastructure/DependencyInjection.cs`
- [X] T028 [US4] (FR-008, FR-009) Declare stub `ReadinessStore` (component-scoped `httpResource`), `ReadinessBar`, `ReadinessChip` and `web/src/app/knowledge/api/readiness.models.ts`

### Tests

- [X] T029 [P] [US4] (FR-008) Tests `tests/Domain.Tests/Progress/ReadinessRuleTests.cs`: interval 7 ready, 6 not ready
- [X] T030 [P] [US4] (FR-009) Tests `tests/Application.Tests/Review/ReadinessCalculatorTests.cs`: all ready → ready, mixed → in-progress, no states → not-started, zero questions omitted (FR-008); 1 of 4 → 25, 2 of 3 → 66, nothing counted → null, domain aggregates concepts not topic percentages
- [X] T031 [P] [US4] (FR-010) Integration tests `tests/Infrastructure.Tests/Review/ReadinessQueriesTests.cs`: seeded fixture matches hand-calculated percentages (FR-009); deprecated question/concept excluded and open questions ignored (FR-008); review states of another user row are not counted
- [X] T032 [P] [US4] (FR-009, FR-010) API tests `tests/Api.Tests/Review/ReadinessEndpointTests.cs`: 200 shape per contract, `percent` null where nothing counts, same result with `lang=uk`
- [X] T033 [P] [US4] (FR-008, FR-009) Specs `web/src/app/knowledge/readiness/readiness.spec.ts` and extensions of `atlas-home`/`atlas-topic`/`concept-page` specs: 25% bar on a topic, no percentage when `percent` is null, per-concept state chip

### Implementation

- [X] T034 [US4] (FR-008) Extract shared `Eligible(scope)` and `States()` into `src/Infrastructure/Review/ReviewQueryables.cs` and use them from `ReviewSessions` (existing review tests stay green)
- [X] T035 [US4] (FR-008, FR-009, FR-010) Implement `ReadinessRule`, `ReadinessCalculator`, `ReadinessQueries` (two queries, grouped LEFT JOIN) and `GetReadinessEndpoint`
- [X] T036 [US4] (FR-008, FR-009, FR-014) Implement the store (provided in `AtlasLayout`), bar and chip; wire into home, domain, topic and concept views; add `atlas.readiness*` and `atlas.state.*` keys to both dictionaries

**Checkpoint**: readiness visible and fresh on each Atlas visit.

---

## Phase 7: User Story 5 - Start a review from where I am (P2)

**Goal**: review links on domain/topic/concept; new `concept` session scope end to end.

**Independent Test**: start a review from a topic and from a concept; the session contains only that
scope's questions.

- [X] T037 [US5] (FR-012) Declare stubs: `SessionScopeType.Concept`, `Session.ScopeConceptSlug`, `ReviewScope.Concept` + `ForConcept` (throwing), `ScopeDto.Concept`/`ConceptTitle`, `ScopeQuery.Concept` in `src/Domain/Progress/`, `src/Application/Review/`, `src/Api/Endpoints/Review/ScopeQuery.cs`

### Tests

- [X] T038 [P] [US5] (FR-012) Tests `tests/Application.Tests/Review/ReviewScopeTests.cs`: concept valid only with domain, topic and concept; other types reject a concept slug
- [X] T039 [P] [US5] (FR-012) Integration tests `tests/Infrastructure.Tests/Persistence/ProgressSchemaTests.cs`: `ck_sessions_scope` accepts a concept row with three slugs, rejects concept without a concept slug and a domain row with one
- [X] T040 [P] [US5] (FR-011, FR-012) Integration tests `tests/Infrastructure.Tests/Review/ReviewSessionsTests.cs`: concept session contains only that concept's non-deprecated MCQs; unknown/deprecated concept → scope not found; starting it finishes the open session; topic session contains only that topic's questions
- [X] T041 [P] [US5] (FR-012) API tests in `tests/Api.Tests/Review/SessionsEndpointTests.cs` and `OverviewEndpointTests.cs`: `scope=concept` → 201 with `scope.type = "concept"` and `conceptTitle`; missing concept → 400; concept with `scope=topic` → 400; unknown concept → 404; overview counts for a concept
- [X] T042 [US5] (FR-011, FR-012) Specs: `atlas-domain`/`atlas-topic`/`concept-page` review links carry `scope`, `domain`, `topic`, `concept` query params; `review-overview.spec.ts` preselects `?scope=concept&…` and the concept select follows the topic

### Implementation

- [X] T043 [US5] (FR-012) Configure the column and rewritten checks in `src/Infrastructure/Persistence/Configurations/ProgressConfigurations.cs`; add migration `AddConceptSessionScope` (`ef-migrations` skill, review the SQL)
- [X] T044 [US5] (FR-012) Implement the concept case in `ReviewScope`, `ReviewQueryables.Eligible`, `ReviewSessions` (resolve, describe, persist, `ScopeOf`), `SessionSummaryBuilder` and the `ScopeQuery` validator rules
- [X] T045 [US5] (FR-011, FR-012, FR-014) Add "Review this domain/topic/concept" links to the Atlas views; extend `web/src/app/review/api/review.models.ts`, `review-api.ts` and `review-overview` (concept scope, select, URL round trip, `scopeLabel`); add `atlas.review*` and `overview.*Concept` keys to both dictionaries

**Checkpoint**: read → practise loop closed.

---

## Phase 8: Polish & Cross-Cutting

- [X] T046 Update `specs/002-review-session/contracts/review-api.md` scope rules pointer and `memory-bank/systemPatterns.md`
- [X] T047 Rebuild the stack, import content; Playwright MCP walkthrough per quickstart.md: keyboard-only, 375 px, EN↔УК on a concept, light/dark highlight contrast (SC-002, SC-004, SC-006)
- [X] T048 `code-reviewer` and `frontend-reviewer`; fix must-fix findings
- [X] T049 `lockflight trace`, `lockflight gate run --profile pr --base main`; memory-bank `activeContext`/`progress`; commit on `dev`

---

## Dependencies & Execution Order

- Setup (T001–T002) → Foundational (T003) → stories.
- US1 → US2 (concept route lives in the layout) → US3 (badges wire into US1/US2 views).
- US4 backend (T027, T029–T032, T034–T035) is independent of US1–US3 and can run in parallel with them; US4 web (T028, T033, T036) needs the US1/US2 views.
- US5 backend (T037–T041, T043–T044) depends on T034 (`ReviewQueryables`); US5 web (T042, T045) needs the US1/US2 views.
- Polish after all stories.

## Parallel Example

```text
# US4 tests together after the stubs (T027, T028):
T029 ReadinessRuleTests · T030 ReadinessCalculatorTests · T031 ReadinessQueriesTests · T032 ReadinessEndpointTests · T033 readiness specs
# Backend and frontend tracks: US4/US5 backend (src/**, tests/**) alongside US1–US3 (web/**)
```

## Implementation Strategy

1. MVP = Setup + US1 + US2 + US3 (P1): the Atlas is browsable and readable in EN/UK.
2. Add US4 (readiness), then US5 (scoped review); each is independently testable.
3. Red/green per .NET story (US4, US5) via `lockflight redgreen red` / `green`; web stories verified by
   failing-then-passing Vitest specs.
