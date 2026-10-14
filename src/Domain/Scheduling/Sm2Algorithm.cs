using System.Text.Json;

namespace InterviewPrep.Domain.Scheduling;

/// <summary>
/// SM-2 with two grades. Good: repetitions + 1, interval 1 → 6 → previous × ease (whole days, rounded away from
/// zero, minimum 1), ease unchanged. Again: repetitions 0, interval 1, ease − 0.2 with a floor of 1.3.
/// Deliberate deviation from canonical SM-2: a wrong answer subtracts a flat 0.2 (the spec fixes it) rather than the
/// quality-derived 0.54 for q = 1.
/// </summary>
public sealed class Sm2Algorithm : ISchedulingAlgorithm
{
    public const string AlgorithmName = "sm2";
    public const double EaseFloor = 1.3;
    public const double WrongEasePenalty = 0.2;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string Name => AlgorithmName;

    public SchedulingResult Next(ReviewSnapshot current, ReviewGrade grade, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(current);

        int repetitions;
        int interval;
        double ease;
        switch (grade)
        {
            case ReviewGrade.Good:
                repetitions = current.Repetitions + 1;
                interval = repetitions switch
                {
                    1 => 1,
                    2 => 6,
                    _ => Math.Max(1, (int)Math.Round(current.IntervalDays * current.Ease, MidpointRounding.AwayFromZero)),
                };
                ease = current.Ease;
                break;
            case ReviewGrade.Again:
                repetitions = 0;
                interval = 1;
                ease = Math.Max(EaseFloor, current.Ease - WrongEasePenalty);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(grade), grade, null);
        }

        var state = JsonSerializer.Serialize(new Sm2State((int)grade), JsonOptions);
        return new SchedulingResult(interval, ease, repetitions, now, now.AddDays(interval), state);
    }

    /// <summary>Serialized into <c>review_states.algorithm_state</c> as <c>{"lastQuality":n}</c>.</summary>
    private sealed record Sm2State(int LastQuality);
}
