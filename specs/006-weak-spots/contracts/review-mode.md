# Contract: review mode `weak`

Extends the review API (002, 003, 004) with a session mode. Scopes, limits, `lang`, resume and the
one-open-session rule are unchanged.

## POST /api/review/sessions

Body gains `mode`: `"regular"` (default) | `"weak"`.

```json
{ "mode": "weak", "scope": "topic", "domain": "dotnet", "topic": "async", "totalLimit": 20 }
```

- Weak sessions contain the schedulable questions in scope whose most recent answer was wrong, newest
  miss first, capped at `totalLimit`; `newLimit` is accepted and ignored.
- 201 `Session` with `"mode": "weak"`.
- 409 ProblemDetails `title: "Nothing to drill"` when no question in scope is weak (no session created,
  an open session stays open).
- 400 for an unknown mode; 404 for an unknown scope.

## Session and summary

`Session` and `SessionSummary` gain `"mode": "regular" | "weak"`.

## GET /api/review/overview

Gains `"weakCount"`: the number of weak questions in the requested scope (independent of mode).
