# Contract: GET /api/review/activity

The local learner's answer activity in a time zone. No `lang`: the payload has no text.

## Query

| Name | Required | Meaning |
|---|---|---|
| `tz` | no | IANA time zone id, e.g. `Europe/Kyiv`. Absent or empty → `UTC`. |

## Response 200

```json
{
  "timeZone": "Europe/Kyiv",
  "today": "2026-11-03",
  "currentStreak": 4,
  "activeToday": false,
  "longestStreak": 9,
  "days": [
    { "date": "2026-10-21", "answers": 0 },
    { "date": "2026-10-22", "answers": 12 },
    "… 14 entries in all, oldest first …",
    { "date": "2026-11-03", "answers": 0 }
  ]
}
```

- `days` always has 14 entries ending at `today`.
- `currentStreak` counts the run ending today, or ending yesterday while `activeToday` is false.

## Response 400

Problem details (`application/problem+json`) when `tz` is not a known time zone, with an error on `tz`.
