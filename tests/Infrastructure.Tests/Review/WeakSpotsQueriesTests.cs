using System.Text.Json;
using Spine.Application.Content;
using Spine.Application.Review;
using Spine.Domain.Progress;
using Spine.Infrastructure.Content;
using Spine.Infrastructure.Persistence;
using Spine.Infrastructure.Persistence.Startup;
using Spine.Infrastructure.Review;
using Spine.Infrastructure.Tests.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace Spine.Infrastructure.Tests.Review;

/// <summary>
/// Weak-spots statistics against a real PostgreSQL with a pinned clock. Attempts are inserted directly with exact times.
/// Hand-calculated history (window = the last 30 days, boundary included):
/// stack-vs-heap — mcq.1: -40d wrong (outside), -5d right, -3d wrong, -1d wrong (weak); mcq.2: -2d right
///   → 4 answers, 2 wrong, accuracy 0.5, last -1d, 1 weak question;
/// span-memory — mcq.1: -30d wrong (boundary), -4d right, -2d right → 3 answers, 1 wrong, last -2d, 0 weak;
/// topic clr-memory → 7 answers, 3 wrong, last -1d, 1 weak.
/// </summary>
[Collection(PostgresCollection.Name)]
[Trait("Category", "Integration")]
public sealed class WeakSpotsQueriesTests(PostgresFixture fixture) : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);

    private ContentDirBuilder _content = null!;

    public async Task InitializeAsync()
    {
        await fixture.ResetAsync();
        _content = new ContentDirBuilder().WithDefaultContent();
        await using var db = fixture.CreateContext();
        var result = await new ContentImporter(db, NullLogger<ContentImporter>.Instance).ImportAsync(new ImportOptions(_content.Dir));
        result.ExitCode.ShouldBe(ImportExitCode.Success, string.Join("\n", result.Errors));
        await new LearnerSeeder(db).SeedAsync();

        await AttemptAsync(TopicTemplate.Mcq1, -40, false);
        await AttemptAsync(TopicTemplate.Mcq1, -5, true);
        await AttemptAsync(TopicTemplate.Mcq1, -3, false);
        await AttemptAsync(TopicTemplate.Mcq1, -1, false);
        await AttemptAsync(TopicTemplate.Mcq2, -2, true);
        await AttemptAsync(TopicTemplate.SpanMcq1, -30, false);
        await AttemptAsync(TopicTemplate.SpanMcq1, -4, true);
        await AttemptAsync(TopicTemplate.SpanMcq1, -2, true);
    }

    public Task DisposeAsync()
    {
        _content.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    [Trait("Requirement", "FR-007")]
    public async Task Figures_match_a_hand_calculation_over_the_window()
    {
        var spots = await GetAsync();

        Assert.Equal(Now.AddDays(-WeaknessRule.WindowDays), spots.Since);
        Assert.Equal((30, 3), (spots.WindowDays, spots.MinAnswers));
        var stack = spots.Concepts.Single(c => c.Concept == TopicTemplate.StackVsHeap);
        Assert.Equal((4, 2, 1), (stack.Answers, stack.Wrong, stack.WeakQuestions));
        Assert.Equal(0.5, stack.Accuracy, 6);
        Assert.Equal(Now.AddDays(-1), stack.LastAnsweredAt);
        var span = spots.Concepts.Single(c => c.Concept == TopicTemplate.SpanMemory);
        Assert.Equal((3, 1, 0), (span.Answers, span.Wrong, span.WeakQuestions));
        var topic = Assert.Single(spots.Topics);
        Assert.Equal(new TopicWeaknessDto(TopicTemplate.Domain, TopicTemplate.Topic, 7, 3, WeaknessRanker.Accuracy(7, 3), Now.AddDays(-1), 1), topic);
    }

    [Fact]
    [Trait("Requirement", "FR-008")]
    public async Task Concepts_are_ranked_weakest_first()
    {
        var spots = await GetAsync();

        Assert.Equal([TopicTemplate.StackVsHeap, TopicTemplate.SpanMemory], spots.Concepts.Select(c => c.Concept));
    }

    [Fact]
    [Trait("Requirement", "FR-011")]
    public async Task Each_concept_carries_the_same_readiness_as_the_readiness_endpoint()
    {
        var spots = await GetAsync();
        await using var db = fixture.CreateContext();
        var readiness = await new ReadinessQueries(db).GetAsync();

        var expected = readiness.Domains.Single().Topics.Single().Concepts.Single(c => c.Concept == TopicTemplate.StackVsHeap);
        Assert.Equal(expected, spots.Concepts.Single(c => c.Concept == TopicTemplate.StackVsHeap).Readiness);
    }

    [Fact]
    [Trait("Requirement", "FR-007")]
    public async Task Deprecated_questions_do_not_count()
    {
        await using (var db = fixture.CreateContext())
        {
            await db.Database.ExecuteSqlAsync($"UPDATE questions SET deprecated_at = {Now} WHERE id = {TopicTemplate.Mcq2}");
        }

        var stack = (await GetAsync()).Concepts.Single(c => c.Concept == TopicTemplate.StackVsHeap);

        Assert.Equal((3, 2), (stack.Answers, stack.Wrong));
    }

    [Fact]
    [Trait("Requirement", "FR-012")]
    public async Task Only_the_local_learners_answers_count()
    {
        var other = Guid.Parse("33333333-3333-3333-3333-333333333333");
        await using (var db = fixture.CreateContext())
        {
            db.Users.Add(new Learner { Id = other, DisplayName = "Someone else", CreatedAt = Now });
            await db.SaveChangesAsync();
        }

        for (var i = 1; i <= 3; i++)
        {
            await AttemptAsync(TopicTemplate.SpanMcq2, -i, false, other);
        }

        var span = (await GetAsync()).Concepts.Single(c => c.Concept == TopicTemplate.SpanMemory);

        Assert.Equal((3, 1, 0), (span.Answers, span.Wrong, span.WeakQuestions));
    }

    [Fact]
    [Trait("Requirement", "FR-009")]
    public async Task Topic_weak_count_includes_concepts_without_answers_in_the_window()
    {
        await using (var db = fixture.CreateContext())
        {
            await db.Database.ExecuteSqlAsync($"DELETE FROM attempts WHERE question_id = {TopicTemplate.SpanMcq1}");
        }

        await AttemptAsync(TopicTemplate.SpanMcq2, -40, false);

        var topic = Assert.Single((await GetAsync()).Topics);

        Assert.Equal((4, 2), (topic.Answers, topic.WeakQuestions));
    }

    [Fact]
    [Trait("Requirement", "FR-012")]
    public async Task Another_learners_later_right_answer_does_not_clear_a_miss()
    {
        var other = Guid.Parse("44444444-4444-4444-4444-444444444444");
        await using (var db = fixture.CreateContext())
        {
            db.Users.Add(new Learner { Id = other, DisplayName = "Someone else", CreatedAt = Now });
            await db.SaveChangesAsync();
        }

        await AttemptAsync(TopicTemplate.Mcq1, 0, true, other);

        Assert.Equal(1, (await GetAsync()).Concepts.Single(c => c.Concept == TopicTemplate.StackVsHeap).WeakQuestions);
    }

    [Fact]
    [Trait("Requirement", "FR-007")]
    public async Task The_window_is_rounded_up_to_a_whole_minute_so_older_answers_never_count()
    {
        await using var db = fixture.CreateContext();

        var spots = await new WeakSpotsQueries(db, new FakeTimeProvider(Now.AddSeconds(37.5))).GetAsync();

        Assert.Equal(Now.AddMinutes(1).AddDays(-WeaknessRule.WindowDays), spots.Since);
        // span-memory's answer from exactly 30 days before 12:00 is now older than the window.
        Assert.DoesNotContain(spots.Concepts, c => c.Concept == TopicTemplate.SpanMemory);
    }

    private async Task<WeakSpotsDto> GetAsync()
    {
        await using var db = fixture.CreateContext();
        return await new WeakSpotsQueries(db, new FakeTimeProvider(Now)).GetAsync();
    }

    private async Task AttemptAsync(string questionId, int days, bool correct, Guid? userId = null)
    {
        await using var db = fixture.CreateContext();
        var at = Now.AddDays(days);
        var answer = JsonSerializer.Serialize(new { selectedIndex = correct ? 1 : 0, selectedText = "Option" });
        await db.Database.ExecuteSqlAsync(
            $"""
             INSERT INTO attempts (id, user_id, item_type, question_id, kata_id, session_id, answered_at, answer, is_correct, score, grading, created_at)
             VALUES ({Guid.NewGuid()}, {userId ?? LocalLearner.Id}, 'question', {questionId}, NULL, NULL, {at}, {answer}::jsonb, {correct}, NULL, NULL, {at})
             """);
    }
}
