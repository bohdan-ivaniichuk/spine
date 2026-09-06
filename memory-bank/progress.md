# Progress — InterviewPrep

## What works

- [x] Workspace shell: Spec Kit, hooks, agents, skills, MCP config, memory-bank, docs (2026-09-06).
- [x] Content contract: `content/schema/content.schema.json`, `manifest.schema.json`, `content/manifest.json`.
- [x] 001 Foundation — schema + migration, seeded learner, `import-content` (idempotent, deprecation,
      dry-run, validate-only), knowledge read endpoints, health, compose (postgres/api/web), web shell.
- [x] Content batch `dotnet/clr-memory` (EN, unreviewed) imported.

## In progress

- [x] 001 tests: 57 unit + 27 integration + 19 API green; both reviewers clean; clean-clone run 30 s to healthy (2026-09-06).
- [x] Visual direction: light/dark themes and tokens in the shell (ADR 0004).

## Planned (in order)

- [ ] 002 Review session (MCQ + SM-2) — spec written 2026-09-06, in implementation
- [ ] 003 Knowledge tree & notes
- [ ] 004 LLM grading (open + kata)
- [ ] 005 Weak-spots & failed-question review
- [ ] Content sprint (user priority, 2026-09-06): after 002 integration, run the content workflow 2–3 topics
      at a time for the rest of domain 1 (`csharp-lang`, `dotnet-platform`), all of domain 2 (`pipeline`,
      `security`, `efcore`, `postgres`, `api-design`) and domain 4 (`principles`, `distributed`,
      `system-design`, `lead`). Author prompt gains a "classic interview questions first" rule: the first
      MCQ and the open question of each concept must be what interviewers actually ask. Angular (3) and
      AI engineering (5) afterwards. `uk` translation pass only for reviewed items.

## Known issues

- Angular 22 CLI requires Node ≥ 22.22.3 (`nvm use 22`); see CLAUDE.md "Things that bite".
- First start on an empty DB logs two benign EF errors about `__EFMigrationsHistory`.
