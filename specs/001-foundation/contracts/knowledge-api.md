# Contract — Knowledge read API and health

Route prefix `api`. JSON, camelCase. Errors as RFC 7807 ProblemDetails (FastEndpoints default).

## GET /api/knowledge/tree

Query: `lang` (`en` | `uk`, default `en`), `includeDeprecated` (bool, default `false`).

200:

```json
{
  "language": "uk",
  "domains": [
    { "slug": "dotnet", "title": "C# та середовище виконання .NET", "titleFallback": false, "sortOrder": 0, "deprecatedAt": null,
      "topics": [
        { "slug": "clr-memory", "title": "Пам'ять і GC", "titleFallback": false, "sortOrder": 0, "deprecatedAt": null,
          "concepts": [
            { "slug": "stack-vs-heap", "title": "Stack vs heap", "titleFallback": true, "level": "M",
              "reviewed": false, "sortOrder": 0, "questionCount": 5, "kataCount": 0, "deprecatedAt": null }
          ] }
      ] }
  ]
}
```

- Domain/topic titles come from the manifest: `title` (English) and the optional `titles.uk`. With `lang=uk` a
  domain/topic is served in Ukrainian when the manifest has `titles.uk`, else in English.
- `titleFallback` (concept, domain and topic) is `true` when `lang` is not `en`, the item has no title in `lang`
  and `en` was used.
- The concept page's `domain.title` / `topic.title` follow the same rule (no flag).
- Counts exclude deprecated questions/katas unless `includeDeprecated=true`.
- Empty database → `{ "language": "en", "domains": [] }`.
- 400 on invalid `lang`.

## GET /api/knowledge/concepts/{domain}/{topic}/{concept}

Query: `lang`, `includeDeprecated` as above.

200:

```json
{
  "domain": { "slug": "dotnet", "title": "C# & .NET Runtime" },
  "topic": { "slug": "clr-memory", "title": "Memory & GC" },
  "slug": "stack-vs-heap", "level": "M", "reviewed": false, "sortOrder": 0,
  "requestedLanguage": "uk", "language": "en", "fallback": true,
  "title": "Stack vs heap", "note": "…markdown…",
  "questionCount": 5, "kataCount": 0, "deprecatedAt": null
}
```

- 404 when the path is unknown, or when the concept or any ancestor is deprecated and
  `includeDeprecated` is not `true`.
- No learner-specific data in either response (FR-019).

## Health

- `GET /health` → 200 `{ "status": "Healthy" }` (liveness, no checks).
- `GET /health/ready` → 200 when the database responds, 503 otherwise (`AddDbContextCheck`).
- nginx: `location /api/ → http://api:8080`, `location = /api/health → http://api:8080/health/ready`,
  `location = /healthz → 200`. The SPA polls `/api/health`.
