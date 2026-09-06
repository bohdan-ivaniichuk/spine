# Tech Context — InterviewPrep

Versions verified on **2026-09-06** by `deep-researcher` (sources in `docs/architecture/STACK.md`).
Re-verify with context7 before pinning anything in a csproj/package.json.

## Runtime & frameworks

| Item | Version | Notes |
|---|---|---|
| .NET | 10 (LTS, GA 2025-11, supported to 2028-11) | Installed locally: SDK 10.0.102. .NET 8/9 end of support 2026-11. |
| ASP.NET Core + FastEndpoints | FastEndpoints 8.3.0 + FastEndpoints.Swagger 8.3.0 | Endpoints, validators (FluentValidation), Swagger; `Send.OkAsync` API; `c.Errors.UseProblemDetails()`. |
| EF Core + Npgsql | Microsoft.EntityFrameworkCore 10.0.11, Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3, EFCore.NamingConventions 10.0.1, dotnet-ef 10.0.11 | jsonb columns via value converters + comparers; snake_case. |
| PostgreSQL | 16+ (Docker `postgres:16-alpine` or 17) | |
| Anthropic .NET SDK | NuGet `Anthropic` 12.46.0 (official first-party since v10; older `tryAGI.Anthropic` is a community fork) | Structured outputs; model via env (`ANTHROPIC_MODEL`). |
| Anthropic models | grading: `claude-sonnet-5`; cheap fallback: `claude-haiku-4-5` | From platform.claude.com model overview. |
| Angular | 22.1.5 (CLI 22.1.7; released 2026-06-03; LTS to 2027-05) | Zoneless, standalone, signals, `httpResource`, `@defer`. **Needs Node ≥ 22.22.3** (`nvm use 22` → 22.23.2). Vitest 4 + jsdom is the CLI default runner. |
| Node | 22 LTS (22.23.2 via nvm; system default 22.19.0 is too old for Angular 22 CLI) | |
| Tailwind | 4.3.3 (`@tailwindcss/postcss`, `@use 'tailwindcss'` in `styles.scss`) | |
| Monaco wrapper | `ngx-monaco-editor-v2` 22.x (supports Angular 14–22) — version UNVERIFIED against npm directly | Alternative: load `monaco-editor` directly in a `@defer` block. |
| Testing | xUnit 2.9.3, Shouldly 4.3, Testcontainers.PostgreSql 4.14, Mvc.Testing 10.0.11; Vitest (web); Playwright MCP | |
| Tooling | Docker 29.6, uv 0.11, Spec Kit (fresh `specify init`, 2026-09-06), JsonSchema.Net 9.4.0, Serilog.AspNetCore 10.0.0 | |

## Claude Code tooling in this repo

| Item | Source | Notes |
|---|---|---|
| Spec Kit skills `speckit-*` | github.com/github/spec-kit (init 2026-09-06) | Skills, not commands (`/speckit-specify`). |
| `angular-developer`, `angular-new-app` | github.com/angular/skills (official) | Installed project-scope via `npx skills add`. |
| `dotnet-webapi`, `create-datadriven-aspnetcore`, `optimizing-ef-core-queries`, `scaffold-dotnet-test-project`, `analyzing-dotnet-performance` | github.com/dotnet/skills (official Microsoft) | Installed project-scope via `npx skills add ... -s <names>`; the same repo is also a Claude Code plugin marketplace (`/plugin marketplace add dotnet/skills`). `skills-lock.json` pins them. |
| `sync-claude-md` | adapted from the MailHub workspace | Reconcile CLAUDE.md, never regenerate. |
| MCP `context7` | `@upstash/context7-mcp` | `CONTEXT7_API_KEY` optional (rate-limited without). |
| MCP `playwright` | `@playwright/mcp` `--headless` | Smoke test through the browser. |
| MCP `postgres` | `postgres-mcp` (Crystal DBA) via `uvx`, `--access-mode=restricted` | Read-only. `DATABASE_URI` env. The archived `@modelcontextprotocol/server-postgres` is NOT used. |
| MCP `angular-cli` | `@angular/cli mcp --read-only` | Live workspace introspection, docs search. |

## Local environment

- macOS (Apple Silicon), zsh. SDKs: dotnet 8/9/10, node 22, docker 29, uv 0.11.
- Ports: API 8080, web dev 4200 / nginx 80, postgres 5432.
- Secrets: `.env` (gitignored; `ANTHROPIC_API_KEY`, `POSTGRES_PASSWORD`, …) documented by `.env.example`.
