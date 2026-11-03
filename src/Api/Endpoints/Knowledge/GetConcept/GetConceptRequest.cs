using Spine.Api.Endpoints.Knowledge.Shared;

namespace Spine.Api.Endpoints.Knowledge.GetConcept;

public sealed class GetConceptRequest : KnowledgeQuery
{
    public string Domain { get; set; } = string.Empty;

    public string Topic { get; set; } = string.Empty;

    public string Concept { get; set; } = string.Empty;
}
