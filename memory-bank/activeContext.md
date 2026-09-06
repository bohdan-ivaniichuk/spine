# Active Context — InterviewPrep

## Where things stand (2026-09-06, branch `dev`)

Feature **001 Foundation** is complete (merged to `main`): stack runs under `docker compose up`,
103 tests green, both reviewers clean, clean-clone quickstart measured. Visual direction chosen
(ADR 0004: light "Calm workspace" + dark "Developer dark"). Feature **002 Review session** is in
progress: spec done, plan/tasks next, implementation via workflow.

**Done**
- Solution `InterviewPrep.sln`: Domain / Application / Infrastructure / Api + 4 test projects, CPM pins.
- Schema: 12 tables, `InitialSchema` migration, migrate-on-start with bounded retry, seeded learner.
- `import-content` CLI verb: schema + rule validation (file/pointer/rule errors), hashes, diff, single
  transaction under `pg_try_advisory_xact_lock`, soft-deprecation, `--validate-only`, `--dry-run`.
- Read API: `/api/knowledge/tree`, `/api/knowledge/concepts/{d}/{t}/{c}`, `/health`, `/health/ready`,
  `/api/meta`; ProblemDetails errors; Swagger in Development.
- Web: Angular 22 zoneless shell with `httpResource` status indicator, Vitest, Tailwind 4, nginx image.
- Compose with health checks; `scripts/validate-content.sh`; ADR 0002; CLAUDE.md commands + gotchas.
- Content: `content/dotnet/clr-memory.json` (7 concepts, 35 questions, 1 kata, EN, unreviewed) generated
  by the content workflow (author → reviewer → fix → assemble); digest in `working/`.

**In flight**
- 002 Review session: `specs/002-review-session/spec.md` (no open clarifications; MCQ only, two-outcome SM-2, session membership stored on the session).

**Next**
1. 002: plan → tasks → implement (workflow: backend SM-2 + endpoints, Angular review feature, tests, reviewers) → user tests a session in the browser.
2. Human review of `working/content-review-digest-clr-memory.md` → flip `reviewed: true` → re-import.
3. 003 Knowledge tree & notes; next content batch `dotnet/async`.

**Waiting on the user**
- Content review of `clr-memory` (flip `"reviewed": true` on accepted items).
