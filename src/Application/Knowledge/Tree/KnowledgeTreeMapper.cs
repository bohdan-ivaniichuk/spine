using Riok.Mapperly.Abstractions;
using Spine.Domain.Content;

namespace Spine.Application.Knowledge.Tree;

/// <summary>Builds the knowledge tree DTO from the three row lists, titles in <c>language</c> with English fallback.</summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public sealed partial class KnowledgeTreeMapper(Language language)
{
    /// <summary>Nests topics under domains and concepts under topics, keeping the order of each row list.</summary>
    public KnowledgeTreeDto ToTree(
        IReadOnlyList<TreeDomainRow> domains,
        IReadOnlyList<TreeTopicRow> topics,
        IReadOnlyList<TreeConceptRow> concepts)
    {
        var conceptsByTopic = concepts.ToLookup(c => c.TopicId);
        var topicsByDomain = topics.ToLookup(t => t.DomainId);

        TreeTopicDto Topic(TreeTopicRow topic) =>
            ToTopic(topic, conceptsByTopic[topic.Id].Select(ToConcept).ToList());

        TreeDomainDto Domain(TreeDomainRow domain) =>
            ToDomain(domain, topicsByDomain[domain.Id].Select(Topic).ToList());

        return new KnowledgeTreeDto(language.ToDbValue(), domains.Select(Domain).ToList());
    }

    [MapPropertyFromSource(nameof(TreeDomainDto.Title), Use = nameof(DomainTitle))]
    [MapPropertyFromSource(nameof(TreeDomainDto.TitleFallback), Use = nameof(DomainTitleFallback))]
    private partial TreeDomainDto ToDomain(TreeDomainRow row, IReadOnlyList<TreeTopicDto> topics);

    [MapPropertyFromSource(nameof(TreeTopicDto.Title), Use = nameof(TopicTitle))]
    [MapPropertyFromSource(nameof(TreeTopicDto.TitleFallback), Use = nameof(TopicTitleFallback))]
    private partial TreeTopicDto ToTopic(TreeTopicRow row, IReadOnlyList<TreeConceptDto> concepts);

    [MapPropertyFromSource(nameof(TreeConceptDto.Title), Use = nameof(ConceptTitle))]
    [MapPropertyFromSource(nameof(TreeConceptDto.TitleFallback), Use = nameof(ConceptTitleFallback))]
    private partial TreeConceptDto ToConcept(TreeConceptRow row);

    private static string LevelValue(Level level) => level.ToDbValue();

    private string DomainTitle(TreeDomainRow row) => Pick(row.Title, row.TitleUk).Title;

    private bool DomainTitleFallback(TreeDomainRow row) => Pick(row.Title, row.TitleUk).Fallback;

    private string TopicTitle(TreeTopicRow row) => Pick(row.Title, row.TitleUk).Title;

    private bool TopicTitleFallback(TreeTopicRow row) => Pick(row.Title, row.TitleUk).Fallback;

    private (string Title, bool Fallback) Pick(string en, string? uk) => LocalizedTitle.Pick(language, en, uk);

    private static string ConceptTitle(TreeConceptRow row) => row.TitleRequested ?? row.TitleEn ?? row.Slug;

    private static bool ConceptTitleFallback(TreeConceptRow row) => row.TitleRequested is null;
}
