# CLAUDE.md

Guidance for Claude Code when working in this repository. The constitution at
`.specify/memory/constitution.md` is the binding statement of principles; this file is the operational
layer (layout, commands, hooks, gotchas). When they disagree, the constitution wins — fix this file.

## What this is

**Spine** ("the senior engineer's interview atlas"; formerly InterviewPrep) — a single-user, locally-run
knowledge platform for Senior Full-stack (Angular + .NET + AI Engineering) interview preparation:
knowledge graph (domain → topic → concept), spaced-repetition quizzes (SM-2 behind `ISchedulingAlgorithm`),
LLM-graded open answers and code katas.
The platform is intentionally thin; most value lives in the content (`content/**/*.json`) and in the
AI-assisted workflow that produces it. Building it is itself a showcase of AI-augmented development.

Single git repository, single solution (`Spine.slnx`, projects `Spine.*`), one Angular app. Default branch for work is `dev`.

## Repository layout

| Path | What's in it |
|---|---|
| `src/Domain`, `src/Application`, `src/Infrastructure`, `src/Api` | Modular monolith, .NET 10, FastEndpoints 8. Layering Domain → Application → Infrastructure → Api, no upward references. `Api` also hosts the `import-content` CLI verb (`Program.cs` branches on `args[0]`). |
| `tests/` | xUnit + Shouldly, mirroring `src/`; `Infrastructure.Tests` and `Api.Tests` use Testcontainers (`Category=Integration`). Fixtures under `tests/Fixtures/`. |
| `web/` | Angular 22 (zoneless, standalone, signals, `httpResource`), Tailwind 4, Vitest; nginx image proxies `/api`. Feature folders `knowledge/`, `review/`, `grading/`. Design tokens (Atlas light/dark, ADR 0006) in `src/styles.scss`; use token utilities, never raw palette classes. UI copy only in `src/app/shared/i18n/messages.{en,uk}.ts` (runtime EN/UK, `?lang=uk` to the API). |
| `docker-compose.yml`, `.env.example` | `postgres` + `api` + `web` with health checks; `docker compose run --rm api import-content` imports `./content` (mounted read-only). |
| `scripts/validate-content.sh` | Validates content against schema + import rules without a database. |
| `content/schema/content.schema.json`, `content/schema/manifest.schema.json` | JSON Schemas for topic seed files and for the manifest. |
| `content/manifest.json` | Ordered domains and topics with titles (mirrors the taxonomy). Import reads it first; unknown (domain, topic) pairs are rejected. |
| `content/<domain>/<topic>.json` | Seed content: concepts, notes, MCQ/open questions, katas, `en` + optional `uk`. Human-reviewed (`"reviewed": true`) before it counts. |
| `specs/000-taxonomy.md` | The knowledge taxonomy — the only source of concept slugs. |
| `specs/NNN-feature/` | Spec Kit feature folders (`spec.md`, `plan.md`, `tasks.md`). |
| `.specify/` | Spec Kit constitution, templates, scripts. |
| `memory-bank/` | Living project state. SessionStart hook loads `activeContext`, `progress`, `systemPatterns`; read `projectbrief`, `productContext`, `techContext` on demand. |
| `docs/` | Stable reference (`architecture/STACK.md`), ADRs (`architecture/decisions/`), how-tos. |
| `working/` | Personal scratch, git-ignored (only its README is tracked). Never commit from here. |
| `TECH_DEBT.md` | Known debt, prioritised. |
| `.claude/` | Agents, hooks, skills, `settings.json` (hook wiring). `settings.local.json` is per-user and ignored. |
| `.mcp.json` | Project-scope MCP servers: `context7`, `playwright`, `postgres` (read-only), `angular-cli` (read-only). |

## Documentation language

**Every committed artifact is English** — code, comments, docs, specs, ADRs, memory-bank, commit
messages. Conversation with the user is usually Ukrainian; that never leaks into the repo. Content JSON:
`en` is canonical and mandatory; `uk` is a separate translation pass that keeps code, identifiers and
framework/CLR terms in English. `working/` is exempt.

## Spec-driven workflow

`/speckit-specify` → `/speckit-clarify` → `/speckit-plan` → `/speckit-tasks` → (`/speckit-analyze`) →
`/speckit-implement`, one feature per `specs/NNN-*/`. The constitution is validated with
`/speckit-constitution`; never regenerate it from a template.

Subagents (`.claude/agents/`):

| Agent | When |
|---|---|
| `solution-architect` | Before `/speckit-plan` on any non-trivial feature. Flags data-model decisions for the user. |
| `code-reviewer` | Before every commit touching `src/`, `tests/`, `content/`, migrations. |
| `frontend-reviewer` | Before every commit touching `web/`. |
| `test-author` | To add/backfill the tests Principle X requires (SM-2, import, grading parsing). |
| `content-author` | Generates/extends `content/<domain>/<topic>.json` from the taxonomy; EN first, `uk` on request; never marks reviewed. |
| `debugger` | Root-cause first, fix second. |
| `deep-researcher` | context7 + web for any framework-version question. Training data about Angular/.NET/Anthropic is stale. |

Content pipeline: taxonomy → `content-author` → JSON seed → human review (flip `"reviewed": true`) →
`import-content` → visible "unreviewed" badge in the app for anything not yet reviewed.

Project skills (`.claude/skills/`, besides `speckit-*` and the official `angular-*` / `dotnet` ones):

| Skill | Use when |
|---|---|
| `web-toolchain` | Before any `ng`/`npm`/`npx` in `web/` — selects Node 22.22+ via nvm, npm 11 for installs, verified test/build flags. |
| `ef-migrations` | Adding/applying/reviewing an EF Core migration — env vars, commands, SQL review checklist, `bin\Debug` clean-up. |
| `sync-claude-md` | Reconciling this file against the code (never regenerate). |

**Rule: turn recurring problems into skills.** When something costs real time and could happen again
(toolchain quirk, multi-step procedure, environment setup), write or extend a skill under
`.claude/skills/<name>/SKILL.md` with the trigger, exact commands, verification and pitfalls, and list it
in the table above. "Things that bite" below is the fallback for one-line gotchas, not a substitute.

## Common commands

```bash
# Backend
dotnet build -c Release
dotnet test --filter "Category!=Integration"          # unit (no Docker)
dotnet test                                            # + Testcontainers (Docker must be running)
export ConnectionStrings__Default="Host=localhost;Port=5432;Database=interviewprep;Username=interviewprep" PGPASSWORD=<from .env>
dotnet ef migrations add <Name> --project src/Infrastructure --startup-project src/Api --output-dir Persistence/Migrations
dotnet run --project src/Api --no-launch-profile -- import-content [--validate-only] [--dry-run]
dotnet run --project src/Api                           # Development profile: http://localhost:8080, Swagger at /swagger

# Frontend (nvm use 22 first — see Things that bite)
cd web && npm ci && npm start                          # dev server on 4200, /api proxied to 8080
cd web && npm test -- --watch=false

# Everything
cp .env.example .env && docker compose up --build      # postgres + api (8080) + web (80)
docker compose run --rm api import-content

# Content validation without a database
scripts/validate-content.sh
```

## Hooks (`.claude/settings.json`)

### Blocking (PreToolUse) — will reject the edit, do not work around

- **`secrets-scan.sh`** — blocks edits that introduce values matching real secret patterns (Anthropic
  `sk-ant-`, OpenAI, AWS, Stripe, Slack, GitHub tokens, bearer JWTs, connection strings with a literal
  password). Put secrets in `.env` (gitignored) or `docker-compose.override.yml`.
- **`local-runtime-files-guard.sh`** — blocks writes to `.env` and `appsettings.Local.json`. Edit
  `*.example` templates instead; the user maintains the real files by hand.
- **`override-yml-protect.sh`** — refuses to overwrite `docker-compose.override.yml` via Write (Edit is
  allowed).

### Confirmation gate (PreToolUse)

- **`git-commit-push-confirm.sh`** — every `git commit` / `git push` asks for explicit confirmation,
  regardless of permission mode. Staging and read-only git are untouched.

### Non-blocking

- **`PostToolUse: dotnet-format.sh`** — `dotnet format whitespace` on every edited `.cs`. Don't
  hand-format.
- **`PostToolUse: web-format.sh`** — `prettier --write` then `ng lint --fix` on `web/**/*.{ts,html,scss}`
  when `web/node_modules` exists.
- **`SessionStart: load-memory-banks.sh`** — loads the three live memory-bank files.
- **`Stop: memory-bank-staleness.sh`** — nags if ≥5 files changed without touching
  `memory-bank/activeContext.md` / `progress.md`. Update them when architecture, decisions or gotchas change.
- **`Notification: notify.sh`** — macOS banner when Claude needs input.

## Git conventions

- Work on `dev`; feature branches are optional for a solo repo. `main` receives merges from `dev`.
- Track new files immediately (`git add`) — migrations `.cs` + `.Designer.cs`, content JSON, tests.
- Commit messages: imperative, English, **no AI-tooling trailers** (no `Co-Authored-By: Claude`, no
  session links). No ticket numbers in code comments.
- Never commit `.env`, `docker-compose.override.yml`, or anything under `working/`.

## Things that bite

- **Angular 22 CLI needs Node ≥ 22.22.3** (`^22.22.3 || ^24.15.0 || >=26`). The machine's default
  `node` may be older; use `nvm use 22` (22.23.2 is installed) before any `ng`/`npm` command in `web/`.
  The Docker build is unaffected (`node:22-alpine`). npm 10's arborist can fail on the jsdom/canvas
  optional peer with "Cannot read properties of null (reading 'edgesOut')" — `npx npm@11 install` works;
  `npm ci` from the lockfile is fine.
- **`dotnet ef` creates a literal `bin\Debug` folder** (backslash in the name) next to `src/Api` and
  `src/Infrastructure` on macOS (Roslyn BuildHost artefacts). `.gitignore` covers it (`**/bin\\Debug/`);
  never `git add` it by hand.
- **The compose project is `spine`, but the postgres volume is pinned to `interviewprep_postgres_data`** (the
  name it got before the rename). Never drop that `name:` or run `down -v`; the database and role stay `interviewprep`.
- **Alpine containers resolve `localhost` to IPv6 first**; nginx/Kestrel listen on IPv4. Healthchecks use
  `127.0.0.1`, not `localhost`.
- **First start on an empty database logs two EF "Failed executing DbCommand" errors** for
  `__EFMigrationsHistory` before the migration creates it. Expected noise from `MigrateAsync`, not a
  failure — the log continues with "Applying 1 migration(s)".
- **`dotnet run --project src/Api` uses `src/Api` as the working directory**, so `import-content`
  walks up to find the nearest `content/schema/content.schema.json`. Pass `--content-dir` (or set
  `CONTENT_DIR`) when importing something else. Use `--no-launch-profile` in scripts to avoid the
  Development profile.
- **nginx `proxy_pass` with a variable** (`http://$api`, used so a restarted api container is
  re-resolved) must have **no URI part**: `proxy_pass http://$api/api/;` sends every request to the
  literal `/api/`. Check a non-health endpoint through `localhost:80` after touching `web/nginx.conf`.
- **Postgres MCP needs the database up before the session starts** — otherwise `postgres-mcp` retries
  the pool and Claude Code drops it after 30 s (`CONNECT_TIMEOUT`). `docker compose up -d postgres`,
  then `/mcp` to reconnect.
- **`secrets-scan` blocks any connection string with a literal password.** Keep passwords out of every
  committed file: compose interpolates `${POSTGRES_PASSWORD}`, local runs export `PGPASSWORD` or
  `ConnectionStrings__Default` from `.env`.

<!-- lockflight:begin -->
## Lockflight

This project is gated by Lockflight: checks, not instructions, decide when work is done. Run Lockflight as
`.lockflight/bin/lockflight <command>` from this directory: it is the engine the hooks run and works however Lockflight
was installed (`lockflight` below means that).

- **Done means a gate verdict.** Before you report a task done or commit, run `lockflight gate run` and report its
  verdict. FAIL or NOT-EARNED means not done.
- **Protected files** (`lockflight.yaml`, `.lockflight/`, Claude Code settings, hooks, agents, skills and commands, build
  and package config, each unit's control files) are changed by a person or a `lockflight` command; the guard blocks
  your edits, and a user's permission in chat does not lift it, so never ask for permission to bypass it. When a task
  needs one changed (a rename, a package bump), write the complete new file in your scratch directory and run
  `lockflight propose <path> --from <file>` (`<path> --delete` to remove one; several pairs in one call), then give
  the user the `git apply` command it prints. It writes one patch per file and applies nothing.
- **Human decisions** are the user's: never run one, even when asked or just to try it, since the guard refuses it.
  Ask the user, who runs it in a separate terminal window (not with `!` in Claude Code: that runs inside the session
  and is refused): `lockflight approve` (sign off a spec or tasks),
  `lockflight baseline` (accept build warnings, failing tests or package advisories the project already has),
  `lockflight redgreen reset` (discard a feature's test-first evidence), `lockflight redgreen amend` (let one locked test
  change when its expectation is wrong), `lockflight review dismiss` (close a review
  finding they judge not to be a defect) and `lockflight update`. Where the Lockflight plugin is installed, approvals, baselines,
  resets and amends can also be given in this session: the user types `/lockflight:approve spec` (or `tasks`), `/lockflight:baseline`, `/lockflight:reset` or
  `/lockflight:amend` themselves; never type them or guess or suggest their codes, and check the result with the
  engine (`lockflight approve verify --feature <id>`, `lockflight redgreen status`).
- Start Claude Code at this directory so these settings load. `lockflight doctor` shows what is missing.
<!-- lockflight:end -->
