using Spine.Application.Review.GetSession;
using Spine.Application.Review.Sessions;
using Spine.Application.Review;
using Spine.Domain.Content;
using Spine.Infrastructure.Review.Sessions;

namespace Spine.Infrastructure.Review.GetSession;

/// <summary>EF implementation of <see cref="IGetSessionUseCase"/>.</summary>
public sealed class GetSessionUseCase(SessionProjector projector) : IGetSessionUseCase
{
    public Task<SessionDto?> ExecuteAsync(Guid sessionId, Language language, CancellationToken ct) =>
        projector.FindAsync(sessionId, language, ct);
}
