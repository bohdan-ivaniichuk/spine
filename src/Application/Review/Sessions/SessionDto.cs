using Spine.Application.Review.Shared;

namespace Spine.Application.Review.Sessions;

public sealed record SessionDto(
    Guid Id,
    ScopeDto Scope,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    int TotalLimit,
    int NewLimit,
    IReadOnlyList<SessionQuestionDto> Questions,
    IReadOnlyList<SessionAnswerDto> Answers,
    IReadOnlyList<string> SkippedQuestionIds,
    string Mode = "regular");
