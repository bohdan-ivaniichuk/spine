# System Patterns — Spine

Living record of architectural patterns and decisions. Keep entries dated; when a pattern is superseded,
say so rather than deleting.

## 2026-09-06 — Workspace bootstrap

- **Modular monolith, four projects:** `Domain` (entities, `ISchedulingAlgorithm`, SM-2), `Application`
  (use cases, interfaces), `Infrastructure` (EF Core/Npgsql, Anthropic client, content importer), `Api`
  (FastEndpoints, validators, `import-content` CLI verb). No MediatR, no bus, no outbox.
- **Content is data:** `content/schema/content.schema.json` (draft 2020-12) is the contract. Files are
  per topic. Ids `<domain>.<topic>.<concept>.<mcq|open|kata>.<n>`; slugs kebab-case; exactly four MCQ
  options with exactly one correct (enforced by schema + import).
- **Import = idempotent upsert** by slug (domain/topic/concept) and id (question/kata); translations
  upsert per `(entity, language)`. Re-import never touches `attempts`/`review_states`.
- **Progress is user-scoped:** `attempts`, `review_states`, `sessions` carry `user_id` → single seeded
  local user. Content tables never reference users.
- **Scheduling interface:** `ISchedulingAlgorithm.Next(state, grade, now)` pure; `review_states` stores
  `interval_days`, `ease`, `repetitions`, `last_reviewed_at`, `due_at`, `algorithm_state jsonb` (FSRS-ready).
- **LLM boundary:** one `IGrader` abstraction in Application; Infrastructure implements it with the
  official `Anthropic` SDK using structured output `{score, met[], missed[], feedback}`, a timeout and a
  `NoApiKeyGrader` fallback. MCQ grading, scheduling and readiness math are deterministic code.
- **Angular:** standalone + signals + `@if/@for/@defer` + OnPush; features mirror backend modules
  (`knowledge`, `review`, `grading`). Monaco only on the kata route, lazily.
- **One command:** `docker compose up` → `postgres` (healthcheck) → `api` (migrate on start) → `web`
  (nginx serving the build, proxying `/api`).

## 2026-09-06 — 001 Foundation as built

- **Keys:** surrogate int for domains/topics/concepts; natural text ids for questions/katas. Progress
  references items via `item_type` + `question_id`/`kata_id` + generated `item_id` (ADR 0002).
- **Import:** `ContentFileSystem.Load` → schema validator (JsonSchema.Net, list output) → rule
  validator (stable rule ids) → `ImportPlanBuilder` (hashes, derived `reviewed`) → `ImportDiff`
  (pure, mutates the tracked graph, never assigns to unchanged rows) → one transaction + advisory lock.
  Exit codes 0/1/2/3 per `specs/001-foundation/contracts/import-cli.md`.
- **Reads:** `IKnowledgeQueries` (Application) implemented in Infrastructure with `AsNoTracking`
  projections; tree = 3 flat queries stitched in memory; English fallback flagged per item.
- **Host:** one executable; `Program.cs` branches to the CLI when `args[0] == "import-content"`;
  Serilog (compact JSON outside Development); FastEndpoints with `api` prefix + ProblemDetails.
- **Web:** zoneless Angular; `httpResource` for server state; nginx maps `/api/health` → `/health/ready`.
- **Compose:** healthchecks on `127.0.0.1`; api waits for postgres, web waits for api; content mounted
  read-only at `/content`. Project name `spine`; the postgres volume is pinned by name to `interviewprep_postgres_data`
  (pre-rename data), and the database/role stay `interviewprep`.

## 2026-09-06 — Visual direction (ADR 0004)

- Light theme "Calm workspace", dark theme "Developer dark"; one typography (Manrope + JetBrains
  Mono). Tokens are CSS variables in `web/src/styles.scss` exposed via Tailwind `@theme inline`;
  `Theme` service sets `data-theme` on `<html>` (`system` = no attribute); header toggle.
- Components use token utilities only (`bg-surface`, `text-muted`, `border-line`, `bg-accent`, …).
- Design canvas artifact "InterviewPrep Design Directions" holds the chosen screens (page 1) and the
  unchosen sketches C/D (page 2).

## 2026-10-14 — 002 Review session as built

- **Session membership** is an ordered `text[]` on `sessions` with the start limits (ADR 0005); one
  answer per question per session is a partial unique index on `attempts (session_id, item_id)`.
- **Concurrency:** Start takes `pg_advisory_xact_lock` then `FOR UPDATE` on open sessions *before*
  planning; Answer and Finish take `FOR UPDATE` on their session row. No cycle (only Start uses the
  advisory lock). Unique violations on either the session-attempt or the review-state index map to
  `DuplicateAnswer` (409).
- **Errors:** nothing to review → 409 (open session untouched); no open session → 204; deprecated
  question → 422; `finish` idempotent.
- **Web:** root `SessionStore` (signals) survives overview → session → summary; the API is the source
  of truth on reload. Keyboard: digits/letters and Enter at document level, arrows inside the option
  radio group. `correct` never leaves the server before an answer.

## Tooling patterns

- Spec Kit feature folders under `specs/NNN-*`; `specs/000-taxonomy.md` is content scope, not a feature.
- Hooks: blocking secrets scan + local-file guard; confirm on commit/push; format after edit.
- Subagents run in fresh context; reviewers are read-only and report by severity.

## Readiness and scopes (004)

- Readiness is derived from `review_states` (never stored) in the review module, not the knowledge read
  model: `IReadinessQueries` → `GET /api/review/readiness`; pure math in `ReadinessCalculator`, rule in
  `Domain/Progress/ReadinessRule` (interval ≥ 7 days).
- Sessions and readiness share `Infrastructure/Review/ReviewQueryables.Eligible` — "schedulable" means the
  same thing in both. Scope types: all / domain / topic / concept (`ck_sessions_scope` mirrors
  `ReviewScope.IsValid` and `ScopeRules`).
- Web `knowledge/`: `AtlasLayout` provides a per-visit `ReadinessStore`; the concept page is a lazy route
  (renderer deps stay out of the initial bundle); cross-feature UI lives in `shared/` (badges, i18n).

