---
name: ef-migrations
description: >-
  Use when adding, reviewing, applying or scripting an EF Core migration in this repo (Infrastructure
  project, Npgsql, snake_case). Gives the exact environment variables, commands, the review checklist for
  the generated SQL (CHECKs, jsonb, generated columns, indexes), and the clean-up of the dotnet-ef
  BuildHost "bin\Debug" artefact. Triggers: "add a migration", "dotnet ef", "update the database",
  "migration script", "schema change".
---

# EF Core migrations (Infrastructure / Npgsql)

## Environment

A local PostgreSQL must be reachable. Either the compose one (`docker compose up -d postgres`) or a
throwaway container:

```bash
docker run -d --name ip-pg -e POSTGRES_PASSWORD=devpass -e POSTGRES_USER=interviewprep -e POSTGRES_DB=interviewprep -p 5432:5432 postgres:16-alpine
```

Never put the password in a committed file (the `secrets-scan` hook blocks it). Export it:

```bash
export ConnectionStrings__Default="Host=localhost;Port=5432;Database=interviewprep;Username=interviewprep;Password=<pw>"
# or keep the password out of the string and export PGPASSWORD=<pw>
```

`AppDbContextDesignTimeFactory` reads `ConnectionStrings__Default`, so `dotnet ef` never starts the
web host. Tool version must match the runtime: `dotnet tool update -g dotnet-ef` (10.0.11 as of 2026-09).

## Commands

```bash
cd "$(git rev-parse --show-toplevel)"
dotnet ef migrations add <Name> --project src/Infrastructure --startup-project src/Api --output-dir Persistence/Migrations
dotnet ef migrations script --project src/Infrastructure --startup-project src/Api --idempotent -o /tmp/schema.sql
dotnet ef database update --project src/Infrastructure --startup-project src/Api
dotnet ef migrations remove --project src/Infrastructure --startup-project src/Api   # undo the last unapplied one
```

## Review the generated SQL before committing

```bash
grep -E "CREATE TABLE|CONSTRAINT ck_|GENERATED ALWAYS|CREATE (UNIQUE )?INDEX|jsonb|text\[\]" /tmp/schema.sql
```

Checklist: every CHECK from `specs/001-foundation/data-model.md` present; `jsonb` (not `text`) for
JSON columns; `GENERATED ALWAYS AS (...) STORED` for `item_id`; unique indexes on slugs/ids;
`timestamp with time zone` everywhere; snake_case names; `ON DELETE` behaviour as designed
(RESTRICT for progress → content). Then `git add` the migration `.cs`, `.Designer.cs` **and**
`AppDbContextModelSnapshot.cs`.

## Pitfalls

- `dotnet ef` leaves a literal `bin\Debug` folder (backslash in the name) under `src/Api` and
  `src/Infrastructure`. It is git-ignored (`**/bin\\Debug/`); if `git status` ever shows it, run
  `rm -rf 'src/Api/bin\Debug' 'src/Infrastructure/bin\Debug'`.
- jsonb / array properties need a `ValueComparer` (see `Configurations/Conventions.cs`) or EF marks
  them modified on every save and the import's "unchanged" guarantee breaks.
- Adding an enum value = editing the CHECK constraint (`ck_<table>_<column>`) in a migration; enums are
  `text`, not PostgreSQL enum types.
- First `MigrateAsync` on an empty database logs two EF "Failed executing DbCommand" errors while it
  probes `__EFMigrationsHistory` — benign.
