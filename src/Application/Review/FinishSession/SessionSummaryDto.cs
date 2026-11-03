using Spine.Application.Review.Shared;

namespace Spine.Application.Review.FinishSession;

public sealed record SessionSummaryDto(
    Guid SessionId,
    ScopeDto Scope,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    int QuestionCount,
    int Answered,
    int Correct,
    double? Accuracy,
    IReadOnlyList<WeakConceptDto> WeakConcepts,
    int DueTomorrow,
    string Mode = "regular");
