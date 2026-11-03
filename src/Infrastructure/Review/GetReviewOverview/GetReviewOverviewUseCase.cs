using Spine.Application.Review;
using Spine.Application.Review.GetReviewOverview;
using Spine.Domain.Content;
using Spine.Infrastructure.Persistence;

namespace Spine.Infrastructure.Review.GetReviewOverview;

/// <summary>EF implementation of <see cref="IGetReviewOverviewUseCase"/>.</summary>
public sealed class GetReviewOverviewUseCase(AppDbContext db) : IGetReviewOverviewUseCase
{
    public Task<ReviewOverviewDto> ExecuteAsync(ReviewScope scope, Language language, CancellationToken ct)
    {
        _ = db;
        throw new NotImplementedException();
    }
}
