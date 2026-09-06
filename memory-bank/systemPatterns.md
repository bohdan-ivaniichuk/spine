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

## Tooling patterns

- Spec Kit feature folders under `specs/NNN-*`; `specs/000-taxonomy.md` is content scope, not a feature.
- Hooks: blocking secrets scan + local-file guard; confirm on commit/push; format after edit.
- Subagents run in fresh context; reviewers are read-only and report by severity.
