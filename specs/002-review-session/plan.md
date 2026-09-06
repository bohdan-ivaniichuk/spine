# Implementation Plan: Daily review session (MCQ + SM-2)

**Branch**: `002-review-session` | **Date**: 2026-09-06 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/002-review-session/spec.md`; architect brief in
[architect-plan.md](architect-plan.md) (full detail — this file is the Spec Kit summary).

## Summary

Ship the daily review loop for multiple-choice questions: an overview with due/new counts per scope,
a server-built session whose ordered membership is frozen at start and stored on the session row, one
question at a time with deterministic instant feedback, SM-2 behind a pure `ISchedulingAlgorithm`, and
a summary with ranked weak concepts and "due tomorrow". Backend contracts land first
([contracts/review-api.md](contracts/review-api.md)); the Angular feature `web/src/app/review/` is built
against them in parallel; both meet at integration.

## Technical Context

**Language/Version**: C# 14 / .NET 10; TypeScript strict / Angular 22 (zoneless, signals)

**Primary Dependencies**: existing stack (FastEndpoints 8.3, EF Core 10 + Npgsql, Serilog); new: `Microsoft.Extensions.TimeProvider.Testing` (tests only). No new Angular dependencies (prompt markdown is a 60-line parser, no `marked`).

**Storage**: PostgreSQL — additive migration `AddSessionMembership` (`sessions.question_ids text[]`, `total_limit`, `new_limit`; partial unique index on `attempts (session_id, item_id)`)

**Testing**: xUnit table tests for SM-2; Application unit tests for planner/summary; Testcontainers tests for session building, one-answer-per-question, SC-004 with `FakeTimeProvider`; API tests per endpoint; Vitest specs for store, parser, option list, session page, overview, summary

**Target Platform**: local Docker Compose; `docker compose up` unchanged except two env passthroughs

**Project Type**: web service + SPA (same repo)

**Performance Goals**: first question < 1 s after Start; feedback < 300 ms (SC-001, SC-002)

**Constraints**: `correct` never leaves the server; grading deterministic; no raw HTML rendering of content; tokens only (ADR 0004)

**Scale/Scope**: 6 endpoints, 5 use cases, 1 migration, 8 Angular components + store + parser

## Constitution Check

| Principle | Status | Note |
|---|---|---|
| I / II content | PASS | no content in code; `options` schema unchanged; explanations rendered from seed |
| IV progress user-scoped | PASS | new columns on `sessions`, index on `attempts`; content tables untouched |
| V scheduling interface | PASS | `ISchedulingAlgorithm` pure in Domain; `algorithm_state` `{lastQuality}`; `review_states.algorithm` from `Name` |
| VI LLM only for judgment | PASS | MCQ grading = index comparison |
| VII boring architecture | PASS | one interface + one EF class, FastEndpoints validators, one `IExceptionHandler` |
| VIII modern Angular | PASS | standalone, signals, `@if/@for`, OnPush, typed form, `httpResource` for overview, no state library |
| IX one command | PASS | `.env.example` + compose passthrough of the two review defaults |
| X tests where they pay | PASS | SM-2 table, planner/summary, integration incl. SC-004, API per endpoint, component specs |

No violations; no Complexity Tracking entries.

## Project Structure

### Documentation (this feature)

```text
specs/002-review-session/
├── spec.md, plan.md, architect-plan.md
├── data-model.md              # schema delta + scheduling types + use cases
├── contracts/review-api.md    # six endpoints + DTOs (the Angular contract)
├── quickstart.md
├── checklists/requirements.md
└── tasks.md
```

### Source Code (delta)

```text
src/Domain/Scheduling/          ISchedulingAlgorithm, ReviewGrade, ReviewSnapshot, SchedulingResult, Sm2Algorithm
src/Domain/Progress/Session.cs  + QuestionIds, TotalLimit, NewLimit; ReviewState.ToSnapshot()/Apply()
src/Application/Review/         IReviewSessions, ReviewDtos, ReviewScope, ReviewException, ReviewOptions, SessionPlanner, SessionSummaryBuilder
src/Infrastructure/Review/      ReviewSessions (EF); Persistence/Configurations (+columns, +index); Migrations/<ts>_AddSessionMembership
src/Api/Endpoints/Review/       GetReviewOverview, StartSession, GetCurrentSession, GetSession, AnswerQuestion, FinishSession, ScopeQuery
src/Api/Errors/                 ReviewExceptionHandler
tests/Domain.Tests/Scheduling/  Sm2AlgorithmTests
tests/Application.Tests/Review/ SessionPlannerTests, SessionSummaryBuilderTests
tests/Infrastructure.Tests/Review/ ReviewSessionsTests (+ MigrationsTests count → 2)
tests/Api.Tests/Review/         Overview/Sessions/Answers/Finish endpoint tests (+ ApiFixture.ResetProgressAsync)
web/src/app/review/             review.routes, api/{review.models,review-api}, session-store, review-overview/, review-session/,
                                question-card/, option-list/, feedback-panel/, progress-bar/, session-summary/, prompt-markdown/
web/src/app/knowledge/api/      knowledge-tree.models.ts (minimal, 003 extends)
web/src/app/app.routes.ts, app.html (routerLink)
```

**Structure Decision**: same layering as 001; Application owns the interface + pure logic, Infrastructure the EF implementation, Api the thin endpoints and the exception→ProblemDetails mapping. Frontend track is disjoint (`web/**`) so backend and frontend agents run in parallel.

## Decisions taken during planning

1. Session membership as `text[]` on `sessions` (not a join table); limits stored on the session.
2. Partial unique index on `attempts (session_id, item_id)` enforces one answer per question per session; 004 may narrow it.
3. "Nothing to review" → 409; `GET /sessions/current` → 204 when none; `finish` idempotent.
4. Weak-concept links use the future 003 route `/knowledge/{domain}/{topic}/{concept}`.
5. Playwright smoke deferred to 003 (two screens to traverse then).
6. English-only payloads in 002.

## Steps (see tasks.md)

B1 Domain scheduling → B2 schema/migration → B3 Application contracts + pure logic → B4 Infrastructure `ReviewSessions` + DI + integration tests → B5 endpoints + exception handler + API tests → B6 runtime config; in parallel F1 models/API service/parser/store → F2 components/routes; then integration, reviewers, docs.
