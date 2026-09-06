namespace InterviewPrep.Domain.Progress;

/// <summary>
/// Spaced-repetition state of one learner for one item (question or kata). At most one per (learner, item).
/// Stores the SM-2 fields plus an opaque <see cref="AlgorithmState"/> so other algorithms (FSRS) can coexist.
/// </summary>
public sealed class ReviewState
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public ItemType ItemType { get; set; }
    public string? QuestionId { get; set; }
    public string? KataId { get; set; }
    /// <summary>Database-generated: COALESCE(question_id, kata_id).</summary>
    public string ItemId { get; private set; } = null!;
    public string Algorithm { get; set; } = "sm2";
    public int IntervalDays { get; set; }
    public double Ease { get; set; } = 2.5;
    public int Repetitions { get; set; }
    public DateTimeOffset? LastReviewedAt { get; set; }
    public DateTimeOffset DueAt { get; set; }
    /// <summary>Algorithm-specific state as JSON; null until the algorithm stores something.</summary>
    public string? AlgorithmState { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
