namespace Spine.Application.Review.GetReviewCoverage;

public sealed record ReviewCoverageDto(IReadOnlyList<TopicCoverageDto> Topics);
