using System.Text.Json;
using System.Text.Json.Nodes;
using InterviewPrep.Application.Content;
using InterviewPrep.Application.Review;
using InterviewPrep.Domain.Progress;
using InterviewPrep.Domain.Scheduling;
using InterviewPrep.Infrastructure.Content;
using InterviewPrep.Infrastructure.Persistence;
using InterviewPrep.Infrastructure.Review;
using InterviewPrep.Infrastructure.Tests.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Npgsql;

namespace InterviewPrep.Infrastructure.Tests.Review;

/// <summary>
/// Session building, answering and finishing against a real PostgreSQL with a pinned clock. Default content has four
/// MCQs in taxonomy order (stack-vs-heap: mcq.1, mcq.2; span-memory: mcq.1, mcq.2), every correct option at index 1.
/// </summary>
[Collection(PostgresCollection.Name)]
[Trait("Category", "Integration")]
public sealed class ReviewSessionsTests(PostgresFixture fixture) : IAsyncLifetime
{
    private static readonly DateTimeOffset Start = new(2026, 9, 6, 10, 0, 0, TimeSpan.Zero);
    private static readonly string[] AllMcqs = [TopicTemplate.Mcq1, TopicTemplate.Mcq2, TopicTemplate.SpanMcq1, TopicTemplate.SpanMcq2];
    private const int Correct = 1;
    private const int Wrong = 0;

    private ContentDirBuilder _content = null!;
    private FakeTimeProvider _clock = null!;
    private AppDbContext _db = null!;
    private ReviewSessions _sut = null!;

    public async Task InitializeAsync()
    {
        await fixture.ResetAsync();
        _content = new ContentDirBuilder().WithDefaultContent();
        await ImportAsync();
        _clock = new FakeTimeProvider(Start);
        _db = fixture.CreateContext();
        _sut = new ReviewSessions(_db, new Sm2Algorithm(), _clock, Options.Create(new ReviewOptions()));
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        _content.Dispose();
    }

    // ---- building ----

    [Fact]
    public async Task Start_builds_membership_in_taxonomy_order_all_new_and_capped_by_the_limits()
    {
        await using (var db = fixture.CreateContext())
        {
            await db.Database.ExecuteSqlAsync($"UPDATE questions SET reviewed = false WHERE id = {TopicTemplate.Mcq2}");
        }

        var session = await _sut.StartAsync(ReviewScope.All, totalLimit: 3, newLimit: 3);

        session.Questions.Select(q => q.Id).ShouldBe([TopicTemplate.Mcq1, TopicTemplate.Mcq2, TopicTemplate.SpanMcq1]);
        session.Questions.ShouldAllBe(q => q.IsNew);
        session.Questions.Select(q => q.Reviewed).ShouldBe([true, false, true]);
        session.Scope.ShouldBe(new ScopeDto("all", null, null, null, null));
        session.StartedAt.ShouldBe(Start);
        session.FinishedAt.ShouldBeNull();
        session.TotalLimit.ShouldBe(3);
        session.NewLimit.ShouldBe(3);
        session.Answers.ShouldBeEmpty();
        session.SkippedQuestionIds.ShouldBeEmpty();

        var first = session.Questions[0];
        first.Level.ShouldBe("M");
        first.Prompt.ShouldBe("Where does a struct field of a class live?");
        first.Options.ShouldBe(["Option A", "Option B", "Option C", "Option D"]);
        first.Domain.ShouldBe(new RefDto("dotnet", "C# & .NET Runtime"));
        first.Topic.ShouldBe(new RefDto("clr-memory", "Memory & GC"));
        first.Concept.ShouldBe(new RefDto("stack-vs-heap", "Stack vs Heap"));

        await using var verify = fixture.CreateContext();
        var stored = await verify.Sessions.SingleAsync();
        stored.QuestionIds.ShouldBe([TopicTemplate.Mcq1, TopicTemplate.Mcq2, TopicTemplate.SpanMcq1]);
        stored.UserId.ShouldBe(LocalLearner.Id);
        stored.ScopeType.ShouldBe(SessionScopeType.All);
    }

    [Fact]
    public async Task New_limit_caps_the_new_questions_even_when_the_session_has_room()
    {
        var session = await _sut.StartAsync(ReviewScope.All, totalLimit: 10, newLimit: 2);

        session.Questions.Select(q => q.Id).ShouldBe([TopicTemplate.Mcq1, TopicTemplate.Mcq2]);
    }

    [Fact]
    public async Task Topic_scope_only_contains_that_topic_and_carries_titles()
    {
        await AddAsyncTopicAsync();

        var all = await _sut.StartAsync(ReviewScope.All, 20, 10);
        var topic = await _sut.StartAsync(ReviewScope.ForTopic("dotnet", "clr-memory"), 20, 10);
        var domain = await _sut.StartAsync(ReviewScope.ForDomain("dotnet"), 20, 10);

        all.Questions.Select(q => q.Id).ShouldBe([.. AllMcqs, AsyncMcq]);
        topic.Questions.Select(q => q.Id).ShouldBe(AllMcqs);
        topic.Questions.ShouldAllBe(q => q.Topic.Slug == "clr-memory");
        topic.Scope.ShouldBe(new ScopeDto("topic", "dotnet", "clr-memory", "C# & .NET Runtime", "Memory & GC"));
        domain.Questions.Count.ShouldBe(5);
        domain.Scope.ShouldBe(new ScopeDto("domain", "dotnet", null, "C# & .NET Runtime", null));
    }

    [Fact]
    public async Task Unknown_or_deprecated_scope_is_ScopeNotFound()
    {
        (await Should.ThrowAsync<ReviewException>(() => _sut.GetOverviewAsync(ReviewScope.ForDomain("nope")))).Kind.ShouldBe(ReviewError.ScopeNotFound);
        (await Should.ThrowAsync<ReviewException>(() => _sut.GetOverviewAsync(ReviewScope.ForTopic("dotnet", "nope")))).Kind.ShouldBe(ReviewError.ScopeNotFound);
        (await Should.ThrowAsync<ReviewException>(() => _sut.StartAsync(ReviewScope.ForTopic("nope", "clr-memory"), 20, 10))).Kind.ShouldBe(ReviewError.ScopeNotFound);

        await using (var db = fixture.CreateContext())
        {
            await db.Database.ExecuteSqlAsync($"UPDATE topics SET deprecated_at = {Start} WHERE slug = 'clr-memory'");
        }

        (await Should.ThrowAsync<ReviewException>(() => _sut.GetOverviewAsync(ReviewScope.ForTopic("dotnet", "clr-memory")))).Kind.ShouldBe(ReviewError.ScopeNotFound);
        (await _sut.GetOverviewAsync(ReviewScope.All)).TotalEligible.ShouldBe(0);
    }

    [Fact]
    public async Task Invalid_scope_shape_is_rejected_before_touching_the_database()
    {
        await Should.ThrowAsync<ArgumentException>(() => _sut.GetOverviewAsync(new ReviewScope(SessionScopeType.Topic, "dotnet", null)));
        await Should.ThrowAsync<ArgumentException>(() => _sut.StartAsync(new ReviewScope(SessionScopeType.All, "dotnet", null), 20, 10));
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => _sut.StartAsync(ReviewScope.All, 0, 10));
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => _sut.StartAsync(ReviewScope.All, 20, 101));
    }

    [Fact]
    public async Task Start_finishes_the_open_session_and_stores_its_counts()
    {
        var first = await _sut.StartAsync(ReviewScope.All, 2, 2);
        await _sut.AnswerAsync(first.Id, TopicTemplate.Mcq1, Correct);
        _clock.Advance(TimeSpan.FromMinutes(5));

        var second = await _sut.StartAsync(ReviewScope.All, 2, 2);

        second.Id.ShouldNotBe(first.Id);
        (await _sut.GetCurrentAsync())!.Id.ShouldBe(second.Id);
        var closed = (await _sut.GetAsync(first.Id)).ShouldNotBeNull();
        closed.FinishedAt.ShouldBe(Start.AddMinutes(5));
        closed.Answers.ShouldHaveSingleItem();

        await using var db = fixture.CreateContext();
        var stored = await db.Sessions.SingleAsync(s => s.Id == first.Id);
        stored.AnsweredCount.ShouldBe(1);
        stored.CorrectCount.ShouldBe(1);
        (await db.Sessions.CountAsync(s => s.FinishedAt == null)).ShouldBe(1);
    }

    [Fact]
    public async Task Start_with_nothing_to_review_is_NothingToReview_and_still_closes_the_open_session()
    {
        var session = await _sut.StartAsync(ReviewScope.All, 20, 10);
        foreach (var id in AllMcqs)
        {
            await _sut.AnswerAsync(session.Id, id, Correct);
        }

        var ex = await Should.ThrowAsync<ReviewException>(() => _sut.StartAsync(ReviewScope.All, 20, 10));

        ex.Kind.ShouldBe(ReviewError.NothingToReview);
        (await _sut.GetCurrentAsync()).ShouldBeNull();
        (await _sut.GetAsync(session.Id))!.FinishedAt.ShouldBe(Start);
    }

    [Fact]
    public async Task Get_and_current_return_null_when_unknown_or_none_open()
    {
        (await _sut.GetCurrentAsync()).ShouldBeNull();
        (await _sut.GetAsync(Guid.NewGuid())).ShouldBeNull();
    }

    // ---- answering ----

    [Fact]
    public async Task Correct_answer_creates_one_attempt_and_an_sm2_review_state()
    {
        var session = await _sut.StartAsync(ReviewScope.All, 4, 4);

        var feedback = await _sut.AnswerAsync(session.Id, TopicTemplate.Mcq1, Correct);

        feedback.ShouldBe(new AnswerFeedbackDto(
            TopicTemplate.Mcq1, Correct, true, Correct,
            "B is right because of the layout rules; A, C and D describe other cases.",
            Start.AddDays(1), 1));

        await using var db = fixture.CreateContext();
        var attempt = await db.Attempts.SingleAsync();
        attempt.UserId.ShouldBe(LocalLearner.Id);
        attempt.ItemType.ShouldBe(ItemType.Question);
        attempt.QuestionId.ShouldBe(TopicTemplate.Mcq1);
        attempt.ItemId.ShouldBe(TopicTemplate.Mcq1);
        attempt.SessionId.ShouldBe(session.Id);
        attempt.AnsweredAt.ShouldBe(Start);
        attempt.IsCorrect.ShouldBe(true);
        JsonNode.Parse(attempt.Answer)!["selectedIndex"]!.GetValue<int>().ShouldBe(1);
        JsonNode.Parse(attempt.Answer)!["selectedText"]!.GetValue<string>().ShouldBe("Option B");

        var state = await db.ReviewStates.SingleAsync();
        state.UserId.ShouldBe(LocalLearner.Id);
        state.QuestionId.ShouldBe(TopicTemplate.Mcq1);
        state.Algorithm.ShouldBe("sm2");
        state.IntervalDays.ShouldBe(1);
        state.Ease.ShouldBe(2.5);
        state.Repetitions.ShouldBe(1);
        state.LastReviewedAt.ShouldBe(Start);
        state.DueAt.ShouldBe(Start.AddDays(1));
        state.CreatedAt.ShouldBe(Start);
        JsonNode.Parse(state.AlgorithmState!)!["lastQuality"]!.GetValue<int>().ShouldBe(4);

        var resumed = (await _sut.GetAsync(session.Id)).ShouldNotBeNull();
        resumed.Answers.ShouldBe([new SessionAnswerDto(TopicTemplate.Mcq1, Correct, true)]);
        resumed.Questions.Single(q => q.Id == TopicTemplate.Mcq1).IsNew.ShouldBeTrue("the state was created by this session");
    }

    [Fact]
    public async Task Wrong_answer_resets_the_streak_lowers_ease_and_is_due_tomorrow()
    {
        var session = await _sut.StartAsync(ReviewScope.All, 4, 4);

        var feedback = await _sut.AnswerAsync(session.Id, TopicTemplate.Mcq2, Wrong);

        feedback.IsCorrect.ShouldBeFalse();
        feedback.CorrectIndex.ShouldBe(Correct);
        feedback.IntervalDays.ShouldBe(1);
        feedback.NextDueAt.ShouldBe(Start.AddDays(1));

        await using var db = fixture.CreateContext();
        var state = await db.ReviewStates.SingleAsync();
        state.Repetitions.ShouldBe(0);
        state.Ease.ShouldBe(2.3, tolerance: 1e-9);
        state.IntervalDays.ShouldBe(1);
        JsonNode.Parse(state.AlgorithmState!)!["lastQuality"]!.GetValue<int>().ShouldBe(1);
        (await db.Attempts.SingleAsync()).IsCorrect.ShouldBe(false);
    }

    [Fact]
    public async Task Second_answer_to_the_same_question_is_DuplicateAnswer_and_changes_nothing()
    {
        var session = await _sut.StartAsync(ReviewScope.All, 4, 4);
        await _sut.AnswerAsync(session.Id, TopicTemplate.Mcq1, Correct);

        var ex = await Should.ThrowAsync<ReviewException>(() => _sut.AnswerAsync(session.Id, TopicTemplate.Mcq1, Wrong));

        ex.Kind.ShouldBe(ReviewError.DuplicateAnswer);
        await using var db = fixture.CreateContext();
        (await db.Attempts.CountAsync()).ShouldBe(1);
        (await db.ReviewStates.SingleAsync()).Repetitions.ShouldBe(1);
    }

    [Fact]
    public async Task Partial_unique_index_rejects_a_raw_second_insert_but_not_session_less_attempts()
    {
        var session = await _sut.StartAsync(ReviewScope.All, 4, 4);
        await _sut.AnswerAsync(session.Id, TopicTemplate.Mcq1, Correct);

        var ex = await Should.ThrowAsync<PostgresException>(() => InsertAttemptAsync(TopicTemplate.Mcq1, session.Id));
        ex.SqlState.ShouldBe(PostgresErrorCodes.UniqueViolation);
        ex.ConstraintName.ShouldBe(ReviewSessions.SessionItemIndex);

        await InsertAttemptAsync(TopicTemplate.Mcq1, sessionId: null);
        await InsertAttemptAsync(TopicTemplate.Mcq1, sessionId: null);
        await using var db = fixture.CreateContext();
        (await db.Attempts.CountAsync()).ShouldBe(3);
    }

    [Fact]
    public async Task Answer_errors_for_unknown_session_foreign_question_and_finished_session()
    {
        (await Should.ThrowAsync<ReviewException>(() => _sut.AnswerAsync(Guid.NewGuid(), TopicTemplate.Mcq1, Correct))).Kind.ShouldBe(ReviewError.SessionNotFound);

        var session = await _sut.StartAsync(ReviewScope.All, 2, 2);
        (await Should.ThrowAsync<ReviewException>(() => _sut.AnswerAsync(session.Id, TopicTemplate.SpanMcq1, Correct))).Kind.ShouldBe(ReviewError.QuestionNotInSession);
        (await Should.ThrowAsync<ReviewException>(() => _sut.AnswerAsync(session.Id, TopicTemplate.Open1, Correct))).Kind.ShouldBe(ReviewError.QuestionNotInSession);
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => _sut.AnswerAsync(session.Id, TopicTemplate.Mcq1, 4));

        await _sut.FinishAsync(session.Id);
        (await Should.ThrowAsync<ReviewException>(() => _sut.AnswerAsync(session.Id, TopicTemplate.Mcq1, Correct))).Kind.ShouldBe(ReviewError.SessionFinished);

        await using var db = fixture.CreateContext();
        (await db.Attempts.CountAsync()).ShouldBe(0);
    }

    // ---- scheduling across days (SC-004) ----

    [Fact]
    public async Task Next_session_contains_what_was_answered_wrong_and_not_what_was_answered_right()
    {
        // Day 0: both new, both right → due tomorrow with interval 1.
        var day0 = await _sut.StartAsync(ReviewScope.All, 2, 2);
        await _sut.AnswerAsync(day0.Id, TopicTemplate.Mcq1, Correct);
        await _sut.AnswerAsync(day0.Id, TopicTemplate.Mcq2, Correct);
        await _sut.FinishAsync(day0.Id);

        // "Review again" right away: nothing due yet, so only the remaining new ones (left unanswered, so they stay new).
        var again = await _sut.StartAsync(ReviewScope.All, 2, 2);
        again.Questions.Select(q => q.Id).ShouldBe([TopicTemplate.SpanMcq1, TopicTemplate.SpanMcq2]);
        await _sut.FinishAsync(again.Id);

        _clock.Advance(TimeSpan.FromDays(1));

        // Day 1: both due; A right (interval 6), B wrong (interval 1, due tomorrow).
        var overview = await _sut.GetOverviewAsync(ReviewScope.All);
        overview.DueCount.ShouldBe(2);
        overview.NewCount.ShouldBe(2);
        var day1 = await _sut.StartAsync(ReviewScope.All, 2, 2);
        day1.Questions.Select(q => q.Id).ShouldBe([TopicTemplate.Mcq1, TopicTemplate.Mcq2]);
        day1.Questions.ShouldAllBe(q => !q.IsNew);
        (await _sut.AnswerAsync(day1.Id, TopicTemplate.Mcq1, Correct)).IntervalDays.ShouldBe(6);
        (await _sut.AnswerAsync(day1.Id, TopicTemplate.Mcq2, Wrong)).IntervalDays.ShouldBe(1);
        var summary = await _sut.FinishAsync(day1.Id);
        summary.DueTomorrow.ShouldBe(1, "only B; A is due in 6 days and the span questions have no review state");

        _clock.Advance(TimeSpan.FromDays(1));

        // Day 2: B is back (due, so first), the never-answered span questions follow as new, A is out.
        var day2 = await _sut.StartAsync(ReviewScope.All, 20, 10);
        day2.Questions.Select(q => q.Id).ShouldBe([TopicTemplate.Mcq2, TopicTemplate.SpanMcq1, TopicTemplate.SpanMcq2]);
        day2.Questions.ShouldNotContain(q => q.Id == TopicTemplate.Mcq1);
        day2.Questions.Select(q => q.IsNew).ShouldBe([false, true, true]);
    }

    [Fact]
    public async Task Due_questions_come_before_new_ones_most_overdue_first()
    {
        var first = await _sut.StartAsync(ReviewScope.All, 1, 1);
        await _sut.AnswerAsync(first.Id, TopicTemplate.Mcq1, Wrong);
        _clock.Advance(TimeSpan.FromHours(1));
        var second = await _sut.StartAsync(ReviewScope.All, 1, 1);
        second.Questions.Single().Id.ShouldBe(TopicTemplate.Mcq2);
        await _sut.AnswerAsync(second.Id, TopicTemplate.Mcq2, Wrong);
        await _sut.FinishAsync(second.Id);

        _clock.Advance(TimeSpan.FromDays(1));
        var session = await _sut.StartAsync(ReviewScope.All, 20, 10);

        session.Questions.Select(q => q.Id).ShouldBe([TopicTemplate.Mcq1, TopicTemplate.Mcq2, TopicTemplate.SpanMcq1, TopicTemplate.SpanMcq2]);
        session.Questions.Select(q => q.IsNew).ShouldBe([false, false, true, true]);
    }

    // ---- resume ----

    [Fact]
    public async Task Question_deprecated_after_start_is_skipped_unless_already_answered()
    {
        var session = await _sut.StartAsync(ReviewScope.All, 4, 4);
        await _sut.AnswerAsync(session.Id, TopicTemplate.Mcq1, Wrong);
        await using (var db = fixture.CreateContext())
        {
            await db.Database.ExecuteSqlAsync($"UPDATE questions SET deprecated_at = {Start} WHERE id IN ({TopicTemplate.Mcq1}, {TopicTemplate.Mcq2})");
        }

        var resumed = (await _sut.GetCurrentAsync()).ShouldNotBeNull();

        resumed.Id.ShouldBe(session.Id);
        resumed.Questions.Select(q => q.Id).ShouldBe([TopicTemplate.Mcq1, TopicTemplate.SpanMcq1, TopicTemplate.SpanMcq2]);
        resumed.SkippedQuestionIds.ShouldBe([TopicTemplate.Mcq2]);
        resumed.Answers.ShouldBe([new SessionAnswerDto(TopicTemplate.Mcq1, Wrong, false)]);
    }

    [Fact]
    public async Task Deprecated_concept_makes_its_questions_ineligible()
    {
        await using (var db = fixture.CreateContext())
        {
            await db.Database.ExecuteSqlAsync($"UPDATE concepts SET deprecated_at = {Start} WHERE slug = {TopicTemplate.SpanMemory}");
        }

        var overview = await _sut.GetOverviewAsync(ReviewScope.All);
        overview.TotalEligible.ShouldBe(2);
        overview.NewCount.ShouldBe(2);
        (await _sut.StartAsync(ReviewScope.All, 20, 10)).Questions.Select(q => q.Id).ShouldBe([TopicTemplate.Mcq1, TopicTemplate.Mcq2]);
    }

    // ---- overview ----

    [Fact]
    public async Task Overview_reports_counts_defaults_and_next_due()
    {
        var fresh = await _sut.GetOverviewAsync(ReviewScope.ForTopic("dotnet", "clr-memory"));
        fresh.Scope.ShouldBe(new ScopeDto("topic", "dotnet", "clr-memory", "C# & .NET Runtime", "Memory & GC"));
        fresh.DueCount.ShouldBe(0);
        fresh.NewCount.ShouldBe(4);
        fresh.TotalEligible.ShouldBe(4);
        fresh.NextDueAt.ShouldBeNull();
        fresh.Defaults.ShouldBe(new SessionDefaultsDto(20, 10));
        fresh.Limits.ShouldBe(new SessionLimitsDto(1, 100));

        var session = await _sut.StartAsync(ReviewScope.All, 4, 4);
        await _sut.AnswerAsync(session.Id, TopicTemplate.Mcq1, Correct);
        _clock.Advance(TimeSpan.FromHours(2));
        await _sut.AnswerAsync(session.Id, TopicTemplate.Mcq2, Wrong);
        await _sut.FinishAsync(session.Id);

        var after = await _sut.GetOverviewAsync(ReviewScope.All);
        after.DueCount.ShouldBe(0);
        after.NewCount.ShouldBe(2);
        after.TotalEligible.ShouldBe(4);
        after.NextDueAt.ShouldBe(Start.AddDays(1), "the earliest of the two upcoming due times");

        // Now = Start + 2h; 23h later the first answer (due Start + 24h) is due, the second (due Start + 26h) is not yet.
        _clock.Advance(TimeSpan.FromHours(23));
        var nextDay = await _sut.GetOverviewAsync(ReviewScope.All);
        nextDay.DueCount.ShouldBe(1);
        nextDay.NextDueAt.ShouldBe(Start.AddHours(2).AddDays(1));

        _clock.Advance(TimeSpan.FromHours(1));
        var later = await _sut.GetOverviewAsync(ReviewScope.All);
        later.DueCount.ShouldBe(2);
        later.NextDueAt.ShouldBeNull();
    }

    [Fact]
    public async Task Overview_defaults_come_from_options()
    {
        var sut = new ReviewSessions(_db, new Sm2Algorithm(), _clock, Options.Create(new ReviewOptions { DefaultTotalLimit = 7, DefaultNewLimit = 3 }));

        (await sut.GetOverviewAsync(ReviewScope.All)).Defaults.ShouldBe(new SessionDefaultsDto(7, 3));
    }

    // ---- finishing ----

    [Fact]
    public async Task Finish_stores_counts_and_returns_weak_concepts_and_due_tomorrow()
    {
        var session = await _sut.StartAsync(ReviewScope.ForTopic("dotnet", "clr-memory"), 4, 4);
        await _sut.AnswerAsync(session.Id, TopicTemplate.Mcq1, Wrong);
        await _sut.AnswerAsync(session.Id, TopicTemplate.Mcq2, Correct);
        await _sut.AnswerAsync(session.Id, TopicTemplate.SpanMcq1, Wrong);
        await _sut.AnswerAsync(session.Id, TopicTemplate.SpanMcq2, Wrong);
        _clock.Advance(TimeSpan.FromMinutes(3));

        var summary = await _sut.FinishAsync(session.Id);

        summary.SessionId.ShouldBe(session.Id);
        summary.Scope.ShouldBe(new ScopeDto("topic", "dotnet", "clr-memory", "C# & .NET Runtime", "Memory & GC"));
        summary.StartedAt.ShouldBe(Start);
        summary.FinishedAt.ShouldBe(Start.AddMinutes(3));
        summary.QuestionCount.ShouldBe(4);
        summary.Answered.ShouldBe(4);
        summary.Correct.ShouldBe(1);
        summary.Accuracy.ShouldBe(0.25);
        summary.WeakConcepts.ShouldBe(
        [
            new WeakConceptDto("dotnet", "clr-memory", "span-memory", "Span and Memory", 2, 2),
            new WeakConceptDto("dotnet", "clr-memory", "stack-vs-heap", "Stack vs Heap", 1, 2),
        ]);
        summary.DueTomorrow.ShouldBe(4, "every first answer, right or wrong, is due in one day");

        await using var db = fixture.CreateContext();
        var stored = await db.Sessions.SingleAsync();
        stored.FinishedAt.ShouldBe(Start.AddMinutes(3));
        stored.AnsweredCount.ShouldBe(4);
        stored.CorrectCount.ShouldBe(1);
    }

    [Fact]
    public async Task Finish_is_idempotent_and_ended_early_counts_only_given_answers()
    {
        var session = await _sut.StartAsync(ReviewScope.All, 4, 4);
        await _sut.AnswerAsync(session.Id, TopicTemplate.Mcq1, Correct);
        var first = await _sut.FinishAsync(session.Id);
        _clock.Advance(TimeSpan.FromHours(1));

        var second = await _sut.FinishAsync(session.Id);

        first.Answered.ShouldBe(1);
        first.Correct.ShouldBe(1);
        first.Accuracy.ShouldBe(1.0);
        first.QuestionCount.ShouldBe(4);
        first.WeakConcepts.ShouldBeEmpty();
        second.FinishedAt.ShouldBe(first.FinishedAt);
        second.Answered.ShouldBe(first.Answered);
        second.Correct.ShouldBe(first.Correct);
        (await _sut.GetCurrentAsync()).ShouldBeNull();
    }

    [Fact]
    public async Task Finish_without_answers_has_null_accuracy()
    {
        var session = await _sut.StartAsync(ReviewScope.All, 4, 4);

        var summary = await _sut.FinishAsync(session.Id);

        summary.Answered.ShouldBe(0);
        summary.Accuracy.ShouldBeNull();
        summary.DueTomorrow.ShouldBe(0);
        (await Should.ThrowAsync<ReviewException>(() => _sut.FinishAsync(Guid.NewGuid()))).Kind.ShouldBe(ReviewError.SessionNotFound);
    }

    // ---- helpers ----

    private const string AsyncMcq = "dotnet.async.tasks.mcq.1";

    private async Task ImportAsync()
    {
        await using var db = fixture.CreateContext();
        var result = await new ContentImporter(db, NullLogger<ContentImporter>.Instance).ImportAsync(new ImportOptions(_content.Dir));
        result.ExitCode.ShouldBe(ImportExitCode.Success, string.Join("\n", result.Errors));
        await new LearnerSeeder(db).SeedAsync();
    }

    /// <summary>A second topic (dotnet/async) with one concept and one MCQ, so scope filtering has something to exclude.</summary>
    private async Task AddAsyncTopicAsync()
    {
        var manifest = TopicTemplate.Manifest();
        ((JsonArray)manifest["domains"]![0]!["topics"]!).Add(new JsonObject { ["slug"] = "async", ["title"] = "Async & Threading" });
        _content.WriteManifest(manifest);

        var mcq = TopicTemplate.McqTranslation("Which type represents a hot task?");
        _content.WriteTopic(new JsonObject
        {
            ["domain"] = "dotnet",
            ["topic"] = "async",
            ["concepts"] = new JsonArray(new JsonObject
            {
                ["slug"] = "tasks",
                ["title"] = "Tasks",
                ["level"] = "M",
                ["translations"] = new JsonObject { ["en"] = new JsonObject { ["title"] = "Tasks", ["note"] = TopicTemplate.Note("Tasks") } },
                ["questions"] = new JsonArray(new JsonObject
                {
                    ["id"] = AsyncMcq,
                    ["type"] = "mcq",
                    ["level"] = "M",
                    ["reviewed"] = true,
                    ["translations"] = new JsonObject { ["en"] = mcq },
                    ["tags"] = new JsonArray("async"),
                }),
                ["katas"] = new JsonArray(),
            }),
        });
        await ImportAsync();
    }

    private async Task InsertAttemptAsync(string questionId, Guid? sessionId)
    {
        await using var db = fixture.CreateContext();
        var now = Start;
        var answer = JsonSerializer.Serialize(new { selectedIndex = 0, selectedText = "Option A" });
        await db.Database.ExecuteSqlAsync(
            $"""
             INSERT INTO attempts (id, user_id, item_type, question_id, kata_id, session_id, answered_at, answer, is_correct, score, grading, created_at)
             VALUES ({Guid.NewGuid()}, {LocalLearner.Id}, 'question', {questionId}, NULL, {sessionId}, {now}, {answer}::jsonb, false, NULL, NULL, {now})
             """);
    }
}
