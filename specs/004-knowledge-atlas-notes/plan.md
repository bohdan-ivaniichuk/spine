# Implementation Plan: Knowledge atlas and concept notes

**Branch**: `dev` (feature id `004-knowledge-atlas-notes`) | **Date**: 2026-10-15 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/004-knowledge-atlas-notes/spec.md`; strategy from the
`solution-architect` agent; decisions D1–D7 below (D1–D3, D6 by the user, D4/D5/D7 defaults).

## Summary

Turn the rail's "Atlas" item into a working section: `/atlas` → domain → topic → concept, each with its
own URL. The concept page renders the note as sanitised Markdown with highlight.js syntax colouring.
Titles and notes follow the EN/UK switch with per-item fallback and an "EN" badge; unreviewed concepts
carry the "unreviewed" badge. A new `GET /api/review/readiness` serves per-concept readiness states and
per-topic/domain percentages, computed in two constant queries from the learner's review states.
Review sessions gain a `concept` scope (new `sessions.scope_concept_slug` column, migration
`AddConceptSessionScope`), and every Atlas level links into the existing review flow with its scope
preselected.

## Technical Context

**Language/Version**: C# 14 / .NET 10; TypeScript strict / Angular 22 (zoneless, signals)

**Primary Dependencies**: existing stack (FastEndpoints 8, EF Core 10 + Npgsql). New web dependencies:
`markdown-it` 14.x, `highlight.js` 11.x (core + 7 languages), dev `@types/markdown-it` 14.x — loaded only
in the lazy concept-page chunk.

**Storage**: PostgreSQL — additive migration `AddConceptSessionScope` (`sessions.scope_concept_slug text
NULL`; `ck_sessions_scope_type` and `ck_sessions_scope` rewritten). Readiness is derived, never stored.

**Testing**: xUnit (2.9.x, `[Trait("Requirement","FR-xxx")]`) — Domain unit tests for the readiness rule,
Application unit tests for the calculator and scope, Testcontainers tests for readiness and concept-scope
sessions, Api tests per endpoint; Vitest specs for the renderer and every Atlas page; Playwright MCP
walkthrough for SC-002/SC-006.

**Target Platform**: local Docker Compose; migration applies on API start; no new env vars.

**Project Type**: web service + SPA (same repo)

**Performance Goals**: concept page with a 900-word note readable < 1 s after navigation (SC-002);
readiness endpoint is two queries regardless of content size.

**Constraints**: no `bypassSecurityTrust*`; raw HTML in notes escaped; no horizontal page scroll at
375 px; Atlas tokens only (ADR 0006); knowledge API stays learner-agnostic (Principle IV).

**Scale/Scope**: ~100 concepts, 1 new endpoint, 1 migration, ~10 Angular components, ~40 i18n keys.

## Decisions

| # | Decision | Source |
|---|---|---|
| D1 | Readiness counts only schedulable questions — the same `Eligible` filter the session planner uses (today: non-deprecated MCQs). Concepts without any count as "without questions". | user |
| D2 | A question is ready when its review state has `interval_days >= 7`. Ease is not used (one miss would otherwise block readiness forever, since SM-2 here never raises ease). | user |
| D3 | `sessions.scope_concept_slug` + rewritten check constraints; ADR 0007. | user |
| D4 | Readiness served by a separate `GET /api/review/readiness` (no `lang`), not inside the knowledge tree: the tree is cached app-wide and `IKnowledgeQueries` never returns learner data. | default |
| D5 | Percent = `floor(100 * ready / counted)`; `null` when `counted == 0`. Domain % aggregates its concepts, not topic percentages. | default |
| D6 | `markdown-it` (`html: false`) + `highlight.js/lib/core` with csharp, typescript, javascript, sql, json, xml (html), bash (shell/sh). | user |
| D7 | Rail domain tabs keep opening Review scoped to the domain (ADR 0006). | default |

Details and alternatives: [research.md](research.md).

## Constitution Check

| Principle | Status | Note |
|---|---|---|
| I content over platform | PASS | reading + readiness only; no new content fields, no search/bookmarks |
| II content is data | PASS | notes stay Markdown in JSON; the renderer only reads them |
| III English first | PASS | per-item fallback reuses `fallback`/`titleFallback`; code never translated |
| IV progress user-scoped | PASS | readiness filtered by `LocalLearner.Id`, served by the review module; new column on `sessions` |
| V scheduling interface | PASS | readiness rule is a pure Domain rule on `interval_days`, outside `ISchedulingAlgorithm`; algorithm-neutral |
| VI LLM only for judgment | PASS | not touched |
| VII boring architecture | PASS | one query class, one endpoint, one validator change, one migration |
| VIII modern Angular | PASS | standalone, OnPush, signals, `httpResource`, `linkedSignal`, component-scoped store, no state library |
| IX one command | PASS | migration on start; `.env.example` unchanged |
| X tests where they pay | PASS | readiness math unit-tested; one integration test per module; renderer XSS tests |

No violations; no Complexity Tracking entries. Re-checked after Phase 1: unchanged.

## Project Structure

### Documentation (this feature)

```text
specs/004-knowledge-atlas-notes/
├── spec.md, plan.md, research.md, data-model.md, quickstart.md
├── contracts/readiness-api.md          # GET /api/review/readiness
├── contracts/review-scope-concept.md   # scope=concept on overview + sessions
├── checklists/requirements.md
└── tasks.md                            # /speckit-tasks
```

### Source Code (delta)

```text
src/Domain/Progress/            SessionScopeType (+Concept), Session (+ScopeConceptSlug), Readiness.cs (ConceptReadiness, ReadinessRule)
src/Application/Review/         ReviewScope (+Concept, ForConcept), ReviewDtos (ScopeDto +Concept/ConceptTitle, Readiness DTOs),
                                ReadinessCalculator, IReadinessQueries
src/Infrastructure/Review/      ReviewQueryables (shared Eligible/States), ReviewSessions (concept case), ReadinessQueries
src/Infrastructure/Persistence/ Configurations/ProgressConfigurations (column + checks), Migrations/<ts>_AddConceptSessionScope
src/Infrastructure/DependencyInjection.cs
src/Api/Endpoints/Review/       ScopeQuery (+concept), GetReadinessEndpoint
tests/Domain.Tests/Progress/    ReadinessRuleTests
tests/Application.Tests/Review/ ReadinessCalculatorTests, ReviewScopeTests
tests/Infrastructure.Tests/     Review/ReadinessQueriesTests, Review/ReviewSessionsTests (+concept), Persistence/ProgressSchemaTests
tests/Api.Tests/Review/         ReadinessEndpointTests, SessionsEndpointTests (+concept), OverviewEndpointTests (+concept)
web/src/app/knowledge/          knowledge.routes, atlas-layout/, atlas-home/, atlas-domain/, atlas-topic/, concept-page/,
                                concept-note/ (+ note-markdown.ts), not-found/, readiness/ (bar, chip, store), badges/ (en, unreviewed),
                                api/readiness.models.ts
web/src/app/review/             api/review.models + review-api (concept scope), review-overview (concept select, titleFallback badges)
web/src/app/                    app.routes.ts (/atlas lazy), app.html (rail link), app.config.ts (withComponentInputBinding)
web/src/app/shared/i18n/        messages.en.ts, messages.uk.ts (atlas.*, badges.*, overview concept keys)
web/src/styles.scss             code highlight tokens (light/dark), .note prose styles
web/package.json + lockfile     markdown-it, highlight.js, @types/markdown-it
docs/architecture/decisions/0007-concept-scope-readiness-and-notes.md
```

**Structure Decision**: same layering as 001–003. Readiness lives in the review module (progress data),
the knowledge read model stays content-only; the web `knowledge/` feature joins the tree and readiness by
slug on the client.

## Steps (red → green per story; app runnable after each)

1. **US1** Atlas routes, layout with breadcrumbs, home/domain/topic pages, "soon" topics, not-found, rail link (FR-001, FR-002, FR-013, FR-015).
2. **US2** Markdown renderer (+ XSS tests), concept page, prev/next, highlight tokens and prose styles (FR-003, FR-004, FR-005).
3. **US3** EN and unreviewed badges on every list and page; `titleFallback` badge in the review overview atlas (FR-006, FR-007, FR-014).
4. **US4** Readiness rule + calculator + query + endpoint; bar/chip/store in the Atlas (FR-008, FR-009, FR-010).
5. **US5** Domain/topic review links; concept scope end to end — migration, scope, sessions, validator, overview select, concept link (FR-011, FR-012).
6. Wrap-up: ADR 0007, Playwright walkthrough (keyboard, 375 px, EN↔УК, both themes), reviewers, memory-bank, `lockflight trace` + `gate run --profile pr`.
