# Implementation Plan: Foundation — content graph, progress store, import, read API, one-command runtime

**Branch**: `001-foundation` | **Date**: 2026-09-06 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/001-foundation/spec.md`

## Summary

Stand up the vertical slice every later feature builds on: one .NET 10 solution
(`Domain → Application → Infrastructure → Api`) with the full content graph and progress schema in
PostgreSQL 16 via EF Core 10 migrations applied at start; an `import-content` CLI verb inside the `Api`
executable that validates `content/manifest.json` and `content/<domain>/<topic>.json` against the
committed JSON Schemas, upserts idempotently by slug/id using content hashes, soft-deprecates what
disappeared and never touches progress; two read endpoints (knowledge tree, concept page) with English
fallback; one seeded learner; and `docker compose up` bringing up `postgres`, `api` and an Angular 22
shell served by nginx with a live "API: OK" indicator. Design details: [research.md](research.md),
[data-model.md](data-model.md), [contracts/](contracts/), [quickstart.md](quickstart.md).

## Technical Context

**Language/Version**: C# 14 on .NET 10 (LTS, SDK 10.0.1xx pinned in `global.json`); TypeScript strict on Angular 22; Node 22 LTS

**Primary Dependencies**: FastEndpoints 8.3.x (+ Swagger), EF Core 10 + Npgsql.EntityFrameworkCore.PostgreSQL 10.0.x, EFCore.NamingConventions, JsonSchema.Net (json-everything, draft 2020-12), Serilog.AspNetCore, Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore; Angular CLI 22, Tailwind; nginx

**Storage**: PostgreSQL 16 (`postgres:16-alpine`), single `AppDbContext`, one migrations folder in Infrastructure, jsonb for options/rubric/answer/grading/algorithm_state

**Testing**: xUnit; Testcontainers.PostgreSql for integration (`Category=Integration`); `Microsoft.AspNetCore.Mvc.Testing` for API tests; Angular CLI default unit runner with `provideHttpClientTesting`

**Target Platform**: Local Docker Compose on macOS/Linux (linux/arm64 + amd64 images); developer machine with dotnet/node for inner loop

**Project Type**: Web service + SPA (single repository: `src/` + `tests/` + `web/`)

**Performance Goals**: full MVP content import < 30 s and zero-change re-import; tree/concept reads < 300 ms; stack healthy < 2 min after build

**Constraints**: no password literals in any committed file (secrets-scan hook); app usable without `ANTHROPIC_API_KEY`; content tables never reference users; import never writes progress tables; whole-directory import only (deprecation sweep must be complete)

**Scale/Scope**: single learner; ~5 domains / ~20 topics / ~120 concepts / ~600 questions; 11 tables; 2 read endpoints; 1 CLI verb; 1 Angular component

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | How the plan honours it |
|---|---|---|
| I Content over platform | PASS | Only infrastructure that every content feature needs; no UI beyond a status indicator. |
| II Content is data, not code | PASS | Validator reads the committed schemas at run time; only seed DTOs mirror the schema; rule checks are documented per rule id. |
| III English first, Ukrainian second | PASS | `en` mandatory by schema; reads fall back to `en` with an explicit flag; `uk` upserted per language, never wiped by omission. |
| IV Progress is user-scoped, single user | PASS | `users` referenced only from `attempts`, `review_states`, `sessions`; content tables have no user column; one seeded learner (fixed GUID), idempotent. |
| V Scheduling is an interface | PASS (deferred code) | `review_states` stores interval/ease/repetitions/last_reviewed_at/due_at + `algorithm_state jsonb` + `algorithm` tag; `ISchedulingAlgorithm` itself is feature 002. |
| VI LLM where judgment is needed | PASS | Nothing reads `ANTHROPIC_*`; `attempts.grading jsonb` reserved for 004. |
| VII Thin backend, boring architecture | PASS | 4 projects, DI + middleware, FastEndpoints validators, one queries interface; no MediatR/bus/outbox; CLI verb is a branch in `Program.cs`. |
| VIII Modern Angular, no state library | PASS | Standalone, zoneless, signals, `httpResource`, `@if`, OnPush; no NgRx. |
| IX One command to run | PASS | Compose with healthchecks + `depends_on: service_healthy`, migrate-on-start fail-fast, `.env.example` complete, `docker compose run --rm api import-content`. |
| X Tests where they pay | PASS | Unit: schema/rule validation, hashing, diff. Integration: migrations, import idempotency + progress untouched, tree/concept reads, seeder. Angular: status component. |

Post-design re-check (after Phase 1): no violations. No Complexity Tracking entries needed.

## Project Structure

### Documentation (this feature)

```text
specs/001-foundation/
├── plan.md              # This file
├── research.md          # Phase 0: decisions + alternatives
├── data-model.md        # Phase 1: tables, keys, constraints, EF mapping notes
├── quickstart.md        # Phase 1: run/validate guide
├── contracts/
│   ├── knowledge-api.md # GET /api/knowledge/tree, GET /api/knowledge/concepts/{domain}/{topic}/{concept}, /health*
│   └── import-cli.md    # `import-content` args, phases, rule ids, exit codes, summary format
├── checklists/requirements.md
└── tasks.md             # Phase 2 (/speckit-tasks)
```

### Source Code (repository root)

```text
InterviewPrep.sln
global.json                      # SDK 10.0.1xx
Directory.Build.props            # net10.0, Nullable, ImplicitUsings, TreatWarningsAsErrors
Directory.Packages.props         # central package management, all versions pinned
.editorconfig
src/
├── Domain/                      # InterviewPrep.Domain — no package references
│   ├── Content/                 # Domain, Topic, Concept, *Translation, Question, Kata, enums, McqOption
│   └── Progress/                # Learner, Attempt, ReviewState, Session, ItemType, SessionScopeType, LocalLearner
├── Application/                 # InterviewPrep.Application — refs Domain; JsonSchema.Net
│   ├── Content/Seed/            # STJ DTOs mirroring the JSON contract
│   ├── Content/Validation/      # ContentSchemaValidator, ContentRuleValidator, ContentValidationError/Result
│   ├── Content/                 # ContentHasher, ImportPlan(Builder), ImportDiff, ImportChangeSet, ImportSummary, ImportOptions, ImportExitCode, IContentImporter
│   └── Knowledge/               # IKnowledgeQueries, KnowledgeTreeDto, ConceptPageDto
├── Infrastructure/              # InterviewPrep.Infrastructure — refs Application; EF Core, Npgsql, NamingConventions
│   ├── Persistence/             # AppDbContext, Configurations/*, DesignTimeFactory, Migrations/, StartupMigrator, LearnerSeeder
│   ├── Content/                 # ContentFileSystem, ContentImporter, ImportLock
│   ├── Knowledge/               # KnowledgeQueries (AsNoTracking projections)
│   └── DependencyInjection.cs   # AddInfrastructure(IConfiguration)
└── Api/                         # InterviewPrep.Api — refs Infrastructure; FastEndpoints, Serilog, HealthChecks
    ├── Program.cs               # args[0]=="import-content" → CLI host; else web host
    ├── Cli/ImportContentCommand.cs
    ├── Endpoints/Knowledge/     # GetKnowledgeTree{Endpoint,Request,Response,Validator}, GetConcept{...}
    ├── Health/HealthEndpoints.cs
    ├── appsettings.json, appsettings.Development.json, Dockerfile
tests/
├── Domain.Tests/
├── Application.Tests/           # schema/rule validators, hasher, diff, RepoContentValidatesTests
├── Infrastructure.Tests/        # Testcontainers: migrations, importer, seeder  [Category=Integration]
├── Api.Tests/                   # WebApplicationFactory + Testcontainers: tree, concept, health
└── Fixtures/content/            # manifest.json + sample topic files (+ invalid variants)
web/                             # Angular 22 (angular-new-app skill), Tailwind, zoneless
├── src/app/{app.config.ts,app.routes.ts,app.ts}
├── src/app/shared/api-status/   # ApiStatus component + spec (httpResource → /api/health)
├── src/app/{knowledge,review,grading}/README.md   # placeholders for 002/003/004
├── proxy.conf.json, Dockerfile, nginx.conf
content/                         # schema/, manifest.json, <domain>/<topic>.json (mounted read-only into api)
docker-compose.yml, .env.example
scripts/validate-content.sh      # wraps `import-content --validate-only`
```

**Structure Decision**: Single repository with a modular-monolith backend under `src/` (4 projects,
strict downward references), mirrored `tests/`, and the SPA under `web/`. Namespaces mirror folders
(`InterviewPrep.Domain.Content`, `InterviewPrep.Api.Endpoints.Knowledge`, …). The CLI verb lives in
the `Api` executable so there is exactly one deployable and one configuration surface.

## Complexity Tracking

No constitution violations to justify.

## Phase summary

- **Phase 0 — research.md**: package choices (JsonSchema.Net over NJsonSchema; EFCore.NamingConventions;
  Testcontainers), key-strategy decisions, polymorphic progress key, health/compose patterns.
- **Phase 1 — data-model.md, contracts/, quickstart.md**: 11 tables with constraints; two read
  endpoints + health; CLI contract; end-to-end validation guide.
- **Phase 2 — tasks.md** (`/speckit-tasks`): ordered, independently verifiable steps 1–12 from the
  architect's plan (skeleton → domain → persistence + migration → host + health + seeder → compose →
  import pure → import DB + CLI → import integration tests → read endpoints → web shell → docs).

## Decisions taken during planning (recorded for `/speckit-analyze`)

1. `concepts.reviewed` is **derived at import** (all non-deprecated questions and katas reviewed); the
   content schema stays unchanged in 001.
2. Session scope stored as `(scope_type, scope_domain_slug, scope_topic_slug)` — topic slugs are only
   unique within a domain.
3. Root `concept.json` / `file.json` are scratch output of the content workflow and are deleted; the
   real topic file is written by the workflow to `content/dotnet/clr-memory.json`.
