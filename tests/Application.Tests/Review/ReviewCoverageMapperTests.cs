using Spine.Application.Review.GetReviewCoverage;

namespace Spine.Application.Tests.Review;

public sealed class ReviewCoverageMapperTests
{
    [Fact]
    [Trait("Requirement", "FR-006")]
    public void Every_topic_is_listed_in_order_with_zeros_where_nothing_is_eligible()
    {
        TopicCoverageRow[] topics = [new(2, "dotnet", "async"), new(1, "dotnet", "clr-memory"), new(3, "angular", "signals")];
        var counts = new Dictionary<int, TopicCoverageCounts> { [1] = new(1, 4, 2, 1), [3] = new(3, 1, 1, 0) };

        var coverage = new ReviewCoverageMapper().ToCoverage(topics, counts);

        coverage.Topics.ShouldBe(
        [
            new TopicCoverageDto("dotnet", "async", 0, 0, 0),
            new TopicCoverageDto("dotnet", "clr-memory", 4, 2, 1),
            new TopicCoverageDto("angular", "signals", 1, 1, 0),
        ]);
    }
}
