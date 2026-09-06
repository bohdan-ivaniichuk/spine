namespace InterviewPrep.Domain.Content;

public sealed class ConceptTranslation
{
    public int ConceptId { get; set; }
    public Language Language { get; set; }
    public required string Title { get; set; }
    /// <summary>Markdown note, 300–900 words in English.</summary>
    public required string Note { get; set; }
    public required string ContentHash { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Concept? Concept { get; set; }
}
