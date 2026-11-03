using Spine.Application.Review;

namespace Spine.Application.Tests.Review;

/// <summary>User story 2 (the 14-day chart), kept apart from the streak tests so each cycle's tests build on its own red commit.</summary>
public sealed class ActivityDailyCountsTests
{
    private static readonly DateOnly Today = new(2026, 11, 3);

    private static DateOnly[] DaysAgo(params int[] days) => days.Select(d => Today.AddDays(-d)).ToArray();

    private static ActivityDto Calculate(params DateOnly[] answers) => ActivityCalculator.Calculate("Europe/Kyiv", Today, answers);

    [Fact]
    [Trait("Requirement", "FR-004")]
    public void Daily_counts_cover_fourteen_days_ending_today_oldest_first_with_zeros()
    {
        var days = ActivityCalculator.DailyCounts(Today, DaysAgo(0, 0, 2, 13, 13, 13));

        days.Count.ShouldBe(14);
        days[0].ShouldBe(new ActivityDayDto(Today.AddDays(-13), 3));
        days[^1].ShouldBe(new ActivityDayDto(Today, 2));
        days[^3].ShouldBe(new ActivityDayDto(Today.AddDays(-2), 1));
        days.Select(d => d.Date).ShouldBe(Enumerable.Range(0, 14).Select(i => Today.AddDays(i - 13)));
        days.Sum(d => d.Answers).ShouldBe(6);
    }

    [Fact]
    [Trait("Requirement", "FR-004")]
    public void Older_answers_leave_the_chart_but_keep_the_longest_streak()
    {
        var answers = DaysAgo(14, 15, 16, 17, 18, 19, 0);

        var activity = Calculate(answers);

        activity.Days.Sum(d => d.Answers).ShouldBe(1);
        activity.LongestStreak.ShouldBe(6);
    }

    [Fact]
    [Trait("Requirement", "FR-004")]
    public void Calculate_serves_the_daily_counts_and_ignores_days_after_today()
    {
        var answers = DaysAgo(0, 1, 1, 5);

        var activity = Calculate([.. answers, Today.AddDays(1)]);

        activity.Days.ShouldBe(ActivityCalculator.DailyCounts(Today, answers));
    }
}
