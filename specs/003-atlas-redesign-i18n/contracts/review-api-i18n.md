# Contract — Review API i18n + coverage (feature 003)

Extends `specs/002-review-session/contracts/review-api.md`. Same conventions: route prefix `api`, camelCase JSON,
RFC 7807 ProblemDetails for every error. Everything not listed here is unchanged.

## `lang` query parameter

Every review endpoint accepts `?lang=en|uk` (default `en`). Any other value → **400** validation ProblemDetails
(`errors.lang: ["lang must be 'en' or 'uk'"]`), the same rule as the knowledge endpoints. On the two POSTs with a
body (`/sessions`, `/answers`) `lang` is a **query** parameter; the bodies are unchanged.

| Endpoint | What `lang` changes |
|---|---|
| `GET /api/review/overview` | nothing today (see "Domain and topic titles"); validated for consistency |
| `POST /api/review/sessions?lang=` | the returned `Session` (prompts, options, concept titles) |
| `GET /api/review/sessions/current?lang=` | same |
| `GET /api/review/sessions/{id}?lang=` | same |
| `POST /api/review/sessions/{id}/answers?lang=` | `explanation` |
| `POST /api/review/sessions/{id}/finish?lang=` | `weakConcepts[].title` |
| `GET /api/review/coverage?lang=` | nothing (no text in the payload); validated for consistency |

Session membership, grading, scheduling and stored attempts are language-independent: a session started with
`lang=en` can be read and answered with `lang=uk` and vice versa. `attempts.answer.selectedText` always stores the
English option text.

### Per-item fallback

Fallback to English is decided **per item**, never per response:

- **Question (prompt + options)** — served in `uk` only when a `uk` translation exists **and** its options are
  index-aligned with English (same count, correct option at the same index). Otherwise the whole question (prompt
  and options together) is served in English. Prompt and options are never mixed across languages, so
  `selectedIndex` always means the same option in both languages.
- **Explanation** — follows the same rule as its question: `uk` explanation only when the question itself would be
  served in `uk`.
- **Concept title** (`SessionQuestion.concept.title`, `WeakConcept.title`) — `uk` title when present, else English.
  No separate flag.

### Domain and topic titles

Domain and topic titles come from `content/manifest.json`, which is English-only today. `scope.domainTitle`,
`scope.topicTitle`, `SessionQuestion.domain.title` and `SessionQuestion.topic.title` are therefore always English,
whatever `lang` is. Translating them needs `uk` titles in the manifest (schema + import) and is deferred; the client
may map slugs to its own UI strings meanwhile.

## New DTO fields

```jsonc
// SessionQuestion — two fields added
{ "id": "…", "level": "M", "reviewed": false, "isNew": true, "prompt": "…", "options": ["…", "…", "…", "…"],
  "domain": Ref, "topic": Ref, "concept": Ref,
  "language": "en" | "uk",   // the language actually served for prompt + options
  "fallback": false }        // true when uk was requested but en was served

// AnswerFeedback — two fields added
{ "questionId": "…", "selectedIndex": 2, "isCorrect": false, "correctIndex": 0, "explanation": "markdown",
  "nextDueAt": "…", "intervalDays": 1,
  "language": "en" | "uk",   // the language actually served for explanation
  "fallback": false }        // true when uk was requested but en was served
```

With `lang=en`, `language` is always `"en"` and `fallback` is always `false`.

## `GET /api/review/coverage`

Per-topic progress for the local learner, for the knowledge map.

```jsonc
// ReviewCoverage
{ "topics": [
    { "domain": "dotnet", "topic": "clr-memory", "eligible": 28, "seen": 12, "due": 0 },
    { "domain": "dotnet", "topic": "csharp-lang", "eligible": 0, "seen": 0, "due": 0 }
] }
```

- One entry per **non-deprecated topic of a non-deprecated domain**, including topics with `eligible: 0`, in
  manifest order (domain `sortOrder`, then topic `sortOrder`).
- `eligible` — the session eligibility rule: non-deprecated MCQ whose concept, topic and domain are not deprecated.
- `seen` — eligible questions that have a review state (answered at least once).
- `due` — eligible questions whose review state has `dueAt <= now`. `due <= seen <= eligible`.

| Method / path | Request | Success | Errors |
|---|---|---|---|
| `GET /api/review/coverage?lang=en\|uk` | query | 200 `ReviewCoverage` | 400 invalid `lang` |
