using Spine.Application.Review;
using Spine.Application.Review.GetReviewOverview;
using Spine.Domain.Content;
using Spine.Infrastructure.Persistence;
using Spine.Infrastructure.Review.Sessions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Spine.Infrastructure.Review.GetReviewOverview;

/// <summary>EF implementation of <see cref="IGetReviewOverviewUseCase"/>: five counting queries over the scope.</summary>
public sealed class GetReviewOverviewUseCase(
    AppDbContext db,
    ReviewScopeResolver resolver,
    TimeProvider clock,
    IOptions<ReviewOptions> options) : IGetReviewOverviewUseCase
{
    private static readonly ReviewOverviewMapper Mapper = new();

    public async Task<ReviewOverviewDto> ExecuteAsync(ReviewScope scope, Language language, CancellationToken ct)
    {
        var scopeDto = await resolver.ResolveAsync(scope, language, ct);
        var counts = await CountAsync(scope, clock.GetUtcNow(), ct);
        return Mapper.ToOverview(
            counts,
            scopeDto,
            new SessionDefaultsDto(options.Value.DefaultTotalLimit, options.Value.DefaultNewLimit),
            new SessionLimitsDto(ReviewOptions.MinLimit, ReviewOptions.MaxLimit));
    }

    private async Task<OverviewCounts> CountAsync(ReviewScope scope, DateTimeOffset now, CancellationToken ct)
    {
        var totalEligible = await db.Eligible(scope).CountAsync(ct);
        var dueCount = await db.DueIds(scope, now).CountAsync(ct);
        var newCount = await db.NewIds(scope).CountAsync(ct);
        var nextDueAt = await db.DueAtOf(scope)
            .Where(dueAt => dueAt > now)
            .Select(dueAt => (DateTimeOffset?)dueAt)
            .MinAsync(ct);
        var weakCount = await db.WeakQuestions(scope).CountAsync(ct);
        return new OverviewCounts(totalEligible, dueCount, newCount, nextDueAt, weakCount);
    }
}
