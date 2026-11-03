namespace Spine.Application.Review.GetReviewCoverage;

/// <summary>Eligible, seen and due MCQs of one topic.</summary>
public sealed record TopicCoverageCounts(int TopicId, int Eligible, int Seen, int Due);
