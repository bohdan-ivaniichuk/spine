# Active Context — InterviewPrep

## Where things stand (2026-09-06, branch `dev`)

Feature **001 Foundation** is implemented end to end and running under `docker compose up`
(postgres + api + web, all healthy); the first content batch `dotnet/clr-memory` is imported.
Test suites (unit, Testcontainers integration, API) are being finalised; reviewers and the clean-clone
run are the remaining 001 steps.

**Done**
- Solution `InterviewPrep.sln`: Domain / Application / Infrastructure / Api + 4 test projects, CPM pins.
- Schema: 11 tables, `InitialSchema` migration, migrate-on-start with bounded retry, seeded learner.
- `import-content` CLI verb: schema + rule validation (file/pointer/rule errors), hashes, diff, single
  transaction under `pg_try_advisory_xact_lock`, soft-deprecation, `--validate-only`, `--dry-run`.
- Read API: `/api/knowledge/tree`, `/api/knowledge/concepts/{d}/{t}/{c}`, `/health`, `/health/ready`,
  `/api/meta`; ProblemDetails errors; Swagger in Development.
- Web: Angular 22 zoneless shell with `httpResource` status indicator, Vitest, Tailwind 4, nginx image.
- Compose with health checks; `scripts/validate-content.sh`; ADR 0002; CLAUDE.md commands + gotchas.
- Content: `content/dotnet/clr-memory.json` (7 concepts, 35 questions, 1 kata, EN, unreviewed) generated
  by the content workflow (author → reviewer → fix → assemble); digest in `working/`.

**In flight**
- Unit tests (Application), integration tests (Infrastructure, Api) — written by test-author agents.
- Aspire: decided — defer to feature 004 (ADR 0003); `working/aspire-research-2026-09-06.md` has the brief.

**Next**
1. Finish 001: reviewers (code-reviewer, frontend-reviewer), clean-clone quickstart, final commit.
2. Human review of `working/content-review-digest-clr-memory.md` → flip `reviewed: true` → re-import.
3. `/speckit-specify` 002 Review session (MCQ + SM-2).
4. Next content batch: `dotnet/async`.

**Waiting on the user**
- Content review of `clr-memory` (flip `"reviewed": true` on accepted items).
