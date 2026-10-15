# Contract: review scope `concept`

Extends the review API from [002](../../002-review-session/contracts/review-api.md) and
[003](../../003-atlas-redesign-i18n/contracts/) with a fourth scope type. All other behaviour (limits,
one open session, resume, `lang`) is unchanged.

## Query parameters (overview, start session)

| Parameter | Rule |
|---|---|
| `scope` | `all` \| `domain` \| `topic` \| `concept` |
| `domain` | required for `domain`, `topic`, `concept` |
| `topic` | required for `topic`, `concept` |
| `concept` | required for `concept`; must be absent otherwise; slug format `^[a-z0-9]+(-[a-z0-9]+)*$` |

Errors: missing/extra parameter → 400 (validation ProblemDetails); unknown or deprecated
domain/topic/concept → 404.

Examples:

```text
GET  /api/review/overview?scope=concept&domain=dotnet&topic=async&concept=task-vs-valuetask
POST /api/review/sessions   { "scope": "concept", "domain": "dotnet", "topic": "async", "concept": "task-vs-valuetask" }
```

(The start-session body uses the same fields as its existing scope parameters.)

## Session membership

A `concept` session contains only that concept's schedulable (non-deprecated MCQ) questions, with the
usual due/new ordering and limits. Starting it finishes any open session first (existing behaviour).

## Scope in responses

`scope` objects gain two additive fields:

```json
{
  "type": "concept",
  "domain": "dotnet", "topic": "async", "concept": "task-vs-valuetask",
  "domainTitle": "C# та середовище виконання .NET",
  "topicTitle": "Асинхронність",
  "conceptTitle": "Task проти ValueTask"
}
```

`concept` and `conceptTitle` are `null` for the other scope types. Titles follow `lang` with English
fallback.
