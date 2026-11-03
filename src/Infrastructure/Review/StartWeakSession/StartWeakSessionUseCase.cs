using Spine.Application.Review;
using Spine.Application.Review.StartWeakSession;
using Spine.Domain.Content;
using Spine.Domain.Progress;
using Spine.Infrastructure.Persistence;
using Spine.Infrastructure.Review.Sessions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Spine.Infrastructure.Review.StartWeakSession;

/// <summary>EF implementation of <see cref="IStartWeakSessionUseCase"/>: newest misses first, through <see cref="SessionStarter"/>.</summary>
public sealed class StartWeakSessionUseCase(AppDbContext db, SessionStarter starter, IOptions<ReviewOptions> options)
    : IStartWeakSessionUseCase
{
    // The new-question limit means nothing in weak mode (a weak question always has a review state); the default is
    // stored so "Review again" round-trips a valid value.
    public Task<SessionDto> ExecuteAsync(ReviewScope scope, int totalLimit, Language language, CancellationToken ct) =>
        starter.StartAsync(scope, SessionMode.Weak, totalLimit, options.Value.DefaultNewLimit, language, async _ =>
        {
            var weakIds = await db.WeakQuestions(scope)
                .OrderByDescending(w => w.MissedAt).ThenBy(w => w.QuestionId)
                .Select(w => w.QuestionId)
                .Take(totalLimit)
                .ToListAsync(ct);
            var plan = SessionPlanner.PlanWeak(weakIds, totalLimit);
            return plan.Length > 0
                ? plan
                : throw new ReviewException(ReviewError.NothingToDrill, "No question in this scope has a wrong last answer.");
        }, ct);
}
