<!--
Sync Impact Report (2026-09-06)
- Version change: 1.0.0 → 1.0.1 (PATCH — structural normalization only, no semantic change)
- Modified principles: none (I–X unchanged)
- Added sections: none
- Removed sections: none
- Structural changes: version line moved to the Spec Kit footer format at the end of Governance;
  amendment log entry added
- Templates: .specify/templates/{plan,spec,tasks}-template.md read the constitution at runtime — no
  edits required
- Follow-up TODOs: none
-->
# InterviewPrep Constitution

## Purpose

A single-user, locally-run knowledge platform for Senior Full-stack (Angular + .NET + AI Engineering) interview preparation. It combines a knowledge graph (domain → topic → concept), spaced-repetition quizzes, LLM-graded open answers and code katas. The platform is intentionally thin: most value lives in the content and in the AI-assisted workflow that produces it. Building it is itself a showcase of AI-augmented development.

## Core Principles

### I. Content over platform
Every feature must be justified by "does this make review sessions better this week". Content authoring, review and scheduling come first; dashboards, gamification, multi-user, billing come later or never. If a feature can be replaced by a markdown file for now, it is.

### II. Content is data, not code
All knowledge (concepts, notes, questions, katas, translations) lives in versioned seed files (`content/**/*.json`) validated against a JSON Schema and imported into PostgreSQL. Content is authored by the `content-author` subagent from `specs/000-taxonomy.md` and human-reviewed before import. No question is ever hard-coded in application code.

### III. English first, Ukrainian second
Canonical content language is English (interview terminology must be native). Every concept and question carries a `translations` map keyed by language (`en`, `uk`); `en` is mandatory, `uk` is generated in a separate translation pass. Code samples, identifiers and technical terms are never translated. All committed artifacts (code, specs, commits, docs) are in English.

### IV. Progress is user-scoped, platform is single-user
There is no authentication in MVP. Progress tables (`attempts`, `review_states`, `sessions`) carry a `user_id` referencing a single seeded local user. Content tables never reference users. This keeps a future multi-tenant path open without building it now.

### V. Scheduling is an interface
Spaced repetition is implemented behind `ISchedulingAlgorithm`. MVP ships SM-2 (interval + ease factor). FSRS may be added later as another implementation; the review-state schema must store enough (`interval_days`, `ease`, `repetitions`, `last_reviewed_at`, `due_at`, plus a JSON `algorithm_state`) to support both.

### VI. LLM where judgment is needed, never where rules suffice
Anthropic API is used only for: grading open answers and code katas against a rubric, generating a personalised "why you were wrong" review, and (later) mock-interview follow-ups. MCQ grading, scheduling, progress math are deterministic code. Every LLM call has a structured output contract, a timeout, and a fallback message; the app must remain fully usable for MCQ/notes if the API key is absent.

### VII. Thin backend, boring architecture
Modular monolith in one solution: `Api` (FastEndpoints), `Application` (use cases, interfaces), `Domain` (entities, scheduling), `Infrastructure` (EF Core, PostgreSQL, Anthropic client). No CQRS/MediatR ceremony, no message bus, no outbox. Request validation lives in FastEndpoints validators. Cross-cutting concerns via middleware and DI only.

### VIII. Modern Angular, no state library
Angular (latest stable), standalone components, signals for state, `@if/@for/@defer` control flow, `OnPush` everywhere, typed reactive forms, `HttpResource`/`resource` or plain `HttpClient` + signals. No NgRx/NGXS. Feature folders mirror backend modules. Monaco editor for katas.

### IX. One command to run
`docker compose up` starts `postgres`, `api` and `web` with health checks, migrations applied on API start, seed import runnable via `docker compose run api import-content`. `.env.example` documents every variable; real `.env` is git-ignored and never written by tooling.

### X. Tests where they pay
Unit tests are mandatory for scheduling (SM-2 transitions), seed validation/import, and grading contract parsing. Integration tests (Testcontainers + PostgreSQL) for the import pipeline and one endpoint per module. Angular: component tests for quiz flow; one Playwright smoke test through the MCP for the review session. No coverage targets.

## Technology Constraints

- Backend: .NET (latest LTS), C#, FastEndpoints, EF Core, Npgsql, FluentValidation, Serilog.
- Database: PostgreSQL 16+. Migrations via EF Core, one migrations project.
- Frontend: Angular latest stable, TypeScript strict, Tailwind for styling, Monaco editor.
- AI: Anthropic Messages API via official .NET SDK or minimal typed HttpClient; model configurable via env; structured outputs enforced.
- Local infra: Docker Compose; nginx serving the Angular build; API on 8080, web on 4200/80.
- Scheduling: SM-2 in MVP behind `ISchedulingAlgorithm`.

## Development Workflow

- Spec-driven: `/speckit.constitution` → `/speckit.specify` → `/speckit.clarify` → `/speckit.plan` → `/speckit.tasks` → `/speckit.implement`, one feature per `specs/NNN-*/`.
- Subagents: `solution-architect` before non-trivial plans, `code-reviewer` + `frontend-reviewer` before every commit, `test-author` for test scaffolding, `content-author` for seed generation, `deep-researcher` (context7 + web) for framework-version questions.
- Content pipeline: taxonomy → `content-author` → JSON seed → human review (mark `"reviewed": true`) → import. Unreviewed content is imported with a visible "unreviewed" badge, never silently.
- Hooks: secrets scan and local-runtime-file guard are blocking; `dotnet format` and `prettier`/`ng lint --fix` run after edits; git commit/push always require confirmation.
- Memory bank (`memory-bank/`) is updated when architecture, decisions or "things that bite" change.
- No AI trailers in commit messages; no ticket numbers in code comments.

## Explicit Non-Goals (MVP)

Authentication, multi-user, billing/subscriptions, voice mock interviews, in-browser code execution/sandboxing, React domain content, mobile app, analytics dashboards beyond a simple per-domain readiness number.

## Governance

This constitution supersedes ad-hoc preferences. Amendments require: a bump of the version (MAJOR for principle removal/redefinition, MINOR for new principle or section, PATCH for wording), a one-line rationale in the amendment log below, and a check that active specs still comply.

### Amendment log
- 1.0.0 — Initial ratification.
- 1.0.1 — Normalized to the Spec Kit constitution structure (version footer, sync impact report); no semantic change.

**Version**: 1.0.1 | **Ratified**: 2026-09-06 | **Last Amended**: 2026-09-06
