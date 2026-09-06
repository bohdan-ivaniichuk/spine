# Quickstart — validating 001 Foundation end to end

Prerequisites: Docker Desktop running; for the inner loop also .NET SDK 10 and Node 22.

## 1. One-command stack

```bash
cp .env.example .env                      # edit POSTGRES_PASSWORD if you like
docker compose up --build                 # first time; later just `docker compose up`
docker compose ps                         # expect postgres, api, web all "healthy" within ~2 min
curl -s localhost:8080/health/ready       # {"status":"Healthy"}
open http://localhost                      # page shows "API: OK"
```

Persistence: `docker compose down && docker compose up` → data still present (named volume).

## 2. Import content

```bash
docker compose run --rm api import-content
# expect a per-file summary; exit 0
docker compose run --rm api import-content
# second run: everything "unchanged", 0 created / 0 updated
scripts/validate-content.sh               # no DB needed; exit 1 on any contract violation
```

Break a file on purpose (five options, two correct options, malformed id) and re-run: expect the
file + JSON pointer + rule on stderr, exit 1, and no partial changes.

## 3. Read API

```bash
curl -s "localhost:8080/api/knowledge/tree?lang=en" | jq '.domains[0].topics[0].concepts[0]'
curl -s "localhost:8080/api/knowledge/concepts/dotnet/clr-memory/stack-vs-heap?lang=uk" | jq '{language,fallback,title}'
curl -si "localhost:8080/api/knowledge/concepts/nope/nope/nope" | head -1     # 404
```

## 4. Tests

```bash
dotnet test --filter "Category!=Integration"      # unit: validators, hasher, diff, repo content
dotnet test                                        # + Testcontainers (Docker running): migrations, import idempotency, progress untouched, endpoints
cd web && npm test -- --watch=false                # ApiStatus component
```

## 5. Inner loop without Docker for the API

```bash
docker compose up -d postgres
export ConnectionStrings__Default="Host=localhost;Port=5432;Database=interviewprep;Username=interviewprep"
export PGPASSWORD=<from .env>
dotnet run --project src/Api                        # migrates, seeds learner, serves on 8080
cd web && npm start                                 # 4200 with /api proxied to 8080
```

## Measured on 2026-09-06 (clean clone, Apple Silicon, Docker layers warm)

| Criterion | Target | Measured |
|---|---|---|
| SC-001 healthy stack after `docker compose up --build` | < 2 min | 30 s (18 s build + 12 s to healthy) |
| SC-002 import of the MVP content set; repeat import | < 30 s; 0 created / 0 updated | 2 s; 0 / 0 (35 questions unchanged) |
| SC-003 contract violations reported with file + pointer + rule | 100% | verified by `ContentSchemaValidatorTests`, `ContentRuleValidatorTests`, importer test (h) |
| SC-004 progress untouched by re-import | 100% | `ContentImporterTests` (d) |
| SC-005 tree / concept reads | < 300 ms | 236 ms cold, 6–9 ms warm |
| SC-006 no table/column renamed for 002–005 | — | checked at each feature's planning |

Expected outcomes map to spec SC-001…SC-005; SC-006 is checked when 002–005 are planned.
