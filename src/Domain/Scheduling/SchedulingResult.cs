namespace InterviewPrep.Domain.Scheduling;

/// <summary>What a scheduling algorithm decided for one answer; applied to the review state as-is.</summary>
public sealed record SchedulingResult(
    int IntervalDays,
    double Ease,
    int Repetitions,
    DateTimeOffset LastReviewedAt,
    DateTimeOffset DueAt,
    string AlgorithmState);
