namespace Spine.Domain.Content;

public sealed class KataTranslation
{
    public required string KataId { get; set; }
    public Language Language { get; set; }
    public required string Task { get; set; }
    public required IReadOnlyList<string> Rubric { get; set; }
    public required string ReferenceSolution { get; set; }
    public required string ContentHash { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Kata? Kata { get; set; }
}
