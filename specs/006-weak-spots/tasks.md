# Tasks: Weak spots

**Input**: `specs/006-weak-spots/` — spec.md, plan.md, research.md, data-model.md,
contracts/review-mode.md, contracts/weak-spots-api.md, quickstart.md.

**Tests (Lockflight)**: mandatory and first. Within each story: stub declarations (bodies
`throw new NotImplementedException()`), then tests (.NET tagged `[Trait("Requirement", "FR-xxx")]`,
integration also `[Trait("Category", "Integration")]`; Vitest specs for web), then implementation.
.NET stories go through `lockflight redgreen red` / `green`.

## Phase 1: Setup

- [X] T001 [P] Write ADR `docs/architecture/decisions/0008-weak-mode-and-weak-spots.md` (session mode column, weakness from attempts, 30-day ranking, separate weak-spots endpoint)

## Phase 2: Foundational (no behaviour change)

- [X] T002 Add `SessionMode` (`src/Domain/Progress/SessionMode.cs`), `Session.Mode`, its mapping and `ck_sessions_mode` in `src/Infrastructure/Persistence/Configurations/ProgressConfigurations.cs`; migration `AddSessionMode` (`ef-migrations` skill, review SQL, strip BOM)
- [X] T003 Append `Mode` to `SessionDto` / `SessionSummaryDto` and `WeakCount` to `ReviewOverviewDto` in `src/Application/Review/ReviewDtos.cs`; fill `Mode` from the session in `ReviewSessions` and `SessionSummaryBuilder`; `WeakCount` stays 0
- [X] T004 Extract the grouped readiness counts into `ReviewQueryables.ConceptReadinessCounts()` and use it from `src/Infrastructure/Review/ReadinessQueries.cs` (readiness tests stay green)

---

## Phase 3: User Story 1 - Drill what I last got wrong (P1)

**Goal**: weak sessions over questions whose last answer was wrong, any scope, existing flow.

**Independent Test**: miss some questions, start a weak session → exactly those; answer one right → gone.

- [X] T005 [US1] (FR-001, FR-002, FR-005) Declare stubs: `IReviewSessions.StartWeakAsync` + `ReviewSessions.StartWeakAsync`, `SessionPlanner.PlanWeak`, `ReviewError.NothingToDrill` (mapped to 409 "Nothing to drill" in `src/Api/Errors/ReviewExceptionHandler.cs`), `StartSessionRequest.Mode`, all bodies throwing

### Tests

- [X] T006 [P] [US1] (FR-002, FR-005) Tests `PlanWeak` keeps order and caps at the total; empty input gives an empty plan in `tests/Application.Tests/Review/SessionPlannerTests.cs`
- [X] T007 [P] [US1] (FR-001, FR-002, FR-003, FR-004, FR-005, FR-006) Integration tests in `tests/Infrastructure.Tests/Review/WeakSessionsTests.cs`: exactly last-answered-wrong questions even when not due; wrong-then-right excluded; deprecated excluded; newest miss first and total limit; topic and concept scopes; answers update review state and attempts; nothing to drill refused without a session and the open one kept; weak start finishes an open regular session; mode `weak` on session and summary; overview `WeakCount`
- [X] T008 [P] [US1] (FR-001, FR-005, FR-006) API tests in `tests/Api.Tests/Review/WeakSessionEndpointTests.cs`: `mode=weak` → 201 with the missed ids and `mode: "weak"`; nothing to drill → 409; unknown mode → 400; overview `weakCount`
- [X] T009 [P] [US1] (FR-001, FR-004, FR-006) Specs: `review-overview.spec.ts` (`?mode=weak` preselects, weak count shown, new limit hidden, start sends `mode: 'weak'`, disabled with a message at 0, toggle writes the URL), `session-store.spec.ts` (`reviewAgain` keeps the mode), review-session chip for weak mode

### Implementation

- [X] T010 [US1] (FR-001, FR-002, FR-003) Implement `ReviewQueryables.LastQuestionAnswers()` / `WeakQuestions(scope)` and `SessionPlanner.PlanWeak`
- [X] T011 [US1] (FR-001, FR-004, FR-005, FR-006) Extract the transactional start core in `src/Infrastructure/Review/ReviewSessions.cs`, implement `StartWeakAsync` and `WeakCount`; endpoint branch and mode validator in `src/Api/Endpoints/Review/StartSessionEndpoint.cs`
- [X] T012 [US1] (FR-001, FR-004, FR-006) Web: `review.models.ts` (`ReviewMode`, `mode`, `weakCount`), `session-store.ts`, review-overview mode toggle and URL, weak chip in `review-session`, i18n keys in `messages.en.ts` / `messages.uk.ts`

**Checkpoint**: weak sessions work end to end.

---

## Phase 4: User Story 2 - See where I am weak (P1)

**Goal**: `/weak` view ranking concepts and topics with note and drill links.

**Independent Test**: seeded history → order and figures match a hand calculation.

- [X] T013 [US2] (FR-007, FR-008, FR-009) Declare stubs: `WeaknessRule`, `ConceptAnswerStats`, `WeaknessRanker.Rank`, weak-spots DTOs, `IWeakSpotsQueries` + `WeakSpotsQueries.GetAsync`, `GetWeakSpotsEndpoint`, DI registration, all bodies throwing

### Tests

- [X] T014 [P] [US2] (FR-007, FR-008, FR-009) Tests in `tests/Application.Tests/Review/WeaknessRankerTests.cs`: accuracy and figures; fewer than 3 answers excluded; order accuracy → wrong → last answered; top 20; topic aggregation, qualification and top 10
- [X] T015 [P] [US2] (FR-007, FR-008, FR-011, FR-012) Integration tests in `tests/Infrastructure.Tests/Review/WeakSpotsQueriesTests.cs`: hand-calculated figures incl. weak questions; answers older than 30 days ignored, boundary included; order; readiness equals the readiness endpoint's; deprecated excluded; another learner's attempts ignored
- [X] T016 [P] [US2] (FR-007, FR-012) API tests in `tests/Api.Tests/Review/WeakSpotsEndpointTests.cs`: 200 shape (window 30, min 3); `lang=en` and `lang=uk` bodies equal; unsupported `lang` → 400
- [X] T017 [P] [US2] (FR-010, FR-011, FR-014, FR-015) Specs `web/src/app/review/weak-spots/weak-spots.spec.ts` (order as served, titles from the tree with EN badge, note and drill hrefs with `mode=weak`, readiness chip, empty state links to Review) and `app.spec.ts` (rail "Weak spots" → `/weak`, active there)

### Implementation

- [X] T018 [US2] (FR-007, FR-008, FR-009) Implement `WeaknessRanker`
- [X] T019 [US2] (FR-007, FR-011, FR-012) Implement `WeakSpotsQueries` (window from `TimeProvider`, constant queries) and the endpoint
- [X] T020 [US2] (FR-010, FR-011, FR-014, FR-015) Web: `weak-spots.models.ts`, `WeakSpots` component at `/weak`, rail item in `app.html`, i18n keys

**Checkpoint**: the learner sees and drills weak spots.

---

## Phase 5: User Story 3 - From the summary to the note (P2)

**Goal**: summary links weak concepts to notes and drills misses.

**Independent Test**: finish a session with misses; follow a link and "Drill these".

### Tests

- [X] T021 [US3] (FR-013) Specs in `web/src/app/review/session-summary/session-summary.spec.ts`: each weak concept links to `/atlas/{d}/{t}/{c}`; "Drill these" only when there were wrong answers and starts a weak session over the summary's scope

### Implementation

- [X] T022 [US3] (FR-013) Implement the links, `SessionStore.drillWeak()` and the summary mode label; i18n keys

---

## Phase 6: Polish & Cross-Cutting

- [X] T023 `code-reviewer` and `frontend-reviewer`; fix must-fix findings
- [X] T024 `lockflight review` (review of record) once; fix confirmed findings
- [X] T025 `lockflight trace`, `lockflight gate run --profile pr --base main`
- [X] T026 Rebuild the stack; Chrome MCP walkthrough per quickstart.md (keyboard, 375 px, EN/УК, light/dark)
- [X] T027 memory-bank (`activeContext`, `progress`, `systemPatterns`), `web/src/app/review/README.md`, `TECH_DEBT.md`; commit on `dev`

---

## Dependencies & Execution Order

- Setup → Foundational (T002–T004) → US1 → US2 → US3 → Polish.
- US2 backend (T013–T016, T018–T019) only needs Foundational; US2 web needs US1's models.
- US3 needs US1 (`mode`, store) and is web only.

## Parallel Example

```text
# US1 tests after the stubs (T005):
T006 SessionPlannerTests · T007 WeakSessionsTests · T008 WeakSessionEndpointTests · T009 web specs
```

## Implementation Strategy

MVP = Foundational + US1 (drill misses). Then US2 (view), then US3 (summary). Red/green per .NET story
(US1, US2); web parts verified by failing-then-passing Vitest specs.
