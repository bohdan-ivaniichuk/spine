namespace Spine.Application.Review;

// Shapes are the Angular contract (specs/002-review-session/contracts/review-api.md, extended by
// specs/003-atlas-redesign-i18n/contracts/review-api-i18n.md); property names serialize camelCase.

public sealed record ScopeDto(string Type, string? Domain, string? Topic, string? DomainTitle, string? TopicTitle);

public sealed record RefDto(string Slug, string Title);

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

public sealed record SessionAnswerDto(string QuestionId, int SelectedIndex, bool IsCorrect);

public sealed record SessionDto(
    Guid Id,
    ScopeDto Scope,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    int TotalLimit,
    int NewLimit,
    IReadOnlyList<SessionQuestionDto> Questions,
    IReadOnlyList<SessionAnswerDto> Answers,
    IReadOnlyList<string> SkippedQuestionIds);

public sealed record AnswerFeedbackDto(
    string QuestionId,
    int SelectedIndex,
    bool IsCorrect,
    int CorrectIndex,
    string Explanation,
    DateTimeOffset NextDueAt,
    int IntervalDays,
    string Language,
    bool Fallback);

public sealed record SessionDefaultsDto(int TotalLimit, int NewLimit);

public sealed record SessionLimitsDto(int Min, int Max);

public sealed record ReviewOverviewDto(
    ScopeDto Scope,
    int DueCount,
    int NewCount,
    int TotalEligible,
    DateTimeOffset? NextDueAt,
    SessionDefaultsDto Defaults,
    SessionLimitsDto Limits);

public sealed record WeakConceptDto(string Domain, string Topic, string Concept, string Title, int Wrong, int Total);

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
    int DueTomorrow);

/// <summary>Per-topic progress of the local learner. Due &lt;= seen &lt;= eligible.</summary>
public sealed record TopicCoverageDto(string Domain, string Topic, int Eligible, int Seen, int Due);

public sealed record ReviewCoverageDto(IReadOnlyList<TopicCoverageDto> Topics);
