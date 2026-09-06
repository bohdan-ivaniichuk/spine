namespace InterviewPrep.Domain.Content;

public sealed class Topic
{
    public int Id { get; set; }
    public int DomainId { get; set; }
    public required string Slug { get; set; }
    public required string Title { get; set; }
    public int SortOrder { get; set; }
    public required string ContentHash { get; set; }
    public DateTimeOffset? DeprecatedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Domain? Domain { get; set; }
    public ICollection<Concept> Concepts { get; } = new List<Concept>();
}
