namespace Spine.Application.Content.Plan;

public sealed class PlanDomain
{
    public required string Slug { get; init; }
    public required string Title { get; init; }
    public string? TitleUk { get; init; }
    public int SortOrder { get; init; }
    public required string Hash { get; init; }
    public List<PlanTopic> Topics { get; } = [];
}
