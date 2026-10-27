# Review feature (002, 006)

Daily review session for multiple-choice questions: overview with due/new counts per scope, one
card at a time with instant feedback, summary with weak concepts. Codes against
`specs/002-review-session/contracts/review-api.md`; the weak mode and the weak-spots view (006) code
against `specs/006-weak-spots/contracts/`.

## Layout

| Path                                                                 | Role                                                                                                                                                          |
| -------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `review.routes.ts`                                                   | `/review` overview, `/review/session`, `/review/summary`; lazy-loaded from `app.routes.ts`                                                                    |
| `api/review.models.ts`                                               | TS interfaces 1:1 with the contract, plus `scopeLabel` / `toScopeQuery` helpers                                                                               |
| `api/review-api.ts`                                                  | `ReviewApi` — six endpoints as promises; `current()` maps 204 → `null`; `overviewUrl`, `errorMessage`                                                         |
| `session-store.ts`                                                   | `SessionStore` (root) — signals for session/status/selection/feedback/answered/summary; `start`, `resume`, `select`, `check`, `next`, `finish`, `reviewAgain` |
| `review-overview/`                                                   | Typed reactive form (scope + limits), `httpResource` for the knowledge tree and the overview counts, resume banner                                            |
| `review-session/`                                                    | The card page; `document:keydown` shortcuts — `1`–`4` / `A`–`D` select, `Enter` checks then advances                                                          |
| `question-card/`, `option-list/`, `feedback-panel/`, `progress-bar/` | Leaf components, token-only styling (ADR 0004)                                                                                                                |
| `prompt-markdown/`                                                   | Dependency-free parser for fenced + inline code; renders through interpolation only (no `innerHTML`)                                                          |
| `session-summary/`                                                   | Counts, accuracy, weak concepts (note peek in place), Review again (keeps the scope, incl. concept)                                                           |
| `api/weak-spots.models.ts`                                           | `GET /api/review/weak-spots` shapes and `WEAK_SPOTS_URL` (slugs and figures only, no `?lang=`)                                                                |
| `weak-spots/`                                                        | `/weak` (top-level route): weakest concepts and topics, note links, weak drills; component-scoped `httpResource`, titles from `KnowledgeTreeSource`           |
| `testing/fixtures.ts`                                                | Contract-shaped fixtures shared by the specs                                                                                                                  |

## Behaviour notes

- Session membership is frozen on the server; the client never computes due-ness.
- `position` is the 1-based index of the question on screen ("7 / 20"); after Check the card stays on
  the checked question until Next.
- A failed Check keeps the selection and shows the error; pressing Check again retries (FR-015).
- The summary lives in memory only: a reload on `/review/summary` returns to the overview.
- Questions in `skippedQuestionIds` (deprecated after start) are filtered out client-side.
- Weak mode (006): `?mode=weak` on `/review` selects it (the URL stays the source of truth, omitted
  for regular); the Today card then shows `weakCount`, hides the new-question limit and sends
  `mode: 'weak'`. A 409 "Nothing to drill" is shown inline and refreshes the counts. "Review again"
  keeps the mode; "Drill these" on the summary starts a weak session over the summary's scope.
  Regular requests carry no `mode` (the server default).
