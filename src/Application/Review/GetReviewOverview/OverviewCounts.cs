namespace Spine.Application.Review.GetReviewOverview;

/// <summary>The overview's five counting queries over one scope.</summary>
public sealed record OverviewCounts(int TotalEligible, int DueCount, int NewCount, DateTimeOffset? NextDueAt, int WeakCount);
