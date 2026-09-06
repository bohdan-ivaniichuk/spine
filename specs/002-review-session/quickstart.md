# Quickstart — validating 002 Review session

Prerequisites: stack from 001 running (`docker compose up --build`) with content imported.

## 1. Overview and session via the API

```bash
curl -s "localhost:8080/api/review/overview?scope=topic&domain=dotnet&topic=clr-memory" | jq
S=$(curl -s -X POST localhost:8080/api/review/sessions -H 'content-type: application/json' \
  -d '{"scope":"topic","domain":"dotnet","topic":"clr-memory","totalLimit":5,"newLimit":5}')
ID=$(echo "$S" | jq -r .id); Q=$(echo "$S" | jq -r '.questions[0].id')
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
