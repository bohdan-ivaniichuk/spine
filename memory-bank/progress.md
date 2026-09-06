# Progress — InterviewPrep

## What works

- [x] Workspace shell: Spec Kit, hooks, agents, skills, MCP config, memory-bank, docs (2026-09-06).
- [x] Content contract: `content/schema/content.schema.json`, `manifest.schema.json`, `content/manifest.json`.
- [x] 001 Foundation — schema + migration, seeded learner, `import-content` (idempotent, deprecation,
      dry-run, validate-only), knowledge read endpoints, health, compose (postgres/api/web), web shell.
- [x] Content batch `dotnet/clr-memory` (EN, unreviewed) imported.

## In progress

- [ ] 001 tests (unit + Testcontainers + API) and reviewer pass; clean-clone run.

## Planned (in order)

- [ ] 002 Review session (MCQ + SM-2)
- [ ] 003 Knowledge tree & notes
- [ ] 004 LLM grading (open + kata)
- [ ] 005 Weak-spots & failed-question review
- [ ] Content batches per taxonomy order; `uk` translation pass per reviewed topic.

## Known issues

- Angular 22 CLI requires Node ≥ 22.22.3 (`nvm use 22`); see CLAUDE.md "Things that bite".
- First start on an empty DB logs two benign EF errors about `__EFMigrationsHistory`.
