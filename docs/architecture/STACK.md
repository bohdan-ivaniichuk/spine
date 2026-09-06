# InterviewPrep — stack & conventions reference

Stable, lookup-style reference. **`CLAUDE.md` is the source of truth for workflow and gotchas; the
constitution for principles.** Versions below were verified on **2026-09-06** (sources at the bottom);
re-verify with context7 before pinning.

## Runtime & frameworks

| Item | Value |
|---|---|
| .NET | 10 LTS (`net10.0`), C# 14 |
| Web | ASP.NET Core 10 + FastEndpoints 8.3.x (validators via FluentValidation, Swagger) |
| ORM | EF Core 10 + Npgsql.EntityFrameworkCore.PostgreSQL 10.0.x; single `AppDbContext`, one migrations project (`Infrastructure`) |
| Database | PostgreSQL 16+ (Docker) |
| LLM | Anthropic Messages API via official NuGet `Anthropic` 12.x; model from `ANTHROPIC_MODEL` (default `claude-sonnet-5`) |
| Frontend | Angular 22, TypeScript strict, Tailwind, Monaco (`ngx-monaco-editor-v2` or direct, lazy) |
| Node | 22 LTS |
| Tests | xUnit, Testcontainers (PostgreSQL), Angular default runner, Playwright (via MCP) |
| Local infra | Docker Compose: `postgres`, `api`, `web` (nginx) |

## Ports

| Service | Port |
|---|---|
| api | 8080 |
| web (dev server) | 4200 |
| web (nginx, compose) | 80 |
| postgres | 5432 |

## Solution layout (target — created by feature 001)

```
InterviewPrep.sln
src/
  Domain/          entities, value objects, ISchedulingAlgorithm, Sm2Algorithm
  Application/     use cases, interfaces (IGrader, IContentImporter, repositories if any)
  Infrastructure/  AppDbContext, migrations, Anthropic client, content importer
  Api/             FastEndpoints endpoints + validators, Program.cs, `import-content` verb
tests/
  Domain.Tests/  Application.Tests/  Infrastructure.Tests/ (Integration)  Api.Tests/
web/               Angular app (src/app/{knowledge,review,grading,shared})
content/           schema/ + <domain>/<topic>.json
docker-compose.yml, .env.example, docker/nginx.conf
```

## Conventions

- Namespaces mirror folders: `InterviewPrep.Domain.Scheduling`, `InterviewPrep.Api.Endpoints.Review`.
- Endpoint per file, `<Verb><Thing>Endpoint` + `<Thing>Request/Response` + validator alongside.
- Tables snake_case (`review_states`), columns snake_case, UTC timestamps (`timestamptz`), `jsonb` for
  `algorithm_state`. Unique indexes on every slug/id column.
- Content ids `<domain>.<topic>.<concept>.<mcq|open|kata>.<n>`; slugs kebab-case; never renamed.
- Angular: feature folders mirror backend modules; standalone + signals + OnPush; no state library.
- English everywhere in the repo; `uk` only inside content translations.

## Sources (2026-09-06)

- .NET 10 GA / support: https://devblogs.microsoft.com/dotnet/announcing-dotnet-10/ ,
  https://devblogs.microsoft.com/dotnet/dotnet-8-9-end-of-support/
- FastEndpoints: https://www.nuget.org/packages/FastEndpoints
- Npgsql EF provider: https://www.nuget.org/packages/npgsql.entityframeworkcore.postgresql/
- Anthropic C# SDK: https://platform.claude.com/docs/en/cli-sdks-libraries/sdks/csharp ,
  https://www.nuget.org/packages/Anthropic
- Models: https://platform.claude.com/docs/en/models/overview
- Angular 22: https://blog.angular.dev/announcing-angular-v22-c52bb83a4664 ; skills/MCP:
  https://angular.dev/ai/agent-skills , https://angular.dev/ai/mcp
- .NET skills: https://github.com/dotnet/skills
- Claude Code MCP config: https://code.claude.com/docs/en/mcp
- Postgres MCP: https://github.com/crystaldba/postgres-mcp (the reference `server-postgres` is archived)
