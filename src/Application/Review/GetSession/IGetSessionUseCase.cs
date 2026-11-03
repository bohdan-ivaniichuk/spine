using Spine.Application.Review.Sessions;
using Spine.Domain.Content;

namespace Spine.Application.Review.GetSession;

/// <summary>One session, open or finished, or null when it does not exist.</summary>
public interface IGetSessionUseCase
{
    Task<SessionDto?> ExecuteAsync(Guid sessionId, Language language, CancellationToken ct);
}
