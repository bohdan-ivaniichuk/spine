# Quickstart: Knowledge atlas and concept notes

## Prerequisites

- Docker running; `.env` from `.env.example`.
- `docker compose up -d --build` (applies `AddConceptSessionScope` on API start), then
  `docker compose run --rm api import-content`.

## Automated

```bash
dotnet test --filter "Category!=Integration"
dotnet test                                   # + Testcontainers
cd web && nvm use 22 && npm test -- --watch=false
~/.lockflight/bin/lockflight trace && ~/.lockflight/bin/lockflight gate run --profile pr --base main
```

## Manual (http://localhost)

1. **US1** Click "Atlas" in the rail → domains in manifest order; a topic without content shows "soon"
   and is not a link. Drill into domain → topic → concept; reload on each level; use Back/Forward.
   Open `/atlas/nope` → "not found" with a link to the Atlas.
2. **US2** Open a concept with a C# block (e.g. `dotnet/async`): headings, lists and highlighted code in
   light and dark themes; "next"/"previous" walk the topic; the last concept has no "next".
3. **US3** Switch to УК on a translated concept (no EN badge) and on an untranslated one (EN badge);
   unreviewed concepts show the badge in both languages; the position stays.
4. **US4** `curl localhost/api/review/readiness | jq` → per-domain/topic `percent` (`null` where
   nothing counts). Answer a topic's questions in review, revisit the Atlas → states move to
   "in progress".
5. **US5** From a topic page, "Review this topic" opens Review with the topic preselected; from a
   concept page, "Review this concept" → session contains only that concept's questions.
6. **SC-006** Keyboard-only walkthrough; DevTools at 375 px — no horizontal page scroll, code blocks
   scroll inside.
