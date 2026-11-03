using Spine.Application.Review.Sessions;
using Spine.Application.Review.Shared;
using Spine.Domain.Content;

namespace Spine.Application.Review.StartWeakSession;

/// <summary>Finishes any open session and starts a weak-spots drill over questions whose last answer was wrong.</summary>
public interface IStartWeakSessionUseCase
{
    /// <exception cref="ReviewException"><see cref="ReviewError.NothingToDrill"/> or <see cref="ReviewError.ScopeNotFound"/>.</exception>
    Task<SessionDto> ExecuteAsync(ReviewScope scope, int totalLimit, Language language, CancellationToken ct);
}
