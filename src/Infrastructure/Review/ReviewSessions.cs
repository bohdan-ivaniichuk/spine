using System.Text.Json;
using Spine.Application.Knowledge;
using Spine.Application.Review;
using Spine.Domain.Content;
using Spine.Domain.Progress;
using Spine.Domain.Scheduling;
using Spine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Spine.Infrastructure.Review;

/// <summary>
/// EF implementation of the review-session use cases for the single local learner. Eligible = non-deprecated MCQ
/// whose concept, topic and domain are not deprecated. Due = has a review state with due_at &lt;= now (most overdue
/// first); new = no review state at all (taxonomy order). Writes run inside the retrying execution strategy with one
/// transaction each, mirroring <see cref="Content.ContentImporter"/>.
/// </summary>
public sealed class ReviewSessions(
    AppDbContext db,
    ISchedulingAlgorithm algorithm,
    TimeProvider clock,
    IOptions<ReviewOptions> options) : IReviewSessions
{
    /// <summary>The partial unique index that closes the double-submit race on <c>attempts</c>.</summary>
    public const string SessionItemIndex = "ix_attempts_session_id_item_id";

    /// <summary>The unique index on <c>review_states (user_id, item_id)</c>: one schedule per learner and item.</summary>
    public const string ReviewStateItemIndex = "ix_review_states_user_id_item_id";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Application-wide advisory lock key for "start a session"; one learner, so one fixed key suffices.</summary>
    private const long StartSessionLockKey = 2002_0001;

    /// <summary>Stored in <c>attempts.answer</c>: the option index plus its text at answer time.</summary>
    private sealed record McqAnswer(int SelectedIndex, string SelectedText);

    /// <summary>Prompt and options of one question translation, as projected for a session card.</summary>
    private sealed record QuestionText(string Prompt, IReadOnlyList<McqOption>? Options);

    public async Task<ReviewOverviewDto> GetOverviewAsync(ReviewScope scope, Language language = Language.En, CancellationToken ct = default)
    {
        var scopeDto = await ResolveScopeAsync(scope, language, ct);
        var now = clock.GetUtcNow();

        var totalEligible = await Eligible(scope).CountAsync(ct);
        var dueCount = await DueIds(scope, now).CountAsync(ct);
        var newCount = await NewIds(scope).CountAsync(ct);
        var nextDueAt = await DueAtOf(scope).Where(dueAt => dueAt > now).Select(dueAt => (DateTimeOffset?)dueAt).MinAsync(ct);

        return new ReviewOverviewDto(
            scopeDto,
            dueCount,
            newCount,
            totalEligible,
            nextDueAt,
            new SessionDefaultsDto(options.Value.DefaultTotalLimit, options.Value.DefaultNewLimit),
            new SessionLimitsDto(ReviewOptions.MinLimit, ReviewOptions.MaxLimit));
    }

    public async Task<SessionDto> StartAsync(ReviewScope scope, int totalLimit, int newLimit, Language language = Language.En, CancellationToken ct = default)
    {
        if (!ReviewOptions.IsWithinBounds(totalLimit))
        {
            throw new ArgumentOutOfRangeException(nameof(totalLimit), totalLimit, $"Must be {ReviewOptions.MinLimit}..{ReviewOptions.MaxLimit}.");
        }

        if (!ReviewOptions.IsWithinBounds(newLimit))
        {
            throw new ArgumentOutOfRangeException(nameof(newLimit), newLimit, $"Must be {ReviewOptions.MinLimit}..{ReviewOptions.MaxLimit}.");
        }

        await ResolveScopeAsync(scope, language, ct);

        var strategy = db.Database.CreateExecutionStrategy();
        var sessionId = await strategy.ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var now = clock.GetUtcNow();
            var userId = LocalLearner.Id;

            // FR-003: a learner never has two open sessions. Two concurrent starts would each see none and both insert
            // under READ COMMITTED, so serialise them on a transaction-scoped advisory lock (released at commit/rollback).
            await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({StartSessionLockKey})", ct);

            // Row-lock the open sessions before reading the plan, so an in-flight Answer on them commits first and its
            // review-state change is visible to the due/new queries below (and Answer/Finish wait for this close).
            // On the empty-plan path the transaction rolls back and the locks are released with nothing closed.
            await db.Database.ExecuteSqlAsync($"SELECT id FROM sessions WHERE user_id = {userId} AND finished_at IS NULL FOR UPDATE", ct);

            var dueIds = await DueIds(scope, now).Take(totalLimit).ToListAsync(ct);
            var newIds = await NewIds(scope).Take(Math.Min(newLimit, totalLimit)).ToListAsync(ct);
            var plan = SessionPlanner.Plan(dueIds, newIds, totalLimit, newLimit);
            if (plan.Length == 0)
            {
                // Nothing starts, so nothing is closed either: the open session (if any) stays resumable.
                throw new ReviewException(ReviewError.NothingToReview, "Nothing is due and there are no new questions in this scope.");
            }

            var open = await db.Sessions.Where(s => s.UserId == userId && s.FinishedAt == null).ToListAsync(ct);
            foreach (var session in open)
            {
                await CloseAsync(session, now, ct);
            }

            var created = new Session
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                StartedAt = now,
                CreatedAt = now,
                ScopeType = scope.Type,
                ScopeDomainSlug = scope.Domain,
                ScopeTopicSlug = scope.Topic,
                ScopeConceptSlug = scope.Concept,
                QuestionIds = plan,
                TotalLimit = totalLimit,
                NewLimit = newLimit,
            };
            db.Sessions.Add(created);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return created.Id;
        });

        return await GetAsync(sessionId, language, ct) ?? throw new InvalidOperationException($"Session '{sessionId}' was not found right after it was created.");
    }

    public async Task<SessionDto?> GetCurrentAsync(Language language = Language.En, CancellationToken ct = default)
    {
        var userId = LocalLearner.Id;
        var session = await db.Sessions.AsNoTracking()
            .Where(s => s.UserId == userId && s.FinishedAt == null)
            .OrderByDescending(s => s.StartedAt)
            .FirstOrDefaultAsync(ct);
        return session is null ? null : await ProjectAsync(session, language, ct);
    }

    public async Task<SessionDto?> GetAsync(Guid sessionId, Language language = Language.En, CancellationToken ct = default)
    {
        var userId = LocalLearner.Id;
        var session = await db.Sessions.AsNoTracking().SingleOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, ct);
        return session is null ? null : await ProjectAsync(session, language, ct);
    }

    public async Task<AnswerFeedbackDto> AnswerAsync(Guid sessionId, string questionId, int selectedIndex, Language language = Language.En, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(questionId);
        ArgumentOutOfRangeException.ThrowIfNegative(selectedIndex);

        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var now = clock.GetUtcNow();
            var userId = LocalLearner.Id;

            await LockSessionAsync(sessionId, ct);
            var session = await db.Sessions.SingleOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, ct)
                ?? throw new ReviewException(ReviewError.SessionNotFound, $"No session '{sessionId}'.");
            if (session.FinishedAt is not null)
            {
                throw new ReviewException(ReviewError.SessionFinished, $"Session '{sessionId}' was finished at {session.FinishedAt:O}.");
            }

            if (!session.QuestionIds.Contains(questionId, StringComparer.Ordinal))
            {
                throw new ReviewException(ReviewError.QuestionNotInSession, $"Question '{questionId}' is not part of session '{sessionId}'.");
            }

            if (await db.Attempts.AnyAsync(a => a.SessionId == sessionId && a.QuestionId == questionId, ct))
            {
                throw new ReviewException(ReviewError.DuplicateAnswer, $"Question '{questionId}' was already answered in session '{sessionId}'.");
            }

            // FR-011: a question deprecated after the session started is skipped on resume, so it cannot be answered either.
            var deprecated = await db.Questions.AsNoTracking()
                .Where(q => q.Id == questionId)
                .Select(q => q.DeprecatedAt != null
                    || q.Concept!.DeprecatedAt != null
                    || q.Concept.Topic!.DeprecatedAt != null
                    || q.Concept.Topic.Domain!.DeprecatedAt != null)
                .SingleOrDefaultAsync(ct);
            if (deprecated)
            {
                throw new ReviewException(ReviewError.QuestionDeprecated, $"Question '{questionId}' was deprecated after session '{sessionId}' started.");
            }

            // Grading always uses the English options; the requested language only picks the explanation.
            var translations = await db.QuestionTranslations.AsNoTracking()
                .Where(t => t.QuestionId == questionId && (t.Language == Language.En || t.Language == language))
                .Select(t => new { t.Language, t.Options, t.Explanation })
                .ToListAsync(ct);
            var translation = translations.SingleOrDefault(t => t.Language == Language.En)
                ?? throw new InvalidOperationException($"Question '{questionId}' has no English translation.");
            var requested = language == Language.En ? null : translations.SingleOrDefault(t => t.Language == language);
            var mcqOptions = translation.Options ?? throw new InvalidOperationException($"Question '{questionId}' has no options.");
            if (selectedIndex >= mcqOptions.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(selectedIndex), selectedIndex, $"Question '{questionId}' has {mcqOptions.Count} options.");
            }

            var correctIndex = IndexOfCorrect(mcqOptions, questionId);
            var isCorrect = selectedIndex == correctIndex;

            db.Attempts.Add(new Attempt
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                ItemType = ItemType.Question,
                QuestionId = questionId,
                SessionId = sessionId,
                AnsweredAt = now,
                CreatedAt = now,
                Answer = JsonSerializer.Serialize(new McqAnswer(selectedIndex, mcqOptions[selectedIndex].Text), Json),
                IsCorrect = isCorrect,
            });

            var state = await db.ReviewStates.SingleOrDefaultAsync(r => r.UserId == userId && r.QuestionId == questionId, ct);
            ReviewSnapshot snapshot;
            if (state is null)
            {
                state = new ReviewState
                {
                    Id = Guid.CreateVersion7(),
                    UserId = userId,
                    ItemType = ItemType.Question,
                    QuestionId = questionId,
                    DueAt = now,
                    CreatedAt = now,
                    UpdatedAt = now,
                };
                db.ReviewStates.Add(state);
                snapshot = ReviewSnapshot.New(now);
            }
            else
            {
                snapshot = state.ToSnapshot();
            }

            state.Apply(algorithm.Next(snapshot, isCorrect ? ReviewGrade.Good : ReviewGrade.Again, now), algorithm.Name);

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: SessionItemIndex or ReviewStateItemIndex,
            })
            {
                // Two submissions raced past the pre-check (the session row lock normally serialises them); the index
                // decided, the client still sees a clean conflict.
                throw new ReviewException(ReviewError.DuplicateAnswer, $"Question '{questionId}' was already answered in session '{sessionId}'.");
            }

            await tx.CommitAsync(ct);
            var served = TranslationPicker.Pick(language, translation.Explanation, mcqOptions, requested?.Explanation, requested?.Options);
            return new AnswerFeedbackDto(
                questionId,
                selectedIndex,
                isCorrect,
                correctIndex,
                served.Text,
                state.DueAt,
                state.IntervalDays,
                served.Language.ToDbValue(),
                served.Fallback);
        });
    }

    public async Task<SessionSummaryDto> FinishAsync(Guid sessionId, Language language = Language.En, CancellationToken ct = default)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var now = clock.GetUtcNow();
            var userId = LocalLearner.Id;

            // Serialises with Answer and with a concurrent Finish: the second Finish sees FinishedAt set and rewrites nothing.
            await LockSessionAsync(sessionId, ct);
            var session = await db.Sessions.SingleOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, ct)
                ?? throw new ReviewException(ReviewError.SessionNotFound, $"No session '{sessionId}'.");

            var rows = await (
                from a in db.Attempts.AsNoTracking()
                join q in db.Questions.AsNoTracking() on a.QuestionId equals q.Id
                where a.SessionId == sessionId
                orderby a.AnsweredAt, a.Id
                select new
                {
                    QuestionId = q.Id,
                    a.IsCorrect,
                    DomainSlug = q.Concept!.Topic!.Domain!.Slug,
                    TopicSlug = q.Concept.Topic.Slug,
                    ConceptSlug = q.Concept.Slug,
                    ConceptTitle = q.Concept.Translations.Where(t => t.Language == language).Select(t => t.Title).FirstOrDefault()
                        ?? q.Concept.Translations.Where(t => t.Language == Language.En).Select(t => t.Title).FirstOrDefault(),
                }).ToListAsync(ct);
            var answered = rows
                .Select(r => new AnsweredItem(r.QuestionId, r.IsCorrect == true, new ConceptRef(r.DomainSlug, r.TopicSlug, r.ConceptSlug, r.ConceptTitle ?? r.ConceptSlug)))
                .ToList();

            if (session.FinishedAt is null)
            {
                session.FinishedAt = now;
                session.AnsweredCount = answered.Count;
                session.CorrectCount = answered.Count(a => a.IsCorrect);
                await db.SaveChangesAsync(ct);
            }

            var scope = ScopeOf(session);
            var tomorrow = now.AddDays(1);
            var dueTomorrow = await DueAtOf(scope).Where(dueAt => dueAt <= tomorrow).CountAsync(ct);
            await tx.CommitAsync(ct);

            return SessionSummaryBuilder.Build(session, await DescribeScopeAsync(session, language, ct), answered, dueTomorrow);
        });
    }

    public async Task<ReviewCoverageDto> GetCoverageAsync(CancellationToken ct = default)
    {
        var now = clock.GetUtcNow();

        var topics = await db.Topics.AsNoTracking()
            .Where(t => t.DeprecatedAt == null && t.Domain!.DeprecatedAt == null)
            .OrderBy(t => t.Domain!.SortOrder).ThenBy(t => t.Domain!.Id).ThenBy(t => t.SortOrder).ThenBy(t => t.Id)
            .Select(t => new { t.Id, DomainSlug = t.Domain!.Slug, t.Slug })
            .ToListAsync(ct);

        // One grouped query: eligible questions left-joined to the learner's review state (unique per item).
        var counts = await (
            from q in Eligible(ReviewScope.All)
            join r in States() on q.Id equals r.QuestionId into states
            from r in states.DefaultIfEmpty()
            select new { q.Concept!.TopicId, Seen = r != null, Due = r != null && r.DueAt <= now })
            .GroupBy(x => x.TopicId)
            .Select(g => new { TopicId = g.Key, Eligible = g.Count(), Seen = g.Count(x => x.Seen), Due = g.Count(x => x.Due) })
            .ToDictionaryAsync(x => x.TopicId, ct);

        return new ReviewCoverageDto(topics
            .Select(t => counts.TryGetValue(t.Id, out var c)
                ? new TopicCoverageDto(t.DomainSlug, t.Slug, c.Eligible, c.Seen, c.Due)
                : new TopicCoverageDto(t.DomainSlug, t.Slug, 0, 0, 0))
            .ToList());
    }

    // ---- queries ----

    private IQueryable<Question> Eligible(ReviewScope scope) => db.Eligible(scope);

    private IQueryable<ReviewState> States() => db.QuestionStates();

    /// <summary>Due dates of every eligible question in scope that has a review state.</summary>
    private IQueryable<DateTimeOffset> DueAtOf(ReviewScope scope) =>
        from q in Eligible(scope)
        join r in States() on q.Id equals r.QuestionId
        select r.DueAt;

    /// <summary>Most overdue first, id as a deterministic tie-break.</summary>
    private IQueryable<string> DueIds(ReviewScope scope, DateTimeOffset now) =>
        from q in Eligible(scope)
        join r in States() on q.Id equals r.QuestionId
        where r.DueAt <= now
        orderby r.DueAt, q.Id
        select q.Id;

    /// <summary>Never answered, in taxonomy order.</summary>
    private IQueryable<string> NewIds(ReviewScope scope)
    {
        var userId = LocalLearner.Id;
        return Eligible(scope)
            .Where(q => !db.ReviewStates.Any(r => r.UserId == userId && r.QuestionId == q.Id))
            .OrderBy(q => q.Concept!.Topic!.Domain!.SortOrder)
            .ThenBy(q => q.Concept!.Topic!.SortOrder)
            .ThenBy(q => q.Concept!.SortOrder)
            .ThenBy(q => q.SortOrder)
            .ThenBy(q => q.Id)
            .Select(q => q.Id);
    }

    // ---- session projection ----

    private async Task<SessionDto> ProjectAsync(Session session, Language language, CancellationToken ct)
    {
        var userId = LocalLearner.Id;
        var ids = session.QuestionIds;
        var startedAt = session.StartedAt;

        var rows = await db.Questions.AsNoTracking()
            .Where(q => ids.Contains(q.Id))
            .Select(q => new
            {
                q.Id,
                q.Level,
                q.Reviewed,
                Deprecated = q.DeprecatedAt != null
                    || q.Concept!.DeprecatedAt != null
                    || q.Concept.Topic!.DeprecatedAt != null
                    || q.Concept.Topic.Domain!.DeprecatedAt != null,
                En = q.Translations.Where(t => t.Language == Language.En).Select(t => new QuestionText(t.Prompt, t.Options)).FirstOrDefault(),
                Requested = language == Language.En
                    ? null
                    : q.Translations.Where(t => t.Language == language).Select(t => new QuestionText(t.Prompt, t.Options)).FirstOrDefault(),
                ConceptSlug = q.Concept!.Slug,
                ConceptTitle = q.Concept.Translations.Where(t => t.Language == language).Select(t => t.Title).FirstOrDefault()
                    ?? q.Concept.Translations.Where(t => t.Language == Language.En).Select(t => t.Title).FirstOrDefault(),
                TopicSlug = q.Concept.Topic!.Slug,
                TopicTitle = q.Concept.Topic.Title,
                TopicTitleUk = q.Concept.Topic.TitleUk,
                DomainSlug = q.Concept.Topic.Domain!.Slug,
                DomainTitle = q.Concept.Topic.Domain.Title,
                DomainTitleUk = q.Concept.Topic.Domain.TitleUk,
                // "New" = no review state existed when the session started.
                HadState = db.ReviewStates.Any(r => r.UserId == userId && r.QuestionId == q.Id && r.CreatedAt < startedAt),
            })
            .ToListAsync(ct);

        var attempts = await db.Attempts.AsNoTracking()
            .Where(a => a.SessionId == session.Id)
            .OrderBy(a => a.AnsweredAt).ThenBy(a => a.Id)
            .Select(a => new { a.QuestionId, a.Answer, a.IsCorrect })
            .ToListAsync(ct);
        var answers = attempts
            .Select(a => new SessionAnswerDto(a.QuestionId!, ParseAnswer(a.Answer).SelectedIndex, a.IsCorrect == true))
            .ToList();
        var answeredIds = answers.Select(a => a.QuestionId).ToHashSet(StringComparer.Ordinal);

        var byId = rows.ToDictionary(r => r.Id, StringComparer.Ordinal);
        var questions = new List<SessionQuestionDto>(ids.Length);
        var skipped = new List<string>();
        foreach (var id in ids)
        {
            // FR-011: deprecated after start → skipped, unless it was already answered (then the counts must line up).
            if (!byId.TryGetValue(id, out var row) || row.En is null || (row.Deprecated && !answeredIds.Contains(id)))
            {
                skipped.Add(id);
                continue;
            }

            var served = TranslationPicker.Pick(language, row.En, row.En.Options, row.Requested, row.Requested?.Options);
            questions.Add(new SessionQuestionDto(
                row.Id,
                row.Level.ToDbValue(),
                row.Reviewed,
                !row.HadState,
                served.Text.Prompt,
                served.Text.Options?.Select(o => o.Text).ToList() ?? [],
                new RefDto(row.DomainSlug, LocalizedTitle.Of(language, row.DomainTitle, row.DomainTitleUk)),
                new RefDto(row.TopicSlug, LocalizedTitle.Of(language, row.TopicTitle, row.TopicTitleUk)),
                new RefDto(row.ConceptSlug, row.ConceptTitle ?? row.ConceptSlug),
                served.Language.ToDbValue(),
                served.Fallback));
        }

        return new SessionDto(
            session.Id,
            await DescribeScopeAsync(session, language, ct),
            session.StartedAt,
            session.FinishedAt,
            session.TotalLimit,
            session.NewLimit,
            questions,
            answers,
            skipped);
    }

    // ---- scope ----

    /// <summary>Validates the scope against live content; deprecated or unknown → <see cref="ReviewError.ScopeNotFound"/>.</summary>
    private async Task<ScopeDto> ResolveScopeAsync(ReviewScope scope, Language language, CancellationToken ct)
    {
        scope.Validate();
        switch (scope.Type)
        {
            case SessionScopeType.All:
                return new ScopeDto("all", null, null, null, null);
            case SessionScopeType.Domain:
                {
                    var row = await db.Domains.AsNoTracking()
                        .Where(d => d.Slug == scope.Domain && d.DeprecatedAt == null)
                        .Select(d => new { d.Title, d.TitleUk })
                        .SingleOrDefaultAsync(ct)
                        ?? throw new ReviewException(ReviewError.ScopeNotFound, $"No domain '{scope.Domain}'.");
                    return new ScopeDto("domain", scope.Domain, null, LocalizedTitle.Of(language, row.Title, row.TitleUk), null);
                }

            case SessionScopeType.Topic:
                {
                    var row = await db.Topics.AsNoTracking()
                        .Where(t => t.Slug == scope.Topic && t.Domain!.Slug == scope.Domain && t.DeprecatedAt == null && t.Domain.DeprecatedAt == null)
                        .Select(t => new { DomainTitle = t.Domain!.Title, DomainTitleUk = t.Domain.TitleUk, t.Title, t.TitleUk })
                        .SingleOrDefaultAsync(ct)
                        ?? throw new ReviewException(ReviewError.ScopeNotFound, $"No topic '{scope.Domain}/{scope.Topic}'.");
                    return new ScopeDto(
                        "topic",
                        scope.Domain,
                        scope.Topic,
                        LocalizedTitle.Of(language, row.DomainTitle, row.DomainTitleUk),
                        LocalizedTitle.Of(language, row.Title, row.TitleUk));
                }

            case SessionScopeType.Concept:
                {
                    var row = await db.Concepts.AsNoTracking()
                        .Where(c => c.Slug == scope.Concept && c.Topic!.Slug == scope.Topic && c.Topic.Domain!.Slug == scope.Domain)
                        .Where(c => c.DeprecatedAt == null && c.Topic!.DeprecatedAt == null && c.Topic.Domain!.DeprecatedAt == null)
                        .Select(c => new
                        {
                            DomainTitle = c.Topic!.Domain!.Title,
                            DomainTitleUk = c.Topic.Domain.TitleUk,
                            TopicTitle = c.Topic.Title,
                            TopicTitleUk = c.Topic.TitleUk,
                            Requested = c.Translations.Where(t => t.Language == language).Select(t => t.Title).FirstOrDefault(),
                            En = c.Translations.Where(t => t.Language == Language.En).Select(t => t.Title).FirstOrDefault(),
                        })
                        .SingleOrDefaultAsync(ct)
                        ?? throw new ReviewException(ReviewError.ScopeNotFound, $"No concept '{scope.Domain}/{scope.Topic}/{scope.Concept}'.");
                    return new ScopeDto(
                        "concept",
                        scope.Domain,
                        scope.Topic,
                        LocalizedTitle.Of(language, row.DomainTitle, row.DomainTitleUk),
                        LocalizedTitle.Of(language, row.TopicTitle, row.TopicTitleUk),
                        scope.Concept,
                        row.Requested ?? row.En ?? scope.Concept);
                }

            default:
                throw new ArgumentOutOfRangeException(nameof(scope), scope.Type, null);
        }
    }

    /// <summary>Titles for a stored session's scope; lenient, because content may have been deprecated since.</summary>
    private async Task<ScopeDto> DescribeScopeAsync(Session session, Language language, CancellationToken ct)
    {
        var type = session.ScopeType.ToDbValue();
        if (session.ScopeDomainSlug is null)
        {
            return new ScopeDto(type, null, null, null, null);
        }

        var domain = await db.Domains.AsNoTracking()
            .Where(d => d.Slug == session.ScopeDomainSlug)
            .Select(d => new { d.Title, d.TitleUk })
            .SingleOrDefaultAsync(ct);
        var domainTitle = domain is null ? null : LocalizedTitle.Of(language, domain.Title, domain.TitleUk);
        string? topicTitle = null;
        if (session.ScopeTopicSlug is not null)
        {
            var topic = await db.Topics.AsNoTracking()
                .Where(t => t.Slug == session.ScopeTopicSlug && t.Domain!.Slug == session.ScopeDomainSlug)
                .Select(t => new { t.Title, t.TitleUk })
                .SingleOrDefaultAsync(ct);
            topicTitle = topic is null ? null : LocalizedTitle.Of(language, topic.Title, topic.TitleUk);
        }

        string? conceptTitle = null;
        if (session.ScopeConceptSlug is not null)
        {
            var concept = await db.Concepts.AsNoTracking()
                .Where(c => c.Slug == session.ScopeConceptSlug && c.Topic!.Slug == session.ScopeTopicSlug && c.Topic.Domain!.Slug == session.ScopeDomainSlug)
                .Select(c => new
                {
                    Requested = c.Translations.Where(t => t.Language == language).Select(t => t.Title).FirstOrDefault(),
                    En = c.Translations.Where(t => t.Language == Language.En).Select(t => t.Title).FirstOrDefault(),
                })
                .SingleOrDefaultAsync(ct);
            conceptTitle = concept is null ? null : concept.Requested ?? concept.En ?? session.ScopeConceptSlug;
        }

        return new ScopeDto(type, session.ScopeDomainSlug, session.ScopeTopicSlug, domainTitle, topicTitle, session.ScopeConceptSlug, conceptTitle);
    }

    private static ReviewScope ScopeOf(Session session) =>
        new(session.ScopeType, session.ScopeDomainSlug, session.ScopeTopicSlug, session.ScopeConceptSlug);

    // ---- helpers ----

    /// <summary>
    /// <c>SELECT ... FOR UPDATE</c> on one session row, held until the surrounding transaction ends. Answer, Finish and
    /// the auto-close in Start all take it, so an answer can never land in a session that is being finished.
    /// </summary>
    private async Task LockSessionAsync(Guid sessionId, CancellationToken ct) =>
        await db.Database.ExecuteSqlAsync($"SELECT id FROM sessions WHERE id = {sessionId} FOR UPDATE", ct);

    private async Task CloseAsync(Session session, DateTimeOffset now, CancellationToken ct)
    {
        var id = session.Id;
        session.FinishedAt = now;
        session.AnsweredCount = await db.Attempts.CountAsync(a => a.SessionId == id, ct);
        session.CorrectCount = await db.Attempts.CountAsync(a => a.SessionId == id && a.IsCorrect == true, ct);
    }

    private static int IndexOfCorrect(IReadOnlyList<McqOption> mcqOptions, string questionId)
    {
        for (var i = 0; i < mcqOptions.Count; i++)
        {
            if (mcqOptions[i].Correct)
            {
                return i;
            }
        }

        throw new InvalidOperationException($"Question '{questionId}' has no correct option.");
    }

    private static McqAnswer ParseAnswer(string json) =>
        JsonSerializer.Deserialize<McqAnswer>(json, Json) ?? throw new InvalidOperationException("Attempt answer is not an MCQ answer.");
}
