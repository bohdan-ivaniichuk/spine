namespace InterviewPrep.Domain.Progress;

/// <summary>A review sitting: scope (all / one domain / one topic) and summary counts filled at finish.</summary>
public sealed class Session
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public SessionScopeType ScopeType { get; set; }
    public string? ScopeDomainSlug { get; set; }
    public string? ScopeTopicSlug { get; set; }
    public int? AnsweredCount { get; set; }
    public int? CorrectCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
