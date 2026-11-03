using Spine.Application.Review.GetCurrentSession;
using Spine.Application.Review.Sessions;
using Spine.Domain.Content;
using Spine.Domain.Progress;
using Spine.Infrastructure.Persistence;
using Spine.Infrastructure.Review.Sessions;
using Microsoft.EntityFrameworkCore;

namespace Spine.Infrastructure.Review.GetCurrentSession;

/// <summary>EF implementation of <see cref="IGetCurrentSessionUseCase"/>: the newest open session, projected.</summary>
public sealed class GetCurrentSessionUseCase(AppDbContext db, SessionProjector projector) : IGetCurrentSessionUseCase
{
    public async Task<SessionDto?> ExecuteAsync(Language language, CancellationToken ct)
    {
        var userId = LocalLearner.Id;
        var session = await db.Sessions.AsNoTracking()
            .Where(s => s.UserId == userId && s.FinishedAt == null)
            .OrderByDescending(s => s.StartedAt)
            .FirstOrDefaultAsync(ct);
        return session is null ? null : await projector.ProjectAsync(session, language, ct);
    }
}
