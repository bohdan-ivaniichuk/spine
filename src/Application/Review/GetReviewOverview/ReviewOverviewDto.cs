using Spine.Application.Review.Shared;

namespace Spine.Application.Review.GetReviewOverview;

public sealed record ReviewOverviewDto(
    ScopeDto Scope,
    int DueCount,
    int NewCount,
    int TotalEligible,
    DateTimeOffset? NextDueAt,
    SessionDefaultsDto Defaults,
    SessionLimitsDto Limits,
    int WeakCount = 0);
