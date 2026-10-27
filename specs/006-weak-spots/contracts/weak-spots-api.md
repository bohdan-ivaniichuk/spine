# Contract: GET /api/review/weak-spots

The local learner's weakest concepts and topics. `lang` is accepted for consistency, validated (400 when
unsupported) and ignored: the payload has slugs and figures only; the client joins titles from the tree.

## Response 200

```json
{
  "since": "2026-09-27T10:00:00+00:00",
  "windowDays": 30,
  "minAnswers": 3,
  "concepts": [
    {
      "domain": "dotnet", "topic": "async", "concept": "sync-context",
      "answers": 6, "wrong": 4, "accuracy": 0.3333,
      "lastAnsweredAt": "2026-10-26T18:02:11+00:00",
      "weakQuestions": 2,
      "readiness": { "concept": "sync-context", "state": "in-progress", "questions": 6, "readyQuestions": 1 }
    }
  ],
  "topics": [
    {
      "domain": "dotnet", "topic": "async",
      "answers": 14, "wrong": 6, "accuracy": 0.5714,
      "lastAnsweredAt": "2026-10-26T18:02:11+00:00",
      "weakQuestions": 3
    }
  ]
}
```

- Window: answers with `answeredAt >= since` (`since = now − windowDays`); only non-deprecated
  schedulable questions count.
- `accuracy = (answers − wrong) / answers`.
- Concepts: `answers >= minAnswers`, ordered by accuracy ascending, wrong descending, last answered
  descending, slug; at most 20.
- Topics: figures summed over their concepts, same qualification and order; at most 10.
- `weakQuestions`: questions whose latest answer is wrong (not windowed).
- `readiness`: as `GET /api/review/readiness` reports for that concept, or `null` when the concept has no
  schedulable questions.
- Empty lists when nothing qualifies.
