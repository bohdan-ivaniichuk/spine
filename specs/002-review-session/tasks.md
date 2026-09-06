# Tasks: Daily review session (MCQ + SM-2)

**Input**: `specs/002-review-session/` — spec.md, plan.md, architect-plan.md (authoritative detail), data-model.md, contracts/review-api.md, quickstart.md

**Tests**: Included (constitution Principle X; SC-003/SC-004 demand them).

**Organization**: two disjoint tracks that run in parallel — Backend (`src/**`, `tests/**`, `Directory.Packages.props`, `.env.example`, `docker-compose.yml`) and Frontend (`web/**`) — then Integration.

## Format: `[ID] [P?] [Story] Description`

Stories: US1 answer with feedback, US2 spaced schedule, US3 scope & size, US4 summary.

---

## Phase 1: Backend track

### B1 Domain scheduling (US2)

- [ ] T001 [US2] Create `src/Domain/Scheduling/ReviewGrade.cs` (Again = 1, Good = 4), `ReviewSnapshot.cs` (record + `New(now)`), `SchedulingResult.cs`, `ISchedulingAlgorithm.cs` (`Name`, `Next(current, grade, now)`) per data-model.md
- [ ] T002 [US2] Create `src/Domain/Scheduling/Sm2Algorithm.cs` (`Name = "sm2"`, floor 1.3, penalty 0.2, `MidpointRounding.AwayFromZero`, `AlgorithmState = {"lastQuality":n}`) and add `ReviewState.ToSnapshot()` / `Apply(SchedulingResult, string algorithm)` in `src/Domain/Progress/ReviewState.cs`
- [ ] T003 [US2] Create `tests/Domain.Tests/Scheduling/Sm2AlgorithmTests.cs` — table per architect-plan §6 (1 → 6 → 15 → 38; Again resets; floor holds over 10 loops; min interval 1; DueAt/LastReviewedAt/AlgorithmState/Name) — green with `dotnet test tests/Domain.Tests`

### B2 Schema (US1, US4)

- [ ] T004 [US1] Extend `src/Domain/Progress/Session.cs` (`QuestionIds string[]`, `TotalLimit`, `NewLimit`) and `src/Infrastructure/Persistence/Configurations/ProgressConfigurations.cs` (`TextArray().HasDefaultValueSql("'{}'")`, defaults 20/10; partial unique index `ix_attempts_session_id_item_id` on `(SessionId, ItemId)` filter `session_id IS NOT NULL`)
- [ ] T005 [US1] Add migration `AddSessionMembership` following `.claude/skills/ef-migrations/SKILL.md` (compose postgres on 5432, password from `.env`); review the script (three `ADD COLUMN` + one partial unique index, nothing else); track `.cs`, `.Designer.cs`, snapshot; update `tests/Infrastructure.Tests/Persistence/MigrationsTests.cs` (applied count 2, index name present)

### B3 Application contracts + pure logic (US1–US4)

- [ ] T006 [P] [US3] Create `src/Application/Review/ReviewScope.cs` (record + `All`/`ForDomain`/`ForTopic` + `Validate()`), `ReviewException.cs` + `ReviewError` enum, `ReviewOptions.cs` (defaults 20/10, min 1, max 100; config keys `REVIEW_DEFAULT_TOTAL_LIMIT`, `REVIEW_DEFAULT_NEW_LIMIT`)
- [ ] T007 [P] [US1] Create `src/Application/Review/ReviewDtos.cs` — positional records matching contracts/review-api.md exactly (ScopeDto, RefDto, SessionQuestionDto, SessionAnswerDto, SessionDto, AnswerFeedbackDto, ReviewOverviewDto, WeakConceptDto, SessionSummaryDto) and `IReviewSessions.cs` (five methods)
- [ ] T008 [P] [US3] Create `src/Application/Review/SessionPlanner.cs` (`Plan(dueIds, newIds, totalLimit, newLimit)`) and `tests/Application.Tests/Review/SessionPlannerTests.cs` (due before new; 4 due + 20 new with 10/3 → 7; 25 due with 20/10 → 20 due 0 new; newLimit > totalLimit clamps; empty → empty)
- [ ] T009 [P] [US4] Create `src/Application/Review/SessionSummaryBuilder.cs` (`AnsweredItem`, `ConceptRef`, `Build(session, attempts, dueTomorrow)`) and `tests/Application.Tests/Review/SessionSummaryBuilderTests.cs` (15/20 → 0.75; ranking wrong desc then domain/topic/concept; all-correct concept excluded; zero answers → null accuracy)

### B4 Infrastructure `ReviewSessions` + DI (US1–US4)

- [ ] T010 [US1] Create `src/Infrastructure/Review/ReviewSessions.cs` implementing `IReviewSessions` per architect-plan §3 (eligibility query, due/new sets, `StartAsync` with execution strategy + transaction and auto-finish, `GetAsync`/`GetCurrentAsync` with membership order + `skippedQuestionIds` + `isNew`, `AnswerAsync` with duplicate pre-check + `23505` catch, `FinishAsync` idempotent with `dueTomorrow`); `now` from `TimeProvider`
- [ ] T011 [US1] Register in `src/Infrastructure/DependencyInjection.cs`: `TryAddSingleton(TimeProvider.System)`, `ISchedulingAlgorithm → Sm2Algorithm`, `IReviewSessions → ReviewSessions`, `Configure<ReviewOptions>`; add `Microsoft.Extensions.TimeProvider.Testing` (10.x line) to `Directory.Packages.props` and reference it from `tests/Infrastructure.Tests` and `tests/Api.Tests`
- [ ] T012 [US2] Create `tests/Infrastructure.Tests/Review/ReviewSessionsTests.cs` (`[Collection("postgres")]`, `Category=Integration`, `FakeTimeProvider`): membership order + `isNew` + limits; topic scope; start auto-finishes open session; answer → one attempt + SM-2 review state; duplicate → `DuplicateAnswer`; raw-SQL second insert hits `23505`; **SC-004** (A right, B wrong, advance 1 day, restart → B in, A out); deprecated-after-start skipped but answer kept; overview counts + `nextDueAt`; finish summary weak concepts + `dueTomorrow`; finish idempotent — green with `dotnet test tests/Infrastructure.Tests`

### B5 API endpoints (US1–US4)

- [ ] T013 [US1] Create `src/Api/Endpoints/Review/ScopeQuery.cs` (base request + FluentValidation: scope set, domain/topic presence rules, slug pattern), `GetReviewOverviewEndpoint.cs`, `StartSessionEndpoint.cs` (201 + `Location`), `GetCurrentSessionEndpoint.cs` (204 when none), `GetSessionEndpoint.cs`, `AnswerQuestionEndpoint.cs` (`selectedIndex` 0..3), `FinishSessionEndpoint.cs` — thin: validate → `IReviewSessions` → send
- [ ] T014 [US1] Create `src/Api/Errors/ReviewExceptionHandler.cs` (`IExceptionHandler`: ScopeNotFound 404, NothingToReview 409, SessionNotFound 404, SessionFinished 409, DuplicateAnswer 409, QuestionNotInSession 422; `application/problem+json`, `instance` = path) and wire `AddExceptionHandler` + `UseExceptionHandler()` before `UseFastEndpoints` in `src/Api/Program.cs`
- [ ] T015 [US1] Create `tests/Api.Tests/Review/OverviewEndpointTests.cs`, `SessionsEndpointTests.cs`, `AnswersEndpointTests.cs`, `FinishEndpointTests.cs` and add `ResetProgressAsync()` (`TRUNCATE attempts, review_states, sessions`) to `tests/Api.Tests/ApiFixture.cs`: camelCase shapes, `options` are strings and `correct` never leaks, 400/404/201+Location/204→200/409 duplicate/422 foreign/409 after finish/idempotent finish — green with `dotnet test tests/Api.Tests`

### B6 Runtime config (US3)

- [ ] T016 [P] [US3] Add `REVIEW_DEFAULT_TOTAL_LIMIT=20` and `REVIEW_DEFAULT_NEW_LIMIT=10` to `.env.example` (commented) and pass them through in `docker-compose.yml` `api.environment`
- [ ] T017 [US1] Verify: `dotnet build -c Release` warning-free, `dotnet test` all green, `dotnet run --project src/Api` logs "Applying 1 migration(s)", Swagger lists the six routes, quickstart §1 curl sequence works

---

## Phase 2: Frontend track (parallel with Phase 1; codes against contracts/review-api.md)

### F1 Models, API service, parser, store

- [ ] T018 [P] [US1] Create `web/src/app/review/api/review.models.ts` (interfaces 1:1 with the contract) and `web/src/app/knowledge/api/knowledge-tree.models.ts` (minimal tree types)
- [ ] T019 [P] [US1] Create `web/src/app/review/api/review-api.ts` (`ReviewApi`, `providedIn: 'root'`, `HttpClient` + `firstValueFrom`; `current()` maps 204 → null via `observe: 'response'`)
- [ ] T020 [P] [US1] Create `web/src/app/review/prompt-markdown/prompt-markdown.parser.ts` (`parsePromptMarkdown(source): Block[]` — fenced code with language + inline code only; no HTML) and `prompt-markdown.parser.spec.ts`
- [ ] T021 [US1] Create `web/src/app/review/session-store.ts` (`SessionStore`, root; signals session/status/selectedIndex/feedback/answered/summary/error; computed questions/position/current/total/isLast/scopeLabel; start/resume/select/check/next/finish/endEarly per architect-plan §5) and `session-store.spec.ts` (start+navigate; select/check posts and locks; failed check keeps selection; next advances; last next finishes; resume 204 → null; resume with answers + skipped ids)

### F2 Components + routes (US1, US3, US4)

- [ ] T022 [P] [US1] Create `web/src/app/review/prompt-markdown/prompt-markdown.ts` (blocks → `<pre class="bg-code-bg text-code-text rounded-card p-4 overflow-x-auto font-mono text-sm">` / `<code class="font-mono bg-surface-2 rounded px-1">`; `inline` input)
- [ ] T023 [P] [US1] Create `web/src/app/review/option-list/option-list.ts|html|spec.ts` (`role="radiogroup"`, buttons `role="radio"`, key hints, token state classes neutral/selected/correct/wrong, locked) and `progress-bar/progress-bar.ts|html`
- [ ] T024 [P] [US1] Create `web/src/app/review/feedback-panel/feedback-panel.ts|html` (`aria-live="polite"`, Correct/Incorrect in `text-ok`/`text-bad` semibold, explanation via prompt-markdown, "next due in N day(s)") and `question-card/question-card.ts|html` (level badge `font-mono`, unreviewed/new badges, breadcrumb, prompt)
- [ ] T025 [US1] Create `web/src/app/review/review-session/review-session.ts|html|spec.ts` (page: progress, card, options, feedback, Check/Next/End; `document:keydown` host listener: 1–4 select, Enter check/next, ignore inputs and modifiers, `preventDefault`; resume on init → redirect to `/review` on 204)
- [ ] T026 [US3] Create `web/src/app/review/review-overview/review-overview.ts|html|spec.ts` (typed `NonNullableFormBuilder` form scopeType/domain/topic/totalLimit/newLimit; tree via `httpResource<KnowledgeTree>`; overview via `httpResource<ReviewOverview>` keyed on the form signal; states loading / nothing-to-review with `nextDueAt` / no content / API error + retry / resume open session; Start → `store.start`)
- [ ] T027 [US4] Create `web/src/app/review/session-summary/session-summary.ts|html|spec.ts` (counts, accuracy, `@for` weak concepts with `routerLink` to `/knowledge/{domain}/{topic}/{concept}`, due tomorrow, Review again, Back)
- [ ] T028 [US1] Create `web/src/app/review/review.routes.ts` (`''` overview, `session`, `summary`), lazy-load from `web/src/app/app.routes.ts` with default redirect to `review`, switch header nav in `web/src/app/app.html` to `routerLink` (+ `RouterLink` import in `app.ts`), replace `web/src/app/review/README.md`
- [ ] T029 [US1] Verify: `nvm use 22`; `npm test -- --watch=false` green; `npm run build` under budgets (styles in Tailwind classes, not `.scss`); tokens only (no `slate-*`/`sky-*`)

---

## Phase 3: Integration, review, docs

- [ ] T030 Run the stack (`docker compose up --build`), import content, walk quickstart §1 and §2 in both themes; fix integration issues on either track
- [ ] T031 [P] Run `code-reviewer` on the backend diff and `frontend-reviewer` on `web/`; fix must-fix findings; re-run all suites
- [ ] T032 [P] Write `docs/architecture/decisions/0005-session-membership.md`; update `memory-bank/systemPatterns.md`, `activeContext.md`, `progress.md`; update `specs/002-review-session/quickstart.md` with measured SC-001/SC-002
- [ ] T033 Final commit on `dev`; merge to `main`

## Dependencies

- B1 → B2 → B3 → B4 → B5 → B6 (B3 tasks T006–T009 parallel among themselves; T016 anytime).
- F1 (T018–T020 parallel; T021 after T019) → F2 (T022–T024 parallel; T025–T027 after; T028 last).
- Phase 3 after both tracks. Never let two agents edit `DependencyInjection.cs`, `Program.cs` or `app.routes.ts` concurrently.
