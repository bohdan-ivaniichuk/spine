# Tasks: Foundation — content graph, progress store, import, read API, one-command runtime

**Input**: Design documents from `/specs/001-foundation/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Included — constitution Principle X mandates unit tests for seed validation/import and
integration tests (Testcontainers + PostgreSQL) for the import pipeline and one endpoint per module.

**Organization**: Tasks are grouped by user story. Setup and Foundational phases hold everything all
stories share (solution, schema, migration, host). Each story phase is independently verifiable.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1 (one-command stack), US2 (import), US3 (read API), US4 (learner & progress storage)

## Path Conventions

Single repository: backend `src/{Domain,Application,Infrastructure,Api}`, tests `tests/*`, SPA
`web/`, content `content/`, compose at the root. Namespaces mirror folders (`InterviewPrep.<Project>.<Folder>`).

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Solution skeleton with pinned versions; build green with zero code.

- [X] T001 Verify package versions with context7/deep-researcher before pinning: FastEndpoints 8.3.x, Microsoft.EntityFrameworkCore 10.0.x (≥ 10.0.4), Npgsql.EntityFrameworkCore.PostgreSQL 10.0.x, EFCore.NamingConventions 10.x (fallback: explicit names), JsonSchema.Net latest, Serilog.AspNetCore, Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore 10.x, xunit + xunit.runner.visualstudio, Microsoft.NET.Test.Sdk, Testcontainers.PostgreSql, Microsoft.AspNetCore.Mvc.Testing 10.x, FluentAssertions or Shouldly (pick one); record in `docs/architecture/STACK.md`
- [X] T002 Create `global.json` (SDK 10.0.1xx, rollForward latestFeature), `Directory.Build.props` (net10.0, Nullable, ImplicitUsings, TreatWarningsAsErrors, InvariantGlobalization=false), `Directory.Packages.props` (central package management, every version from T001), `.editorconfig`
- [X] T003 Create `InterviewPrep.sln` with `src/Domain/InterviewPrep.Domain.csproj`, `src/Application/InterviewPrep.Application.csproj` (ref Domain), `src/Infrastructure/InterviewPrep.Infrastructure.csproj` (ref Application), `src/Api/InterviewPrep.Api.csproj` (ref Infrastructure, `<OutputType>Exe`, `<InvariantGlobalization>true`) and `tests/{Domain,Application,Infrastructure,Api}.Tests/InterviewPrep.*.Tests.csproj` (xUnit; each refs its `src` counterpart; Infrastructure.Tests + Api.Tests also ref Testcontainers.PostgreSql; Api.Tests refs Mvc.Testing); solution folders `src`, `tests`
- [X] T004 [P] Add `.gitignore` entries `web/.angular/`, `**/bin/`, `**/obj/` (already partly present) and `.dockerignore` files for `src/Api` (bin/obj/tests/web) and `web` (node_modules/dist/.angular)
- [X] T005 Verify: `dotnet build -c Release` and `dotnet test` (0 tests) are green; commit "Solution skeleton"

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Domain model, persistence, migration, web host with health, configuration. Every user story
depends on this.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

### Domain

- [X] T006 [P] Create content entities in `src/Domain/Content/`: `Domain.cs`, `Topic.cs`, `Concept.cs`, `ConceptTranslation.cs`, `Question.cs`, `QuestionTranslation.cs`, `Kata.cs`, `KataTranslation.cs` with properties per data-model.md (int ids for domain/topic/concept, string ids for question/kata, `SortOrder`, `ContentHash`, `DeprecatedAt`, `CreatedAt`, `UpdatedAt`, navigation collections; no reference to users)
- [X] T007 [P] Create enums and value types in `src/Domain/Content/`: `Level.cs` (M/S/L), `QuestionType.cs` (Mcq/Open), `KataLanguage.cs` (CSharp/TypeScript/Sql), `Language.cs` (En/Uk), `McqOption.cs` (record Text, Correct); string mapping helpers (`ToDbValue()/Parse`) kept in Domain
- [X] T008 [P] Create progress entities in `src/Domain/Progress/`: `Learner.cs` (Guid Id, DisplayName, PreferredLanguage, CreatedAt), `ReviewState.cs`, `Attempt.cs`, `Session.cs`, `ItemType.cs` (Question/Kata), `SessionScopeType.cs` (All/Domain/Topic), `LocalLearner.cs` (`public static readonly Guid Id = Guid.Parse("00000000-0000-0000-0000-000000000001")`)

### Persistence

- [X] T009 Create `src/Infrastructure/Persistence/AppDbContext.cs` with `DbSet`s for all 12 tables, `UseSnakeCaseNamingConvention()` applied in `AddInfrastructure`, `OnModelCreating` applying configurations from assembly
- [X] T010 [P] Create content entity configurations in `src/Infrastructure/Persistence/Configurations/`: `DomainConfiguration.cs`, `TopicConfiguration.cs`, `ConceptConfiguration.cs`, `ConceptTranslationConfiguration.cs`, `QuestionConfiguration.cs`, `QuestionTranslationConfiguration.cs`, `KataConfiguration.cs`, `KataTranslationConfiguration.cs` — keys, unique indexes, CHECK constraints (level/type/language/kata language/time limit/id regex/options-xor-rubric), `jsonb` columns with value converters **and ValueComparers** for options/rubric, `text[]` tags, `timestamptz` per data-model.md
- [X] T011 [P] Create progress configurations in `src/Infrastructure/Persistence/Configurations/`: `LearnerConfiguration.cs`, `ReviewStateConfiguration.cs`, `AttemptConfiguration.cs`, `SessionConfiguration.cs` — two nullable FKs + `item_type` CHECK + computed stored `item_id` (`HasComputedColumnSql("COALESCE(question_id, kata_id)", stored: true)`), unique `(user_id, item_id)`, `(user_id, due_at)` index, attempts indexes, sessions scope CHECK, `algorithm` default `'sm2'`, `ease` default 2.5
- [X] T012 Create `src/Infrastructure/Persistence/AppDbContextDesignTimeFactory.cs` (`IDesignTimeDbContextFactory<AppDbContext>` reading `ConnectionStrings__Default` env, fallback `Host=localhost;Database=interviewprep;Username=interviewprep`, password via `PGPASSWORD`) and `src/Infrastructure/DependencyInjection.cs` (`AddInfrastructure(IConfiguration)` registering `AppDbContext` with Npgsql, naming convention, `EnableRetryOnFailure`)
- [X] T013 Start a local database (`docker run -d --name ip-pg -e POSTGRES_PASSWORD=... -e POSTGRES_USER=interviewprep -e POSTGRES_DB=interviewprep -p 5432:5432 postgres:16-alpine`), generate `InitialSchema` migration into `src/Infrastructure/Persistence/Migrations/` (`dotnet ef migrations add InitialSchema --project src/Infrastructure --startup-project src/Api --output-dir Persistence/Migrations`), review the generated SQL for every CHECK/index/computed column, `dotnet ef database update`; `git add` migration `.cs` + `.Designer.cs` + snapshot
- [X] T014 Create `src/Infrastructure/Persistence/StartupMigrator.cs` (retry `CanConnectAsync` every 3 s up to `DB_STARTUP_TIMEOUT_SECONDS`, default 90; then `MigrateAsync`; throw → caller exits 1) and `src/Infrastructure/Persistence/LearnerSeeder.cs` (`ExecuteSqlAsync` `INSERT INTO users … ON CONFLICT (id) DO NOTHING` with `LocalLearner.Id`, 'Local learner', 'en')

### Api host

- [X] T015 Create `src/Api/Program.cs`: branch `args[0] == "import-content"` → `ImportContentCommand.RunAsync(args[1..])` (stub returning 0 until Phase 4); else `WebApplication.CreateBuilder`, Serilog (console; compact JSON when `ASPNETCORE_ENVIRONMENT != Development`), `AddInfrastructure`, `AddFastEndpoints()`, `SwaggerDocument()` (Development only), `AddHealthChecks().AddDbContextCheck<AppDbContext>("db", tags: ["ready"])`; startup: `StartupMigrator` → `LearnerSeeder` → `UseFastEndpoints(c => c.Endpoints.RoutePrefix = "api")`; `public partial class Program {}` for tests
- [X] T016 [P] Create `src/Api/Health/HealthEndpoints.cs` mapping `/health` (predicate `_ => false`) and `/health/ready` (tag `ready`) with a JSON response writer `{ "status": "Healthy" }`
- [X] T017 [P] Create `src/Api/appsettings.json` (Serilog minimum levels, no secrets) and `src/Api/appsettings.Development.json` (Development overrides, no password; connection string without password so `PGPASSWORD` supplies it)
- [X] T018 Verify: with the local database running and `PGPASSWORD` exported, `dotnet run --project src/Api` migrates, seeds one learner (check via postgres MCP `select count(*) from users`), `/health` and `/health/ready` return 200; stop the DB → API exits non-zero after the timeout; commit "Foundational: domain, persistence, migration, host"

**Checkpoint**: Schema exists, host runs, learner seeded — user stories can start.

---

## Phase 3: User Story 1 — Bring the platform up with one command (Priority: P1) 🎯 MVP

**Goal**: `docker compose up` → postgres + api + web healthy; data persists; web shows "API: OK".

**Independent Test**: Clean machine with Docker: `cp .env.example .env && docker compose up --build`
→ all three healthy in < 2 min; browser shows "API: OK"; `down`/`up` keeps the `users` row.

### Compose (postgres + api) — can be done right after Phase 2

- [X] T019 [P] [US1] Create `src/Api/Dockerfile` (multi-stage: `sdk:10.0` restore with csproj layer cache → publish Release → `aspnet:10.0-alpine`, non-root `app` user, `ASPNETCORE_URLS=http://+:8080`, `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true`, `EXPOSE 8080`)
- [X] T020 [P] [US1] Create `.env.example` with commented variables: `POSTGRES_USER`, `POSTGRES_PASSWORD=change-me`, `POSTGRES_DB`, `POSTGRES_PORT`, `API_PORT`, `WEB_PORT`, `CONTENT_DIR=/content`, `DB_STARTUP_TIMEOUT_SECONDS=90`, `ASPNETCORE_ENVIRONMENT=Production`, `ANTHROPIC_API_KEY=` (unused until 004), `ANTHROPIC_MODEL=claude-sonnet-5`, `DATABASE_URI=` (postgres MCP, dev only)
- [X] T021 [US1] Create `docker-compose.yml`: `postgres` (`postgres:16-alpine`, env from `.env`, volume `postgres_data`, `pg_isready` healthcheck, port `${POSTGRES_PORT:-5432}`), `api` (build `src/Api/Dockerfile` with context `.`, `ConnectionStrings__Default=Host=postgres;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}`, `CONTENT_DIR=/content`, volume `./content:/content:ro`, `depends_on: postgres: condition: service_healthy`, healthcheck `wget -qO- http://localhost:8080/health/ready`, `start_period: 30s`, `restart: unless-stopped`, port `${API_PORT:-8080}:8080`)
- [X] T022 [US1] Verify: `cp .env.example .env && docker compose up --build` → postgres + api healthy; `docker compose down && docker compose up` keeps the learner row; commit "Compose: postgres + api"

### Web shell

- [X] T023 [US1] Scaffold Angular 22 app in `web/` using the `angular-new-app` skill (standalone, zoneless, signals, strict TS, SCSS, the CLI's default unit runner; project name `web`), then add Tailwind per the `angular-developer` skill guidance; add `web/proxy.conf.json` (`/api` → `http://localhost:8080`) and wire `npm start` to use it
- [X] T024 [US1] Create `web/src/app/shared/api-status/api-status.ts|html|scss` (standalone, OnPush): `health = httpResource<{ status: string }>(() => '/api/health')`; template with `@if (health.isLoading())` / `@else if (health.hasValue())` "API: OK" / `@else` "API: unreachable" + retry button `health.reload()`; 30 s `setInterval` reload cleared via `DestroyRef`; render it in `web/src/app/app.html` with the app title
- [X] T025 [P] [US1] Create `web/src/app/shared/api-status/api-status.spec.ts` with `provideHttpClient()` + `provideHttpClientTesting()`: flush `{status:"Healthy"}` → "API: OK"; flush error → "API: unreachable"
- [X] T026 [P] [US1] Create placeholder folders `web/src/app/knowledge/README.md`, `web/src/app/review/README.md`, `web/src/app/grading/README.md` (one line each: which feature fills it)
- [X] T027 [US1] Create `web/Dockerfile` (`node:22-alpine` `npm ci && npm run build` → `nginx:alpine`, copy `dist/web/browser` to `/usr/share/nginx/html`) and `web/nginx.conf` (`try_files $uri /index.html`; `location /api/ { proxy_pass http://api:8080/api/; }`; `location = /api/health { proxy_pass http://api:8080/health/ready; }`; `location = /healthz { return 200; }`)
- [X] T028 [US1] Add `web` service to `docker-compose.yml` (build `web/`, healthcheck `wget -qO- http://localhost/healthz`, `depends_on: api: condition: service_healthy`, port `${WEB_PORT:-80}:80`)
- [X] T029 [US1] Run `frontend-reviewer` on `web/`; fix findings; verify `cd web && npm test -- --watch=false` green, `docker compose up --build` → three healthy services, `http://localhost` shows "API: OK", stopping `api` flips the indicator; commit "Web shell + compose"

**Checkpoint**: US1 complete — SC-001 verifiable.

---

## Phase 4: User Story 2 — Load curated content from seed files (Priority: P1)

**Goal**: `import-content` validates manifest + topic files, upserts idempotently, soft-deprecates,
never touches progress, prints a summary, exits with documented codes.

**Independent Test**: Import `content/`; re-import → 0 created / 0 updated; edit one note and one
option text → exactly those translations updated, ids unchanged; progress rows untouched; invalid file
→ nothing written, errors with file + pointer + rule.

### Pure part (Application, no DB) — unit tests first

- [X] T030 [P] [US2] Create test fixtures in `tests/Fixtures/content/`: `manifest.json` (2 domains, 3 topics), `dotnet/sample-topic.json` (2 concepts: 2 mcq + 1 open + 1 kata, `en` + one `uk` translation), `invalid/five-options.json`, `invalid/two-correct.json`, `invalid/bad-id.json`, `invalid/unknown-topic.json`, `invalid/rubric-on-mcq.json`; mark them `<Content Include>` copied to output in `tests/Application.Tests` and `tests/Infrastructure.Tests`
- [X] T031 [P] [US2] Create seed DTOs in `src/Application/Content/Seed/`: `ManifestFile.cs`, `TopicFile.cs`, `ConceptSeed.cs`, `ConceptTranslationSeed.cs`, `QuestionSeed.cs`, `QuestionTranslationSeed.cs`, `McqOptionSeed.cs`, `KataSeed.cs`, `KataTranslationSeed.cs` (System.Text.Json, camelCase, mirror both schemas 1:1)
- [X] T032 [P] [US2] Create `src/Application/Content/Validation/ContentValidationError.cs` (File, JsonPointer, Rule, Message; `ToString()` = `<file>:<pointer> [<rule>] <message>`) and `ContentValidationResult.cs`
- [X] T033 [US2] Create `src/Application/Content/Validation/ContentSchemaValidator.cs` using JsonSchema.Net: load `schema/content.schema.json` and `schema/manifest.schema.json` from the content dir, evaluate each file with `OutputFormat.List`, keep failing leaf nodes with a non-empty error, map to `ContentValidationError` (rule = keyword)
- [X] T034 [US2] Create `tests/Application.Tests/Content/ContentSchemaValidatorTests.cs`: valid fixture passes; each invalid fixture yields an error with the right file, pointer and keyword; multiple errors in one file are all reported; note < 200 chars rejected; extra property rejected
- [X] T035 [US2] Create `src/Application/Content/Validation/ContentRuleValidator.cs` implementing rules `manifest-duplicates`, `manifest-pair`, `file-path`, `duplicate-topic-file`, `concept-slug-unique`, `id-prefix`, `id-type`, `mcq-one-correct`, `concept-title`, `duplicate-id` (each error carries the rule id and, for duplicates, both files)
- [X] T036 [US2] Create `tests/Application.Tests/Content/ContentRuleValidatorTests.cs`: one test per rule id (two correct, zero correct, id prefix mismatch, type/id segment mismatch, unknown manifest pair, wrong path, duplicate topic file, duplicate id across two files names both, concept title drift, manifest duplicates)
- [X] T037 [P] [US2] Create `src/Application/Content/ContentHasher.cs` (SHA-256 hex over canonical STJ serialization: fixed property order, no whitespace; separate hashes for entity scalars and each translation) and `tests/Application.Tests/Content/ContentHasherTests.cs` (key order / whitespace irrelevant; note edit changes translation hash only; omitting `uk` leaves the entity hash unchanged)
- [X] T038 [US2] Create `src/Application/Content/ImportPlan.cs`, `ImportPlanBuilder.cs` (manifest → domains/topics with sort order; files → concepts/questions/katas with sort order + hashes; derive `concept.reviewed`), `ImportChangeSet.cs`, `ImportSummary.cs` (per-file and totals: created/updated/unchanged/deprecated per entity type), `ImportOptions.cs` (ContentDir, ValidateOnly, DryRun), `ImportExitCode.cs` (0/1/2/3), `IContentImporter.cs`
- [X] T039 [US2] Create `src/Application/Content/ImportDiff.cs` — pure `Compute(existingSnapshot, plan, now)`: create / update (hash differs or was deprecated → fields set, `UpdatedAt = now`, `DeprecatedAt = null`) / unchanged (**entity object not touched**) / deprecate (absent → `DeprecatedAt = now`) / translations per language, absent languages untouched / existing question with different type → error
- [X] T040 [US2] Create `tests/Application.Tests/Content/ImportDiffTests.cs`: fresh → all created; same → all unchanged and objects identical (reference/props untouched); edited note → one translation update; absent question → deprecated; reappearing → reactivated + counted updated; `uk` omitted → kept; type mismatch → error
- [X] T041 [US2] Create `tests/Application.Tests/Content/RepoContentValidatesTests.cs`: locate the repo `content/` dir (walk up from test output; skip if absent), run schema + rule validators, assert zero errors (replaces the ajv-cli dependency)
- [X] T042 [US2] Verify `dotnet test --filter "Category!=Integration"` green; commit "Import: validation, hashing, diff"

### DB part + CLI (Infrastructure, Api)

- [X] T043 [P] [US2] Create `src/Infrastructure/Content/ContentFileSystem.cs` (discover `<content-dir>/*/*.json` excluding `schema/`, read manifest, parse to `JsonNode` + DTOs) and `src/Infrastructure/Content/ImportLock.cs` (`SELECT pg_try_advisory_xact_lock(7411001)` on the current transaction)
- [X] T044 [US2] Create `src/Infrastructure/Content/ContentImporter.cs` (`IContentImporter`): validate (schema + rules) → return exit 1 with errors; `ValidateOnly` → exit 0; else `BeginTransaction`, lock (false → exit 2), load tracked content graph, `ImportPlanBuilder` + `ImportDiff`, apply change set to tracked entities (never touching progress tables), `SaveChangesAsync`, commit or rollback on `DryRun`; single `now`; catch → exit 3 with logged error; register in `AddInfrastructure`
- [X] T045 [US2] Create `src/Api/Cli/ImportContentCommand.cs`: parse `--content-dir`, `--validate-only`, `--dry-run`; resolve content dir (`--content-dir` → `CONTENT_DIR` → `./content`); build a plain `Host` with `AddInfrastructure`, Serilog Warning+; run `IContentImporter`; print errors to stderr in the contract format and the summary table to stdout; return the exit code; wire the branch in `src/Api/Program.cs`
- [X] T046 [P] [US2] Create `scripts/validate-content.sh` (`dotnet run --project src/Api -c Release -- import-content --validate-only "$@"`, executable) and update `.claude/agents/content-author.md` to prefer it over `ajv-cli` when the solution exists
- [X] T047 [US2] Verify manually: `dotnet run --project src/Api -- import-content` prints summary and exits 0; second run all unchanged; `--validate-only` works with the DB stopped; `docker compose run --rm api import-content` works inside the stack; commit "Import: DB apply + CLI"

### Integration tests (Testcontainers)

- [X] T048 [US2] Create `tests/Infrastructure.Tests/PostgresFixture.cs` (Testcontainers `postgres:16-alpine`, collection fixture, `AppDbContext` factory with migrations applied) and `[Trait("Category","Integration")]` collection definition
- [X] T049 [P] [US2] Create `tests/Infrastructure.Tests/Persistence/MigrationsTests.cs`: `MigrateAsync` on empty DB; second call no-op; `information_schema` has the 12 tables, the unique indexes and CHECK constraints from data-model.md
- [X] T050 [US2] Create `tests/Infrastructure.Tests/Content/ContentImporterTests.cs`: import fixture → counts/ids; re-import → 0 created / 0 updated and identical `updated_at`; edit note + option text → exactly two translation updates, ids unchanged; insert `review_state` + `attempt` for a question, change its prompt, re-import → progress rows byte-identical; remove a question → `deprecated_at` set, progress kept; re-add → cleared; invalid file among valid → zero rows written, exit 1; advisory lock held on another connection → exit 2; `--dry-run` leaves DB unchanged
- [X] T051 [US2] Run `code-reviewer` on the import path; fix findings; verify `dotnet test` (Docker running) green; commit "Import: integration tests"

**Checkpoint**: US2 complete — SC-002, SC-003, SC-004 verifiable.

---

## Phase 5: User Story 3 — Browse the knowledge tree and read a concept (Priority: P2)

**Goal**: two read endpoints per `contracts/knowledge-api.md` with English fallback and deprecation
filtering.

**Independent Test**: After import, tree mirrors manifest + file order with counts; concept page in
`en`/`uk` with fallback flag; unknown or deprecated → 404.

- [X] T052 [P] [US3] Create `src/Application/Knowledge/IKnowledgeQueries.cs`, `KnowledgeTreeDto.cs` (Language, Domains[Slug, Title, SortOrder, DeprecatedAt, Topics[…, Concepts[Slug, Title, TitleFallback, Level, Reviewed, SortOrder, QuestionCount, KataCount, DeprecatedAt]]]), `ConceptPageDto.cs` (Domain/Topic {Slug, Title}, Slug, Level, Reviewed, SortOrder, RequestedLanguage, Language, Fallback, Title, Note, QuestionCount, KataCount, DeprecatedAt)
- [X] T053 [US3] Create `src/Infrastructure/Knowledge/KnowledgeQueries.cs`: tree = three `AsNoTracking` queries (domains; topics; concepts with requested-language and `en` title sub-selects and non-deprecated question/kata count sub-selects, honouring `includeDeprecated`) stitched in memory ordered by `sort_order`; concept = one query with sub-selects; returns null for unknown/deprecated (unless included); register in `AddInfrastructure`
- [X] T054 [P] [US3] Create `src/Api/Endpoints/Knowledge/GetKnowledgeTreeRequest.cs` (Lang default "en", IncludeDeprecated default false) + `GetKnowledgeTreeValidator.cs` (lang ∈ en|uk) + `GetKnowledgeTreeEndpoint.cs` (`GET /knowledge/tree`, `AllowAnonymous`, `Produces<KnowledgeTreeDto>`)
- [X] T055 [P] [US3] Create `src/Api/Endpoints/Knowledge/GetConceptRequest.cs` (route Domain/Topic/Concept + Lang + IncludeDeprecated) + `GetConceptValidator.cs` + `GetConceptEndpoint.cs` (`GET /knowledge/concepts/{domain}/{topic}/{concept}`, 404 via `SendNotFoundAsync`)
- [X] T056 [US3] Create `tests/Api.Tests/ApiFixture.cs` (`WebApplicationFactory<Program>` pointed at a Testcontainers DB via `ConnectionStrings__Default`, runs the importer against `tests/Fixtures/content` once) and `tests/Api.Tests/Knowledge/KnowledgeTreeEndpointTests.cs` (empty before import; after import mirrors ordering, counts, `reviewed` derivation, `includeDeprecated` toggle, `lang=uk` fallback flag; 400 on bad lang), `tests/Api.Tests/Knowledge/ConceptEndpointTests.cs` (`en`, `uk`, fallback marker, deprecated → 404, unknown → 404), `tests/Api.Tests/HealthTests.cs`
- [X] T057 [US3] Run `code-reviewer` on the read path; fix findings; verify `curl` examples from quickstart.md §3, Swagger lists both endpoints, `dotnet test` green; commit "Knowledge read endpoints"

**Checkpoint**: US3 complete — SC-005 verifiable.

---

## Phase 6: User Story 4 — Progress is recorded per learner from day one (Priority: P3)

**Goal**: exactly one seeded learner, idempotent; progress tables exist and are empty; content rows
never reference learners. (Schema and seeder were built in Phase 2; this phase proves them.)

**Independent Test**: after first start exactly one `users` row; after restart still one; progress
tables empty; no content table has a `user_id` column.

- [X] T058 [P] [US4] Create `tests/Infrastructure.Tests/Persistence/LearnerSeederTests.cs`: two runs → exactly one row with `LocalLearner.Id` and `preferred_language = 'en'`
- [X] T059 [P] [US4] Create `tests/Infrastructure.Tests/Persistence/ProgressSchemaTests.cs`: inserting a `review_state` with both FKs null or mismatched `item_type` fails the CHECK; duplicate `(user_id, item_id)` fails the unique index; `item_id` equals the set FK; `sessions` scope CHECK enforced; content tables have no `user_id` column (`information_schema.columns`)
- [X] T060 [US4] Verify `dotnet test` green; commit "Progress schema tests"

**Checkpoint**: US4 complete — Principle IV proven by tests.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [X] T061 [P] Update `README.md` quickstart (copy `.env.example`, `docker compose up`, both import commands, local `PGPASSWORD` inner loop) from `specs/001-foundation/quickstart.md`
- [X] T062 [P] Run the `sync-claude-md` skill to reconcile `CLAUDE.md` (layout table: `src/`, `tests/`, `web/` no longer "planned"; commands; hooks unchanged)
- [X] T063 [P] Create `docs/architecture/decisions/0002-foundation-data-model.md` (surrogate vs natural keys, polymorphic progress key, soft-deprecation, derived `concepts.reviewed`, content hashes, text enums + CHECK, session scope columns)
- [X] T064 [P] Update `memory-bank/activeContext.md`, `progress.md`, `systemPatterns.md`, `techContext.md` (pinned versions) and close `TECH_DEBT.md` item 1.1
- [X] T065 Run `code-reviewer` (whole 001 diff vs `dev` base) and `frontend-reviewer`; fix must-fix findings
- [X] T066 Run quickstart.md end to end from a clean clone (`git clone` into a temp dir, follow README only); record timings for SC-001/SC-002/SC-005 in `specs/001-foundation/quickstart.md`; final commit "001 Foundation complete"

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: no dependencies.
- **Foundational (Phase 2)**: depends on Phase 1; blocks all stories.
- **US1 (Phase 3)**: compose part (T019–T022) right after Phase 2; web part (T023–T029) independent of US2/US3.
- **US2 (Phase 4)**: after Phase 2; pure part (T030–T042) can run in parallel with US1 web work.
- **US3 (Phase 5)**: after Phase 2; tests need US2's importer for fixtures (T056 depends on T044).
- **US4 (Phase 6)**: after Phase 2; independent of US1–US3.
- **Polish (Phase 7)**: after all stories.

### User Story Dependencies

- US1 → none (uses the seeded learner from Phase 2 for the persistence check).
- US2 → none.
- US3 → US2 (fixture import in API tests).
- US4 → none.

### Within each story

Models → services → endpoints/CLI → tests → reviewer → verify → commit.

### Parallel opportunities

- Phase 2: T006/T007/T008 together; T010/T011 together; T016/T017 together.
- US1: T019/T020 together; T025/T026 together; web work (T023–T029) in parallel with US2 pure part.
- US2: T030/T031/T032 together; T037 in parallel with T033–T036; T043/T046 in parallel with T044/T045; T049 in parallel with T050.
- US3: T052 in parallel with T054/T055 (after DTOs exist).
- US4: T058/T059 together, and the whole phase in parallel with US3.
- Polish: T061–T064 together.

---

## Parallel Example: User Story 2 (pure part)

```text
# Fixtures, DTOs and error types have no dependencies — run together:
T030 tests/Fixtures/content/*  |  T031 src/Application/Content/Seed/*  |  T032 ContentValidationError.cs
# Then hasher in parallel with schema validator + its tests:
T037 ContentHasher.cs + tests   |  T033 ContentSchemaValidator.cs → T034 tests
# Then rule validator → diff → repo validation test:
T035 → T036 → T038 → T039 → T040 → T041 → T042
```

---

## Implementation Strategy

### MVP first (User Story 1 + Phase 2)

1. Phases 1–2 (skeleton, schema, host, seeder).
2. Phase 3 compose part → `docker compose up` with postgres + api healthy.
3. Phase 3 web part → three healthy services and "API: OK". **Demo-able MVP.**

### Incremental delivery

4. Phase 4 (import) → content in the database, idempotent, deprecation, progress-safe.
5. Phase 5 (read API) → tree + concept page → feature 003 becomes pure UI.
6. Phase 6 (progress tests) → Principle IV proven.
7. Phase 7 → docs, memory-bank, reviewers, clean-clone run.

### Notes

- Commit after every phase checkpoint; `git commit` asks for confirmation by hook design.
- Never commit `.env`; `.env.example` is the only tracked env file.
- Run `code-reviewer` before backend commits and `frontend-reviewer` before `web/` commits.
