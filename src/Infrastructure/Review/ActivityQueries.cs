using Spine.Application.Review;
using Spine.Infrastructure.Persistence;

namespace Spine.Infrastructure.Review;

/// <summary>
/// Reads the answer times of the local learner's question attempts and turns them into local dates for
/// <see cref="ActivityCalculator"/>. Correctness, session, mode and later deprecation do not matter; katas never count.
/// </summary>
public sealed class ActivityQueries(AppDbContext db, TimeProvider clock) : IActivityQueries
{
    public Task<ActivityDto> GetAsync(TimeZoneInfo zone, CancellationToken ct = default)
    {
        _ = (db, clock);
        throw new NotImplementedException();
    }
}
