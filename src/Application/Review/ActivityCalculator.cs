namespace Spine.Application.Review;

/// <summary>
/// Pure streak and daily-count math over the local dates of answers (one date per answer). An active day has at least
/// one answer; days after <c>today</c> (clock skew) are ignored.
/// </summary>
public static class ActivityCalculator
{
    /// <summary>Days in the activity chart, today included.</summary>
    public const int WindowDays = 14;

    public static ActivityDto Calculate(string timeZone, DateOnly today, IEnumerable<DateOnly> answerDays) =>
        throw new NotImplementedException();
}
