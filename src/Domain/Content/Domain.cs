namespace Spine.Domain.Content;

/// <summary>Top-level knowledge area (e.g. dotnet, angular). Ordered and titled by the manifest.</summary>
public sealed class Domain
{
    public int Id { get; set; }
    public required string Slug { get; set; }
    public required string Title { get; set; }

    /// <summary>Ukrainian title from the manifest's <c>titles.uk</c>; null when the manifest has none (English is served).</summary>
    public string? TitleUk { get; set; }

    public int SortOrder { get; set; }
    public required string ContentHash { get; set; }
    public DateTimeOffset? DeprecatedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<Topic> Topics { get; } = new List<Topic>();
}
