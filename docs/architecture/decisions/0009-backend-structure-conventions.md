# 9. Backend structure conventions

- **Status:** Accepted
- **Date:** 2026-11-03
- **Feature:** [008 Backend structure](../../../specs/008-backend-structure/spec.md)

## Context

A code review of the backend found files that were hard to navigate: endpoints with their request and
validator types inline and route strings repeated in tests, a `Program.cs` that held the database start-up,
one `ReviewSessions` service with eight operations, large queries with anonymous projections assembled by
hand, and files that declared up to twenty types. Feature 008 restructures the code without changing the
HTTP contract.

## Decision

**Endpoints.** One folder per endpoint, `src/Api/Endpoints/<Feature>/<Name>/`, holding `{Name}Endpoint.cs`,
`{Name}Request.cs`, `{Name}Validator.cs` (and a `*Response.cs` when the endpoint owns its response).
Shared query shapes and rules live in `Endpoints/Shared/` and `Endpoints/<Feature>/Shared/`. Routes are
constants in one `ApiRoutes` class with a nested class per feature; `ApiRoutes.All` lists every endpoint
route, and API tests build URLs from it (`ApiUrl.Prefix + ApiRoutes.…`, `ApiUrl.For(route, ("id", …))`).
Fluent configuration chains put one call per line; lines in `src/Api` are at most 120 characters.

**Start-up.** `Program.cs` is wiring only. `IDatabaseInitializer` (Infrastructure, `Persistence/Startup/`)
migrates with a timeout and seeds the learner; a failure is logged as critical and the process exits 1.

**Use cases.** One class per review use case: the interface `I<UseCase>UseCase` in
`Application/Review/<UseCase>/` next to its DTOs, the EF implementation `<UseCase>UseCase` in
`Infrastructure/Review/<UseCase>/`. The `UseCase` suffix exists because a type must not share its
namespace's last segment. Shared session collaborators (`ReviewScopeResolver`, `SessionScopeDescriber`,
`SessionProjector`, `SessionStarter`, `SessionLocks`) sit in `Infrastructure/Review/Sessions/`;
`AddReview()` registers the module. No mediator library.

**Reads.** Queries project into named row records and a Riok.Mapperly mapper builds the DTO
(`KnowledgeTreeMapper`, `ConceptPageMapper`, `ReviewCoverageMapper`, `ReviewOverviewMapper`). Mappers take the
request language through their constructor; enum wire values and fallbacks are explicit `Use` methods,
because Mapperly maps enums by name by default. `RequiredMappingStrategy.Target` keeps unmapped source
members (ids, the other language's title) from failing the build. Large queries are split into named steps
(`LoadDomainsAsync`, `CountByTopicAsync`, …).

**Files.** Every `src` file declares exactly one top-level type, named like the file (`Program.cs` excepted;
EF migrations are generated and excluded). An enum's helpers live in `<Enum>Extensions.cs` beside it. No
`src` file exceeds 250 lines. Folders map to namespaces: `Content/{Import,Plan,Diff,Summary,Seed,Validation}`,
`Review/{Shared,Sessions,Readiness,WeakSpots,Activity,<UseCase>}`, `Knowledge/{Tree,Concept}`,
`Persistence/Configurations/{Content,Progress}`.

**Enforcement.** `tests/Spine.ArchitectureTests` checks layering, the endpoint folder layout, route literals,
Api line length and the source-file rules (a small comment- and string-aware scanner, `SourceLayout`).
Response snapshots in `tests/Api.Tests/Snapshots` pin every endpoint's status, body and SQL command count,
so a refactor that changes the contract fails.

## Consequences

- Adding an endpoint means a new folder, a route constant (and an `ApiRoutes.All` entry) and a validator;
  the architecture tests name what is missing.
- Structural rules cannot fail through production code, so in a Lockflight red/green cycle they count as
  already green; they were added after the last cycle, and FR-001, FR-003, FR-008 and FR-009 are verified by
  the architecture tests rather than by a red → green trace. FR-007 is verified by the snapshots.
- Mapperly is a source generator (compile time only, no runtime dependency).
