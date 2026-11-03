# Implementation Plan: Activity and streak

**Branch**: `007-activity-streak` | **Date**: 2026-11-03 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/007-activity-streak/spec.md` (approved 2026-11-03).

## Summary

A read-only activity card on the review overview: current streak (alive through yesterday while today has
no answer yet), longest streak, and 14 daily answer counts, all in the learner's time zone. The API reads
the local learner's question attempts (`attempts.answered_at`), converts each to a calendar date in the
requested IANA zone and hands the dates to a pure `ActivityCalculator`. `GET /api/review/activity?tz=`
validates the zone (400 when unknown, UTC when absent). The web card requests the browser's zone and draws
CSS bars with a visually hidden text alternative. No schema change; the API image gains `tzdata`.

## Technical Context

**Language/Version**: C# 14 / .NET 10; TypeScript strict / Angular 22 (zoneless, signals)

**Primary Dependencies**: existing stack only. Runtime image gains the Alpine `tzdata` package (OS data,
not a NuGet/npm dependency).

**Storage**: PostgreSQL, read-only; no migration. Reads `attempts (user_id, item_type, answered_at)`.

**Testing**: xUnit 2.9 + Shouldly with `[Trait("Requirement","FR-xxx")]` — Application unit tests for
`ActivityCalculator`; Testcontainers tests for `ActivityQueries` with `FakeTimeProvider` wrapped in
`PostgresTimeProvider`; Api tests for the endpoint. Vitest for the card. Playwright walkthrough at the end.

**Target Platform**: local Docker Compose (Alpine runtime image) and GitHub Actions (ubuntu).

**Project Type**: web service + SPA (same repo)

**Performance Goals**: card visible < 1 s with a year of daily reviews (SC-002): ~7k timestamps, one
indexed query, conversion in memory.

**Constraints**: learner-scoped; language-independent payload; Atlas tokens only; no horizontal scroll at
375 px.

**Scale/Scope**: 1 endpoint, 1 query class, 1 pure calculator, 1 web component, Dockerfile one-liner.

## Decisions

| # | Decision | Source |
|---|---|---|
| D1 | Day = calendar date of `answered_at` in the requested IANA zone; no `tz` → UTC; unknown → 400. | user (spec) |
| D2 | Any question attempt counts (any correctness, mode, scope, deprecation); katas never. | user (spec) |
| D3 | Zone conversion in .NET (`TimeZoneInfo.FindSystemTimeZoneById`), not SQL `AT TIME ZONE`: one zone database (the one the validator uses), pure unit-testable math. | default |
| D4 | The query returns raw timestamps (no SQL grouping); ~7k rows a year is cheap. | default |
| D5 | Days after "today" (clock skew) are ignored by the calculator. | default |
| D6 | Payload echoes the resolved `timeZone` and `today`; no `lang` parameter (no text in the payload). | default |
| D7 | Alpine runtime image installs `tzdata`; without it every non-UTC zone would be "unknown" in the container while tests pass on macOS/ubuntu. | research |

Details: [research.md](research.md).

## Constitution Check

| Principle | Status | Note |
|---|---|---|
| I content over platform | PASS | no content change |
| II content is data | PASS | none |
| III English first | PASS | EN + UK copy, payload has no text |
| IV progress user-scoped | PASS | filters `LocalLearner.Id` |
| V scheduling interface | PASS | reads attempts only; SM-2 untouched |
| VI LLM only for judgment | PASS | no LLM |
| VII boring architecture | PASS | one query class, one pure calculator, one endpoint |
| VIII modern Angular | PASS | standalone, OnPush, signals, `httpResource` |
| IX one command | PASS | no migration; `tzdata` baked into the image |
| X tests where they pay | PASS | streak math unit-tested; zone boundaries integration-tested |

No violations. Re-checked after Phase 1: unchanged.

## Project Structure

### Documentation (this feature)

```text
specs/007-activity-streak/
├── spec.md, plan.md, research.md, data-model.md, quickstart.md
├── contracts/activity-api.md       # GET /api/review/activity
├── checklists/requirements.md
└── tasks.md
```

### Source Code (delta)

```text
src/Application/Review/   ActivityCalculator (new), ActivityDtos (new: ActivityDto, ActivityDayDto),
                          IActivityQueries (new)
src/Infrastructure/       Review/ActivityQueries (new), DependencyInjection (+IActivityQueries)
src/Api/                  Endpoints/Review/GetActivityEndpoint (new: request, validator, endpoint), Dockerfile (+tzdata)
tests/Application.Tests/Review/   ActivityCalculatorTests (new)
tests/Infrastructure.Tests/Review/ ActivityQueriesTests (new)
tests/Api.Tests/Review/           ActivityEndpointTests (new)
web/src/app/review/       api/activity.models.ts (new), activity-card/ (new), review-overview (hosts the card)
web/src/app/shared/i18n/  messages.en.ts, messages.uk.ts
```

### Declared signatures

```csharp
// Application/Review
public sealed record ActivityDayDto(DateOnly Date, int Answers);
public sealed record ActivityDto(string TimeZone, DateOnly Today, int CurrentStreak, bool ActiveToday,
                                 int LongestStreak, IReadOnlyList<ActivityDayDto> Days);
public static class ActivityCalculator
{
    public const int WindowDays = 14;
    public static ActivityDto Calculate(string timeZone, DateOnly today, IEnumerable<DateOnly> answerDays);
    // US2: the 14 daily counts; until then Calculate returns an empty Days list.
    public static IReadOnlyList<ActivityDayDto> DailyCounts(DateOnly today, IEnumerable<DateOnly> answerDays);
}
public interface IActivityQueries
{
    Task<ActivityDto> GetAsync(TimeZoneInfo zone, CancellationToken ct = default);
}
// Infrastructure/Review
public sealed class ActivityQueries(AppDbContext db, TimeProvider clock) : IActivityQueries;
// Api/Endpoints/Review
public sealed class GetActivityRequest { [QueryParam] public string? Tz { get; set; } }
public sealed class GetActivityEndpoint(IActivityQueries activity) : Endpoint<GetActivityRequest, ActivityDto>;
```

**Structure Decision**: activity is progress data, so it lives in the review module next to readiness and
weak spots; the card is a child of the review overview.

## Steps (red → green per story; app runnable after each)

1. Setup: contract, Dockerfile `tzdata`.
2. **US1** streak (FR-001, FR-002, FR-003, FR-005, FR-006, FR-007, FR-009): stubs → tests → red →
   implementation → green; then the web card with streak, hint, empty and error states.
3. **US2** 14-day chart (FR-004, FR-008): stubs → tests → red → implementation → green; then bars and text
   alternative in the card.
4. Wrap-up: reviewers, trace, `gate run --profile pr`, `lockflight ci`, `lockflight review`, Playwright
   walkthrough (EN/UK, light/dark, 375 px), README screenshot, memory-bank.
