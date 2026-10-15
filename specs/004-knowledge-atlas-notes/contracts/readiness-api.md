# Contract: GET /api/review/readiness

Readiness of the local learner per domain, topic and concept, independent of the UI language (FR-010).
`lang` is accepted for consistency with the other review endpoints, validated (400 when unsupported) and
ignored. Served by the review module.

## Response 200

```json
{
  "domains": [
    {
      "domain": "dotnet",
      "ready": 1,
      "counted": 9,
      "percent": 11,
      "topics": [
        {
          "topic": "async",
          "ready": 1,
          "counted": 4,
          "percent": 25,
          "concepts": [
            { "concept": "task-vs-valuetask", "state": "ready", "questions": 5, "readyQuestions": 5 },
            { "concept": "sync-context", "state": "in-progress", "questions": 6, "readyQuestions": 2 },
            { "concept": "cancellation", "state": "not-started", "questions": 4, "readyQuestions": 0 }
          ]
        }
      ]
    }
  ]
}
```

- Every live (non-deprecated) domain and topic is present, in manifest order, even when nothing counts
  (`counted: 0`, `percent: null`).
- `concepts` lists only concepts with at least one schedulable question; others are omitted.
- `state`: `not-started` | `in-progress` | `ready` (see [data-model.md](../data-model.md)).
- `percent`: integer, `floor(100 * ready / counted)`, or `null` when `counted == 0`.
- Titles are not included; the client joins with `GET /api/knowledge/tree` by slug.

## DTOs

```text
ReadinessDto(IReadOnlyList<DomainReadinessDto> Domains)
DomainReadinessDto(string Domain, int Ready, int Counted, int? Percent, IReadOnlyList<TopicReadinessDto> Topics)
TopicReadinessDto(string Topic, int Ready, int Counted, int? Percent, IReadOnlyList<ConceptReadinessDto> Concepts)
ConceptReadinessDto(string Concept, string State, int Questions, int ReadyQuestions)
```
