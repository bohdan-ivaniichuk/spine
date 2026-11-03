using System.Text.Json;
using Spine.Application.Content;
using Spine.Application.Review;
using Spine.Domain.Progress;
using Spine.Domain.Scheduling;
using Spine.Infrastructure.Content;
using Spine.Infrastructure.Persistence;
using Spine.Infrastructure.Review;
using Spine.Infrastructure.Tests.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace Spine.Infrastructure.Tests.Review;

/// <summary>
/// Activity against a real PostgreSQL with a pinned clock. In November Europe/Kyiv is UTC+2: 21:30Z and 22:30Z on
/// 2 November are 23:30 on the 2nd and 00:30 on the 3rd in Kyiv, but the same UTC day.
/// </summary>
[Collection(PostgresCollection.Name)]
[Trait("Category", "Integration")]
public sealed class ActivityQueriesTests(PostgresFixture fixture) : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 11, 3, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset LateEvening = new(2026, 11, 2, 21, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset AfterMidnight = new(2026, 11, 2, 22, 30, 0, TimeSpan.Zero);
    private static readonly TimeZoneInfo Kyiv = TimeZoneInfo.FindSystemTimeZoneById("Europe/Kyiv");

    private ContentDirBuilder _content = null!;

    public async Task InitializeAsync()
    {
        await fixture.ResetAsync();
        _content = new ContentDirBuilder().WithDefaultContent();
        await using var db = fixture.CreateContext();
        var result = await new ContentImporter(db, NullLogger<ContentImporter>.Instance).ImportAsync(new ImportOptions(_content.Dir));
        result.ExitCode.ShouldBe(ImportExitCode.Success, string.Join("\n", result.Errors));
        await new LearnerSeeder(db).SeedAsync();
    }

    public Task DisposeAsync()
    {
        _content.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    [Trait("Requirement", "FR-001")]
    [Trait("Requirement", "FR-002")]
    public async Task Answers_around_local_midnight_fall_on_consecutive_days_in_the_requested_zone()
    {
        await QuestionAttemptAsync(TopicTemplate.Mcq1, LateEvening, correct: true);
        await QuestionAttemptAsync(TopicTemplate.Mcq2, AfterMidnight, correct: true);

        var activity = await GetAsync(Kyiv);

        activity.TimeZone.ShouldBe("Europe/Kyiv");
        activity.Today.ShouldBe(new DateOnly(2026, 11, 3));
        activity.CurrentStreak.ShouldBe(2);
        activity.ActiveToday.ShouldBeTrue();
    }

    [Fact]
    [Trait("Requirement", "FR-001")]
    [Trait("Requirement", "FR-002")]
    public async Task The_same_answers_are_one_day_in_utc()
    {
        await QuestionAttemptAsync(TopicTemplate.Mcq1, LateEvening, correct: true);
        await QuestionAttemptAsync(TopicTemplate.Mcq2, AfterMidnight, correct: true);

        var activity = await GetAsync(TimeZoneInfo.Utc);

        activity.Today.ShouldBe(new DateOnly(2026, 11, 3));
        activity.CurrentStreak.ShouldBe(1);
        activity.ActiveToday.ShouldBeFalse();
        activity.LongestStreak.ShouldBe(1);
    }

    [Fact]
    [Trait("Requirement", "FR-003")]
    public async Task The_longest_streak_spans_the_whole_history()
    {
        foreach (var daysAgo in new[] { 40, 41, 42, 43 })
        {
            await QuestionAttemptAsync(TopicTemplate.Mcq1, Now.AddDays(-daysAgo), correct: true);
        }

        await QuestionAttemptAsync(TopicTemplate.Mcq1, Now, correct: true);

        var activity = await GetAsync(Kyiv);

        activity.CurrentStreak.ShouldBe(1);
        activity.LongestStreak.ShouldBe(4);
    }

    [Fact]
    [Trait("Requirement", "FR-001")]
    public async Task Each_answer_uses_the_offset_in_force_at_its_time_across_a_dst_change()
    {
        // Kyiv leaves summer time (UTC+3 → UTC+2) on 25 October 2026. 21:30Z on the 24th is 00:30 on the 25th (summer
        // offset); a fixed winter offset would put it on the 24th, and a fixed summer offset would move 21:30Z on the
        // 27th (23:30 local, winter) to the 28th.
        await QuestionAttemptAsync(TopicTemplate.Mcq1, new DateTimeOffset(2026, 10, 24, 21, 30, 0, TimeSpan.Zero), correct: true);
        await QuestionAttemptAsync(TopicTemplate.Mcq2, new DateTimeOffset(2026, 10, 27, 21, 30, 0, TimeSpan.Zero), correct: true);

        var activity = await GetAsync(Kyiv);

        activity.Days.Single(d => d.Date == new DateOnly(2026, 10, 25)).Answers.ShouldBe(1);
        activity.Days.Single(d => d.Date == new DateOnly(2026, 10, 27)).Answers.ShouldBe(1);
        activity.Days.Single(d => d.Date == new DateOnly(2026, 10, 24)).Answers.ShouldBe(0);
        activity.Days.Single(d => d.Date == new DateOnly(2026, 10, 28)).Answers.ShouldBe(0);
    }

    [Fact]
    [Trait("Requirement", "FR-004")]
    public async Task Days_hold_fourteen_local_days_with_the_seeded_counts()
    {
        await QuestionAttemptAsync(TopicTemplate.Mcq1, LateEvening, correct: true);
        await QuestionAttemptAsync(TopicTemplate.Mcq2, AfterMidnight, correct: false);
        await QuestionAttemptAsync(TopicTemplate.SpanMcq1, AfterMidnight.AddMinutes(5), correct: true);
        await QuestionAttemptAsync(TopicTemplate.Mcq1, Now.AddDays(-20), correct: true);

        var activity = await GetAsync(Kyiv);

        activity.Days.Count.ShouldBe(14);
        activity.Days[0].Date.ShouldBe(new DateOnly(2026, 10, 21));
        activity.Days[^1].ShouldBe(new ActivityDayDto(new DateOnly(2026, 11, 3), 2));
        activity.Days[^2].ShouldBe(new ActivityDayDto(new DateOnly(2026, 11, 2), 1));
        activity.Days.Sum(d => d.Answers).ShouldBe(3, "the answer 20 days ago is outside the window");
    }

    [Fact]
    [Trait("Requirement", "FR-005")]
    public async Task Wrong_weak_session_and_deprecated_answers_count_and_katas_do_not()
    {
        // A regular session answered wrong yesterday, then a weak drill of the same question today.
        var clock = new FakeTimeProvider(Now.AddDays(-1));
        await using (var db = fixture.CreateContext())
        {
            var sessions = new ReviewSessions(db, new Sm2Algorithm(), clock, Options.Create(new ReviewOptions()));
            var regular = await sessions.StartAsync(ReviewScope.All, 1, 1);
            await sessions.AnswerAsync(regular.Id, regular.Questions[0].Id, 0);
            await sessions.FinishAsync(regular.Id);
            clock.SetUtcNow(Now);
            var weak = await sessions.StartWeakAsync(ReviewScope.All, 1);
            weak.Mode.ShouldBe("weak");
            await sessions.AnswerAsync(weak.Id, weak.Questions[0].Id, 0);
        }

        // Two days ago: a question that has been deprecated since, and a kata that never counts.
        await QuestionAttemptAsync(TopicTemplate.SpanMcq1, Now.AddDays(-2), correct: false);
        await using (var db = fixture.CreateContext())
        {
            await db.Database.ExecuteSqlAsync($"UPDATE questions SET deprecated_at = {Now} WHERE id = {TopicTemplate.SpanMcq1}");
        }

        await KataAttemptAsync(Now.AddDays(-3));

        var activity = await GetAsync(Kyiv);

        activity.CurrentStreak.ShouldBe(3, "today (weak drill), yesterday (wrong) and two days ago (deprecated) count");
        activity.LongestStreak.ShouldBe(3, "the kata three days ago does not extend the run");
    }

    private async Task<ActivityDto> GetAsync(TimeZoneInfo zone)
    {
        await using var db = fixture.CreateContext();
        return await new ActivityQueries(db, new FakeTimeProvider(Now)).GetAsync(zone);
    }

    private async Task QuestionAttemptAsync(string questionId, DateTimeOffset at, bool correct)
    {
        await using var db = fixture.CreateContext();
        var answer = JsonSerializer.Serialize(new { selectedIndex = correct ? 1 : 0, selectedText = "Option" });
        await db.Database.ExecuteSqlAsync(
            $"""
             INSERT INTO attempts (id, user_id, item_type, question_id, kata_id, session_id, answered_at, answer, is_correct, score, grading, created_at)
             VALUES ({Guid.NewGuid()}, {LocalLearner.Id}, 'question', {questionId}, NULL, NULL, {at}, {answer}::jsonb, {correct}, NULL, NULL, {at})
             """);
    }

    private async Task KataAttemptAsync(DateTimeOffset at)
    {
        await using var db = fixture.CreateContext();
        var answer = JsonSerializer.Serialize(new { code = "struct Point {}" });
        await db.Database.ExecuteSqlAsync(
            $"""
             INSERT INTO attempts (id, user_id, item_type, question_id, kata_id, session_id, answered_at, answer, is_correct, score, grading, created_at)
             VALUES ({Guid.NewGuid()}, {LocalLearner.Id}, 'kata', NULL, {TopicTemplate.KataId}, NULL, {at}, {answer}::jsonb, NULL, 0.5, NULL, {at})
             """);
    }
}
