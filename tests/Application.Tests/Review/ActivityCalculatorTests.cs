using Spine.Application.Review;

namespace Spine.Application.Tests.Review;

public sealed class ActivityCalculatorTests
{
    private static readonly DateOnly Today = new(2026, 11, 3);

    /// <summary>One answer on each listed day, counted back from <see cref="Today"/> (0 = today, 1 = yesterday).</summary>
    private static DateOnly[] DaysAgo(params int[] days) => days.Select(d => Today.AddDays(-d)).ToArray();

    private static ActivityDto Calculate(params DateOnly[] answers) => ActivityCalculator.Calculate("Europe/Kyiv", Today, answers);

    [Fact]
    [Trait("Requirement", "FR-002")]
    public void A_run_ending_today_is_the_current_streak()
    {
        var activity = Calculate(DaysAgo(0, 1, 2));

        activity.CurrentStreak.ShouldBe(3);
        activity.ActiveToday.ShouldBeTrue();
    }

    [Fact]
    [Trait("Requirement", "FR-002")]
    public void Today_without_answers_keeps_the_run_that_reached_yesterday()
    {
        var activity = Calculate(DaysAgo(1, 2, 3));

        activity.CurrentStreak.ShouldBe(3);
        activity.ActiveToday.ShouldBeFalse();
    }

    [Fact]
    [Trait("Requirement", "FR-002")]
    public void A_last_answer_two_days_ago_breaks_the_streak()
    {
        var activity = Calculate(DaysAgo(2, 3));

        activity.CurrentStreak.ShouldBe(0);
        activity.ActiveToday.ShouldBeFalse();
    }

    [Fact]
    [Trait("Requirement", "FR-003")]
    public void The_longest_streak_comes_from_an_older_run()
    {
        var activity = Calculate(DaysAgo(0, 1, 5, 6, 7, 8, 9));

        activity.CurrentStreak.ShouldBe(2);
        activity.LongestStreak.ShouldBe(5);
    }

    [Fact]
    [Trait("Requirement", "FR-003")]
    public void The_longest_streak_is_never_shorter_than_the_current_one()
    {
        var activity = Calculate(DaysAgo(1, 2, 3, 4));

        activity.LongestStreak.ShouldBe(4);
    }

    [Fact]
    [Trait("Requirement", "FR-002")]
    public void Several_answers_on_one_day_make_one_active_day()
    {
        var activity = Calculate(DaysAgo(0, 0, 0, 1, 1));

        activity.CurrentStreak.ShouldBe(2);
        activity.LongestStreak.ShouldBe(2);
    }

    [Fact]
    [Trait("Requirement", "FR-002")]
    public void Days_after_today_are_ignored()
    {
        // Counted, tomorrow and the day after would form a run of 2 and make today look inactive-but-alive.
        var activity = Calculate([Today.AddDays(1), Today.AddDays(2), .. DaysAgo(1)]);

        activity.CurrentStreak.ShouldBe(1);
        activity.ActiveToday.ShouldBeFalse();
        activity.LongestStreak.ShouldBe(1);
    }

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

    [Fact]
    [Trait("Requirement", "FR-003")]
    public void No_answers_give_empty_streaks_and_echo_the_zone_and_today()
    {
        var activity = Calculate();

        activity.ShouldSatisfyAllConditions(
            () => activity.CurrentStreak.ShouldBe(0),
            () => activity.LongestStreak.ShouldBe(0),
            () => activity.ActiveToday.ShouldBeFalse(),
            () => activity.TimeZone.ShouldBe("Europe/Kyiv"),
            () => activity.Today.ShouldBe(Today));
    }
}
