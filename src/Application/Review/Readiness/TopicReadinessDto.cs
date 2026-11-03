namespace Spine.Application.Review.Readiness;

public sealed record TopicReadinessDto(string Topic, int Ready, int Counted, int? Percent, IReadOnlyList<ConceptReadinessDto> Concepts);
