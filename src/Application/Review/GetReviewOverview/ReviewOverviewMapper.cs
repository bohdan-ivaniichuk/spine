using Riok.Mapperly.Abstractions;

namespace Spine.Application.Review.GetReviewOverview;

/// <summary>Builds the overview DTO from the counts, the resolved scope and the session-size settings.</summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public sealed partial class ReviewOverviewMapper
{
    public partial ReviewOverviewDto ToOverview(
        OverviewCounts counts, ScopeDto scope, SessionDefaultsDto defaults, SessionLimitsDto limits);
}
