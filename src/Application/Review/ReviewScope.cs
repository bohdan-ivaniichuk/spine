using InterviewPrep.Domain.Progress;

namespace InterviewPrep.Application.Review;

/// <summary>What a session or overview covers: everything, one domain, or one topic of a domain.</summary>
public sealed record ReviewScope(SessionScopeType Type, string? Domain, string? Topic)
{
    public static readonly ReviewScope All = new(SessionScopeType.All, null, null);

    public static ReviewScope ForDomain(string domainSlug) => new(SessionScopeType.Domain, domainSlug, null);

    public static ReviewScope ForTopic(string domainSlug, string topicSlug) => new(SessionScopeType.Topic, domainSlug, topicSlug);

    /// <summary>Same rule as the <c>ck_sessions_scope</c> check constraint.</summary>
    public bool IsValid => Type switch
    {
        SessionScopeType.All => Domain is null && Topic is null,
        SessionScopeType.Domain => !string.IsNullOrWhiteSpace(Domain) && Topic is null,
        SessionScopeType.Topic => !string.IsNullOrWhiteSpace(Domain) && !string.IsNullOrWhiteSpace(Topic),
        _ => false,
    };

    public void Validate()
    {
        if (!IsValid)
        {
            throw new ArgumentException($"Scope '{Type.ToDbValue()}' requires " + Type switch
            {
                SessionScopeType.All => "neither a domain nor a topic.",
                SessionScopeType.Domain => "a domain and no topic.",
                SessionScopeType.Topic => "both a domain and a topic.",
                _ => "a known scope type.",
            });
        }
    }
}
