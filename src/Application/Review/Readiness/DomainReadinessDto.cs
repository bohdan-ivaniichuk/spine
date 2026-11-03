namespace Spine.Application.Review.Readiness;

public sealed record DomainReadinessDto(string Domain, int Ready, int Counted, int? Percent, IReadOnlyList<TopicReadinessDto> Topics);
