using Spine.Application.Review;
using Spine.Application.Review.GetReviewCoverage;
using Spine.Domain.Content;
using Spine.Infrastructure.Persistence;

namespace Spine.Infrastructure.Review.GetReviewCoverage;

/// <summary>EF implementation of <see cref="IGetReviewCoverageUseCase"/>.</summary>
public sealed class GetReviewCoverageUseCase(AppDbContext db) : IGetReviewCoverageUseCase
{
    public Task<ReviewCoverageDto> ExecuteAsync(CancellationToken ct)
    {
        _ = db;
        throw new NotImplementedException();
    }
}
