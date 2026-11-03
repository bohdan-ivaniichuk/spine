using Spine.Application.Review.Sessions;
using Spine.Application.Review.Shared;
using Spine.Domain.Content;

namespace Spine.Application.Review.StartSession;

/// <summary>Finishes any open session and starts a regular one: due questions first, then new ones, within the limits.</summary>
public interface IStartSessionUseCase
{
    /// <exception cref="ReviewException"><see cref="ReviewError.NothingToReview"/> or <see cref="ReviewError.ScopeNotFound"/>.</exception>
    Task<SessionDto> ExecuteAsync(ReviewScope scope, int totalLimit, int newLimit, Language language, CancellationToken ct);
}
