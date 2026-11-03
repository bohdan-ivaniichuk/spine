using Spine.Application.Review.Shared;

namespace Spine.Application.Review.Sessions;

/// <summary>A question as the card shows it. Options are text only; which one is correct never leaves the server.</summary>
public sealed record SessionQuestionDto(
    string Id,
    string Level,
    bool Reviewed,
    bool IsNew,
    string Prompt,
    IReadOnlyList<string> Options,
    RefDto Domain,
    RefDto Topic,
    RefDto Concept,
    string Language,
    bool Fallback);
