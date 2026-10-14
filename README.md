# Spine

*The senior engineer's interview atlas.*

A single-user, locally-run interview-preparation platform for Senior Full-stack (Angular + .NET + AI
Engineering): knowledge graph → spaced-repetition quizzes (SM-2) → LLM-graded open answers and katas.
Content is data (`content/**/*.json`), authored with AI, reviewed by a human.

- Principles: `.specify/memory/constitution.md`
- Content scope: `specs/000-taxonomy.md`; content contract: `content/schema/`, `content/manifest.json`
- Working with Claude Code here: `CLAUDE.md`
- Stack reference: `docs/architecture/STACK.md`; decisions: `docs/architecture/decisions/`

## Run

```bash
cp .env.example .env                 # set POSTGRES_PASSWORD; ANTHROPIC_API_KEY is optional (feature 004)
docker compose up --build            # postgres + api (http://localhost:8080) + web (http://localhost)
docker compose run --rm api import-content   # validate + import content/ (idempotent; re-run any time)
```

`http://localhost` shows the web shell with an "API: OK" indicator; `http://localhost:8080/api/knowledge/tree`
returns the imported knowledge tree.

## Develop

Prerequisites: .NET SDK 10, Node 22.22+ (`nvm use 22`), Docker.

```bash
docker compose up -d postgres
export ConnectionStrings__Default="Host=localhost;Port=5432;Database=interviewprep;Username=interviewprep"
export PGPASSWORD=<POSTGRES_PASSWORD from .env>
dotnet run --project src/Api          # migrates, seeds the local learner, serves on 8080 with Swagger
cd web && npm ci && npm start         # Angular dev server on 4200, /api proxied to 8080

dotnet test --filter "Category!=Integration"   # unit tests
dotnet test                                    # + Testcontainers integration tests (Docker running)
scripts/validate-content.sh                    # content contract check, no database
```

See `specs/001-foundation/quickstart.md` for the end-to-end validation scenarios.
