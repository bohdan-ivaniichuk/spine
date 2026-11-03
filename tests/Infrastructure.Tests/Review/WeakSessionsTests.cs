using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Spine.Application.Content.Import;
using Spine.Application.Review.AnswerQuestion;
using Spine.Application.Review.Shared;
using Spine.Domain.Progress;
using Spine.Domain.Scheduling;
using Spine.Infrastructure.Content;
using Spine.Infrastructure.Persistence;
using Spine.Infrastructure.Persistence.Startup;
using Spine.Infrastructure.Tests.Content;

namespace Spine.Infrastructure.Tests.Review;

/// <summary>
/// Weak sessions against a real PostgreSQL with a pinned clock that advances one minute before every answer, so
/// "newest miss first" is well defined. Default content: stack-vs-heap (mcq.1, mcq.2) and span-memory (mcq.1, mcq.2),
/// every correct option at index 1.
/// </summary>
[Collection(PostgresCollection.Name)]
[Trait("Category", "Integration")]
public sealed class WeakSessionsTests(PostgresFixture fixture) : IAsyncLifetime
{
    private static readonly DateTimeOffset Start = new(2026, 10, 20, 10, 0, 0, TimeSpan.Zero);
    private const int Correct = 1;
    private const int Wrong = 0;

    private ContentDirBuilder _content = null!;
    private FakeTimeProvider _clock = null!;
    private AppDbContext _db = null!;
    private ReviewSessionsHarness _sut = null!;

    public async Task InitializeAsync()
    {
        await fixture.ResetAsync();
        _content = new ContentDirBuilder().WithDefaultContent();
        await using (var db = fixture.CreateContext())
        {
            var result = await new ContentImporter(db, NullLogger<ContentImporter>.Instance).ImportAsync(new ImportOptions(_content.Dir));
            result.ExitCode.ShouldBe(ImportExitCode.Success, string.Join("\n", result.Errors));
            await new LearnerSeeder(db).SeedAsync();
        }

        _clock = new FakeTimeProvider(Start);
        _db = fixture.CreateContext();
        _sut = new ReviewSessionsHarness(_db, new Sm2Algorithm(), _clock, Options.Create(new ReviewOptions()));
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        _content.Dispose();
    }

    [Fact]
    [Trait("Requirement", "FR-001")]
    public async Task Weak_session_holds_exactly_the_questions_last_answered_wrong_even_when_not_due()
    {
        await ReviewAsync((TopicTemplate.Mcq1, Wrong), (TopicTemplate.Mcq2, Correct), (TopicTemplate.SpanMcq1, Wrong));

        var weak = await _sut.StartWeakAsync(ReviewScope.All, 20);

        Assert.Equal([TopicTemplate.SpanMcq1, TopicTemplate.Mcq1], weak.Questions.Select(q => q.Id));
        Assert.Equal("weak", weak.Mode);
    }

    [Fact]
    [Trait("Requirement", "FR-001")]
    public async Task A_question_answered_wrong_and_then_right_leaves_the_weak_set()
    {
        await ReviewAsync((TopicTemplate.Mcq1, Wrong), (TopicTemplate.SpanMcq1, Wrong));
        var drill = await _sut.StartWeakAsync(ReviewScope.All, 20);
        await AnswerAsync(drill.Id, TopicTemplate.Mcq1, Correct);
        await _sut.FinishAsync(drill.Id);

        var again = await _sut.StartWeakAsync(ReviewScope.All, 20);

        Assert.Equal([TopicTemplate.SpanMcq1], again.Questions.Select(q => q.Id));
    }

    [Fact]
    [Trait("Requirement", "FR-001")]
    public async Task Deprecated_questions_are_never_weak()
    {
        await ReviewAsync((TopicTemplate.Mcq1, Wrong), (TopicTemplate.SpanMcq1, Wrong));
        await using (var db = fixture.CreateContext())
        {
            await db.Database.ExecuteSqlAsync($"UPDATE questions SET deprecated_at = {Start} WHERE id = {TopicTemplate.SpanMcq1}");
        }

        var weak = await _sut.StartWeakAsync(ReviewScope.All, 20);

        Assert.Equal([TopicTemplate.Mcq1], weak.Questions.Select(q => q.Id));
    }

    [Fact]
    [Trait("Requirement", "FR-002")]
    public async Task Newest_miss_comes_first_and_the_total_limit_applies()
    {
        await ReviewAsync((TopicTemplate.Mcq1, Wrong), (TopicTemplate.Mcq2, Wrong), (TopicTemplate.SpanMcq1, Wrong));

        var weak = await _sut.StartWeakAsync(ReviewScope.All, 2);

        Assert.Equal([TopicTemplate.SpanMcq1, TopicTemplate.Mcq2], weak.Questions.Select(q => q.Id));
    }

    [Fact]
    [Trait("Requirement", "FR-003")]
    public async Task Topic_and_concept_scopes_limit_the_weak_questions()
    {
        await ReviewAsync((TopicTemplate.Mcq1, Wrong), (TopicTemplate.SpanMcq1, Wrong));

        var concept = await _sut.StartWeakAsync(ReviewScope.ForConcept(TopicTemplate.Domain, TopicTemplate.Topic, TopicTemplate.StackVsHeap), 20);
        var topic = await _sut.StartWeakAsync(ReviewScope.ForTopic(TopicTemplate.Domain, TopicTemplate.Topic), 20);

        Assert.Equal([TopicTemplate.Mcq1], concept.Questions.Select(q => q.Id));
        Assert.Equal("concept", concept.Scope.Type);
        Assert.Equal([TopicTemplate.SpanMcq1, TopicTemplate.Mcq1], topic.Questions.Select(q => q.Id));
    }

    [Fact]
    [Trait("Requirement", "FR-004")]
    public async Task Answers_in_a_weak_session_update_the_schedule_and_history_and_the_summary_says_weak()
    {
        await ReviewAsync((TopicTemplate.Mcq1, Wrong));
        var drill = await _sut.StartWeakAsync(ReviewScope.All, 20);

        var feedback = await AnswerAsync(drill.Id, TopicTemplate.Mcq1, Correct);
        var summary = await _sut.FinishAsync(drill.Id);

        Assert.True(feedback.IsCorrect);
        Assert.Equal("weak", summary.Mode);
        await using var db = fixture.CreateContext();
        Assert.Equal(2, await db.Attempts.CountAsync(a => a.QuestionId == TopicTemplate.Mcq1));
        var state = await db.ReviewStates.SingleAsync(r => r.QuestionId == TopicTemplate.Mcq1);
        Assert.Equal(1, state.Repetitions);
        Assert.Equal(SessionMode.Weak, (await db.Sessions.SingleAsync(s => s.Id == drill.Id)).Mode);
    }

    [Fact]
    [Trait("Requirement", "FR-005")]
    public async Task Nothing_to_drill_creates_no_session_and_keeps_the_open_one()
    {
        var open = await _sut.StartAsync(ReviewScope.All, 2, 2);

        var ex = await Assert.ThrowsAsync<ReviewException>(() => _sut.StartWeakAsync(ReviewScope.All, 20));

        Assert.Equal(ReviewError.NothingToDrill, ex.Kind);
        Assert.Equal(open.Id, (await _sut.GetCurrentAsync())!.Id);
        await using var db = fixture.CreateContext();
        Assert.Equal(1, await db.Sessions.CountAsync());
    }

    [Fact]
    [Trait("Requirement", "FR-001")]
    public async Task Starting_a_weak_session_finishes_the_open_regular_one()
    {
        var regular = await _sut.StartAsync(ReviewScope.All, 4, 4);
        await AnswerAsync(regular.Id, TopicTemplate.Mcq1, Wrong);

        var weak = await _sut.StartWeakAsync(ReviewScope.All, 20);

        Assert.Equal(weak.Id, (await _sut.GetCurrentAsync())!.Id);
        Assert.NotNull((await _sut.GetAsync(regular.Id))!.FinishedAt);
        Assert.Equal([TopicTemplate.Mcq1], weak.Questions.Select(q => q.Id));
    }

    [Fact]
    [Trait("Requirement", "FR-006")]
    public async Task Overview_counts_the_weak_questions_in_scope()
    {
        await ReviewAsync((TopicTemplate.Mcq1, Wrong), (TopicTemplate.Mcq2, Correct), (TopicTemplate.SpanMcq1, Wrong));

        var all = await _sut.GetOverviewAsync(ReviewScope.All);
        var concept = await _sut.GetOverviewAsync(ReviewScope.ForConcept(TopicTemplate.Domain, TopicTemplate.Topic, TopicTemplate.StackVsHeap));

        Assert.Equal(2, all.WeakCount);
        Assert.Equal(1, concept.WeakCount);
    }

    /// <summary>One regular session over everything, answering only the given questions, then finished.</summary>
    private async Task ReviewAsync(params (string Id, int Index)[] answers)
    {
        var session = await _sut.StartAsync(ReviewScope.All, 20, 20);
        foreach (var (id, index) in answers)
        {
            await AnswerAsync(session.Id, id, index);
        }

        await _sut.FinishAsync(session.Id);
    }

    private Task<AnswerFeedbackDto> AnswerAsync(Guid sessionId, string questionId, int index)
    {
        _clock.Advance(TimeSpan.FromMinutes(1));
        return _sut.AnswerAsync(sessionId, questionId, index);
    }
}
