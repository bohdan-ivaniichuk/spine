# Progress — Spine

## What works

- [x] Workspace shell: Spec Kit, hooks, agents, skills, MCP config, memory-bank, docs (2026-09-06).
- [x] Content contract: `content/schema/content.schema.json`, `manifest.schema.json`, `content/manifest.json`.
- [x] 001 Foundation — schema + migration, seeded learner, `import-content` (idempotent, deprecation,
      dry-run, validate-only), knowledge read endpoints, health, compose (postgres/api/web), web shell.
- [x] Content batch `dotnet/clr-memory` (EN, unreviewed) imported.
- [x] Content batch `dotnet/async` (EN, unreviewed) imported.
- [x] 002 Review session — MCQ + SM-2, session API, Angular review flow; 210 .NET + 68 web tests;
      SC-001…SC-006 measured in `specs/002-review-session/quickstart.md` (2026-10-14).
- [x] 003 Atlas redesign, EN/UK UI, coverage endpoint, rename to Spine — 237 .NET + 80 web tests
      (2026-10-15, ADR 0006).
- [x] `uk` domain/topic titles (manifest + importer + migration); `clr-memory`/`async` reviewed + `uk`;
      content sprint 1 for domains 1, 2, 4 (409 questions, unreviewed); 267 .NET + 81 web tests (2026-10-15).
- [x] 004 Knowledge atlas & notes — Atlas routes, Markdown notes, readiness endpoint, concept review scope;
      322 .NET + 158 web tests, gate PASS (2026-10-15, ADR 0007).
- [x] 006 Weak spots — weak review mode, weak-spots view, summary drill; 356 .NET + 188 web tests, gate PASS,
      Lockflight review approve (2026-10-28, ADR 0008).

## In progress

- [x] 001 tests: 57 unit + 27 integration + 19 API green; both reviewers clean; clean-clone run 30 s to healthy (2026-09-06).
- [x] Visual direction: light/dark themes and tokens in the shell (ADR 0004).

## Planned (in order)

- [ ] 005 LLM grading (open + kata) (was 004)
- [ ] Content sprint (user priority, 2026-09-06): after 002 integration, run the content workflow 2–3 topics
      at a time for the rest of domain 1 (`csharp-lang`, `dotnet-platform`), all of domain 2 (`pipeline`,
      `security`, `efcore`, `postgres`, `api-design`) and domain 4 (`principles`, `distributed`,
      `system-design`, `lead`). Author prompt gains a "classic interview questions first" rule: the first
      MCQ and the open question of each concept must be what interviewers actually ask. Angular (3) and
      AI engineering (5) afterwards. `uk` translation pass only for reviewed items.

## Known issues

- Angular 22 CLI requires Node ≥ 22.22.3 (`nvm use 22`); see CLAUDE.md "Things that bite".
- First start on an empty DB logs two benign EF errors about `__EFMigrationsHistory`.
- Postgres MCP times out at session start unless the `postgres` container is already running.
