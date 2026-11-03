using Spine.Application.Review.Shared;
using Spine.Domain.Content;

namespace Spine.Application.Review.FinishSession;

/// <summary>Finishes the session (idempotent) and returns its summary.</summary>
public interface IFinishSessionUseCase
{
    /// <exception cref="ReviewException"><see cref="ReviewError.SessionNotFound"/>.</exception>
    Task<SessionSummaryDto> ExecuteAsync(Guid sessionId, Language language, CancellationToken ct);
}
