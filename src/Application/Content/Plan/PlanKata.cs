using Spine.Domain.Content;

namespace Spine.Application.Content.Plan;

public sealed class PlanKata
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public KataLanguage Language { get; init; }
    public bool Reviewed { get; init; }
    public int TimeLimitMinutes { get; init; }
    public int SortOrder { get; init; }
    public required string Hash { get; init; }
    public Dictionary<Language, PlanKataTranslation> Translations { get; } = [];
}
