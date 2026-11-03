# Implementation Plan: Backend structure refactor

**Branch**: `008-backend-structure` | **Date**: 2026-11-03 | **Spec**: [spec.md](spec.md)

**Input**: approved spec; strategy from the `solution-architect` agent (2026-11-03); Mapperly 4.3.1 checked on NuGet
and in its docs (context7).

## Summary

Restructure the backend without changing anything a client sees. Endpoints move into one folder each with separate
endpoint, request and validator files and take their paths from `ApiRoutes`; Program.cs only wires, and a
`DatabaseInitializer` prepares the database; the 654-line review service becomes eight use-case classes plus four
shared collaborators; read queries return named rows that Mapperly maps to DTOs after loading; every source file
holds one top-level type. Response snapshots, SQL round-trip counts and new architecture tests prove that nothing
changed and keep it that way.

## Technical Context

**Language/Version**: C# 14 / .NET 10

**Primary Dependencies**: FastEndpoints 8, EF Core 10 + Npgsql (existing); **Riok.Mapperly 4.3.1** (new, build-time
source generator, `PrivateAssets="all"`, no runtime dependency).

**Storage**: PostgreSQL — no schema change, no migration; `AppDbContext` and `Persistence/Migrations` do not move.

**Testing**: xUnit + Shouldly; new response-snapshot tests with a SQL command-counting interceptor (`Api.Tests`); new
`tests/Spine.ArchitectureTests` (reflection + source scans, no package); mapper unit tests (`Application.Tests`);
initializer and use-case integration tests (`Infrastructure.Tests`).

**Target Platform**: unchanged (Docker Compose, GitHub Actions).

**Project Type**: web service (the web app is out of scope).

**Constraints**: byte-identical JSON and no extra SQL round trips per request (FR-007); identical start-up behaviour
(exit code 1, same log message, same `DB_STARTUP_TIMEOUT_SECONDS`).

**Scale/Scope**: ~110 backend source files touched or moved; 14 endpoints; 8 review use cases.

## Decisions

| # | Decision | Source |
|---|---|---|
| D1 | `ApiRoutes` static class with nested `Knowledge`, `Review`, `Meta`, `Health`, a `Prefix` and an `All` list. | user |
| D2 | Mapperly maps loaded rows to DTOs; the EF `Select` into a row stays hand-written (language-dependent subqueries). | user + research |
| D3 | One class per review use case with a `UseCase` suffix (no type may share its namespace's last segment); interfaces in Application, implementations in Infrastructure; shared `ReviewScopeResolver`, `SessionScopeDescriber`, `SessionProjector`, `SessionStarter`, `SessionLocks`. | user + architect |
| D4 | Strict one top-level type per file in `src/`; enum + `<Enum>Extensions` siblings; 250-line cap for `src/` only. | user |
| D5 | The start endpoint injects both start use cases (it routes on `mode`). | default |
| D6 | Tests reach the use cases through a test-side `ReviewSessionsHarness` that keeps the old method names, so ~170 call sites stay. | default |
| D7 | `DatabaseInitializer` is an explicit call returning `bool`, not a hosted service (keeps exit code 1). The critical log's category becomes `DatabaseInitializer`; the message is unchanged. | default |
| D8 | Readiness, weak spots, activity and session projection get named rows but no Mapperly mapper: they are calculations, not mappings. | architect |
| D9 | Snapshot and architecture tests are recorded before `redgreen start` and are not part of red/green cycles; red/green applies to the new seams (initializer, `ApiRoutes.All`, use cases, mappers). | default |

Details and alternatives: [research.md](research.md).

## Constitution Check

| Principle | Status | Note |
|---|---|---|
| I–III content, data, languages | PASS | content and payloads untouched |
| IV user-scoped progress | PASS | no `user_id` or schema change |
| V scheduling interface | PASS | `ISchedulingAlgorithm` untouched; answer use case still calls it |
| VI LLM | PASS | none |
| VII boring architecture | PASS | plain classes and DI; no mediator or handler pipeline; Mapperly is compile-time only (ADR 0009) |
| VIII Angular | PASS | web untouched |
| IX one command | PASS | no new service or env key |
| X tests where they pay | PASS | snapshots + SQL counts guard behaviour; architecture tests guard structure |

No violations. Re-checked after design: unchanged.

## Project Structure

### Documentation (this feature)

```text
specs/008-backend-structure/
├── spec.md, plan.md, research.md, quickstart.md
├── checklists/requirements.md
└── tasks.md
```

### Source Code (target, abridged)

```text
src/Api/Program.cs                                  < 50 lines: wiring only
src/Api/Startup/{LoggingSetup,SwaggerSetup,FastEndpointsSetup,DatabaseStartup}.cs
src/Api/Endpoints/ApiRoutes.cs
src/Api/Endpoints/Shared/{LanguageQuery,LanguageRules,LanguageQueryValidator}.cs
src/Api/Endpoints/<Feature>/Shared/…                 KnowledgeQuery; ScopeQuery, ScopeRules
src/Api/Endpoints/<Feature>/<Name>/{<Name>Endpoint,<Name>Request,<Name>Validator}.cs   (14 endpoints)

src/Application/Knowledge/{Tree,Concept}/…          DTOs, rows, Mapperly mappers
src/Application/Review/Shared/…                     ReviewScope, ReviewError, ReviewException, ReviewOptions, …
src/Application/Review/<UseCase>/I<UseCase>UseCase.cs + its DTOs   (8 use cases)
src/Application/Review/{Readiness,WeakSpots,Activity}/…
src/Application/Content/{Import,Plan,Diff,Summary,Seed,Validation}/…   one type per file; ImportDiff split

src/Infrastructure/Persistence/Startup/{IDatabaseInitializer,DatabaseInitializer,DatabaseStartupOptions,StartupMigrator,LearnerSeeder}.cs
src/Infrastructure/Persistence/Configurations/{Content,Progress}/<Entity>Configuration.cs
src/Infrastructure/Review/Sessions/{ReviewScopeResolver,SessionScopeDescriber,SessionProjector,SessionStarter,SessionLocks,…}.cs
src/Infrastructure/Review/<UseCase>/<UseCase>UseCase.cs
src/Infrastructure/Review/ReviewServiceCollectionExtensions.cs

src/Domain/**/<Enum>.cs + <Enum>Extensions.cs

tests/Api.Tests/Snapshots/{SnapshotFixture,CommandCountingInterceptor,SnapshotTests}.cs + cases/*.txt
tests/Spine.ArchitectureTests/{LayerDependencyTests,EndpointLayoutTests,SourceFileRulesTests,RouteLiteralTests}.cs
tests/Infrastructure.Tests/Review/ReviewSessionsHarness.cs
```

### Declared signatures (stubs for red)

```csharp
// Infrastructure/Persistence/Startup
public interface IDatabaseInitializer { Task<bool> InitializeAsync(CancellationToken ct); }
public sealed class DatabaseInitializer(StartupMigrator migrator, LearnerSeeder seeder,
    IOptions<DatabaseStartupOptions> options, ILogger<DatabaseInitializer> logger) : IDatabaseInitializer;
// Api/Endpoints
public static class ApiRoutes { public const string Prefix = "api"; public static IReadOnlyList<string> All { get; } }
// Application/Review/<UseCase> — one ExecuteAsync each, e.g.
public interface IAnswerQuestionUseCase
{ Task<AnswerFeedbackDto> ExecuteAsync(Guid sessionId, string questionId, int selectedIndex, Language language, CancellationToken ct); }
// Application/Knowledge/Tree
[Mapper] public sealed partial class KnowledgeTreeMapper(Language language);
```

**Structure Decision**: feature folders inside each layer, layering unchanged (Domain ← Application ←
Infrastructure ← Api).

## Steps (app green after each)

0. Safety net before `redgreen start`: response snapshots + SQL counts recorded on today's code; protected-file batch
   (Mapperly, architecture-test project, solution, `.editorconfig` RMG severities) via `lockflight propose`;
   architecture-test project with the layering rule.
1. **US2** initializer (red/green), Program.cs to wiring.
2. **US1** `ApiRoutes` + route-table test (red/green), then one endpoint per commit; Api architecture rules.
3. **US3** use-case classes (red/green on one smoke test per use case), collaborators, harness, delete the old service.
4. **US4** mappers (red/green on mapper unit tests), one query per commit, snapshots after each.
5. **US5** one type per file across `src/`, enum pairs, `ImportDiff` split; FR-008 rule; ADR 0009, CLAUDE.md, memory-bank.
