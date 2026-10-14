namespace Spine.Domain.Scheduling;

/// <summary>Immutable view of a review-state row as the scheduling algorithm sees it.</summary>
public sealed record ReviewSnapshot(
    int IntervalDays,
    double Ease,
    int Repetitions,
    DateTimeOffset? LastReviewedAt,
    DateTimeOffset DueAt,
    string? AlgorithmState)
{
    /// <summary>The state of an item that has never been answered: due now, default ease, no history.</summary>
    public static ReviewSnapshot New(DateTimeOffset now) => new(0, 2.5, 0, null, now, null);
}
