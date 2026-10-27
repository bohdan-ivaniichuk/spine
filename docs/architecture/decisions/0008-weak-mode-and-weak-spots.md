# 8. Weak review mode and weak-spots statistics

- **Status:** Accepted
- **Date:** 2026-10-27
- **Feature:** [006 Weak spots](../../../specs/006-weak-spots/spec.md)

## Context

Feature 006 lets the learner drill what they last got wrong and see their weakest concepts. Two things
needed a decision: how a "weak" session is told apart from a regular one, and where "wrong" and
"weak" come from.

## Decision

**Session mode as a column.** `sessions.mode text NOT NULL DEFAULT 'regular'` with
`ck_sessions_mode IN ('regular','weak')` (migration `AddSessionMode`). Resume, the summary and "Review
again" need the mode, and it cannot be derived from the frozen membership afterwards. Weak sessions store
the default new limit, which is not applied in weak mode.

**Weakness comes from attempts, not from the scheduler.** A question is weak when the local learner's
latest attempt on it has `is_correct = false`; SM-2 fields are not used, so the definition survives a
move to FSRS (Principle V). Weak sessions take the schedulable questions in scope (the shared `Eligible`
filter) that are weak, newest miss first, capped at the total limit, ignoring due dates; answers then go
through the usual scheduling.

**Statistics.** `GET /api/review/weak-spots` ranks concepts by accuracy over the last 30 days (at least 3
answers; ties by more wrong answers, then the most recent answer), top 20, and topics the same way over
their concepts' answers, top 10. Slugs and figures only — titles are joined on the client, so the payload
does not depend on the language. Readiness per ranked concept reuses the 004 calculator.

## Consequences

- Feature 005 must always set `is_correct` for LLM-graded answers (for example from a score threshold);
  otherwise those answers count as answered but never as wrong.
- No new index: the latest-answer lookup uses `ix_attempts_user_id_item_id_answered_at`; revisit if a
  query plan shows the 30-day scan matters.
