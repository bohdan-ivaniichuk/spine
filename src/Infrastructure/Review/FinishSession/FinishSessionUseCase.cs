using Spine.Application.Review;
using Spine.Application.Review.FinishSession;
using Spine.Domain.Content;
using Spine.Infrastructure.Persistence;

namespace Spine.Infrastructure.Review.FinishSession;

/// <summary>EF implementation of <see cref="IFinishSessionUseCase"/>.</summary>
public sealed class FinishSessionUseCase(AppDbContext db) : IFinishSessionUseCase
{
    public Task<SessionSummaryDto> ExecuteAsync(Guid sessionId, Language language, CancellationToken ct)
    {
        _ = db;
        throw new NotImplementedException();
    }
}
