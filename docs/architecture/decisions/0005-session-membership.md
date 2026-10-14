# 5. Review session membership stored on the session row

- **Status:** Accepted
- **Date:** 2026-10-14

## Context

Feature 002 (review session) needs a session whose question list is fixed when it starts: a reload
resumes the same questions in the same order (SC-005), each question takes at most one answer per
session (FR-005), and the summary covers exactly that list. The 001 schema has `sessions` (scope,
timestamps) and `attempts` (optional `session_id`), but no record of which questions belong to a
session.

Options considered:

1. **Join table** `session_questions (session_id, question_id, position)` — normalised, but one more
   table, entity and configuration for a list that is written once and never edited.
2. **`jsonb` column** on `sessions` — flexible, but order and membership checks are less natural and
   there is no existing convention for it.
3. **`text[]` column** on `sessions` — ordered, written once, membership check is
   `question_id = ANY(question_ids)`; `tags` already uses the `TextArray()` convention with its
   `ValueComparer`.

## Decision

Store membership as `sessions.question_ids text[] NOT NULL DEFAULT '{}'` (ordered), together with the
limits the session was started with (`total_limit`, `new_limit`). The list is computed by the
server-side planner at start and never changes afterwards. One answer per question per session is
enforced in the database by the partial unique index
`ix_attempts_session_id_item_id ON attempts (session_id, item_id) WHERE session_id IS NOT NULL`; the
API maps the violation (and the pre-check) to `409 Conflict`. Migration: `AddSessionMembership`
(additive only).

Related API decisions: "nothing to review" → `409`; `GET /api/review/sessions/current` → `204` when no
session is open; `finish` is idempotent.

## Consequences

- Resume and summary need no extra query beyond the session row and its attempts.
- Questions deprecated after a session started stay in the stored list, but the session payload
  reports them as `skippedQuestionIds` unless they were already answered (FR-011), so counts stay
  consistent.
- Per-question metadata inside a session (e.g. position-specific state) would require the join table;
  revisit if 004/005 need it.
- Feature 004 (open questions and katas in sessions) may need to narrow or extend the unique index,
  because `item_id` covers both questions and katas.
