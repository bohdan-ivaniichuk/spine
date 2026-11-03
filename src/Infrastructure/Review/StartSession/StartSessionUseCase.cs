using Spine.Application.Review.Sessions;
using Spine.Application.Review.Shared;
using Spine.Application.Review.StartSession;
using Spine.Application.Review;
using Spine.Domain.Content;
using Spine.Domain.Progress;
using Spine.Infrastructure.Persistence;
using Spine.Infrastructure.Review.Sessions;
using Microsoft.EntityFrameworkCore;

namespace Spine.Infrastructure.Review.StartSession;

/// <summary>EF implementation of <see cref="IStartSessionUseCase"/>: due first, then new, through <see cref="SessionStarter"/>.</summary>
public sealed class StartSessionUseCase(AppDbContext db, SessionStarter starter) : IStartSessionUseCase
{
    public async Task<SessionDto> ExecuteAsync(
        ReviewScope scope, int totalLimit, int newLimit, Language language, CancellationToken ct)
    {
        if (!ReviewOptions.IsWithinBounds(newLimit))
        {
            throw new ArgumentOutOfRangeException(
                nameof(newLimit), newLimit, $"Must be {ReviewOptions.MinLimit}..{ReviewOptions.MaxLimit}.");
        }

        return await starter.StartAsync(scope, SessionMode.Regular, totalLimit, newLimit, language, async now =>
        {
            var dueIds = await db.DueIds(scope, now).Take(totalLimit).ToListAsync(ct);
            var newIds = await db.NewIds(scope).Take(Math.Min(newLimit, totalLimit)).ToListAsync(ct);
            var plan = SessionPlanner.Plan(dueIds, newIds, totalLimit, newLimit);
            return plan.Length > 0
                ? plan
                : throw new ReviewException(
                    ReviewError.NothingToReview, "Nothing is due and there are no new questions in this scope.");
        }, ct);
    }
}
