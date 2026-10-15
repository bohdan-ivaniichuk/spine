# 7. Concept review scope, readiness and concept notes

- **Status:** Accepted
- **Date:** 2026-10-15
- **Feature:** [004 Knowledge atlas and concept notes](../../../specs/004-knowledge-atlas-notes/spec.md)

## Context

Feature 004 opens the "Atlas" section: browse domain → topic → concept, read the concept note, see how
ready each part is, and start a review from wherever the learner is. Three choices needed a decision:
how a single-concept review is stored (ADR 0005 asked to revisit scope in 004/005), what "ready" means
with the current scheduler, and how notes are rendered safely.

## Decision

**Concept scope as a column.** `sessions.scope_concept_slug text NULL` next to the domain and topic
slugs; `ck_sessions_scope_type` allows `concept`, and `ck_sessions_scope` requires domain + topic +
concept for it and a null concept for every other type. Migration `AddConceptSessionScope` is additive;
its down step widens concept sessions to their topic so sessions and attempts survive a rollback.
Rejected: a generic `scope_path` (rewrites rows and checks) and deriving the concept from
`question_ids` (summary, resume and "Review again" need the stored scope).

**Readiness is derived, never stored.** A question is ready when its review state has
`interval_days >= 7` (three correct answers in a row since the last miss under SM-2). Ease is not used:
this SM-2 variant never raises ease after a miss, so an ease threshold would block a question forever;
an interval threshold is also algorithm-neutral for FSRS. Only schedulable questions count — the same
`Eligible` filter the session planner uses (today: non-deprecated MCQs) — so readiness widens on its own
when open questions become reviewable. A concept is ready when all of them are ready, in progress when
any has a review state, not started otherwise; concepts without schedulable questions have no state.
Topic and domain percentages are the floor of ready / counted concepts (a domain aggregates concepts,
not topic percentages), null when nothing counts.

**Served by the review module.** `GET /api/review/readiness` (constant 4 queries, no text, `lang`
accepted and ignored) rather than extending the knowledge tree: the tree is cached app-wide and the
knowledge read model stays free of learner data (Principle IV). The web joins both by slug.

**Notes: markdown-it + highlight.js core.** `markdown-it` with `html: false` (raw HTML escaped, unsafe
link schemes rejected), rendered to a string and bound with `[innerHTML]` so Angular's sanitizer is a
second layer; no `bypassSecurityTrust*`. `highlight.js/lib/core` registers only csharp, typescript,
javascript, sql, json, xml/html and bash/shell. Both load only in the lazy concept-page chunk (≈ 60 kB
gzipped); the initial bundle is unchanged. Code colours are Atlas tokens for both themes (≥ 4.5:1).
Rejected: `marked` (no switch to escape HTML), `ngx-markdown` (extra Angular layer, Prism), Shiki (WASM
weight).

## Consequences

- Readiness thresholds are fixed in `ReadinessRule` and repeated in one SQL predicate; a boundary test
  keeps them in step. Changing the scheduler's interval progression changes what "ready" means.
- A fourth scope type touches every scope consumer (validator, planner, summary, web models); the
  compiler and the scope tests cover them.
- Internal links inside notes are plain hrefs (full reload) until notes start cross-linking.
