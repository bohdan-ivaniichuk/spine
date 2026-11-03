namespace Spine.Application.Content.Plan;

public sealed class PlanTopic
{
    public required string Slug { get; init; }
    public required string Title { get; init; }
    public string? TitleUk { get; init; }
    public int SortOrder { get; init; }
    public required string Hash { get; init; }
    /// <summary>Relative path of the topic file, or null when the manifest lists a topic that has no file yet.</summary>
    public string? File { get; init; }
    public List<PlanConcept> Concepts { get; } = [];
}
