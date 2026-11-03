using Spine.Domain.Content;

namespace Spine.Application.Content.Plan;

public sealed class PlanConcept
{
    public required string Slug { get; init; }
    public Level Level { get; init; }
    public bool Reviewed { get; init; }
    public int SortOrder { get; init; }
    public required string Hash { get; init; }
    public Dictionary<Language, PlanConceptTranslation> Translations { get; } = [];
    public List<PlanQuestion> Questions { get; } = [];
    public List<PlanKata> Katas { get; } = [];
}
