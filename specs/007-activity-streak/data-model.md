# Data Model: Activity and streak

No schema change. Everything is derived per request.

## Source

- **attempts** (existing): `user_id`, `item_type`, `answered_at timestamptz`. Filter: `user_id =
  LocalLearner.Id` and `item_type = 'question'` (katas excluded). Correctness, session, mode and the
  question's deprecation are ignored.

## Derived

- **Answer day** — `DateOnly`: the calendar date of `answered_at` in the requested zone.
- **ActivityDayDto** — `Date` (`DateOnly`), `Answers` (≥ 0).
- **ActivityDto**
  - `TimeZone` — the resolved IANA id (`UTC` when none was requested).
  - `Today` — today's date in that zone (from the injected clock).
  - `CurrentStreak` — ≥ 0; run ending today, or yesterday while today is inactive.
  - `ActiveToday` — today has ≥ 1 answer.
  - `LongestStreak` — ≥ `CurrentStreak`.
  - `Days` — exactly 14 entries, `Today − 13` … `Today`, oldest first.

## Validation

- `tz`: absent or empty → UTC; otherwise must resolve with `TimeZoneInfo.FindSystemTimeZoneById`, else 400
  problem with an error on `tz`.
