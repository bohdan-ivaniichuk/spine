using Spine.Domain.Progress;

namespace Spine.Application.Review;

/// <summary>
/// Per concept: how many schedulable questions it has, how many have a review state, and how many of those are
/// ready under <see cref="ReadinessRule"/>.
/// </summary>
public sealed record ConceptCounts(string Concept, int Questions, int Stated, int Ready);

public sealed record TopicCounts(string Topic, IReadOnlyList<ConceptCounts> Concepts);

public sealed record DomainCounts(string Domain, IReadOnlyList<TopicCounts> Topics);

/// <summary>
/// Pure readiness math. A concept with no schedulable question has no state and is excluded from every percentage;
/// topic and domain percentages are the floor of ready / counted concepts (a domain aggregates its concepts, not its
/// topics' percentages), or null when nothing counts.
/// </summary>
public static class ReadinessCalculator
{
    public static ConceptReadiness? StateOf(ConceptCounts counts)
    {
        ArgumentNullException.ThrowIfNull(counts);
        if (counts.Questions == 0)
        {
            return null;
        }

        return counts.Ready == counts.Questions ? ConceptReadiness.Ready
            : counts.Stated > 0 ? ConceptReadiness.InProgress
            : ConceptReadiness.NotStarted;
    }

    public static int? Percent(int ready, int counted) => counted == 0 ? null : 100 * ready / counted;

    public static ReadinessDto Build(IReadOnlyList<DomainCounts> domains)
    {
        ArgumentNullException.ThrowIfNull(domains);
        return new ReadinessDto(domains.Select(BuildDomain).ToList());
    }

    private static DomainReadinessDto BuildDomain(DomainCounts domain)
    {
        var topics = domain.Topics.Select(BuildTopic).ToList();
        var ready = topics.Sum(t => t.Ready);
        var counted = topics.Sum(t => t.Counted);
        return new DomainReadinessDto(domain.Domain, ready, counted, Percent(ready, counted), topics);
    }

    private static TopicReadinessDto BuildTopic(TopicCounts topic)
    {
        var concepts = topic.Concepts
            .Select(c => (Counts: c, State: StateOf(c)))
            .Where(x => x.State is not null)
            .Select(x => new ConceptReadinessDto(x.Counts.Concept, x.State!.Value.ToWireValue(), x.Counts.Questions, x.Counts.Ready))
            .ToList();
        var ready = concepts.Count(c => c.State == ConceptReadiness.Ready.ToWireValue());
        return new TopicReadinessDto(topic.Topic, ready, concepts.Count, Percent(ready, concepts.Count), concepts);
    }
}
