# Implementation Plan: Weak spots

**Branch**: `dev` (feature id `006-weak-spots`) | **Date**: 2026-10-27 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/006-weak-spots/spec.md`; strategy from the
`solution-architect` agent; data-model decisions confirmed by the user on 2026-10-27.

## Summary

Add a **weak** review mode next to the regular one, a weak-spots view and summary links. A weak
session holds the schedulable questions in scope whose most recent answer was wrong (newest miss first,
total limit applies) and runs through the existing start/resume/answer/finish flow. The mode is stored
on the session (`sessions.mode`, migration `AddSessionMode`). `GET /api/review/weak-spots` ranks
concepts (top 20) and topics (top 10) by 30-day accuracy with at least 3 answers, computed in a constant
number of queries and ranked by a pure `WeaknessRanker`. The web gains `/weak`, a mode toggle in the
review overview (`?mode=weak`), a weak-mode chip on the session page, and note links plus "Drill these"
on the summary.

## Technical Context

**Language/Version**: C# 14 / .NET 10; TypeScript strict / Angular 22 (zoneless, signals)

**Primary Dependencies**: existing stack only (FastEndpoints 8, EF Core 10 + Npgsql). No new packages.

**Storage**: PostgreSQL — additive migration `AddSessionMode` (`sessions.mode text NOT NULL DEFAULT
'regular'`, `ck_sessions_mode`). No new index: the latest-answer lookup uses
`ix_attempts_user_id_item_id_answered_at`.

**Testing**: xUnit 2.9 with `[Trait("Requirement","FR-xxx")]` — Application unit tests for
`SessionPlanner.PlanWeak` and `WeaknessRanker`; Testcontainers tests for weak sessions and weak-spots
statistics with `FakeTimeProvider`; Api tests per endpoint. Vitest for the web. Chrome MCP walkthrough
at the end.

**Target Platform**: local Docker Compose; the migration applies on API start; no new env vars.

**Project Type**: web service + SPA (same repo)

**Performance Goals**: weak-spots view usable < 1 s with a year of daily reviews (SC-004); ~7k attempts.

**Constraints**: statistics are learner-scoped and language-independent; weakness reads `attempts`, not
SM-2 fields (Principle V); Atlas tokens only.

**Scale/Scope**: 1 migration, 1 new endpoint, 1 extended endpoint, 1 new view, 3 changed views.

## Decisions

| # | Decision | Source |
|---|---|---|
| D1 | `sessions.mode` (`regular`/`weak`, default `regular`, check constraint); enum `SessionMode`. | user |
| D2 | No new index; latest answer per question via the existing attempts index. | user |
| D3 | Weak sessions store the default new limit; it is not applied in weak mode. | default |
| D4 | Wrong answer = `attempts.is_correct = false` (null never counts as wrong). | user |
| D5 | `StartWeakAsync` is a separate use case sharing the transactional start core (same locks). | default |
| D6 | `ReviewError.NothingToDrill` → 409 "Nothing to drill". | default |
| D7 | Overview always returns `weakCount`; the mode toggle is client-side. | default |
| D8 | Weak-spots payload has slugs and figures only (no text, `lang` ignored); readiness per ranked concept is computed server-side with the 004 calculator. | default |
| D9 | A topic qualifies with ≥ 3 answers across its concepts in the window. | user |
| D10 | The session page shows a weak-mode chip. | user |

Details: [research.md](research.md).

## Constitution Check

| Principle | Status | Note |
|---|---|---|
| I content over platform | PASS | no content changes |
| II content is data | PASS | none |
| III English first | PASS | titles joined client-side with existing fallback |
| IV progress user-scoped | PASS | mode on `sessions`; every query filters `LocalLearner.Id` |
| V scheduling interface | PASS | weakness from `attempts`; answers still go through `ISchedulingAlgorithm` |
| VI LLM only for judgment | PASS | no LLM |
| VII boring architecture | PASS | one query class, one endpoint, one validator rule |
| VIII modern Angular | PASS | standalone, OnPush, signals, `httpResource`, no state library |
| IX one command | PASS | additive migration on start |
| X tests where they pay | PASS | pure ranker/planner unit tests, one integration class per query surface |

No violations. Re-checked after Phase 1: unchanged.

## Project Structure

### Documentation (this feature)

```text
specs/006-weak-spots/
├── spec.md, plan.md, research.md, data-model.md, quickstart.md
├── contracts/review-mode.md        # mode on start/session/summary, weakCount on overview
├── contracts/weak-spots-api.md     # GET /api/review/weak-spots
├── checklists/requirements.md
└── tasks.md
```

### Source Code (delta)

```text
src/Domain/Progress/            SessionMode (new), Session (+Mode), WeaknessRule (new)
src/Application/Review/         IReviewSessions (+StartWeakAsync), SessionPlanner (+PlanWeak), ReviewException
                                (+NothingToDrill), ReviewDtos (+Mode, +WeakCount, weak-spots DTOs), SessionSummaryBuilder,
                                WeaknessRanker (new), IWeakSpotsQueries (new)
src/Infrastructure/Review/      ReviewQueryables (+LastQuestionAnswers, +WeakQuestions, +ConceptReadinessCounts),
                                ReviewSessions (start core, StartWeakAsync, WeakCount, mode), ReadinessQueries (refactor),
                                WeakSpotsQueries (new)
src/Infrastructure/Persistence/ Configurations/ProgressConfigurations, Migrations/<ts>_AddSessionMode
src/Api/                        Endpoints/Review/StartSessionEndpoint (+mode), GetWeakSpotsEndpoint (new),
                                Errors/ReviewExceptionHandler (+NothingToDrill)
tests/Application.Tests/Review/ SessionPlannerTests (+weak), WeaknessRankerTests (new)
tests/Infrastructure.Tests/     Review/WeakSessionsTests (new), Review/WeakSpotsQueriesTests (new)
tests/Api.Tests/Review/         WeakSessionEndpointTests (new), WeakSpotsEndpointTests (new)
web/src/app/                    app.routes.ts (/weak), app.html (rail item)
web/src/app/review/             weak-spots/ (new), api/review.models + weak-spots.models, session-store,
                                review-overview (mode toggle), review-session (chip), session-summary (links, drill)
web/src/app/shared/i18n/        messages.en.ts, messages.uk.ts
docs/architecture/decisions/0008-weak-mode-and-weak-spots.md
```

**Structure Decision**: same layering as 001–004; weakness lives in the review module (progress data);
the view sits in `web/src/app/review/weak-spots/` behind a top-level `/weak` route.

## Steps (red → green per story; app runnable after each)

1. Setup: ADR 0008, contracts.
2. Foundational (no behaviour change): `SessionMode` + migration, `Mode` on session/summary DTOs,
   `ConceptReadinessCounts` extracted (readiness tests stay green).
3. **US1** weak sessions (FR-001…FR-006): stubs → tests → red → implementation → green; then web
   overview toggle, store, session chip.
4. **US2** weak-spots view (FR-007…FR-012, FR-014, FR-015): stubs → tests → red → implementation → green;
   then `/weak`, rail item.
5. **US3** summary links and drill (FR-013): web.
6. Wrap-up: reviewers, `lockflight review`, trace, `gate run --profile pr`, Chrome MCP walkthrough,
   memory-bank.
