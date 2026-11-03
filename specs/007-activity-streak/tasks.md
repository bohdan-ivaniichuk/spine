# Tasks: Activity and streak

**Input**: `specs/007-activity-streak/` — spec.md, plan.md, research.md, data-model.md,
contracts/activity-api.md, quickstart.md.

**Tests (Lockflight)**: mandatory and first. Within each story: stub declarations (bodies
`throw new NotImplementedException()`), then tests (.NET tagged `[Trait("Requirement", "FR-xxx")]`,
integration also `[Trait("Category", "Integration")]`; Vitest specs for web), then implementation.
.NET stories go through `lockflight redgreen red` / `green`. API tests build the query from the real
`GetActivityRequest` type so they reach production code.

## Phase 1: Setup

- [ ] T001 [P] Write the contract `specs/007-activity-streak/contracts/activity-api.md` (done with the plan)
- [ ] T002 [P] Install `tzdata` in the runtime stage of `src/Api/Dockerfile` (research R2)

---

## Phase 2: User Story 1 - Keep my streak going (P1)

**Goal**: current and longest streak in the learner's time zone on the overview.

**Independent Test**: seeded answers on known local dates → streaks match a hand count.

- [ ] T003 [US1] (FR-001, FR-002, FR-003, FR-005) Declare stubs: `ActivityDayDto` / `ActivityDto` in `src/Application/Review/ActivityDtos.cs`, `ActivityCalculator.Calculate` in `src/Application/Review/ActivityCalculator.cs`, `IActivityQueries` + `ActivityQueries.GetAsync` in `src/Infrastructure/Review/ActivityQueries.cs` (DI registration), `GetActivityRequest` / validator / `GetActivityEndpoint` in `src/Api/Endpoints/Review/GetActivityEndpoint.cs`, all bodies throwing

### Tests for User Story 1

- [ ] T004 [P] [US1] (FR-002, FR-003) Tests in `tests/Application.Tests/Review/ActivityCalculatorTests.cs`: run ending today; today inactive keeps yesterday's run and `ActiveToday` false; last answer 2 days ago gives 0; longest over an older run; several answers on a day count once; days after today ignored; no answers gives 0/0
- [ ] T005 [P] [US1] (FR-001, FR-002, FR-003, FR-005) Integration tests in `tests/Infrastructure.Tests/Review/ActivityQueriesTests.cs`: 23:30 and 00:30 Europe/Kyiv fall on consecutive local days within one UTC day; UTC zone puts them on one day; wrong answers, weak-session answers and answers to deprecated questions count; kata attempts do not; `Today` and `TimeZone` follow the clock and the zone
- [ ] T006 [P] [US1] (FR-001) API tests in `tests/Api.Tests/Review/ActivityEndpointTests.cs`: no `tz` → 200 with `timeZone` `UTC`; `tz=Europe/Kyiv` → 200 with that zone; unknown zone → 400 problem with an error on `tz`; camelCase shape per the contract
- [ ] T007 [P] [US1] (FR-006, FR-007, FR-008, FR-009) Specs in `web/src/app/review/activity-card/activity-card.spec.ts`: requests the browser zone; shows current and longest streak with EN/UK plurals; hint only when the streak is alive and today is inactive; empty state with a link to start a review; error state; the overview hosts the card

### Implementation for User Story 1

- [ ] T008 [US1] (FR-002, FR-003) Implement the streak part of `ActivityCalculator.Calculate` (`Days` stays empty until US2)
- [ ] T009 [US1] (FR-001, FR-005) Implement `ActivityQueries.GetAsync` (question attempts of the local learner, converted to local dates, today from the injected clock) and the endpoint with the `tz` validator
- [ ] T010 [US1] (FR-006, FR-007, FR-008, FR-009) Web: `web/src/app/review/api/activity.models.ts`, `activity-card` component (streak, hint, empty and error states), hosted in `review-overview`; i18n keys in `messages.en.ts` / `messages.uk.ts`

**Checkpoint**: streaks work end to end.

---

## Phase 3: User Story 2 - See my last two weeks (P2)

**Goal**: 14 daily counts with an accessible chart.

**Independent Test**: seeded answers inside and outside the window → 14 counts match a hand count.

- [ ] T011 [US2] (FR-004) Declare the stub `ActivityCalculator.DailyCounts` (body throwing; not yet called by `Calculate`)

### Tests for User Story 2

- [ ] T012 [P] [US2] (FR-004) Tests in `tests/Application.Tests/Review/ActivityCalculatorTests.cs`: exactly 14 days ending today, oldest first; zeros for empty days; older answers excluded from the days but still in the longest streak; `Calculate` returns the same days
- [ ] T013 [P] [US2] (FR-004) Integration test in `tests/Infrastructure.Tests/Review/ActivityQueriesTests.cs` and API test in `tests/Api.Tests/Review/ActivityEndpointTests.cs`: `days` has 14 entries ending at `today` with the seeded counts
- [ ] T014 [P] [US2] (FR-004, FR-008) Specs in `web/src/app/review/activity-card/activity-card.spec.ts`: 14 bars scaled to the busiest day; the text alternative lists the period total and each day's count; weekday labels follow the language

### Implementation for User Story 2

- [ ] T015 [US2] (FR-004) Implement `ActivityCalculator.DailyCounts` and use it in `Calculate`
- [ ] T016 [US2] (FR-004, FR-008) Web: bars with Atlas tokens and the visually hidden text alternative in `activity-card`; i18n keys

**Checkpoint**: card complete.

---

## Phase 4: Polish

- [ ] T017 `code-reviewer` and `frontend-reviewer`; fix findings
- [ ] T018 `lockflight trace`, `lockflight gate run --profile pr --base main`, `lockflight ci --no-review-expected`
- [ ] T019 Quickstart walkthrough with Playwright (both zones, container `tzdata`, EN/УК, light/dark, 375 px); README screenshot of the overview
- [ ] T020 Update `memory-bank/activeContext.md` and `memory-bank/progress.md`
