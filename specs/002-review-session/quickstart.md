# Quickstart — validating 002 Review session

Prerequisites: stack from 001 running (`docker compose up --build`) with content imported.

## 1. Overview and session via the API

```bash
curl -s "localhost:8080/api/review/overview?scope=topic&domain=dotnet&topic=clr-memory" | jq
S=$(curl -s -X POST localhost:8080/api/review/sessions -H 'content-type: application/json' \
  -d '{"scope":"topic","domain":"dotnet","topic":"clr-memory","totalLimit":5,"newLimit":5}')
ID=$(printf '%s' "$S" | jq -r .id); Q=$(printf '%s' "$S" | jq -r '.questions[0].id')   # printf, not echo: zsh echo expands \n inside JSON
curl -s -X POST "localhost:8080/api/review/sessions/$ID/answers" -H 'content-type: application/json' -d "{\"questionId\":\"$Q\",\"selectedIndex\":0}" | jq
curl -s -X POST "localhost:8080/api/review/sessions/$ID/answers" -H 'content-type: application/json' -d "{\"questionId\":\"$Q\",\"selectedIndex\":1}" -w " %{http_code}\n"   # 409
curl -s -X POST "localhost:8080/api/review/sessions/$ID/finish" | jq '{answered,correct,accuracy,weakConcepts,dueTomorrow}'
curl -s -o /dev/null -w "%{http_code}\n" localhost:8080/api/review/sessions/current                           # 204 after finish
```

## 2. In the browser

1. `http://localhost/review` — pick a scope, see "N due · M new", press Start.
2. Answer with clicks or keys 1–4, Enter to check, Enter to continue; feedback shows the explanation.
3. Reload mid-session — it resumes at the first unanswered question.
4. Finish (or End session) — summary with accuracy, weak concepts, due tomorrow; "Review again".
5. Toggle the theme — card states readable in both.

## 3. Tests

```bash
dotnet test --filter "Category!=Integration"     # SM-2 table, planner, summary
dotnet test                                       # + session building, one-answer, SC-004 (FakeTimeProvider), API endpoints
cd web && npm test -- --watch=false               # store, parser, option list, session page, overview, summary
```

Success criteria: SC-001…SC-006 in spec.md.

## Measured (2026-10-14, local Docker on macOS, through nginx on :80)

| Criterion | Target | Measured |
|---|---|---|
| SC-001 Start → first question | < 1 s | API `POST /sessions` 19 ms warm, 526–586 ms on the first call after a container restart (JIT); first question rendered within that plus the lazy route chunk |
| SC-002 Check → feedback | < 300 ms | API `POST /answers` 12 ms warm, 59–215 ms cold; no page reload |
| SC-003 SM-2 transitions tested | 100% | `Sm2AlgorithmTests` table (20 cases incl. the 36500-day cap) |
| SC-004 No repeats next session | none repeated | `ReviewSessionsTests` with `FakeTimeProvider` |
| SC-005 Reload mid-session | nothing lost | browser: reload at 3/5 resumed at 4/5 with the same list; API test `Current_resumes_with_the_same_question_order_and_the_given_answers` |
| SC-006 Card-state contrast | AA | light ok/ok-soft 4.8:1, bad/bad-soft 4.7:1 (after the ADR 0004 amendment); dark 9.3:1 and 6.2:1 |

Measure in a visible tab: Chrome throttles timers in background tabs to 1 s, which makes in-page
polling report ~1000 ms regardless of the real latency. Use the Resource Timing API or curl.
