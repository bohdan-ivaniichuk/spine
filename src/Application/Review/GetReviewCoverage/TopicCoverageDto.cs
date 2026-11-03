namespace Spine.Application.Review.GetReviewCoverage;

/// <summary>Per-topic progress of the local learner. Due &lt;= seen &lt;= eligible.</summary>
public sealed record TopicCoverageDto(string Domain, string Topic, int Eligible, int Seen, int Due);
