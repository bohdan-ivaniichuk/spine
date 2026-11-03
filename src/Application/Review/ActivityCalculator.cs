namespace Spine.Application.Review;

/// <summary>
/// Pure streak and daily-count math over the local dates of answers (one date per answer). An active day has at least
/// one answer; days after <c>today</c> (clock skew) are ignored.
/// </summary>
public static class ActivityCalculator
{
    /// <summary>Days in the activity chart, today included.</summary>
    public const int WindowDays = 14;

    public static ActivityDto Calculate(string timeZone, DateOnly today, IEnumerable<DateOnly> answerDays)
    {
        ArgumentNullException.ThrowIfNull(answerDays);
        var active = answerDays.Where(d => d <= today).ToHashSet();

        var activeToday = active.Contains(today);
        var current = RunEndingAt(active, activeToday ? today : today.AddDays(-1));

        return new ActivityDto(timeZone, today, current, activeToday, LongestRun(active), []);
    }

    /// <summary>Consecutive active days ending at <paramref name="last"/>; 0 when that day is inactive.</summary>
    private static int RunEndingAt(HashSet<DateOnly> active, DateOnly last)
    {
        var run = 0;
        for (var day = last; active.Contains(day); day = day.AddDays(-1))
        {
            run++;
        }

        return run;
    }

    private static int LongestRun(HashSet<DateOnly> active)
    {
        var longest = 0;
        foreach (var day in active)
        {
            // Only a run's first day starts a count, so every run is walked once.
            if (!active.Contains(day.AddDays(-1)))
            {
                var run = 1;
                while (active.Contains(day.AddDays(run)))
                {
                    run++;
                }

                longest = Math.Max(longest, run);
            }
        }

        return longest;
    }
}
