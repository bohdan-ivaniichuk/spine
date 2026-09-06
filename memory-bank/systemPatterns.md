# System Patterns — InterviewPrep

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
  read-only at `/content`.

## Tooling patterns

- Spec Kit feature folders under `specs/NNN-*`; `specs/000-taxonomy.md` is content scope, not a feature.
- Hooks: blocking secrets scan + local-file guard; confirm on commit/push; format after edit.
- Subagents run in fresh context; reviewers are read-only and report by severity.
