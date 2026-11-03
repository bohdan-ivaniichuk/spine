# Research: Backend structure refactor

## R1 — Mapperly

- **Version**: Riok.Mapperly 4.3.1 is the latest stable (5.0 is in `-next` preview). Installed with
  `ExcludeAssets="runtime" PrivateAssets="all"` — generated code only, no runtime dependency.
- **Strictness**: `[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Both)]` plus the unmapped-member
  diagnostics raised to error in `.editorconfig`; `TreatWarningsAsErrors` is already on.
- **Traps**: enums map to strings by *name* by default ("Mid", not "M") — every enum target needs an explicit
  `Use` method; `?? slug` fallbacks must be explicit methods too.
- **Not used for IQueryable projection**: the row projections contain language-dependent subqueries
  (`Translations.Where(t => t.Language == language)`), so the EF `Select` stays hand-written and Mapperly maps the
  loaded rows. SQL and round-trip counts therefore do not change.
- To confirm while implementing (context7): additional mapping parameters and instance mappers with constructor
  state in 4.x.
- Sources: <https://mapperly.riok.app/docs/getting-started/installation/>,
  <https://mapperly.riok.app/docs/configuration/mapper/>.

## R2 — Row types and EF translation

- A row that later LINQ operators build on must be a class with `init` properties (EF cannot translate member
  access through constructor parameters); positional records only for final projections.
- Anonymous types inside a single LINQ expression may stay; the spec bans them across method boundaries only.

## R3 — Review service split

- Eight use cases: overview, coverage, start, start weak, get current, get by id, answer, finish. Shared
  collaborators: `ReviewScopeResolver`, `SessionScopeDescriber`, `SessionProjector`, `SessionStarter` (today's
  `StartCoreAsync`, same order of bounds check → resolve → execution strategy → clear → transaction → advisory lock →
  `FOR UPDATE` → plan → close → insert → commit, projection after the strategy), `SessionLocks` (identical SQL), and
  `McqAnswerJson`.
- `UseCase` suffix: a type named like its namespace's last segment makes C# name resolution ambiguous
  (`Spine.Domain.Content.Domain` already shows it).
- **Alternative rejected**: a mediator/handler library — adds indirection Principle VII forbids without need.

## R4 — Database initializer

- Explicit `InitializeAsync` returning `bool`, called from Program.cs; failure logs the same critical message and
  the process returns 1. A hosted service would turn failure into an unhandled-exception exit code.
- `ApplicationStopping` may not fire before `RunAsync`; today a stop signal during start-up kills the process, and
  that is preserved.

## R5 — Proving "nothing changed"

- **Response snapshots**: a dedicated fixture (own container, pinned `FakeTimeProvider`, `tests/Fixtures/api-content`)
  scripts progress through the API, then calls every endpoint and variant; GUIDs normalised in order of appearance,
  `traceId` masked; cases stored as `status/location/sql` header + raw body under
  `tests/Api.Tests/Snapshots/cases/`. Recorded on today's code and committed first; `SPINE_SNAPSHOTS=record`
  re-records.
- **SQL counts**: a `DbCommandInterceptor` counts commands per request; each case asserts `≤` the recorded count.
- **Alternative rejected**: Verify — another package for ~80 lines of helper.

## R6 — Architecture tests

- Project `tests/Spine.ArchitectureTests` — the name `lockflight init --architecture-tests` scaffolds, so the engine
  recognises it.
- Plain reflection (layering via referenced assemblies; endpoint ↔ request ↔ validator in one namespace, exactly
  one validator per request) and source scans (one top-level type per file, `<Enum>Extensions` siblings, 250-line
  cap in `src/`, 120-character lines in Api, no route literals in endpoints or `"/api/` literals in Api tests).
- These tests do not call production members, so they cannot be a Lockflight RED_ASSERT; they are added before
  `redgreen start` (structural rules turned on per phase as the code is moved). Watching how the engine treats this
  is part of the experiment.
- **Alternative**: NetArchTest.eNhancedEdition — fallback if type-level dependency messages are needed.
