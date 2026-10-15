using Spine.Domain.Progress;

namespace Spine.Application.Review;

/// <summary>What a session or overview covers: everything, one domain, one topic of a domain, or one concept of a topic.</summary>
public sealed record ReviewScope(SessionScopeType Type, string? Domain, string? Topic, string? Concept = null)
{
    public static readonly ReviewScope All = new(SessionScopeType.All, null, null);

    public static ReviewScope ForDomain(string domainSlug) => new(SessionScopeType.Domain, domainSlug, null);

    public static ReviewScope ForTopic(string domainSlug, string topicSlug) => new(SessionScopeType.Topic, domainSlug, topicSlug);

    public static ReviewScope ForConcept(string domainSlug, string topicSlug, string conceptSlug) =>
        new(SessionScopeType.Concept, domainSlug, topicSlug, conceptSlug);

    /// <summary>Same rule as the <c>ck_sessions_scope</c> check constraint.</summary>
    public bool IsValid => Type switch
    {
        SessionScopeType.All => Domain is null && Topic is null && Concept is null,
        SessionScopeType.Domain => !string.IsNullOrWhiteSpace(Domain) && Topic is null && Concept is null,
        SessionScopeType.Topic => !string.IsNullOrWhiteSpace(Domain) && !string.IsNullOrWhiteSpace(Topic) && Concept is null,
        SessionScopeType.Concept => !string.IsNullOrWhiteSpace(Domain) && !string.IsNullOrWhiteSpace(Topic) && !string.IsNullOrWhiteSpace(Concept),
        _ => false,
    };

    public void Validate()
    {
        if (!IsValid)
        {
            throw new ArgumentException($"Scope '{Type.ToDbValue()}' requires " + Type switch
            {
                SessionScopeType.All => "no domain, topic or concept.",
                SessionScopeType.Domain => "a domain and no topic or concept.",
                SessionScopeType.Topic => "a domain and a topic and no concept.",
                SessionScopeType.Concept => "a domain, a topic and a concept.",
                _ => "a known scope type.",
            });
        }
    }
}
