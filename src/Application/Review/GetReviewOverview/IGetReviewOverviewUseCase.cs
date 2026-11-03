using Spine.Application.Review.Shared;
using Spine.Domain.Content;

namespace Spine.Application.Review.GetReviewOverview;

/// <summary>How many questions are due, new and weak in a scope, the next due time and the session-size defaults.</summary>
public interface IGetReviewOverviewUseCase
{
    /// <exception cref="ReviewException"><see cref="ReviewError.ScopeNotFound"/> for an unknown or deprecated scope.</exception>
    Task<ReviewOverviewDto> ExecuteAsync(ReviewScope scope, Language language, CancellationToken ct);
}
