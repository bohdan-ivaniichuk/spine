# Architect plan — 002 Review session (solution-architect, 2026-09-06)

## Goal

Ship the daily review loop for multiple-choice questions: an overview that reports due/new counts for a scope (all / domain / topic), a server-built session whose membership is frozen at start and persisted so reload resumes deterministically, one-question-at-a-time MCQ cards with instant deterministic feedback, SM-2 scheduling behind `ISchedulingAlgorithm` (pure, unit-tested for every transition in User Story 2), and a summary with ranked weak concepts and "due tomorrow". Acceptance bar: FR-001…FR-015 met, SC-003 covered by Domain unit tests, SC-004 covered by an integration test with `FakeTimeProvider`, SC-005 covered by resume tests on both sides, both themes using token classes only, everything running under `docker compose up`.

## Affected layers / files

Backend (`src/**`, `tests/**`):
- `src/Domain/Scheduling/ISchedulingAlgorithm.cs`, `ReviewGrade.cs`, `ReviewSnapshot.cs`, `SchedulingResult.cs`, `Sm2Algorithm.cs` (new)
- `src/Domain/Progress/Session.cs` (+3 properties: `QuestionIds`, `TotalLimit`, `NewLimit`)
- `src/Application/Review/IReviewSessions.cs`, `ReviewDtos.cs`, `ReviewScope.cs`, `ReviewException.cs`, `SessionPlanner.cs`, `SessionSummaryBuilder.cs`, `ReviewOptions.cs` (new)
- `src/Infrastructure/Review/ReviewSessions.cs` (new), `src/Infrastructure/Persistence/Configurations/ProgressConfigurations.cs` (session columns + attempts partial unique index), `src/Infrastructure/Persistence/Migrations/<ts>_AddSessionMembership.cs` + `.Designer.cs` + `AppDbContextModelSnapshot.cs`, `src/Infrastructure/DependencyInjection.cs` (`TimeProvider`, `ISchedulingAlgorithm`, `IReviewSessions`, `ReviewOptions`)
- `src/Api/Endpoints/Review/GetReviewOverviewEndpoint.cs`, `StartSessionEndpoint.cs`, `GetCurrentSessionEndpoint.cs`, `GetSessionEndpoint.cs`, `AnswerQuestionEndpoint.cs`, `FinishSessionEndpoint.cs`, `ScopeQuery.cs`; `src/Api/Errors/ReviewExceptionHandler.cs`; `src/Api/Program.cs` (`AddExceptionHandler`, `UseExceptionHandler`)
- `tests/Domain.Tests/Scheduling/Sm2AlgorithmTests.cs`; `tests/Application.Tests/Review/SessionPlannerTests.cs`, `SessionSummaryBuilderTests.cs`; `tests/Infrastructure.Tests/Review/ReviewSessionsTests.cs`, `tests/Infrastructure.Tests/Persistence/MigrationsTests.cs` (applied-count assertion 1 → 2); `tests/Api.Tests/Review/*EndpointTests.cs`, `tests/Api.Tests/ApiFixture.cs` (`ResetProgressAsync`)
- `Directory.Packages.props` (+`Microsoft.Extensions.TimeProvider.Testing`), `.env.example`, `docker-compose.yml` (two new env passthroughs)

Frontend (`web/**`):
- `web/src/app/app.routes.ts` (lazy `review` routes + default redirect), `web/src/app/app.html` + `app.ts` (`routerLink` instead of `href` in the header nav — otherwise every nav click is a full reload and the store is lost)
- `web/src/app/review/review.routes.ts`, `api/review.models.ts`, `api/review-api.ts`, `session-store.ts`, `review-overview/`, `review-session/`, `question-card/`, `option-list/`, `feedback-panel/`, `progress-bar/`, `session-summary/`, `prompt-markdown/` (+ `prompt-markdown.parser.ts`), `web/src/app/knowledge/api/knowledge-tree.models.ts` (minimal tree types; 003 extends)
- `web/src/app/review/README.md` (replace placeholder)

Docs: `docs/architecture/decisions/0005-session-membership.md`, `memory-bank/systemPatterns.md`, `memory-bank/activeContext.md`, `progress.md`.

## Approach

### 1. Domain — scheduling (pure)

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

### 2. Schema delta (data-model decision — flagged for the user)

The spec's Key Entities paragraph says both "gains no new columns" and "stored with the session as an ordered list of question ids". Those cannot both be true; the second is the requirement (FR-011, SC-005), so the plan adds columns. Additive only — no 001 column is renamed, retyped or dropped, no existing CHECK changes.

`sessions`:
- `question_ids text[] NOT NULL DEFAULT '{}'` — ordered membership. `text[]` over `jsonb` because `tags` already uses the `TextArray()` convention (with its `ValueComparer`), membership checks are `question_id = ANY(question_ids)`, and a Postgres array keeps order.
- `total_limit int NOT NULL DEFAULT 20`, `new_limit int NOT NULL DEFAULT 10` — FR-003 "record the new session with its scope and limits".

`attempts`:
- partial unique index `ix_attempts_session_id_item_id ON attempts (session_id, item_id) WHERE session_id IS NOT NULL` — the database-level guarantee for FR-005 (double-click race). Configured with `e.HasIndex(x => new { x.SessionId, x.ItemId }).IsUnique().HasFilter("session_id IS NOT NULL")`. `item_id` is a stored generated column, so it is indexable. See Open question 2 regarding feature 004.

EF mapping in `SessionConfiguration`: `e.Property(x => x.QuestionIds).TextArray().HasDefaultValueSql("'{}'")`, `e.Property(x => x.TotalLimit).HasDefaultValue(20)`, `e.Property(x => x.NewLimit).HasDefaultValue(10)`. Entity: `public string[] QuestionIds { get; set; } = [];`, `public int TotalLimit { get; set; }`, `public int NewLimit { get; set; }`.

Migration name: `AddSessionMembership`. Procedure (from `.claude/skills/ef-migrations/SKILL.md`):
1. `docker compose up -d postgres`; `export ConnectionStrings__Default="Host=localhost;Port=5432;Database=interviewprep;Username=interviewprep" PGPASSWORD=<from .env>`.
2. `dotnet ef migrations add AddSessionMembership --project src/Infrastructure --startup-project src/Api --output-dir Persistence/Migrations`.
3. `dotnet ef migrations script --project src/Infrastructure --startup-project src/Api --idempotent -o /tmp/schema.sql` then `grep -E "ALTER TABLE sessions|text\[\]|CREATE UNIQUE INDEX ix_attempts_session_id_item_id|WHERE session_id" /tmp/schema.sql`. Expect: three `ADD COLUMN`s with defaults, one partial unique index, nothing else.
4. `git add` the `.cs`, `.Designer.cs` and `AppDbContextModelSnapshot.cs`; `rm -rf 'src/Api/bin\Debug' 'src/Infrastructure/bin\Debug'` if they appear.
5. Update `tests/Infrastructure.Tests/Persistence/MigrationsTests.cs`: `GetAppliedMigrationsAsync()).Count().ShouldBe(1)` becomes `2`; add `unique.ShouldContain("ix_attempts_session_id_item_id")`.

### 3. Application — use cases

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

### 4. API contracts (FastEndpoints, `api` prefix, camelCase JSON, ProblemDetails)

Shared shapes:

```jsonc
// Scope (appears in session, overview, summary)
{ "type": "all" | "domain" | "topic", "domain": "dotnet" | null, "topic": "clr-memory" | null,
  "domainTitle": "C# & .NET Runtime" | null, "topicTitle": "Memory & GC" | null }

// Ref
{ "slug": "span-memory", "title": "Span<T>, Memory<T> and zero-allocation code" }

// SessionQuestion
{ "id": "dotnet.clr-memory.stack-vs-heap.mcq.1", "level": "M" | "S" | "L", "reviewed": false, "isNew": true,
  "prompt": "markdown with ```csharp fences and `inline code`", "options": ["…", "…", "…", "…"],
  "domain": Ref, "topic": Ref, "concept": Ref }

// SessionAnswer (already recorded, for resume)
{ "questionId": "…", "selectedIndex": 2, "isCorrect": false }

// Session
{ "id": "uuid", "scope": Scope, "startedAt": "2026-09-06T10:00:00+00:00", "finishedAt": null,
  "totalLimit": 20, "newLimit": 10, "questions": SessionQuestion[], "answers": SessionAnswer[],
  "skippedQuestionIds": [] }

// AnswerFeedback
{ "questionId": "…", "selectedIndex": 2, "isCorrect": false, "correctIndex": 0,
  "explanation": "markdown", "nextDueAt": "2026-09-07T10:00:00+00:00", "intervalDays": 1 }

// ReviewOverview
{ "scope": Scope, "dueCount": 0, "newCount": 28, "totalEligible": 28, "nextDueAt": null | "…",
  "defaults": { "totalLimit": 20, "newLimit": 10 }, "limits": { "min": 1, "max": 100 } }

// SessionSummary
{ "sessionId": "uuid", "scope": Scope, "startedAt": "…", "finishedAt": "…", "questionCount": 20,
  "answered": 20, "correct": 15, "accuracy": 0.75 | null,
  "weakConcepts": [ { "domain": "dotnet", "topic": "clr-memory", "concept": "gc-modes",
                      "title": "GC modes", "wrong": 2, "total": 3 } ],
  "dueTomorrow": 5 }
```

Endpoints:

| Method / path | Request | Success | Errors |
|---|---|---|---|
| `GET /api/review/overview?scope=all\|domain\|topic&domain=&topic=` | query (`ScopeQuery` base class with FluentValidation: `scope` in set; `domain` required when scope≠all; `topic` required iff scope=topic; slugs match `^[a-z0-9]+(-[a-z0-9]+)*$`) | 200 `ReviewOverview` | 400 validation; 404 `title: "Scope not found"` |
| `POST /api/review/sessions` | body `{ "scope": "topic", "domain": "dotnet", "topic": "clr-memory", "totalLimit": 20, "newLimit": 10 }` (`totalLimit`/`newLimit` optional → defaults; each 1..100) | 201 `Session`, `Location: /api/review/sessions/{id}` | 400; 404 scope; 409 `title: "Nothing to review"` |
| `GET /api/review/sessions/current` | – | 200 `Session` or **204** (no open session) | – |
| `GET /api/review/sessions/{id}` | route guid | 200 `Session` (open or finished) | 400 non-guid; 404 `title: "Session not found"` |
| `POST /api/review/sessions/{id}/answers` | `{ "questionId": "…", "selectedIndex": 0..3 }` | 200 `AnswerFeedback` | 400; 404 session; 409 `title: "Session already finished"`; 409 `title: "Question already answered"`; 422 `title: "Question not in session"` |
| `POST /api/review/sessions/{id}/finish` | – | 200 `SessionSummary` (idempotent) | 404 |

Error mapping lives in one `ReviewExceptionHandler : IExceptionHandler` (`src/Api/Errors/`) registered with `builder.Services.AddExceptionHandler<ReviewExceptionHandler>()` and `app.UseExceptionHandler()` placed before `UseFastEndpoints`; it maps `ReviewError` → status/title above and writes `application/problem+json` with `instance = request path`, exactly like the existing 404 in `GetConceptEndpoint`. Non-`ReviewException` exceptions fall through to the default ProblemDetails 500. Endpoint classes stay one-liners: validate → call `IReviewSessions` → `Send.OkAsync` / `Send.CreatedAtAsync<GetSessionEndpoint>(...)` / `Send.NoContentAsync()`.

Angular codes against the table above; the C# records are `ReviewDtos.cs` positional records with the same property names (PascalCase → camelCase by the existing serializer setting).

### 5. Angular feature `web/src/app/review/`

Angular 22, zoneless, standalone, OnPush, signals; file naming follows the shell (`api-status.ts`, no `.component`).

Routes (`review.routes.ts`, lazy-loaded from `app.routes.ts` via `loadChildren`; add `{ path: '', redirectTo: 'review', pathMatch: 'full' }`):
- `/review` → `ReviewOverview`
- `/review/session` → `ReviewSession` (card page; guard-free: on init it calls `store.resume()` which loads `sessions/current` and redirects to `/review` when 204)
- `/review/summary` → `SessionSummary` (redirects to `/review` when the store has no summary — summary is not persisted client-side; "Review again" and reload both go through the API)

`api/review.models.ts`: TS interfaces `ReviewScopeType`, `ReviewScope`, `Ref`, `SessionQuestion`, `SessionAnswer`, `Session`, `AnswerFeedback`, `ReviewOverview`, `WeakConcept`, `SessionSummary`, `StartSessionRequest` — one-to-one with section 4.

`api/review-api.ts` (`ReviewApi`, `providedIn: 'root'`, `HttpClient` + `firstValueFrom`): `overview(scope)`, `start(req)`, `current()` (maps 204 → `null` via `observe: 'response'`), `get(id)`, `answer(id, questionId, selectedIndex)`, `finish(id)`. Plain promises: the store owns state; `httpResource` is used only where the request is a pure function of signals (the overview, keyed on the scope form value).

`session-store.ts` (`SessionStore`, `providedIn: 'root'` so it survives the session → summary navigation):
- signals: `session`, `status: 'idle'|'loading'|'ready'|'checking'|'checked'|'finishing'|'finished'|'error'`, `selectedIndex: number|null`, `feedback: AnswerFeedback|null`, `answered: Map<string, SessionAnswer>` (seeded from `session.answers`, extended on each feedback), `summary`, `error: string|null`
- computed: `questions` (session questions minus `skippedQuestionIds`), `position` (= answered count among `questions`), `current` (first unanswered), `total`, `isLast`, `scopeLabel`
- methods: `start(req)` → `api.start` → set session, navigate `/review/session`; `resume()` → `api.current`; `select(i)` (no-op when `checked`); `check()` → `api.answer` (status `checking`; on failure: status `ready`, `error` set, `selectedIndex` kept — FR-015 retry); `next()` → clears selection/feedback; when no unanswered left → `finish()`; `finish()` → `api.finish` → `summary`, navigate `/review/summary`; `endEarly()` = `finish()` after a confirm.

Components:
- `review-overview/review-overview.ts|html|spec.ts` — typed reactive form (`NonNullableFormBuilder`): `scopeType`, `domain`, `topic`, `totalLimit`, `newLimit` (limits validated 1–100, prefilled from `overview.defaults` once). Domain/topic selects come from `httpResource<KnowledgeTree>(() => '/api/knowledge/tree')` (type in `knowledge/api/knowledge-tree.models.ts`). Overview counts via `httpResource<ReviewOverview>(() => overviewUrl(scopeSignal()))` where `scopeSignal = toSignal(form.valueChanges, { initialValue })`. States: loading; nothing due & nothing new → empty state with `nextDueAt` (formatted with `DatePipe`) or "import content" hint when `totalEligible === 0`; resource error → "API unreachable" + retry; open session detected via `api.current()` on init → "Resume" button. Start → `store.start(...)`.
- `review-session/review-session.ts|html|spec.ts` — page: progress bar, question card, option list, feedback panel, Check / Next / End session buttons. Keyboard: `host: { '(document:keydown)': 'onKeydown($event)' }` — ignore when `event.target` is an input/textarea/select or when a modifier is held; `'1'..'4'` → `store.select(n-1)` while unchecked; `Enter` → `check()` if a selection exists and status is `ready`, `next()` if `checked`; call `preventDefault()` so a focused button does not also fire `click` (double trigger).
- `question-card/` — inputs `question`, `position`, `total`, `scopeLabel`; shows level badge (`font-mono`), "unreviewed" badge when `!reviewed`, "new" badge when `isNew`, concept/topic breadcrumb; renders prompt through `<app-prompt-markdown [source]="question().prompt" />`.
- `option-list/` — inputs `options`, `selected`, `correctIndex` (null until checked), `locked`; output `selectedChange`. `role="radiogroup"`; each option a `<button type="button" role="radio" [attr.aria-checked]>` with the 1–4 key hint in `font-mono`; option text through `<app-prompt-markdown inline>` (inline code only). State classes (tokens only): neutral `border-line bg-surface`; selected `border-accent bg-accent-soft`; correct `border-ok bg-ok-soft`; wrong selection `border-bad bg-bad-soft`; `disabled` when locked.
- `feedback-panel/` — input `feedback`; header "Correct" / "Incorrect" with `text-ok`/`text-bad` (font-semibold, ≥14px), explanation via `app-prompt-markdown`, "next due in N day(s)" line in `text-muted`. `aria-live="polite"`.
- `progress-bar/` — inputs `position`, `total`, `label`; a `bg-surface-2` track with `bg-accent` fill (`width` bound via `[style.width.%]`), "7 / 20" in `font-mono`.
- `session-summary/` — reads `store.summary()`; counts, accuracy, `@for` weak concepts each linking to `/knowledge/{domain}/{topic}/{concept}` (feature 003 route; until then it can open the concept endpoint data in place — see Open question 5), "Due tomorrow: N", "Review again" (→ `store.start` with the same scope and the session's limits), "Back to overview".
- `prompt-markdown/prompt-markdown.parser.ts` + `prompt-markdown.ts` — no `marked`, no `innerHTML`, no sanitizer bypass. A pure parser `parsePromptMarkdown(source): Block[]` where `Block = { kind: 'paragraph', inlines: Inline[] } | { kind: 'code', language: string, code: string }` and `Inline = { kind: 'text' | 'code', text: string }`. Recognises only fenced ``` blocks (with optional language tag) and backtick inline code; everything else is literal text (Angular interpolation escapes it), so raw HTML in content can never execute. Template renders `<pre class="bg-code-bg text-code-text rounded-card p-4 overflow-x-auto font-mono text-sm"><code>` and `<code class="font-mono bg-surface-2 rounded px-1">`; input `inline` renders a single paragraph as a `<span>`. Syntax highlighting is out of scope (003 decides the library; the `--color-syntax-*` tokens are ready).

Vitest specs (pattern from `api-status.spec.ts`: `provideHttpClientTesting`, `TestBed.tick()`, `await fixture.whenStable()`):
- `prompt-markdown.parser.spec.ts` — fenced block with language, inline code inside a paragraph, unterminated fence treated as code to end, `<script>` stays text, empty string → `[]`.
- `session-store.spec.ts` — start stores session & navigates; select/check posts `{questionId, selectedIndex}` and moves to `checked`; failed check keeps selection and sets error; next advances `position`; last next triggers finish and stores summary; resume with 204 leaves `session` null; resume with answers starts at first unanswered and honours `skippedQuestionIds`.
- `option-list.spec.ts` — click selects; locked ignores clicks; classes for correct/wrong/selected.
- `review-session.spec.ts` — key `2` selects option 2, Enter checks, Enter again advances; keys ignored inside an input.
- `review-overview.spec.ts` — renders "0 due · 28 new"; empty state shows `nextDueAt`; Start disabled when nothing to review; limits outside 1–100 invalid.
- `session-summary.spec.ts` — counts/accuracy text, weak concepts in order, "Review again" calls `start` with same scope.

### 6. Tests (backend)

- `tests/Domain.Tests/Scheduling/Sm2AlgorithmTests.cs` (`[Theory]` table over `(interval, ease, reps, grade) → (interval, ease, reps)`): new+Good → (1, 2.5, 1); (1, 2.5, 1)+Good → (6, 2.5, 2); (6, 2.5, 2)+Good → (15, 2.5, 3); (15, 2.5, 3)+Good → (38, 2.5, 4) (rounding away from zero); (6, 2.5, 2)+Again → (1, 2.3, 0); (1, 1.4, 0)+Again → (1, 1.3, 0); (1, 1.3, 0)+Again → (1, 1.3, 0) floor holds; Again then Good → (1, 2.3, 1); (0, 2.5, 5)+Good → interval min 1. Plus: `DueAt == now + interval days`, `LastReviewedAt == now`, `AlgorithmState == {"lastQuality":4}` / `{"lastQuality":1}`, `Name == "sm2"`, ease never below 1.3 in a 10-step Again loop.
- `tests/Application.Tests/Review/SessionPlannerTests.cs`: due before new; 4 due + 20 new with 10/3 → 7 items (US3-2); 25 due with 20/10 → 20 due, 0 new (US2-6); newLimit > totalLimit clamps; empty inputs → empty.
- `tests/Application.Tests/Review/SessionSummaryBuilderTests.cs`: 15/20 → 0.75; weak concepts ranked wrong desc then slug; concept with only correct answers excluded; zero answers → accuracy null.
- `tests/Infrastructure.Tests/Review/ReviewSessionsTests.cs` (`[Collection(PostgresCollection.Name)]`, `Category=Integration`, `ContentDirBuilder.WithDefaultContent()`, `FakeTimeProvider` pinned to a fixed instant): start builds membership in taxonomy order, all `isNew`, capped by limits; topic scope only contains that topic; start finishes an open session and stores its counts; answer creates one attempt and one review state with SM-2 values; second answer throws `DuplicateAnswer`; concurrent double insert hits the partial unique index (raw SQL second insert → `23505`); SC-004: answer A right and B wrong, `clock.Advance(1 day)`, start again → B in, A out; `GetAsync` skips a question deprecated after start but keeps its answer; overview counts and `nextDueAt`; finish summary weak concepts and `dueTomorrow` (wrong answers count, right ones do not); finish is idempotent.
- `tests/Api.Tests/Review/OverviewEndpointTests.cs`, `SessionsEndpointTests.cs`, `AnswersEndpointTests.cs`, `FinishEndpointTests.cs` (`[Collection("api")]`, real repo content, `fixture.ResetProgressAsync()` = `TRUNCATE attempts, review_states, sessions` in `InitializeAsync`): 200 shapes (assert JSON property names in camelCase, `options` are strings, no `correct` leaks), 400 on bad scope/limits/selectedIndex, 404 unknown domain, 201 + `Location`, 204 then 200 for `current`, 409 duplicate answer, 422 foreign question, 409 answer after finish, finish returns summary and is repeatable. One endpoint-per-module minimum is exceeded deliberately because every endpoint is a contract the Angular agent codes blind against.
- Angular: section 5. Optional last step: one Playwright smoke through the MCP (`/review` → Start → answer → summary).

## Step-by-step plan

Backend track (B) touches `src/**`, `tests/**`, `Directory.Packages.props`, `.env.example`, `docker-compose.yml`; frontend track (F) touches `web/**` only. F can start immediately from section 4 of this plan (its specs use `HttpTestingController`, not the server); the two tracks meet at step 9.

1. **B1 — Domain scheduling.** Add `src/Domain/Scheduling/*` and `ReviewState.ToSnapshot()/Apply()`. Write `Sm2AlgorithmTests`. Verify: `dotnet test --filter "Category!=Integration"` green.
2. **B2 — Schema.** Extend `Session`, `SessionConfiguration`, `AttemptConfiguration`; run the migration procedure (section 2); update `MigrationsTests`. Verify: `dotnet test tests/Infrastructure.Tests --filter "FullyQualifiedName~MigrationsTests"`; `dotnet run --project src/Api` boots and logs "Applying 1 migration(s)". App still runnable.
3. **B3 — Application contracts + pure logic.** `IReviewSessions`, `ReviewDtos`, `ReviewScope`, `ReviewException`, `ReviewOptions`, `SessionPlanner`, `SessionSummaryBuilder` + their unit tests. Verify: unit tests green; `dotnet build -c Release` warns nothing (TreatWarningsAsErrors).
4. **B4 — Infrastructure `ReviewSessions` + DI.** `TimeProvider`, `ISchedulingAlgorithm`, `IReviewSessions`, options binding; add `Microsoft.Extensions.TimeProvider.Testing` to `Directory.Packages.props` and the Infrastructure/Api test projects. Write `ReviewSessionsTests` including SC-004. Verify: `dotnet test tests/Infrastructure.Tests`.
5. **B5 — API endpoints + exception handler.** Six endpoints, `ScopeQuery` validators, `ReviewExceptionHandler`, `Program.cs` wiring. Api tests. Verify: `dotnet test tests/Api.Tests`; manual `curl -X POST localhost:8080/api/review/sessions -d '{"scope":"all"}'` and Swagger shows the six routes.
6. **B6 — Runtime config.** `.env.example`: `REVIEW_DEFAULT_TOTAL_LIMIT=20`, `REVIEW_DEFAULT_NEW_LIMIT=10`; `docker-compose.yml` passthrough. Verify: `docker compose up --build`, `curl localhost:8080/api/review/overview?scope=all` returns defaults.
7. **F1 — Models, API service, parser, store.** `review.models.ts`, `review-api.ts`, `prompt-markdown.parser.ts`, `session-store.ts` + specs. Verify: `npm test -- --watch=false` (after `nvm use 22`).
8. **F2 — Components + routes.** Leaf components first (`prompt-markdown`, `option-list`, `progress-bar`, `feedback-panel`, `question-card`), then pages (`review-overview`, `review-session`, `session-summary`), `review.routes.ts`, `app.routes.ts`, header `routerLink`. Verify: specs green, `ng build` under budget (`anyComponentStyle` 4 kB — keep styles in Tailwind classes, not `.scss`), manual check of both themes with the API from B5.
9. **Integration + polish.** Run the full stack, walk US1–US4 by hand, run `code-reviewer` + `frontend-reviewer`, optional Playwright smoke via MCP, write ADR 0005 and update `memory-bank/systemPatterns.md`, `activeContext.md`, `progress.md`, `web/src/app/review/README.md`.

Parallelism: B1–B3 are sequential (B4 depends on all three); F1 can start at any time; F2 depends on F1; B5/B6 can run while F2 is in flight. Do not let two agents edit `DependencyInjection.cs` or `Program.cs` concurrently — both are in B4/B5 only.

## Invariants & risks

Constitution:
- I Content over platform — no new content shape; a markdown file cannot replace scheduling, so this is justified. Explanations and prompts render as-is from the seed.
- II Content is data — no question text in code; `options` schema unchanged; `correct` never leaves the server.
- IV Progress is user-scoped — new columns are on `sessions` and the index is on `attempts`; content tables untouched (`ProgressSchemaTests.Content_tables_have_no_user_id_column` still holds).
- V Scheduling is an interface — `ISchedulingAlgorithm` pure in Domain, `algorithm_state` carries `{ "lastQuality": n }`, `review_states.algorithm` written from `Name`; FSRS would be another `ISchedulingAlgorithm` with its own state JSON and no schema change.
- VI LLM only for judgment — MCQ grading is `selectedIndex == correctIndex`.
- VII Boring architecture — one interface, one EF class, FastEndpoints validators, one `IExceptionHandler`; no MediatR/bus/outbox.
- VIII Modern Angular — standalone, signals, `@if/@for`, OnPush, typed reactive form for limits, `httpResource` for overview, no state library.
- IX One command — two new env vars in `.env.example` and compose; migration applied on API start.
- X Tests where they pay — SM-2 table, planner/summary unit tests, integration tests for order/limits/one-answer/SC-004, Api tests per endpoint, component specs for the quiz flow.

Risks and mitigations:
- **EF `text[]` mapping**: reuse `Conventions.TextArray()` (has the `ValueComparer`); `HasDefaultValueSql("'{}'")` so legacy rows are valid; assert `text[]` in the migration script grep. Filtering with `q.Id = ANY(session.QuestionIds)` translates via `session.QuestionIds.Contains(q.Id)`.
- **Explicit transactions + `EnableRetryOnFailure`**: EF throws `InvalidOperationException` unless the transaction runs inside `CreateExecutionStrategy().ExecuteAsync`; copy the `ContentImporter` pattern.
- **TimeProvider in tests**: `FakeTimeProvider` from `Microsoft.Extensions.TimeProvider.Testing` (pin the version to the 10.x line in `Directory.Packages.props`); Api tests can swap it via `WithWebHostBuilder(b => b.ConfigureTestServices(s => s.AddSingleton<TimeProvider>(fake)))` if a clock-advancing endpoint test is ever wanted, but SC-004 is proven at the Infrastructure level to keep `ApiFixture` untouched.
- **Rounding**: `MidpointRounding.AwayFromZero` explicitly; the 37.5 → 38 test pins it.
- **Duplicate answers**: pre-check gives a clean 409; the partial unique index closes the race; the `DbUpdateException` catch translates the constraint name so the client still sees 409.
- **Zoneless Angular + keyboard events**: `document:keydown` host listener runs inside Angular's event handling so signal writes schedule CD without zones; specs must dispatch `new KeyboardEvent('keydown', { key: 'Enter', bubbles: true })` on `document` and then `await fixture.whenStable()`; `preventDefault` on Enter avoids the button-click double fire.
- **Markdown-in-prompt safety**: no `innerHTML`; parser output is rendered through interpolation only; raw HTML in a seed shows as text. Angular's built-in sanitizer is not relied upon.
- **Dark-theme contrast (SC-006)**: `text-ok` on `bg-ok-soft` in light (`#1f9d61` on `#e6f6ee`) is ~3.6:1 — below AA for small text. Keep body text in `text-text` inside tinted cards; use `text-ok`/`text-bad` only for the state word (semibold, ≥ 14 px) and for the border; run a contrast check on both themes for the three option states and adjust `--color-ok-soft`/`--color-bad-soft` in `styles.scss` if needed (token change, not component change).
- **Api tests share one database**: progress tables are truncated per test class; Knowledge tests do not read progress, so ordering inside the `api` collection is safe.
- **`MigrationsTests` count assertion** will fail until updated — do it in B2, not later.
- **Header `href` links** currently full-reload the SPA; switching to `routerLink` is required for the root-provided store to survive navigation.

## Decisions / trade-offs

- **Membership as `text[]` columns on `sessions`** (vs a `session_items` table): one row per session, order preserved, `ANY()` membership check, no join for resume; a join table would only pay off with per-item metadata, which 002 does not have. Flagged as the data-model decision.
- **Application interface + Infrastructure EF implementation** (vs use-case classes with repositories): matches `IKnowledgeQueries`/`IContentImporter`; the parts worth unit-testing without a database (`SessionPlanner`, `SessionSummaryBuilder`, SM-2) are pure and live in Application/Domain.
- **Exceptions with a kind + one `IExceptionHandler`** (vs result types in every endpoint): six endpoints, one mapping table; Application stays HTTP-agnostic.
- **204 for "no open session"** (vs 404): not an error state for the client; `httpResource`/`HttpClient` handle it without an error branch.
- **Finish is idempotent; no `GET …/summary`**: the summary page holds its data in the store; a reload with an empty store returns to the overview. Persisting/reading summaries is 005's concern.
- **English only on session/answer payloads**: the spec does not mention language; adding `lang` to four endpoints now is scope creep. The translation fallback pattern from `KnowledgeQueries` can be lifted later without contract breakage (add an optional query param).
- **No `marked`**: the prompt subset is two constructs; a 60-line parser is safer and dependency-free. 003 will pick a full renderer for notes; `prompt-markdown` stays for cards.
- **Auto-finish summary of the previous session is not returned** by `POST /sessions`; it is stored (counts) and can be retrieved by `POST /sessions/{id}/finish` (idempotent) if a client ever needs it.
- Out of scope: hard/easy grades, shuffling, per-learner persisted limits, syntax highlighting, uk language, summary persistence, readiness numbers.

## Open questions

1. **Schema (decide personally):** approve `sessions.question_ids text[] NOT NULL DEFAULT '{}'`, `sessions.total_limit int NOT NULL DEFAULT 20`, `sessions.new_limit int NOT NULL DEFAULT 10`, and the partial unique index `attempts (session_id, item_id) WHERE session_id IS NOT NULL`. The spec's "gains no new columns" sentence should be amended.
2. **Does feature 004 need more than one attempt per item per session** (e.g. resubmitting a kata after feedback)? If yes, the unique index should be restricted to MCQ (`WHERE session_id IS NOT NULL AND item_type = 'question'` is not enough since open questions share the type) — alternative is app-level enforcement only. Recommendation: keep the index; 004 can drop it in its own migration if its design needs retries.
3. "Nothing to review" on `POST /sessions`: 409 (proposed) or 422?
4. Should `POST /sessions` return the auto-finished previous session's summary (e.g. `previousSummary`) so the UI can show "your last session was finished"? Proposed: no.
5. Weak-concept links: route to `/knowledge/{domain}/{topic}/{concept}` (003's future route, 404 until then) or open the concept read endpoint in an in-place panel as the spec suggests? Proposed: `routerLink` to the future route now; 003 fills it.
6. Include the Playwright smoke test in 002 (constitution X lists it) or defer to 003 when there is a second screen to traverse?

Key files referenced: `specs/002-review-session/spec.md`, `src/Domain/Progress/Session.cs`, `src/Infrastructure/Persistence/Configurations/ProgressConfigurations.cs`, `src/Infrastructure/Persistence/Configurations/Conventions.cs`, `src/Infrastructure/DependencyInjection.cs`, `src/Api/Program.cs`, `tests/Infrastructure.Tests/Persistence/MigrationsTests.cs`, `web/src/app/app.routes.ts`, `web/src/app/app.html`, `web/src/styles.scss`, `.claude/skills/ef-migrations/SKILL.md`.
