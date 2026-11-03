using Riok.Mapperly.Abstractions;

namespace Spine.Application.Review.GetReviewCoverage;

/// <summary>Builds the coverage DTO: every topic in order, zero counts where nothing is eligible.</summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public sealed partial class ReviewCoverageMapper
{
    private static readonly TopicCoverageCounts None = new(0, 0, 0, 0);

    public ReviewCoverageDto ToCoverage(
        IReadOnlyList<TopicCoverageRow> topics, IReadOnlyDictionary<int, TopicCoverageCounts> counts) =>
        new(topics.Select(t => ToTopic(t, counts.GetValueOrDefault(t.Id, None))).ToList());

    private TopicCoverageDto ToTopic(TopicCoverageRow topic, TopicCoverageCounts counts) =>
        ToTopic(topic, counts.Eligible, counts.Seen, counts.Due);

    [MapProperty(nameof(TopicCoverageRow.DomainSlug), nameof(TopicCoverageDto.Domain))]
    [MapProperty(nameof(TopicCoverageRow.Slug), nameof(TopicCoverageDto.Topic))]
    private partial TopicCoverageDto ToTopic(TopicCoverageRow topic, int eligible, int seen, int due);
}
