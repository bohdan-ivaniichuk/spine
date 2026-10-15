# Research: Knowledge atlas and concept notes

## R1. Which questions count for readiness (D1)

- **Decision**: only questions the review flow can schedule, selected by the same `Eligible(scope)`
  queryable the session planner uses (today: non-deprecated MCQs of live concepts/topics/domains).
- **Rationale**: every concept has at least one open question, and open questions never get a
  `review_states` row until feature 005 schedules them. Counting them would keep readiness at 0%.
  Sharing the filter makes readiness widen automatically when 005 changes it.
- **Alternatives**: all questions (readiness stuck at 0% until 005); a separate "readiness filter"
  (drifts from the planner).

## R2. Readiness threshold (D2)

- **Decision**: a question is ready when `review_states.interval_days >= 7`.
- **Rationale**: `Sm2Algorithm` keeps ease on Good and subtracts 0.2 on Again (floor 1.3); ease never
  recovers, so "ease ≥ 2.5" would make one miss permanent. Interval progression on Good is 1 → 6 → 15,
  so `>= 7` means three correct answers in a row since the last miss. The rule is algorithm-neutral
  (FSRS also produces intervals).
- **Alternatives**: ease ≥ 2.5 and interval ≥ 7 (permanent penalty); let Good raise ease (changes
  scheduling and existing SM-2 tests).

## R3. Where readiness is served (D4)

- **Decision**: `GET /api/review/readiness`, no query parameters, served by `IReadinessQueries` in the
  review module.
- **Rationale**: the tree is a root-level cached resource and would go stale after a session;
  `IKnowledgeQueries` is content-only (Principle IV boundary); mirrors `/api/review/coverage`; no `lang`
  makes FR-010 true by construction.
- **Alternatives**: extend the tree DTO (stale, mixes learner data into content reads); per-concept
  readiness on the concept page (N requests).

## R4. Query shape

- **Decision**: two constant queries — live concepts with topic/domain slugs in manifest order; one
  grouped query `Eligible(All) LEFT JOIN States()` per concept with `Questions`, `Stated`, `Ready`
  counts. A pure `ReadinessCalculator` builds states and percentages (floor, `null` when nothing counts).
- **Rationale**: no N+1; same pattern as `GetCoverageAsync`; the math is unit-testable.

## R5. Concept session scope (D3)

- **Decision**: nullable `sessions.scope_concept_slug`; `ck_sessions_scope_type` allows `'concept'`;
  `ck_sessions_scope` requires domain + topic + concept for `concept`, and `scope_concept_slug IS NULL`
  for the other types. ADR 0007 records it (ADR 0005 asked to revisit scope in 004/005).
- **Rationale**: additive; existing rows remain valid; summary, resume and "Review again" need the
  stored scope.
- **Alternatives**: generic `scope_path` (rewrites rows and checks); derive the concept from
  `question_ids` (loses scope for summary/resume).

## R6. Markdown and highlighting (D6)

- **Decision**: `markdown-it` 14.x with `html: false`, `linkify: false`, `typographer: false`; tables
  via the built-in GFM table rule; `highlight` hook → `highlight.js/lib/core` with `csharp`,
  `typescript`, `javascript`, `sql`, `json`, `xml` (as `html`), `bash` (aliases `shell`, `sh`); unknown
  or `text` fences render escaped without highlighting. Render to a string, bind with `[innerHTML]`
  so Angular's sanitizer is a second layer; no `bypassSecurityTrust*`. Tables are wrapped in a
  horizontally scrollable container; `pre` blocks scroll horizontally.
- **Rationale**: markdown-it escapes raw HTML and rejects `javascript:`/`vbscript:`/`data:` links by
  default (FR-004). Content uses headings, lists, tables (14 notes) and fences csharp/sql/json/xml/bash.
  Both libraries load only in the lazy concept-page chunk (≈ 55 KB gz); the initial bundle is unchanged.
- **Alternatives**: `marked` (smaller, but no switch to escape HTML); `ngx-markdown` (extra Angular
  layer, Prism); Shiki (WASM/grammar weight); extending `prompt-markdown.parser.ts` (rebuilding a
  parser).
- **Theme**: new tokens `--color-code-{keyword,string,comment,type,number,title,attr}` for light and
  dark Atlas, each ≥ 4.5:1 on `--color-code-bg`, mapped from `.hljs-*` inside `.note`; hand-written
  prose styles with token utilities (no `@tailwindcss/typography`).

## R7. Angular data flow

- **Decision**: routes `/atlas`, `/atlas/:domain`, `/atlas/:domain/:topic`,
  `/atlas/:domain/:topic/:concept` under a lazy `KNOWLEDGE_ROUTES` with an `AtlasLayout` (breadcrumbs);
  `withComponentInputBinding()` for slug inputs. Tree from the existing root `KnowledgeTreeSource`
  (reloaded on Atlas entry); readiness from a `ReadinessStore` provided in `AtlasLayout` (fresh per
  Atlas visit); concept page via `httpResource` keyed by slug + `lang` (last request wins on a language
  switch). Prev/next and breadcrumbs come from the tree. "Soon" = a topic with zero live concepts.
- **Rationale**: no knowledge API change; component-scoped state; reload-safe URLs (FR-002).

## R8. Rounding and rail (D5, D7)

- Floor rounding so 100% appears only when every counted concept is ready.
- Rail domain tabs keep their 003 behaviour (Review scoped to a domain); inside the Atlas the active
  domain tab is highlighted only.
