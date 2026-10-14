namespace InterviewPrep.Domain.Progress;

/// <summary>
/// A review sitting: scope (all / one domain / one topic), the ordered membership frozen at start, the limits used,
/// and summary counts filled at finish.
/// </summary>
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
    /// <summary>Ordered question ids chosen at start (due first, then new); never changes afterwards.</summary>
    public string[] QuestionIds { get; set; } = [];
    public int TotalLimit { get; set; }
    public int NewLimit { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
