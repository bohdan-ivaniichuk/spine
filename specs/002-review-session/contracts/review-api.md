# Contract — Review API (feature 002)

Route prefix `api`, camelCase JSON, RFC 7807 ProblemDetails for every error. The Angular feature codes against this file only.

## Endpoints and DTOs (FastEndpoints, `api` prefix, camelCase JSON, ProblemDetails)

Shared shapes:

```jsonc
// Scope (appears in session, overview, summary)
{ "type": "all" | "domain" | "topic", "domain": "dotnet" | null, "topic": "clr-memory" | null,
  "domainTitle": "C# & .NET Runtime" | null, "topicTitle": "Memory & GC" | null }

// Ref
{ "slug": "span-memory", "title": "Span<T>, Memory<T> and zero-allocation code" }

// SessionQuestion
{ "id": "dotnet.clr-memory.stack-vs-heap.mcq.1", "level": "M" | "S" | "L", "reviewed": false, "isNew": true,
  "prompt": "markdown with ```csharp fences and `inline code`", "options": ["…", "…", "…", "…"],
  "domain": Ref, "topic": Ref, "concept": Ref }

// SessionAnswer (already recorded, for resume)
{ "questionId": "…", "selectedIndex": 2, "isCorrect": false }

// Session
{ "id": "uuid", "scope": Scope, "startedAt": "2026-09-06T10:00:00+00:00", "finishedAt": null,
  "totalLimit": 20, "newLimit": 10, "questions": SessionQuestion[], "answers": SessionAnswer[],
  "skippedQuestionIds": [] }

// AnswerFeedback
{ "questionId": "…", "selectedIndex": 2, "isCorrect": false, "correctIndex": 0,
  "explanation": "markdown", "nextDueAt": "2026-09-07T10:00:00+00:00", "intervalDays": 1 }

// ReviewOverview
{ "scope": Scope, "dueCount": 0, "newCount": 28, "totalEligible": 28, "nextDueAt": null | "…",
  "defaults": { "totalLimit": 20, "newLimit": 10 }, "limits": { "min": 1, "max": 100 } }

// SessionSummary
{ "sessionId": "uuid", "scope": Scope, "startedAt": "…", "finishedAt": "…", "questionCount": 20,
  "answered": 20, "correct": 15, "accuracy": 0.75 | null,
  "weakConcepts": [ { "domain": "dotnet", "topic": "clr-memory", "concept": "gc-modes",
                      "title": "GC modes", "wrong": 2, "total": 3 } ],
  "dueTomorrow": 5 }
```

Endpoints:

| Method / path | Request | Success | Errors |
|---|---|---|---|
| `GET /api/review/overview?scope=all\|domain\|topic&domain=&topic=` | query (`ScopeQuery` base class with FluentValidation: `scope` in set; `domain` required when scope≠all; `topic` required iff scope=topic; slugs match `^[a-z0-9]+(-[a-z0-9]+)*$`) | 200 `ReviewOverview` | 400 validation; 404 `title: "Scope not found"` |
| `POST /api/review/sessions` | body `{ "scope": "topic", "domain": "dotnet", "topic": "clr-memory", "totalLimit": 20, "newLimit": 10 }` (`totalLimit`/`newLimit` optional → defaults; each 1..100) | 201 `Session`, `Location: /api/review/sessions/{id}` | 400; 404 scope; 409 `title: "Nothing to review"` |
| `GET /api/review/sessions/current` | – | 200 `Session` or **204** (no open session) | – |
| `GET /api/review/sessions/{id}` | route guid | 200 `Session` (open or finished) | 400 non-guid; 404 `title: "Session not found"` |
| `POST /api/review/sessions/{id}/answers` | `{ "questionId": "…", "selectedIndex": 0..3 }` | 200 `AnswerFeedback` | 400; 404 session; 409 `title: "Session already finished"`; 409 `title: "Question already answered"`; 422 `title: "Question not in session"` |
| `POST /api/review/sessions/{id}/finish` | – | 200 `SessionSummary` (idempotent) | 404 |

Error mapping lives in one `ReviewExceptionHandler : IExceptionHandler` (`src/Api/Errors/`) registered with `builder.Services.AddExceptionHandler<ReviewExceptionHandler>()` and `app.UseExceptionHandler()` placed before `UseFastEndpoints`; it maps `ReviewError` → status/title above and writes `application/problem+json` with `instance = request path`, exactly like the existing 404 in `GetConceptEndpoint`. Non-`ReviewException` exceptions fall through to the default ProblemDetails 500. Endpoint classes stay one-liners: validate → call `IReviewSessions` → `Send.OkAsync` / `Send.CreatedAtAsync<GetSessionEndpoint>(...)` / `Send.NoContentAsync()`.

Angular codes against the table above; the C# records are `ReviewDtos.cs` positional records with the same property names (PascalCase → camelCase by the existing serializer setting).


## Decisions

- `POST /api/review/sessions` with nothing due and nothing new → **409** `title: "Nothing to review"`.
- `POST /api/review/sessions` does not return the auto-finished previous session's summary.
- Session and answer payloads are English only in 002 (`lang` may be added later as an optional query parameter).
