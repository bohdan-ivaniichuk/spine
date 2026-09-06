# Tech Context — InterviewPrep

Versions verified on **2026-09-06** by `deep-researcher` (sources in `docs/architecture/STACK.md`).
Re-verify with context7 before pinning anything in a csproj/package.json.

## Runtime & frameworks

| Item | Version | Notes |
|---|---|---|
| .NET | 10 (LTS, GA 2025-11, supported to 2028-11) | Installed locally: SDK 10.0.102. .NET 8/9 end of support 2026-11. |
| ASP.NET Core + FastEndpoints | FastEndpoints 8.3.0 (2026-08-20) | Endpoints, validators (FluentValidation), Swagger. |
| EF Core + Npgsql | Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3 (needs EF Core ≥ 10.0.4) | jsonb for `algorithm_state`. |
| PostgreSQL | 16+ (Docker `postgres:16-alpine` or 17) | |
| Anthropic .NET SDK | NuGet `Anthropic` 12.46.0 (official first-party since v10; older `tryAGI.Anthropic` is a community fork) | Structured outputs; model via env (`ANTHROPIC_MODEL`). |
| Anthropic models | grading: `claude-sonnet-5`; cheap fallback: `claude-haiku-4-5` | From platform.claude.com model overview. |
| Angular | 22 (released 2026-06-03; LTS to 2027-05) | Standalone, signals, `resource`/`httpResource`, `@defer`. |
| Node | 22 LTS (installed 22.19.0) | |
| Tailwind | latest stable at scaffold time | |
| Monaco wrapper | `ngx-monaco-editor-v2` 22.x (supports Angular 14–22) — version UNVERIFIED against npm directly | Alternative: load `monaco-editor` directly in a `@defer` block. |
| Testing | xUnit, Testcontainers (PostgreSQL), Angular default runner, Playwright MCP | |
| Tooling | Docker 29.6, uv 0.11, Spec Kit (fresh `specify init`, 2026-09-06) | |

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
