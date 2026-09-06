# CLAUDE.md

Guidance for Claude Code when working in this repository. The constitution at
`.specify/memory/constitution.md` is the binding statement of principles; this file is the operational
layer (layout, commands, hooks, gotchas). When they disagree, the constitution wins — fix this file.

## What this is

**InterviewPrep** — a single-user, locally-run knowledge platform for Senior Full-stack
(Angular + .NET + AI Engineering) interview preparation: knowledge graph (domain → topic → concept),
spaced-repetition quizzes (SM-2 behind `ISchedulingAlgorithm`), LLM-graded open answers and code katas.
The platform is intentionally thin; most value lives in the content (`content/**/*.json`) and in the
AI-assisted workflow that produces it. Building it is itself a showcase of AI-augmented development.

Single git repository, single solution, one Angular app. Default branch for work is `dev`.

## Repository layout

| Path | What's in it |
|---|---|
| `src/Domain`, `src/Application`, `src/Infrastructure`, `src/Api` | Modular monolith, .NET 10, FastEndpoints. Layering Domain → Application → Infrastructure → Api, no upward references. *(planned — created by feature 001)* |
| `tests/` | xUnit test projects mirroring `src/`; Testcontainers + PostgreSQL for integration. *(planned)* |
| `web/` | Angular (latest stable), standalone components, signals, Tailwind, Monaco for katas. *(planned)* |
| `content/schema/content.schema.json` | JSON Schema every seed file must validate against. |
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

## Common commands

```bash
# Backend (once feature 001 lands)
dotnet build -c Release
dotnet test --filter "Category!=Integration"          # unit
dotnet test                                            # + Testcontainers (Docker must be running)
dotnet ef migrations add <Name> --project src/Infrastructure --startup-project src/Api
dotnet run --project src/Api -- import-content         # validate + upsert content/**/*.json

# Frontend
cd web && npm ci && npm start                          # dev server on 4200
cd web && npm test -- --watch=false

# Everything
docker compose up --build                              # postgres + api (8080) + web (80)
docker compose run --rm api import-content

# Content validation without the API
npx -y ajv-cli@5 validate -s content/schema/content.schema.json -d "content/**/*.json" --spec=draft2020 --strict=false
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

_Empty for now. Add an entry the first time something costs more than 30 minutes; each entry is one
paragraph: symptom → cause → what to do instead._
