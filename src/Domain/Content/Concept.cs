namespace InterviewPrep.Domain.Content;

public sealed class Concept
{
    public int Id { get; set; }
    public int TopicId { get; set; }
    public required string Slug { get; set; }
    public Level Level { get; set; }
    /// <summary>Derived at import: every non-deprecated question and kata under the concept is reviewed.</summary>
    public bool Reviewed { get; set; }
    public int SortOrder { get; set; }
    public required string ContentHash { get; set; }
    public DateTimeOffset? DeprecatedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Topic? Topic { get; set; }
    public ICollection<ConceptTranslation> Translations { get; } = new List<ConceptTranslation>();
    public ICollection<Question> Questions { get; } = new List<Question>();
    public ICollection<Kata> Katas { get; } = new List<Kata>();
}
