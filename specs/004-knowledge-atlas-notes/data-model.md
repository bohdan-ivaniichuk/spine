# Data model: Knowledge atlas and concept notes

Content tables (`domains`, `topics`, `concepts`, `concept_translations`, `questions`, …) are unchanged.

## Schema delta — migration `AddConceptSessionScope`

`sessions`:

| Column | Type | Note |
|---|---|---|
| `scope_concept_slug` | `text NULL` | new; set only for `scope_type = 'concept'` |

Constraints (dropped and re-created):

- `ck_sessions_scope_type`: `scope_type IN ('all','domain','topic','concept')`
- `ck_sessions_scope`:
  - `all` → domain, topic, concept slugs all `NULL`
  - `domain` → domain `NOT NULL`, topic and concept `NULL`
  - `topic` → domain and topic `NOT NULL`, concept `NULL`
  - `concept` → domain, topic and concept `NOT NULL`

Existing rows stay valid (the new column is `NULL`). The down migration widens concept sessions to their
topic (keeping sessions and attempts), drops the column and restores the 003 constraints.

## Domain

- `SessionScopeType.Concept` (db value `"concept"`).
- `Session.ScopeConceptSlug : string?`.
- `ConceptReadiness { NotStarted, InProgress, Ready }` (wire values `not-started`, `in-progress`,
  `ready`).
- `ReadinessRule`: `MinIntervalDays = 7`; `IsReady(int intervalDays) => intervalDays >= MinIntervalDays`.

## Application

- `ReviewScope(Type, Domain, Topic, Concept)` with `ForConcept(domain, topic, concept)`; validity mirrors
  `ck_sessions_scope`.
- `ScopeDto(Type, Domain, Topic, DomainTitle, TopicTitle, Concept = null, ConceptTitle = null)` — the two
  new fields are appended, so existing shapes stay valid.
- `IReadinessQueries.GetAsync(CancellationToken) → ReadinessDto`.
- `ReadinessCalculator.Build(IReadOnlyList<DomainCounts>) → ReadinessDto` over the nested
  `DomainCounts(Domain, Topics)` → `TopicCounts(Topic, Concepts)` → `ConceptCounts(Concept, Questions, Stated,
  Ready)`, in manifest order.

## Derived readiness (not stored)

Per concept, over its schedulable questions (the session planner's `Eligible` filter) and the local
learner's `review_states` (`item_type = 'question'`):

| Condition | State |
|---|---|
| `Questions == 0` | omitted — no state, excluded from percentages |
| `Ready == Questions` | `ready` |
| `Stated > 0` | `in-progress` |
| otherwise | `not-started` |

`Ready` counts review states with `interval_days >= 7`.

Per topic / domain: `Ready` = ready concepts, `Counted` = concepts with `Questions > 0`,
`Percent = Counted == 0 ? null : floor(100 * Ready / Counted)`. Domains aggregate all their concepts.

## State transitions

Readiness follows review history: answering moves a concept from `not-started` to `in-progress` on the
first review state; it becomes `ready` once every schedulable question reaches an interval ≥ 7 days; a
wrong answer resets that question's interval (SM-2) and the concept drops back to `in-progress`.
