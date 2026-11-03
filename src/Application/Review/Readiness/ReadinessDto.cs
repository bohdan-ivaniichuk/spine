namespace Spine.Application.Review.Readiness;

public sealed record ReadinessDto(IReadOnlyList<DomainReadinessDto> Domains);
