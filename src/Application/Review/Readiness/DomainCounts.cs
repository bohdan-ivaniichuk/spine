namespace Spine.Application.Review.Readiness;

public sealed record DomainCounts(string Domain, IReadOnlyList<TopicCounts> Topics);
