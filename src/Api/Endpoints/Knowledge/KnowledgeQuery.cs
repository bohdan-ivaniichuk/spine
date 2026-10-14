using FastEndpoints;

namespace Spine.Api.Endpoints.Knowledge;

/// <summary>Shared query parameters of the knowledge read endpoints.</summary>
public abstract class KnowledgeQuery : LanguageQuery
{
    /// <summary>Include deprecated domains/topics/concepts (default false).</summary>
    [QueryParam]
    public bool IncludeDeprecated { get; set; }
}
