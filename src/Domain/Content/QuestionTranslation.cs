namespace InterviewPrep.Domain.Content;

public sealed class QuestionTranslation
{
    public required string QuestionId { get; set; }
    public Language Language { get; set; }
    public required string Prompt { get; set; }
    public required string Explanation { get; set; }
    /// <summary>Exactly four options with exactly one correct; null for open questions.</summary>
    public IReadOnlyList<McqOption>? Options { get; set; }
    /// <summary>Three to five grading criteria; null for multiple-choice questions.</summary>
    public IReadOnlyList<string>? Rubric { get; set; }
    public required string ContentHash { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Question? Question { get; set; }
}
