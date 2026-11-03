namespace Spine.Application.Review.GetReviewOverview;

/// <summary>Builds the overview DTO from the counts, the resolved scope and the session-size settings.</summary>
public sealed class ReviewOverviewMapper
{
    public ReviewOverviewDto ToOverview(
        OverviewCounts counts, ScopeDto scope, SessionDefaultsDto defaults, SessionLimitsDto limits)
    {
        _ = (counts, scope, defaults, limits);
        throw new NotImplementedException();
    }
}
