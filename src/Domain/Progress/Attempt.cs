namespace Spine.Domain.Progress;

/// <summary>One answer of a learner to one item at a point in time; optionally part of a session.</summary>
public sealed class Attempt
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public ItemType ItemType { get; set; }
    public string? QuestionId { get; set; }
    public string? KataId { get; set; }
    /// <summary>Database-generated: COALESCE(question_id, kata_id).</summary>
    public string ItemId { get; private set; } = null!;
    public Guid? SessionId { get; set; }
    public DateTimeOffset AnsweredAt { get; set; }
    /// <summary>The given answer as JSON (selected option, free text, code).</summary>
    public required string Answer { get; set; }
    public bool? IsCorrect { get; set; }
    /// <summary>0..1 for graded open answers and katas.</summary>
    public double? Score { get; set; }
    /// <summary>Grading details as JSON (feature 004 structured output).</summary>
    public string? Grading { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
