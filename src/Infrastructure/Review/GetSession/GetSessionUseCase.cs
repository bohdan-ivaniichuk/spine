using Spine.Application.Review;
using Spine.Application.Review.GetSession;
using Spine.Domain.Content;
using Spine.Infrastructure.Persistence;

namespace Spine.Infrastructure.Review.GetSession;

/// <summary>EF implementation of <see cref="IGetSessionUseCase"/>.</summary>
public sealed class GetSessionUseCase(AppDbContext db) : IGetSessionUseCase
{
    public Task<SessionDto?> ExecuteAsync(Guid sessionId, Language language, CancellationToken ct)
    {
        _ = db;
        throw new NotImplementedException();
    }
}
