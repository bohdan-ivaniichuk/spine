# Research: Weak spots

## R1. Where "last answer wrong" comes from

- **Decision**: from `attempts` (latest attempt per question for the local learner, `is_correct = false`).
- **Rationale**: `review_states` cannot tell it reliably; SM-2's `repetitions == 0` would happen to match
  but ties the feature to one algorithm (Principle V).
- **Query**: "first per group" — `GroupBy(QuestionId).Select(g => g.OrderByDescending(AnsweredAt)
  .ThenByDescending(Id).First())`, which EF Core 10 + Npgsql translates to `ROW_NUMBER() OVER`, using
  `ix_attempts_user_id_item_id_answered_at`. Fallback if composition fails: an anti-join
  (`!Attempts.Any(b => same question && b.AnsweredAt > a.AnsweredAt)`).
- **Alternatives**: raw `DISTINCT ON` via `FromSql` (not composable with `Eligible`).

## R2. Session mode storage

- **Decision**: `sessions.mode text NOT NULL DEFAULT 'regular'` + `ck_sessions_mode`, enum `SessionMode`.
- **Rationale**: resume, summary and "Review again" must keep the mode; derivation from membership is
  impossible after the fact.
- **Alternatives**: jsonb flags (no convention, no check).

## R3. Start path

- **Decision**: `StartWeakAsync(scope, totalLimit, language)` sharing the transactional core of
  `StartAsync` (advisory lock → open-session lock → plan → nothing? refuse : close open → insert).
  An empty plan throws `NothingToDrill` and rolls back, so the open session stays (FR-005).
- **Rationale**: no change to existing call sites; no meaningless `newLimit`.

## R4. Statistics

- **Decision**: window `[now − 30 days, now]` from the injected `TimeProvider`; per concept over
  attempts joined to `Eligible(All)`: answers, wrong, last answered; weak questions (current, not
  windowed); live concept slugs; readiness counts for the ranked concepts only. Pure `WeaknessRanker`:
  concepts with ≥ 3 answers, accuracy ascending (compared by cross-multiplication), then wrong
  descending, then last answered descending, then slug; top 20. Topics aggregate their concepts'
  figures, qualify with ≥ 3 answers, same order, top 10.
- **Rationale**: constant query count; deterministic ranking; FR-011 reuses the 004 calculator.

## R5. Web

- **Decision**: `/weak` route with a component-scoped `httpResource`; titles from `KnowledgeTreeSource`;
  drill links preselect (`/review?mode=weak&scope=…`); the overview toggle writes `mode=weak` to the URL
  and shows `weakCount`; the summary's "Drill these" starts a weak session over the session's scope.
- **Rationale**: same patterns as the Atlas and readiness (004); URL stays the source of truth.

## R6. Lockflight red/green constraints

- All signatures, DTO fields, enum values and the migration exist before each `redgreen red` (test
  projects lock afterwards).
- Tests count relative to what they seed, never fixture totals; no full-DTO equality on DTOs that gain
  fields.
- Where a stub returns a value (`WeakCount`), assert with xUnit `Assert.*` without a message so the red
  is RED_ASSERT; API tests assert the status first with `Assert.Equal`.
