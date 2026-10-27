# Quickstart: Weak spots

## Prerequisites

`docker compose up -d --build` (applies `AddSessionMode`), content imported.

## Automated

```bash
dotnet test
cd web && nvm use 22 && npm test -- --watch=false
.lockflight/bin/lockflight trace && .lockflight/bin/lockflight gate run --profile pr --base main
```

## Manual (Chrome MCP, http://localhost)

1. **US1** Review a topic, answer some questions wrong. In Review switch to "Weak spots": the count
   equals the misses; start → only those questions, newest miss first, a weak chip on the session page.
   Answer one right, finish, start weak again → it is gone. With no misses the start is disabled with a
   message.
2. **US2** Open "Weak spots" in the rail: concepts with ≥ 3 answers ranked by accuracy; note link opens
   the Atlas concept; "Drill" opens Review with `mode=weak` and the concept preselected. Empty history →
   empty state with a link to Review.
3. **US3** Finish a session with misses: weak concepts link to their notes; "Drill these" starts a weak
   session.
4. **SC-005** Keyboard-only pass, 375 px, EN/УК, light/dark.
