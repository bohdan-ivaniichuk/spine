using Spine.Domain.Content;

namespace Spine.Application.Knowledge;

/// <summary>Read side of the content graph. Never returns learner-specific data.</summary>
public interface IKnowledgeQueries
{
    Task<KnowledgeTreeDto> GetTreeAsync(Language language, bool includeDeprecated, CancellationToken ct = default);

    /// <summary>Null when the path is unknown, or when the concept or an ancestor is deprecated and not included.</summary>
    Task<ConceptPageDto?> GetConceptAsync(string domainSlug, string topicSlug, string conceptSlug, Language language, bool includeDeprecated, CancellationToken ct = default);
}
