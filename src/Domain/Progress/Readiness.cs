namespace Spine.Domain.Progress;

/// <summary>How far the local learner is with one concept, judged over its schedulable questions.</summary>
public enum ConceptReadiness
{
    NotStarted,
    InProgress,
    Ready,
}

/// <summary>
/// When a question counts as learned: its review state has an interval of at least <see cref="MinIntervalDays"/>
/// days (three correct answers in a row since the last miss under SM-2). Ease is deliberately not used: SM-2 never
/// raises it, so one miss would block readiness forever. Algorithm-neutral, so FSRS intervals work the same way.
/// </summary>
public static class ReadinessRule
{
    public const int MinIntervalDays = 7;

    public static bool IsReady(int intervalDays) => intervalDays >= MinIntervalDays;

    /// <summary>Wire value: <c>not-started</c>, <c>in-progress</c>, <c>ready</c>.</summary>
    public static string ToWireValue(this ConceptReadiness readiness) => readiness switch
    {
        ConceptReadiness.NotStarted => "not-started",
        ConceptReadiness.InProgress => "in-progress",
        ConceptReadiness.Ready => "ready",
        _ => throw new ArgumentOutOfRangeException(nameof(readiness), readiness, null),
    };
}
