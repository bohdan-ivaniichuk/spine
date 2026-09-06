# Research — 001 Foundation

Decisions with rationale and alternatives. Versions verified 2026-09-06 (see
`docs/architecture/STACK.md`); pin exact versions in `Directory.Packages.props` after a context7 check.

## R1. JSON Schema validation library

- **Decision**: `JsonSchema.Net` (json-everything).
- **Rationale**: full draft 2020-12 (`if/then`, `not`, `$defs`, `uniqueItems`, `unevaluated*`) and
  list/hierarchical output with `instanceLocation` (JSON pointer) + `keywordLocation` + per-keyword
  message — exactly what FR-010 needs. Schemas are loaded from `<content-dir>/schema/*.json` at run
  time so the committed contract remains the single source of truth.
- **Alternatives**: NJsonSchema (partial 2020-12, weaker error locations); `ajv` via Node (second
  runtime in the API image). Rejected.

## R2. Primary keys

- **Decision**: surrogate `int identity` for `domains`/`topics`/`concepts` (slugs unique within
  parent); **natural `text` PK** = content id for `questions`/`katas`.
- **Rationale**: FR-009 makes question/kata ids immutable and globally unique; progress rows then carry
  the human-readable id and survive a DB rebuild + re-import. Domain/topic/concept slugs are
  parent-scoped, so a natural key would be a composite repeated in every child FK.
- **Alternatives**: int everywhere (progress references opaque ints); dotted natural keys everywhere
  (composite FKs). Rejected.

## R3. Polymorphic key for `review_states` / `attempts`

- **Decision**: `item_type text` + two nullable FKs `question_id`, `kata_id` + CHECK "exactly one set
  and matches `item_type`" + stored generated column `item_id text GENERATED ALWAYS AS
  (COALESCE(question_id, kata_id)) STORED`; UNIQUE `(user_id, item_id)` on `review_states`.
- **Rationale**: real referential integrity with plain FKs while still exposing the spec's literal key
  (learner, item type, item id). Soft-deprecation guarantees content rows are never deleted, so
  `ON DELETE RESTRICT` never blocks an import.
- **Alternatives**: bare `item_id text` + CHECK, no FK (orphan progress one typo away); a
  `content_items` supertype table (extra table + extra insert per question, more import ceremony).

## R4. Change detection

- **Decision**: `content_hash char(64)` (SHA-256 over a canonical STJ serialization with fixed property
  order) on all five content entities and the three translation tables; entity hash covers scalar
  fields only, translation hash per language; `updated_at` moves only when the hash changes; unchanged
  entities are never assigned to (no EF UPDATE).
- **Risk**: jsonb/array properties without a `ValueComparer` are always flagged modified — every
  converted property gets a comparer, and `ImportDiffTests` asserts unchanged entities are untouched.

## R5. Deprecation and reactivation

- **Decision**: `deprecated_at timestamptz NULL` on domains, topics, concepts, questions, katas. Import
  always processes the whole content dir (no per-file mode), sweeps absent items to deprecated, clears
  the flag on reappearance. Reads exclude deprecated by default (`includeDeprecated=true` to include).
- **Rationale**: spec clarification 1; whole-directory import keeps the sweep correct.

## R6. Naming, enums, timestamps

- **Decision**: `EFCore.NamingConventions` `UseSnakeCaseNamingConvention()`; enums stored as `text`
  with CHECK constraints (not PostgreSQL enum types — adding a value is a plain constraint change);
  all timestamps `timestamptz` in UTC; `sort_order` instead of the reserved `order`.
- **Fallback**: if the NamingConventions package has no 10.x release, use explicit
  `ToTable`/`HasColumnName` in configurations (mechanical).

## R7. Hosting the CLI verb

- **Decision**: `Program.cs` branches on `args[0] == "import-content"` and builds a plain
  `Host` (no Kestrel/FastEndpoints) reusing `AddInfrastructure(configuration)`; args parsed by hand
  (`--content-dir`, `--validate-only`, `--dry-run`). `IDesignTimeDbContextFactory<AppDbContext>` in
  Infrastructure keeps `dotnet ef` independent of the host.
- **Alternatives**: second console project (duplicate config surface); `System.CommandLine` (dependency
  for three flags). Rejected.

## R8. Startup order and health

- **Decision**: `StartupMigrator` retries `CanConnectAsync` for `DB_STARTUP_TIMEOUT_SECONDS` (default
  90, 3 s back-off), then `MigrateAsync` once; failure → Fatal log, exit 1. Then `LearnerSeeder`
  (`INSERT … ON CONFLICT DO NOTHING`, fixed GUID `00000000-0000-0000-0000-000000000001`). `/health`
  liveness (no checks), `/health/ready` with `AddDbContextCheck`. nginx proxies `/api/` → api and
  `/api/health` → `/health/ready` so the SPA uses `/api/*` only.
- **Alternatives**: `HasData` seeding (bakes timestamps into the snapshot, migration per tweak);
  migrate via a separate one-shot container (second image). Rejected.

## R9. Docker images

- **Decision**: api: `mcr.microsoft.com/dotnet/sdk:10.0` build → `aspnet:10.0-alpine` runtime, non-root,
  `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true`, healthcheck via busybox `wget`. web: `node:22-alpine`
  build → `nginx:alpine`, `try_files $uri /index.html`, `location = /healthz`. postgres:
  `postgres:16-alpine` + named volume + `pg_isready` healthcheck.
- **Secrets**: no committed file contains a password; compose builds `ConnectionStrings__Default` from
  `POSTGRES_*`; local dev exports `PGPASSWORD` or `ConnectionStrings__Default` from `.env`.

## R10. Concurrency guard for import

- **Decision**: `SELECT pg_try_advisory_xact_lock(7411001)` inside the single import transaction;
  `false` → "import already in progress", exit 2. Readers see old or new graph, never a mix.

## R11. Read-path query shape

- **Decision**: tree = three flat `AsNoTracking` queries (domains; topics; concepts with title
  sub-selects for requested + `en` language and count sub-selects) stitched in memory; concept page =
  one query with sub-selects. Route `/{domain}/{topic}/{concept}` because concept slugs are only unique
  within a topic by contract.

## R12. Angular shell

- **Decision**: scaffold with the `angular-new-app` skill (Angular 22, standalone, zoneless), Tailwind,
  one `ApiStatus` component using `httpResource<{status:string}>(() => '/api/health')`, 30 s reload
  via `setInterval` cleaned up with `DestroyRef`, `proxy.conf.json` for `ng serve`. Feature folders
  `knowledge/`, `review/`, `grading/` created as README placeholders.
