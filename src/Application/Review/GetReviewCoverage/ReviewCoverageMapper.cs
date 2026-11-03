namespace Spine.Application.Review.GetReviewCoverage;

/// <summary>Builds the coverage DTO: every topic in order, zero counts where nothing is eligible.</summary>
public sealed class ReviewCoverageMapper
{
    public ReviewCoverageDto ToCoverage(
        IReadOnlyList<TopicCoverageRow> topics, IReadOnlyDictionary<int, TopicCoverageCounts> counts)
    {
        _ = (topics, counts);
        throw new NotImplementedException();
    }
}
