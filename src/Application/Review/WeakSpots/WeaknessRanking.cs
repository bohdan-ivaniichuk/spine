namespace Spine.Application.Review.WeakSpots;

public sealed record WeaknessRanking(IReadOnlyList<ConceptAnswerStats> Concepts, IReadOnlyList<TopicAnswerStats> Topics);
