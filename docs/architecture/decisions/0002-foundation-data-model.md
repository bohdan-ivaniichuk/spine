# 2. Foundation data model: keys, polymorphic progress, soft-deprecation, content hashes

- **Status:** Accepted
- **Date:** 2026-09-06

## Context

Feature 001 lays down the relational model every later feature builds on: the content graph
(domains → topics → concepts → questions/katas, with per-language translations) and the learner's
progress (attempts, review states, sessions). The content contract (`content/schema/*.json`) makes
question and kata ids immutable and globally unique, while domain/topic/concept slugs are only unique
within their parent. The taxonomy says "never rename a slug, deprecate instead". The spec clarifications
(2026-09-06) fixed four choices: soft-deprecation, review states for questions **and** katas in one
table, a content manifest for domain/topic order and titles, and an optional session link on attempts.

## Decision

1. **Surrogate `int` keys** for `domains`, `topics`, `concepts` (slug unique within the parent);
   **natural `text` keys** for `questions` and `katas` (the content id). Progress rows therefore carry
   human-readable ids and survive a database rebuild followed by a re-import.
2. **Polymorphic item reference** on `review_states` and `attempts`: `item_type` + two nullable FKs
   (`question_id`, `kata_id`) + a CHECK that exactly one is set and matches the type + a stored generated
   column `item_id = COALESCE(question_id, kata_id)`. Real referential integrity, one unique index
   `(user_id, item_id)`, and the spec's literal key (learner, item type, item id).
3. **Soft-deprecation** via `deprecated_at` on all five content entities. The importer never deletes;
   absent items are deprecated, reappearing items reactivated. Progress rows are never touched by import.
4. **Content hashes** (`content_hash`, SHA-256 over a canonical serialization) on every content and
   translation row; `updated_at` moves only when the hash changes, and unchanged entities are never
   assigned to, so a repeat import emits zero UPDATEs.
5. **Derived `concepts.reviewed`** = every non-deprecated question and kata under the concept is
   reviewed; the schema has no concept-level flag and 001 does not change the schema.
6. **Text enums with CHECK constraints** instead of PostgreSQL enum types; `sort_order` instead of the
   reserved `order`; session scope as `(scope_type, scope_domain_slug, scope_topic_slug)` because topic
   slugs are only unique within a domain; `review_states.algorithm` (default `sm2`) so FSRS rows can
   coexist later.
7. **Manifest-driven ordering**: `content/manifest.json` is the source of domain/topic titles and order;
   a topic file whose (domain, topic) pair is not in the manifest is rejected.

Rejected: int keys everywhere (progress would reference opaque ints); dotted natural keys everywhere
(composite FKs); a `content_items` supertype table (extra insert per question); hard delete on import
(progress loss); PostgreSQL enum types (harder to evolve inside a migration transaction).

## Consequences

- Feature 002 gets `review_states`/`attempts` ready for both questions and katas without a migration.
- Feature 004 stores structured grading output in `attempts.grading jsonb` without a schema change.
- Re-importing content is safe to run at any time, including while the API serves reads (single
  transaction under an advisory lock; readers see the old or the new graph).
- Deprecated content stays visible to the tree/concept reads only with `includeDeprecated=true`.
