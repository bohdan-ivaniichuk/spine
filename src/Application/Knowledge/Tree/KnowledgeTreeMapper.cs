using Spine.Domain.Content;

namespace Spine.Application.Knowledge.Tree;

/// <summary>Builds the knowledge tree DTO from the three row lists, titles in <c>language</c> with English fallback.</summary>
public sealed class KnowledgeTreeMapper(Language language)
{
    public KnowledgeTreeDto ToTree(
        IReadOnlyList<TreeDomainRow> domains, IReadOnlyList<TreeTopicRow> topics, IReadOnlyList<TreeConceptRow> concepts)
    {
        _ = (language, domains, topics, concepts);
        throw new NotImplementedException();
    }
}
