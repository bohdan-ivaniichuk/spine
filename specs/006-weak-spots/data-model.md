# Data model: Weak spots

## Schema delta — migration `AddSessionMode`

`sessions`:

| Column | Type | Note |
|---|---|---|
| `mode` | `text NOT NULL DEFAULT 'regular'` | `ck_sessions_mode`: `mode IN ('regular','weak')` |

Existing rows become `regular`. Down drops the column (weak sessions read back as regular; attempts
untouched). Weak sessions store the default `new_limit`, which is not applied in weak mode.

## Domain

- `SessionMode { Regular, Weak }` with db values `regular` / `weak`.
- `Session.Mode : SessionMode`.
- `WeaknessRule`: `WindowDays = 30`, `MinAnswers = 3`, `MaxConcepts = 20`, `MaxTopics = 10`.

## Application

- `IReviewSessions.StartWeakAsync(ReviewScope scope, int totalLimit, Language language, CancellationToken)`.
- `SessionPlanner.PlanWeak(IReadOnlyList<string> weakIdsNewestMissFirst, int totalLimit)`.
- `ReviewError.NothingToDrill`.
- DTOs (fields appended with defaults): `SessionDto.Mode`, `SessionSummaryDto.Mode` (`"regular"` |
  `"weak"`), `ReviewOverviewDto.WeakCount`.
- `IWeakSpotsQueries.GetAsync(CancellationToken) → WeakSpotsDto`.
- `WeaknessRanker.Rank(IReadOnlyList<ConceptAnswerStats>) → (concepts, topics)` where
  `ConceptAnswerStats(Domain, Topic, Concept, Answers, Wrong, LastAnsweredAt, WeakQuestions)`.

## Derived (never stored)

- **Weak question**: schedulable (`Eligible`) question whose latest attempt by the local learner has
  `is_correct = false`.
- **Concept weakness** over `answered_at >= now − 30 days`: answers, wrong, accuracy = (answers − wrong) /
  answers, last answered; plus current weak questions (not windowed).
- **Topic weakness**: sums of its concepts' figures, max last answered.
- Ranking: answers ≥ 3; accuracy ascending; wrong descending; last answered descending; slug.
