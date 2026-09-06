# Data model delta — 002 Review session

Additive only; no 001 column is renamed, retyped or dropped (SC-006 of 001 holds).

## Schema delta (approved 2026-09-06)

The spec's Key Entities paragraph says both "gains no new columns" and "stored with the session as an ordered list of question ids". Those cannot both be true; the second is the requirement (FR-011, SC-005), so the plan adds columns. Additive only — no 001 column is renamed, retyped or dropped, no existing CHECK changes.

`sessions`:
- `question_ids text[] NOT NULL DEFAULT '{}'` — ordered membership. `text[]` over `jsonb` because `tags` already uses the `TextArray()` convention (with its `ValueComparer`), membership checks are `question_id = ANY(question_ids)`, and a Postgres array keeps order.
- `total_limit int NOT NULL DEFAULT 20`, `new_limit int NOT NULL DEFAULT 10` — FR-003 "record the new session with its scope and limits".

`attempts`:
- partial unique index `ix_attempts_session_id_item_id ON attempts (session_id, item_id) WHERE session_id IS NOT NULL` — the database-level guarantee for FR-005 (double-click race). Configured with `e.HasIndex(x => new { x.SessionId, x.ItemId }).IsUnique().HasFilter("session_id IS NOT NULL")`. `item_id` is a stored generated column, so it is indexable. See Open question 2 regarding feature 004.

EF mapping in `SessionConfiguration`: `e.Property(x => x.QuestionIds).TextArray().HasDefaultValueSql("'{}'")`, `e.Property(x => x.TotalLimit).HasDefaultValue(20)`, `e.Property(x => x.NewLimit).HasDefaultValue(10)`. Entity: `public string[] QuestionIds { get; set; } = [];`, `public int TotalLimit { get; set; }`, `public int NewLimit { get; set; }`.

Migration name: `AddSessionMembership`. Procedure (from `/Users/bogdanivaniichuk/Interview Preparation/.claude/skills/ef-migrations/SKILL.md`):
1. `docker compose up -d postgres`; `export ConnectionStrings__Default="Host=localhost;Port=5432;Database=interviewprep;Username=interviewprep" PGPASSWORD=<from .env>`.
2. `dotnet ef migrations add AddSessionMembership --project src/Infrastructure --startup-project src/Api --output-dir Persistence/Migrations`.
3. `dotnet ef migrations script --project src/Infrastructure --startup-project src/Api --idempotent -o /tmp/schema.sql` then `grep -E "ALTER TABLE sessions|text\[\]|CREATE UNIQUE INDEX ix_attempts_session_id_item_id|WHERE session_id" /tmp/schema.sql`. Expect: three `ADD COLUMN`s with defaults, one partial unique index, nothing else.
4. `git add` the `.cs`, `.Designer.cs` and `AppDbContextModelSnapshot.cs`; `rm -rf 'src/Api/bin\Debug' 'src/Infrastructure/bin\Debug'` if they appear.
5. Update `tests/Infrastructure.Tests/Persistence/MigrationsTests.cs`: `GetAppliedMigrationsAsync()).Count().ShouldBe(1)` becomes `2`; add `unique.ShouldContain("ix_attempts_session_id_item_id")`.


## Domain — scheduling types (pure)

Namespace `InterviewPrep.Domain.Scheduling`. No package references (System.Text.Json is in the BCL).

```csharp
public enum ReviewGrade { Again = 1, Good = 4 }   // enum values are the SM-2 quality numbers

/// Immutable view of a review_states row; New(now) is the state of a never-answered item.
public sealed record ReviewSnapshot(int IntervalDays, double Ease, int Repetitions,
    DateTimeOffset? LastReviewedAt, DateTimeOffset DueAt, string? AlgorithmState)
{
    public static ReviewSnapshot New(DateTimeOffset now) => new(0, 2.5, 0, null, now, null);
}

public sealed record SchedulingResult(int IntervalDays, double Ease, int Repetitions,
    DateTimeOffset LastReviewedAt, DateTimeOffset DueAt, string AlgorithmState);

public interface ISchedulingAlgorithm
{
    string Name { get; }                                   // "sm2" → review_states.algorithm
    SchedulingResult Next(ReviewSnapshot current, ReviewGrade grade, DateTimeOffset now);
}
```

`Sm2Algorithm` (`Name = "sm2"`, `EaseFloor = 1.3`, `WrongEasePenalty = 0.2`):
- `Good`: `reps = current.Repetitions + 1`; `interval = reps == 1 ? 1 : reps == 2 ? 6 : Math.Max(1, (int)Math.Round(current.IntervalDays * current.Ease, MidpointRounding.AwayFromZero))`; `ease = current.Ease`.
- `Again`: `reps = 0`; `interval = 1`; `ease = Math.Max(1.3, current.Ease - 0.2)`.
- Always: `LastReviewedAt = now`, `DueAt = now.AddDays(interval)`, `AlgorithmState = $"{{\"lastQuality\":{(int)grade}}}"` (serialize via `JsonSerializer` with a tiny `Sm2State(int LastQuality)` record; the string is what goes into `algorithm_state jsonb`).
- Deliberate deviation from canonical SM-2: q=4 leaves ease unchanged (that is also canonical), but q=1 subtracts 0.2 rather than 0.54 — the spec (FR-008) fixes 0.2; document it in the class summary. Use `MidpointRounding.AwayFromZero` explicitly so 37.5 → 38 and the tests are not at the mercy of banker's rounding.

Application-side application of a result to the entity: `ReviewState.Apply(SchedulingResult r, string algorithm)` — a small method on the entity (sets the six fields + `UpdatedAt`) keeps the mapping in one place; add `ReviewSnapshot ToSnapshot()` next to it.


## Application use cases


Follow the 001 pattern (interface + DTOs + pure logic in Application; EF implementation in Infrastructure), which is how `IKnowledgeQueries`/`IContentImporter` already work. No repository abstraction, no MediatR.

`ReviewScope` (Application): `sealed record ReviewScope(SessionScopeType Type, string? Domain, string? Topic)` with `static ReviewScope All`, `ForDomain(slug)`, `ForTopic(domain, topic)` and a `Validate()` that enforces the same rule as `ck_sessions_scope`.

`IReviewSessions` (one interface, five use cases; `now` is never a parameter — the implementation reads `TimeProvider.GetUtcNow()` once per call and passes it into pure code):

```csharp
public interface IReviewSessions
{
    Task<ReviewOverviewDto> GetOverviewAsync(ReviewScope scope, CancellationToken ct);
    Task<SessionDto> StartAsync(ReviewScope scope, int totalLimit, int newLimit, CancellationToken ct);
    Task<SessionDto?> GetCurrentAsync(CancellationToken ct);           // open session or null
    Task<SessionDto?> GetAsync(Guid sessionId, CancellationToken ct);   // null → 404
    Task<AnswerFeedbackDto> AnswerAsync(Guid sessionId, string questionId, int selectedIndex, CancellationToken ct);
    Task<SessionSummaryDto> FinishAsync(Guid sessionId, CancellationToken ct);  // idempotent
}
```

Errors: `ReviewException(ReviewError Kind, string Detail)` with `enum ReviewError { ScopeNotFound, NothingToReview, SessionNotFound, SessionFinished, QuestionNotInSession, DuplicateAnswer }`. Application knows nothing about HTTP; the Api maps kinds to statuses (section 4).

Pure helpers (unit-tested without a database):
- `SessionPlanner.Plan(IReadOnlyList<string> dueIdsOrdered, IReadOnlyList<string> newIdsOrdered, int totalLimit, int newLimit)` → `string[]`: take `due` up to `totalLimit`, then `new` up to `Math.Min(newLimit, totalLimit - dueTaken)`. Newly-taken count is 0 when due already fills the session (US2-6).
- `SessionSummaryBuilder.Build(session, IReadOnlyList<AnsweredItem> attempts, int dueTomorrow)` where `AnsweredItem(questionId, isCorrect, ConceptRef(domainSlug, topicSlug, conceptSlug, conceptTitle))` → counts, `accuracy = answered == 0 ? null : correct / (double)answered`, weak concepts = group by concept where wrong ≥ 1, ordered by `wrong desc, domain asc, topic asc, concept asc` (the spec's "then by slug", made total).

`ReviewOptions` (`DefaultTotalLimit = 20`, `DefaultNewLimit = 10`, `MinLimit = 1`, `MaxLimit = 100`) bound from configuration keys `REVIEW_DEFAULT_TOTAL_LIMIT` / `REVIEW_DEFAULT_NEW_LIMIT`; the overview DTO carries the defaults so the client never hardcodes them.

`ReviewSessions` (Infrastructure, `AppDbContext db, ISchedulingAlgorithm algorithm, TimeProvider clock, IOptions<ReviewOptions> options`):

Eligibility query, shared by overview/start/finish (`AsNoTracking`):
`db.Questions.Where(q => q.Type == QuestionType.Mcq && q.DeprecatedAt == null && q.Concept!.DeprecatedAt == null && q.Concept.Topic!.DeprecatedAt == null && q.Concept.Topic.Domain!.DeprecatedAt == null)` plus scope filter on `Concept.Topic.Domain.Slug` / `Concept.Topic.Slug`. Scope existence is validated first (domain/topic lookup, non-deprecated) → `ScopeNotFound`.

- Due set: join `db.ReviewStates.Where(r => r.UserId == LocalLearner.Id && r.ItemType == ItemType.Question)` on `QuestionId`, `r.DueAt <= now`, ordered `r.DueAt asc, q.Id asc` (most overdue first, deterministic tie-break).
- New set: eligible where `!db.ReviewStates.Any(r => r.UserId == uid && r.QuestionId == q.Id)`, ordered `Domain.SortOrder, Topic.SortOrder, Concept.SortOrder, q.SortOrder, q.Id`.
- `GetOverviewAsync`: counts of both sets plus `nextDueAt = Min(due_at) where due_at > now` in scope (null when none) and `totalEligible`.
- `StartAsync`: wrap in `db.Database.CreateExecutionStrategy().ExecuteAsync(...)` + one transaction (required because `EnableRetryOnFailure` is on; mirror `ContentImporter`). Inside: (1) load the open session (`FinishedAt == null`) for the learner; if present, compute its summary from its attempts and set `FinishedAt/AnsweredCount/CorrectCount`; (2) query due ids (`Take(totalLimit)`) and new ids (`Take(Math.Min(newLimit, totalLimit))`), `SessionPlanner.Plan`; empty → `NothingToReview`; (3) insert `Session` (`Guid.CreateVersion7()`, `StartedAt = CreatedAt = now`, scope, limits, `QuestionIds`); commit; return `GetAsync(id)`.
- `GetAsync`/`GetCurrentAsync`: load the session; one query for the questions `WHERE q.Id = ANY(session.QuestionIds)` projecting id, level, reviewed, deprecated flags of the chain, EN prompt/options, concept/topic/domain slug+title; re-order in memory by `QuestionIds` index; questions whose chain is deprecated go to `skippedQuestionIds` (FR-011) unless they already have an attempt in this session (then they stay so the counts line up); `isNew` = no review state existed before this session — simplest correct definition: `!db.ReviewStates.Any(...)` OR the state's `CreatedAt >= session.StartedAt`. Attempts for the session → `answers[]`. Options are projected to `string[]` (text only; `correct` never leaves the server).
- `AnswerAsync`: execution strategy + transaction. Load session (tracked) → `SessionNotFound` / `SessionFinished`; `QuestionIds.Contains(questionId)` else `QuestionNotInSession`; `db.Attempts.AnyAsync(a => a.SessionId == id && a.QuestionId == questionId)` → `DuplicateAnswer` (clean path); load EN translation options; `correctIndex = options.FindIndex(o => o.Correct)`; `isCorrect = selectedIndex == correctIndex`; insert `Attempt` (`Answer = {"selectedIndex":n,"selectedText":"…"}` via `JsonSerializer` with web defaults, `IsCorrect`, `SessionId`, `AnsweredAt = CreatedAt = now`); load `ReviewState` by `(UserId, QuestionId)` tracked or create (`Guid.CreateVersion7()`, `ItemType.Question`, `CreatedAt = now`); `state.Apply(algorithm.Next(state.ToSnapshot() ?? ReviewSnapshot.New(now), isCorrect ? Good : Again, now), algorithm.Name)`; `SaveChanges`; catch `DbUpdateException { InnerException: PostgresException { SqlState: "23505", ConstraintName: "ix_attempts_session_id_item_id" } }` → `DuplicateAnswer` (race path). Return feedback with `nextDueAt` and `intervalDays`.
- `FinishAsync`: execution strategy + transaction. Load session → `SessionNotFound`. Load attempts joined to concept refs. If open: set `FinishedAt = now`, counts, save. Always compute `dueTomorrow` = count of eligible-in-scope questions with a review state `due_at <= now + 24h` (this includes the ones just answered wrong: `answered_at + 1 day <= now + 1 day`). Return `SessionSummaryBuilder.Build(...)`. Calling it on an already-finished session returns the same summary without changing stored values.

DI (`AddInfrastructure`): `services.TryAddSingleton(TimeProvider.System); services.AddSingleton<ISchedulingAlgorithm, Sm2Algorithm>(); services.AddScoped<IReviewSessions, ReviewSessions>(); services.Configure<ReviewOptions>(configuration)` (bind the two env keys). Tests replace `TimeProvider` with `FakeTimeProvider`.


## Decision on the attempts unique index

Kept for 002. If feature 004 needs several attempts per item per session (kata resubmission), 004 drops or narrows the index in its own migration.
