using Spine.Application.Review;
using Spine.Application.Review.GetCurrentSession;
using Spine.Domain.Content;
using Spine.Infrastructure.Persistence;

namespace Spine.Infrastructure.Review.GetCurrentSession;

/// <summary>EF implementation of <see cref="IGetCurrentSessionUseCase"/>.</summary>
public sealed class GetCurrentSessionUseCase(AppDbContext db) : IGetCurrentSessionUseCase
{
    public Task<SessionDto?> ExecuteAsync(Language language, CancellationToken ct)
    {
        _ = db;
        throw new NotImplementedException();
    }
}
