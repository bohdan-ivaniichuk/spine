using FastEndpoints;
using Spine.Api.Endpoints.Shared;

namespace Spine.Api.Endpoints.Knowledge.Shared;

/// <summary>Shared query parameters of the knowledge read endpoints.</summary>
public abstract class KnowledgeQuery : LanguageQuery
{
    /// <summary>Include deprecated domains/topics/concepts (default false).</summary>
    [QueryParam]
    public bool IncludeDeprecated { get; set; }
}
