namespace Spine.Domain.Content;

/// <summary>A code kata keyed by its stable content id: &lt;domain&gt;.&lt;topic&gt;.&lt;concept&gt;.kata.&lt;n&gt;.</summary>
public sealed class Kata
{
    public required string Id { get; set; }
    public int ConceptId { get; set; }
    public required string Title { get; set; }
    public KataLanguage Language { get; set; }
    public bool Reviewed { get; set; }
    public int TimeLimitMinutes { get; set; }
    public int SortOrder { get; set; }
    public required string ContentHash { get; set; }
    public DateTimeOffset? DeprecatedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Concept? Concept { get; set; }
    public ICollection<KataTranslation> Translations { get; } = new List<KataTranslation>();
}
