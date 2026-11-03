namespace Spine.Application.Review.Readiness;

public sealed record TopicCounts(string Topic, IReadOnlyList<ConceptCounts> Concepts);
