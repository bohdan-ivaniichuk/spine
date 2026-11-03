# Tasks: Backend structure refactor

**Input**: `specs/008-backend-structure/` — spec.md, plan.md, research.md, quickstart.md.

**Tests (Lockflight)**: mandatory and first. Within each story: stub declarations (bodies
`throw new NotImplementedException()`), then tests tagged `[Trait("Requirement", "FR-xxx")]`, then the move/refactor.
Red/green covers the new seams; the response snapshots and the architecture rules are recorded in Setup, before
`lockflight redgreen start`, and must stay green afterwards. Existing tests keep their assertions; only `using`,
construction and URL-building lines may change. Use `git mv` for moved files.

## Phase 1: Setup (before `redgreen start`)

- [ ] T001 Response-snapshot safety net in `tests/Api.Tests/Snapshots/` (`SnapshotFixture`, `CommandCountingInterceptor`, `SnapshotTests`): scripted progress with a pinned clock, every endpoint and variant, GUID/traceId normalisation, SQL count ≤ recorded; record `cases/*.txt` on today's code and commit
- [ ] T002 Protected-file batch via `lockflight propose` for the user to apply: `Riok.Mapperly` 4.3.1 in `Directory.Packages.props` and `src/Application/Spine.Application.csproj` (`PrivateAssets="all"`), new `tests/Spine.ArchitectureTests/Spine.ArchitectureTests.csproj`, `Spine.slnx`, `.editorconfig` Mapperly diagnostics as errors
- [ ] T003 `tests/Spine.ArchitectureTests/LayerDependencyTests.cs` (layering) green on today's code; scaffold `EndpointLayoutTests`, `SourceFileRulesTests`, `RouteLiteralTests` with their rules switched off until their phase

---

## Phase 2: User Story 2 - A Program.cs that only wires things up (P1)

**Goal**: database preparation is an injectable component; Program.cs is wiring only.

**Independent Test**: empty DB → migrated and seeded; unreachable DB → critical log and exit code 1.

- [ ] T004 [US2] (FR-004) Declare stubs `IDatabaseInitializer`, `DatabaseInitializer`, `DatabaseStartupOptions` in `src/Infrastructure/Persistence/Startup/` (bodies throwing) and register them

### Tests for User Story 2

- [ ] T005 [US2] (FR-004) Integration tests in `tests/Infrastructure.Tests/Persistence/DatabaseInitializerTests.cs`: empty database → migrations applied, local learner seeded, returns true; unreachable host with a 1 s timeout → returns false and logs the critical message; a cancelled token stops preparation

### Implementation for User Story 2

- [ ] T006 [US2] (FR-004) Implement `DatabaseInitializer` (migrate within `DB_STARTUP_TIMEOUT_SECONDS`, seed, catch, log, false); move `StartupMigrator` and `LearnerSeeder` into `Persistence/Startup/`
- [ ] T007 [US2] (FR-004) `src/Api/Startup/{LoggingSetup,SwaggerSetup,FastEndpointsSetup,DatabaseStartup}.cs`; Program.cs under 50 lines with `if (!await app.InitializeDatabaseAsync()) return 1;`; verify with compose (healthy; exit 1 with postgres stopped)

**Checkpoint**: start-up identical, Program.cs wiring only.

---

## Phase 3: User Story 1 - Find and read any endpoint in seconds (P1)

**Goal**: one folder per endpoint, routes from `ApiRoutes`, metadata one call per line.

**Independent Test**: every API scenario passes with URLs built from `ApiRoutes`; snapshots unchanged.

- [ ] T008 [US1] (FR-002) Declare the stub `ApiRoutes` in `src/Api/Endpoints/ApiRoutes.cs` (`Prefix`, nested `Knowledge`, `Review`, `Meta`, `Health` templates, `All` throwing)

### Tests for User Story 1

- [ ] T009 [US1] (FR-002) Tests in `tests/Api.Tests/Routing/ApiRoutesTests.cs`: every registered endpoint route equals `"/" + ApiRoutes.Prefix + entry` for exactly one `ApiRoutes.All` entry, and every entry is served by exactly one endpoint

### Implementation for User Story 1

- [ ] T010 [US1] (FR-002) Implement `ApiRoutes.All`; point `Health` at `ApiRoutes.Health`; add the test-side `ApiUrl.For(route, …)` helper and build every Api test URL from `ApiRoutes`
- [ ] T011 [US1] (FR-001, FR-003) Move the 14 endpoints, one commit each, to `src/Api/Endpoints/<Feature>/<Name>/{Endpoint,Request,Validator}.cs`; shared bases to `Endpoints/Shared` and `Endpoints/<Feature>/Shared`; routes from `ApiRoutes`; `Description` one call per line, long `Summary` strings wrapped
- [ ] T012 [US1] (FR-001, FR-002, FR-003, FR-009) Switch on `EndpointLayoutTests` (request + exactly one validator per endpoint namespace, folder holds only its files), `RouteLiteralTests` and the 120-character rule for `src/Api`

**Checkpoint**: endpoints restructured, snapshots unchanged.

---

## Phase 4: User Story 3 - One class per review use case (P2)

**Goal**: eight use cases replace the review service; endpoints depend on one use case.

**Independent Test**: review suites pass unchanged through the harness; snapshots unchanged.

- [ ] T013 [US3] (FR-005) Declare stubs: the eight `I<UseCase>UseCase` interfaces in `src/Application/Review/<UseCase>/` and their `<UseCase>UseCase` classes in `src/Infrastructure/Review/<UseCase>/` (`ExecuteAsync` throwing), `AddReview()` registration

### Tests for User Story 3

- [ ] T014 [US3] (FR-005) Integration tests in `tests/Infrastructure.Tests/Review/UseCases/`: one contract scenario per use case calling its `ExecuteAsync` directly (overview counts, coverage rows, start, start weak, current, get by id, answer feedback, finish summary)

### Implementation for User Story 3

- [ ] T015 [US3] (FR-005) Extract `SessionLocks`, `McqAnswerJson`, `ReviewScopeResolver`, `SessionScopeDescriber`, `SessionProjector`, `SessionStarter` from `ReviewSessions` (same SQL, same order inside the execution strategy)
- [ ] T016 [US3] (FR-005) Implement the eight use cases on the collaborators; point each endpoint at its use case (the start endpoint at both start use cases)
- [ ] T017 [US3] (FR-005) Delete `IReviewSessions` and `ReviewSessions` and move the review DTOs into the use-case folders; existing suites reach the use cases through a `ReviewSessionsHarness` that keeps the old method names, and `IndexNames` replaces `ReviewSessions.SessionItemIndex`

**Checkpoint**: no review file above 250 lines; snapshots unchanged.

---

## Phase 5: User Story 4 - Read queries that read like prose (P2)

**Goal**: named query steps and rows; Mapperly builds DTOs after loading.

**Independent Test**: snapshots identical, SQL counts ≤ recorded.

- [ ] T018 [US4] (FR-006) Declare row types and mapper stubs in `src/Application/Knowledge/{Tree,Concept}/` and `src/Application/Review/{GetReviewOverview,GetReviewCoverage}/` (`KnowledgeTreeMapper`, `ConceptPageMapper`, `ReviewCoverageMapper`, `ReviewOverviewMapper`, methods throwing)

### Tests for User Story 4

- [ ] T019 [P] [US4] (FR-006, FR-007) Unit tests in `tests/Application.Tests/Knowledge/` and `tests/Application.Tests/Review/` for the four mappers: requested-language title, English fallback flag, `Level` wire value ("M"/"S"/"L"), missing translations, counts and child ordering preserved

### Implementation for User Story 4

- [ ] T020 [US4] (FR-006, FR-007) Replace the stubs with Mapperly `[Mapper]` partial classes (required mapping `Both`, explicit `Use` for enums and fallbacks)
- [ ] T021 [US4] (FR-006, FR-007) One query per commit: knowledge tree, concept page, coverage, overview → named steps returning rows, mapped by the mappers; readiness and weak spots → named rows, hand-written assembly; snapshots and SQL counts after each

**Checkpoint**: queries readable, behaviour identical.

---

## Phase 6: User Story 5 - One type per file, everywhere (P3)

**Goal**: one top-level type per file across `src`; conventions documented.

**Independent Test**: architecture tests pass; a two-type file fails them.

- [ ] T022 [US5] (FR-008) Declare the stub `SourceLayout.TopLevelTypes(string source)` in `tests/Spine.ArchitectureTests/` support code (throwing) used by the source-file rules

### Tests for User Story 5

- [ ] T023 [US5] (FR-008, FR-009) Tests in `tests/Spine.ArchitectureTests/SourceFileRulesTests.cs`: one top-level type per `src` file (Migrations excluded), enum helpers only in `<Enum>Extensions.cs` beside the enum, no `src` file above 250 lines; plus fixture strings proving the scanner counts types correctly

### Implementation for User Story 5

- [ ] T024 [US5] (FR-008) Implement the scanner; split the seven Domain enums into `<Enum>.cs` + `<Enum>Extensions.cs` and `ConceptReadiness`
- [ ] T025 [US5] (FR-008) Split Application Content into `Import`, `Plan`, `Diff` (change set and diff algorithm apart, `ImportDiff.Compute` unchanged), `Summary`, `Seed`, `Validation`; split the remaining multi-type files and the EF configurations into `Configurations/{Content,Progress}/`; `dotnet ef migrations has-pending-model-changes` shows none
- [ ] T026 [US5] (FR-010) ADR `docs/architecture/decisions/0009-backend-structure-conventions.md`; update the CLAUDE.md layout table and `memory-bank/systemPatterns.md`

**Checkpoint**: all rules on, all green.

---

## Phase 7: Polish

- [ ] T027 `code-reviewer`; quickstart walkthrough (compose start, exit code 1, web smoke)
- [ ] T028 `lockflight trace`, `lockflight gate run --profile pr --base main`, `lockflight ci --no-review-expected`, `lockflight review`
