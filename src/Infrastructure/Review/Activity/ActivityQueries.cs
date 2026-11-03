using Spine.Application.Review.Activity;
using Spine.Application.Review;
using Spine.Domain.Progress;
using Spine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Spine.Infrastructure.Review.Activity;

/// <summary>
/// Reads the answer times of the local learner's question attempts and turns them into local dates for
/// <see cref="ActivityCalculator"/>. Correctness, session, mode and later deprecation do not matter; katas never count.
/// One query projecting <c>answered_at</c> only (a single learner's few thousand rows a year need no index); the zone
/// conversion happens here, with the same zone data the API validated the request against.
/// </summary>
public sealed class ActivityQueries(AppDbContext db, TimeProvider clock) : IActivityQueries
{
    public async Task<ActivityDto> GetAsync(TimeZoneInfo zone, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(zone);
        var userId = LocalLearner.Id;

        var answeredAt = await db.Attempts.AsNoTracking()
            .Where(a => a.UserId == userId && a.ItemType == ItemType.Question)
            .Select(a => a.AnsweredAt)
            .ToListAsync(ct);

        var today = LocalDate(clock.GetUtcNow(), zone);
        return ActivityCalculator.Calculate(zone.Id, today, answeredAt.Select(at => LocalDate(at, zone)));
    }

    private static DateOnly LocalDate(DateTimeOffset at, TimeZoneInfo zone) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(at, zone).DateTime);
}
