using Spine.Domain.Content;

namespace Spine.Application.Content.Plan;

public sealed class PlanQuestion
{
    public required string Id { get; init; }
    public QuestionType Type { get; init; }
    public Level Level { get; init; }
    public bool Reviewed { get; init; }
    public string[] Tags { get; init; } = [];
    public int SortOrder { get; init; }
    public required string Hash { get; init; }
    public Dictionary<Language, PlanQuestionTranslation> Translations { get; } = [];
}
