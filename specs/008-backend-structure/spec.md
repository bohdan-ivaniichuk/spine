# Feature Specification: Backend structure refactor

**Feature Branch**: `008-backend-structure`

**Created**: 2026-11-03

**Status**: Draft

**Input**: User description (code review of the backend, 2026-11-03): "Under each endpoint there must be a
separate folder; requests go into separate files, the validator into its own file too. The path in the endpoint
goes into a separate static class split by domains or features, not a string in the endpoint itself. The
Description chain is too long to fit on one screen — split it over a few lines. The database start-up block in
Program.cs should go into a separate file and be injected cleanly so Program.cs does not keep growing. ImportDiff
holds both a class and the logic that manipulates it — split it, in a folder of its own. Some enums too: split the
enum and the code that works with it, but keep them next to each other. Large queries such as the knowledge tree
are very hard to read — use a mapper or split them into smaller queries. Prepare a spec for a global refactor."

## Clarifications

### Session 2026-11-03

- Q: Where do routes live? → A: One route table, `ApiRoutes`, with a nested class per feature (Knowledge, Review,
  Meta…). Endpoints and tests both take their paths from it.
- Q: How is mapping done in read queries? → A: Mapperly (source-generated mappers); queries return named row
  types, never anonymous types across method boundaries.
- Q: How far is the 654-line review sessions service split? → A: One class per use case; each endpoint injects only
  the use case it calls; tests are re-pointed to the new classes.
- Q: File layout rule? → A: Strict: one top-level type per file across Domain, Application, Infrastructure and Api;
  an enum and its helpers sit side by side as `Level.cs` and `LevelExtensions.cs`.
- Q: Does anything visible change? → A: No. HTTP contracts, JSON payloads, status codes, database schema, the
  import CLI and start-up behaviour stay exactly as they are.

## User Scenarios & Testing *(mandatory)*

The "users" of this feature are the people who read and change the backend: the maintainer today and community
contributors once Spine is open to them. Learners must notice nothing.

### User Story 1 - Find and read any endpoint in seconds (Priority: P1)

A maintainer opens the Api project and finds every endpoint in a folder named after it, holding the endpoint, its
request and its validator as separate short files. The route is a named entry in one route table, and the
endpoint's metadata reads one call per line.

**Why this priority**: Endpoints are the first thing a contributor touches; today a single file mixes three types,
a string route and a 200-character metadata line.

**Independent Test**: Pick any endpoint; its folder contains only its own files; its route comes from the route
table; every request in the API test suite is built from the same table and still passes.

**Acceptance Scenarios**:

1. **Given** the Api project, **When** a maintainer looks for the concept page endpoint, **Then** they find a folder with the endpoint, request and validator files and nothing else.
2. **Given** the route table, **When** the API tests build their URLs from it, **Then** every existing API scenario passes with identical responses.
3. **Given** any endpoint's metadata, **When** it is read in an editor 120 characters wide, **Then** no line is wider and each produced status sits on its own line.
4. **Given** a new endpoint written with a string route or with its request in the endpoint file, **When** the architecture tests run, **Then** they fail and name the file.

---

### User Story 2 - A Program.cs that only wires things up (Priority: P1)

The start-up file composes services and the pipeline; preparing the database (migrate within the timeout, seed
the local learner) is a separate injectable component it calls once.

**Why this priority**: Program.cs is where every cross-cutting change lands; keeping it to wiring stops it from
becoming the place where logic hides.

**Independent Test**: Start the API on an empty database: it migrates and seeds as before. Start it with the
database unreachable: it logs a critical error and exits with code 1 as before.

**Acceptance Scenarios**:

1. **Given** an empty database, **When** the API starts, **Then** all migrations are applied and the local learner exists, exactly as before.
2. **Given** an unreachable database, **When** the start-up timeout passes, **Then** the process logs a critical error and exits with code 1 without serving requests.
3. **Given** a stop signal during start-up, **When** the database is still being prepared, **Then** preparation is cancelled and the process stops.

---

### User Story 3 - One class per review use case (Priority: P2)

Starting a session, starting a weak-spots drill, reading the current or a given session, answering, finishing and
the overview are each a separate class with its own folder; each endpoint depends only on the use case it serves.

**Why this priority**: The single 654-line service is the hardest file in the codebase to change safely.

**Independent Test**: The full review test suite (unit, integration, API) passes against the new classes with
unchanged assertions; locking and transactions behave as before.

**Acceptance Scenarios**:

1. **Given** the review module, **When** a maintainer opens the answer use case, **Then** it contains only the answering logic and the collaborators it needs.
2. **Given** concurrent start requests, **When** they race, **Then** exactly one session stays open, as today.
3. **Given** every review endpoint, **When** its dependencies are listed, **Then** it depends on one use case, not on the whole review service.

---

### User Story 4 - Read queries that read like prose (Priority: P2)

Large read queries are split into small, named steps that return named row types, and a generated mapper turns
rows into the response objects.

**Why this priority**: Anonymous projections with a dozen fields make the knowledge tree and statistics queries
hard to review and easy to break.

**Independent Test**: For a seeded database, every read endpoint returns byte-identical JSON before and after, with
no more database round trips than before.

**Acceptance Scenarios**:

1. **Given** the knowledge tree query, **When** a maintainer reads it, **Then** it is a short sequence of named steps (load domains, load topics, load concepts, assemble) with no anonymous types crossing method boundaries.
2. **Given** a seeded database, **When** the tree, concept, overview, coverage, readiness, weak-spots and activity endpoints are called, **Then** their JSON matches the pre-refactor responses exactly.
3. **Given** any refactored read query, **When** it runs, **Then** it issues no more SQL round trips than before.

---

### User Story 5 - One type per file, everywhere (Priority: P3)

Every source file holds one top-level type. Enums keep their helpers in a sibling file. Multi-type files (review and
knowledge DTO bundles, the import change set and diff, seed file models, EF configurations, request base classes)
are split into feature folders. Architecture tests keep it that way.

**Why this priority**: It makes every other rule cheap to follow, but carries no risk on its own, so it comes last.

**Independent Test**: The architecture tests pass; a file with two top-level types makes them fail.

**Acceptance Scenarios**:

1. **Given** the `Level` enum, **When** a maintainer opens its folder, **Then** `Level.cs` holds the enum and `LevelExtensions.cs` beside it holds the parsing and formatting.
2. **Given** the import diff, **When** a maintainer opens its folder, **Then** the change set and the diff algorithm are separate files.
3. **Given** a source file with two top-level types, **When** the architecture tests run, **Then** they fail naming the file.

---

### Edge Cases

- Namespaces change with folders; JSON property names come from the DTO members, not namespaces, so payloads must
  not change — the response snapshots guard it.
- FastEndpoints discovers endpoints by type, not by folder; moving files must not drop or duplicate an endpoint.
- Validators are discovered by request type; a validator that loses its pairing would silently stop validating —
  every existing 400 scenario must still return 400.
- Generated mappers must not map members by accident: unmapped or ambiguous members fail the build.
- The `import-content` CLI shares the infrastructure; its output and exit codes must not change.
- Tests may change namespaces, construction of the classes under test and URL building, never their assertions.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Each HTTP endpoint MUST live in its own folder holding the endpoint, its request and its validator as separate files.
- **FR-002**: Every endpoint route MUST come from one route table grouped by feature, and API tests MUST build their URLs from the same table.
- **FR-003**: Endpoint metadata MUST be written one call per line, with no source line in the Api project longer than 120 characters.
- **FR-004**: Database preparation at start-up (migrate within the configured timeout, then seed the local learner) MUST be a separate injectable component; on failure the process MUST log a critical error and exit with code 1, and a stop signal MUST cancel it.
- **FR-005**: Each review use case (overview, coverage, start, start weak, get current, get by id, answer, finish) MUST be its own class, and each endpoint MUST depend only on the use case it calls.
- **FR-006**: Read queries (knowledge tree, concept page, overview, coverage, readiness, weak spots, activity) MUST be composed of named steps that return named row types, with response objects built by generated mappers.
- **FR-007**: For the same data, every endpoint MUST return the same status and byte-identical JSON as before, and no request MUST make more database round trips than before.
- **FR-008**: Every source file in the backend MUST declare exactly one top-level type; an enum's helpers MUST live in a sibling `<Enum>Extensions` file.
- **FR-009**: Automated architecture tests MUST fail when FR-001, FR-002, FR-003 or FR-008 is violated, or when a layer references a higher one.
- **FR-010**: The conventions MUST be documented in an ADR and in the project guide so new code follows them.

### Key Entities

- **Route table**: one static class with a nested class per feature; each entry is a route template.
- **Use case**: one class per review operation with the collaborators it needs; replaces the single review service.
- **Row type**: a named record a query step returns; mapped to a response object by a generated mapper.
- **Database initializer**: the component that prepares the database before the API serves requests.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Every existing automated test passes with unchanged assertions after the refactor.
- **SC-002**: Response snapshots of all read endpoints for a seeded database are identical before and after.
- **SC-003**: No backend source file holds more than one top-level type, and no source file is longer than 250 lines
  (the largest is 654 today).
- **SC-004**: Program.cs is under 50 lines and contains no database or business logic.
- **SC-005**: A maintainer locates any endpoint's route, request and validator in under 10 seconds from the route
  table or the folder tree.

## Assumptions

- The web app, content files, database schema and docker setup are out of scope.
- Mapperly is added as a new package (a protected build file, so the change goes through `lockflight propose`).
- Behaviour-preserving refactor: red/green applies to the new seams (route table, database initializer, mappers,
  architecture rules); the existing suite is the regression net for everything else.
- Test classes may be renamed only where the class under test is renamed; test method names stay.
