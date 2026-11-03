using Spine.Application.Review;
using Spine.Application.Review.StartWeakSession;
using Spine.Domain.Content;
using Spine.Infrastructure.Persistence;

namespace Spine.Infrastructure.Review.StartWeakSession;

/// <summary>EF implementation of <see cref="IStartWeakSessionUseCase"/>.</summary>
public sealed class StartWeakSessionUseCase(AppDbContext db) : IStartWeakSessionUseCase
{
    public Task<SessionDto> ExecuteAsync(ReviewScope scope, int totalLimit, Language language, CancellationToken ct)
    {
        _ = db;
        throw new NotImplementedException();
    }
}
