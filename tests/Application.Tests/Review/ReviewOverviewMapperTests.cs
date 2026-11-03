using Spine.Application.Review;
using Spine.Application.Review.GetReviewOverview;

namespace Spine.Application.Tests.Review;

public sealed class ReviewOverviewMapperTests
{
    [Fact]
    [Trait("Requirement", "FR-006")]
    public void Overview_carries_counts_scope_and_session_sizes()
    {
        var next = new DateTimeOffset(2026, 11, 4, 9, 0, 0, TimeSpan.Zero);
        var scope = new ScopeDto("topic", "dotnet", "async", ".NET", "Async");

        var overview = new ReviewOverviewMapper().ToOverview(
            new OverviewCounts(TotalEligible: 9, DueCount: 3, NewCount: 4, NextDueAt: next, WeakCount: 2),
            scope,
            new SessionDefaultsDto(20, 10),
            new SessionLimitsDto(1, 50));

        overview.ShouldBe(new ReviewOverviewDto(
            scope, DueCount: 3, NewCount: 4, TotalEligible: 9, NextDueAt: next,
            new SessionDefaultsDto(20, 10), new SessionLimitsDto(1, 50), WeakCount: 2));
    }
}
