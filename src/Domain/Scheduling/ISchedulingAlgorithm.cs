namespace Spine.Domain.Scheduling;

/// <summary>
/// Spaced-repetition scheduling behind an interface (constitution Principle V). Implementations are pure:
/// the current time is passed in and no I/O happens.
/// </summary>
public interface ISchedulingAlgorithm
{
    /// <summary>Stored in <c>review_states.algorithm</c> (e.g. "sm2").</summary>
    string Name { get; }

    SchedulingResult Next(ReviewSnapshot current, ReviewGrade grade, DateTimeOffset now);
}
