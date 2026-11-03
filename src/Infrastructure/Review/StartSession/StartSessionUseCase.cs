using Spine.Application.Review;
using Spine.Application.Review.StartSession;
using Spine.Domain.Content;
using Spine.Infrastructure.Persistence;

namespace Spine.Infrastructure.Review.StartSession;

/// <summary>EF implementation of <see cref="IStartSessionUseCase"/>.</summary>
public sealed class StartSessionUseCase(AppDbContext db) : IStartSessionUseCase
{
    public Task<SessionDto> ExecuteAsync(ReviewScope scope, int totalLimit, int newLimit, Language language, CancellationToken ct)
    {
        _ = db;
        throw new NotImplementedException();
    }
}
