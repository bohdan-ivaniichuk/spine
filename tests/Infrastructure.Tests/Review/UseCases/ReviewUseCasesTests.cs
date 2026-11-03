using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Spine.Application.Content.Import;
using Spine.Application.Review.AnswerQuestion;
using Spine.Application.Review.FinishSession;
using Spine.Application.Review.GetCurrentSession;
using Spine.Application.Review.GetReviewCoverage;
using Spine.Application.Review.GetReviewOverview;
using Spine.Application.Review.GetSession;
using Spine.Application.Review.Sessions;
using Spine.Application.Review.Shared;
using Spine.Application.Review.StartSession;
using Spine.Application.Review.StartWeakSession;
using Spine.Domain.Content;
using Spine.Infrastructure.Content;
using Spine.Infrastructure.Persistence;
using Spine.Infrastructure.Persistence.Startup;
using Spine.Infrastructure.Review;
using Spine.Infrastructure.Tests.Content;

namespace Spine.Infrastructure.Tests.Review.UseCases;

/// <summary>
/// One contract scenario per review use case (FR-005), each resolved from <c>AddReview()</c> exactly as the API resolves
/// it. Default content: four MCQs (stack-vs-heap mcq.1/2, span-memory mcq.1/2), every correct option at index 1.
/// </summary>
[Collection(PostgresCollection.Name)]
[Trait("Category", "Integration")]
public sealed class ReviewUseCasesTests(PostgresFixture fixture) : IAsyncLifetime
{
    private static readonly DateTimeOffset Start = new(2026, 11, 3, 9, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _clock = new(Start);
    private ContentDirBuilder _content = null!;
    private ServiceProvider _services = null!;

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

        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(_clock);
        services.AddDbContext<AppDbContext>(o => DependencyInjection.ConfigureDbContext(o, fixture.ConnectionString));
        services.AddReview(new ConfigurationBuilder().Build());
        _services = services.BuildServiceProvider(validateScopes: true);
    }

    public async Task DisposeAsync()
    {
        await _services.DisposeAsync();
        _content.Dispose();
    }

    /// <summary>Runs <paramref name="action"/> with a use case from a fresh scope, as one HTTP request would.</summary>
    private async Task<TResult> Use<TUseCase, TResult>(Func<TUseCase, Task<TResult>> action)
        where TUseCase : notnull
    {
        await using var scope = _services.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<TUseCase>());
    }

    private Task<SessionDto> StartAllAsync() =>
        Use<IStartSessionUseCase, SessionDto>(u => u.ExecuteAsync(ReviewScope.All, 4, 4, Language.En, CancellationToken.None));

    [Fact]
    [Trait("Requirement", "FR-005")]
    public async Task Overview_counts_new_due_and_weak_with_the_defaults()
    {
        var overview = await Use<IGetReviewOverviewUseCase, ReviewOverviewDto>(u => u.ExecuteAsync(ReviewScope.All, Language.En, CancellationToken.None));

        (overview.TotalEligible, overview.NewCount, overview.DueCount, overview.WeakCount).ShouldBe((4, 4, 0, 0));
        overview.Defaults.ShouldBe(new SessionDefaultsDto(20, 10));
    }

    [Fact]
    [Trait("Requirement", "FR-005")]
    public async Task Coverage_lists_the_topic_with_its_eligible_questions()
    {
        var coverage = await Use<IGetReviewCoverageUseCase, ReviewCoverageDto>(u => u.ExecuteAsync(CancellationToken.None));

        coverage.Topics.ShouldContain(t => t.Domain == TopicTemplate.Domain && t.Topic == TopicTemplate.Topic && t.Eligible == 4 && t.Seen == 0);
    }

    [Fact]
    [Trait("Requirement", "FR-005")]
    public async Task Start_builds_a_regular_session_in_taxonomy_order()
    {
        var session = await StartAllAsync();

        session.Mode.ShouldBe("regular");
        session.Questions.Select(q => q.Id).ShouldBe([TopicTemplate.Mcq1, TopicTemplate.Mcq2, TopicTemplate.SpanMcq1, TopicTemplate.SpanMcq2]);
    }

    [Fact]
    [Trait("Requirement", "FR-005")]
    public async Task Start_weak_drills_the_question_answered_wrong()
    {
        var first = await StartAllAsync();
        await Use<IAnswerQuestionUseCase, AnswerFeedbackDto>(u => u.ExecuteAsync(first.Id, TopicTemplate.Mcq2, 0, Language.En, CancellationToken.None));

        var weak = await Use<IStartWeakSessionUseCase, SessionDto>(u => u.ExecuteAsync(ReviewScope.All, 5, Language.En, CancellationToken.None));

        weak.Mode.ShouldBe("weak");
        weak.Questions.Select(q => q.Id).ShouldBe([TopicTemplate.Mcq2]);
    }

    [Fact]
    [Trait("Requirement", "FR-005")]
    public async Task Current_returns_the_open_session_and_nothing_after_finish()
    {
        var session = await StartAllAsync();

        (await Use<IGetCurrentSessionUseCase, SessionDto?>(u => u.ExecuteAsync(Language.En, CancellationToken.None)))!.Id.ShouldBe(session.Id);
        await Use<IFinishSessionUseCase, SessionSummaryDto>(u => u.ExecuteAsync(session.Id, Language.En, CancellationToken.None));
        (await Use<IGetCurrentSessionUseCase, SessionDto?>(u => u.ExecuteAsync(Language.En, CancellationToken.None))).ShouldBeNull();
    }

    [Fact]
    [Trait("Requirement", "FR-005")]
    public async Task Get_by_id_returns_the_session_or_null()
    {
        var session = await StartAllAsync();

        (await Use<IGetSessionUseCase, SessionDto?>(u => u.ExecuteAsync(session.Id, Language.En, CancellationToken.None)))!.Questions.Count.ShouldBe(4);
        (await Use<IGetSessionUseCase, SessionDto?>(u => u.ExecuteAsync(Guid.NewGuid(), Language.En, CancellationToken.None))).ShouldBeNull();
    }

    [Fact]
    [Trait("Requirement", "FR-005")]
    public async Task Answer_grades_and_schedules_the_first_review_a_day_later()
    {
        var session = await StartAllAsync();

        var feedback = await Use<IAnswerQuestionUseCase, AnswerFeedbackDto>(
            u => u.ExecuteAsync(session.Id, TopicTemplate.Mcq1, 1, Language.En, CancellationToken.None));

        (feedback.IsCorrect, feedback.CorrectIndex, feedback.IntervalDays).ShouldBe((true, 1, 1));
        feedback.NextDueAt.ShouldBe(Start.AddDays(1));
    }

    [Fact]
    [Trait("Requirement", "FR-005")]
    public async Task Finish_summarises_the_answers()
    {
        var session = await StartAllAsync();
        await Use<IAnswerQuestionUseCase, AnswerFeedbackDto>(u => u.ExecuteAsync(session.Id, TopicTemplate.Mcq1, 1, Language.En, CancellationToken.None));

        var summary = await Use<IFinishSessionUseCase, SessionSummaryDto>(u => u.ExecuteAsync(session.Id, Language.En, CancellationToken.None));

        (summary.Answered, summary.Correct, summary.QuestionCount).ShouldBe((1, 1, 4));
        summary.FinishedAt.ShouldBe(Start);
    }
}
