namespace Spine.Domain.Progress;

/// <summary>
/// When a question counts as learned: its review state has an interval of at least <see cref="MinIntervalDays"/>
/// days (three correct answers in a row since the last miss under SM-2). Ease is deliberately not used: SM-2 never
/// raises it, so one miss would block readiness forever. Algorithm-neutral, so FSRS intervals work the same way.
/// </summary>
public static class ReadinessRule
{
    public const int MinIntervalDays = 7;

    public static bool IsReady(int intervalDays) => intervalDays >= MinIntervalDays;
}
