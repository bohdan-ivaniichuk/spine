using Spine.Domain.Content;

namespace Spine.Application.Content.Plan;

public sealed record PlanQuestionTranslation(
    string Prompt,
    string Explanation,
    IReadOnlyList<McqOption>? Options,
    IReadOnlyList<string>? Rubric,
    string Hash);
