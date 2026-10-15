# Knowledge feature — the Atlas (004)

Browse domain → topic → concept and read concept notes; readiness per level and links into a scoped
review. Codes against `GET /api/knowledge/tree`, `GET /api/knowledge/concepts/{domain}/{topic}/{concept}`
and `GET /api/review/readiness` (`specs/004-knowledge-atlas-notes/contracts/readiness-api.md`).

| Path                                           | Role                                                                                                               |
| ---------------------------------------------- | ------------------------------------------------------------------------------------------------------------------ |
| `knowledge.routes.ts`                          | `/atlas`, `/atlas/:domain`, `/atlas/:domain/:topic`, lazy `/atlas/:domain/:topic/:concept`; slugs arrive as inputs |
| `api/`                                         | Tree, concept and readiness models; `KnowledgeTreeSource` (root, shared with the rail and the review overview)     |
| `atlas-layout/`                                | Breadcrumbs from the URL + tree, `<router-outlet>`; reloads the tree and provides `ReadinessStore` per visit       |
| `atlas-home/`, `atlas-domain/`, `atlas-topic/` | The levels; topics without concepts are "soon" (not links)                                                         |
| `concept-page/`, `concept-note/`               | Note page (lazy chunk); `renderNote` = markdown-it (`html: false`) + highlight.js core, bound via `[innerHTML]`    |
| `readiness/`                                   | `ReadinessStore`, `ReadinessBar` (no number when `percent` is null), `ReadinessChip`                               |
| `badges/`                                      | `EnBadge` (UK fallback), `UnreviewedBadge`; reused by the review session card                                      |
| `not-found/`, `atlas-status/`                  | Unknown/deprecated paths; tree loading/error state                                                                 |
| `testing/atlas-fixtures.ts`                    | Tree, concept and readiness fixtures for the specs                                                                 |

Known limitation: links inside notes are rendered as plain `href`s, so an internal link (e.g. to
`/atlas/…`) does a full page reload instead of a router navigation; external `http(s)` links open in
a new tab with a screen-reader hint. Intercepting internal links is not handled yet.
