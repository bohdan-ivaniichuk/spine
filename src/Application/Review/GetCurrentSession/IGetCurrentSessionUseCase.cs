using Spine.Domain.Content;

namespace Spine.Application.Review.GetCurrentSession;

/// <summary>The open session for resume, or null when none is open.</summary>
public interface IGetCurrentSessionUseCase
{
    Task<SessionDto?> ExecuteAsync(Language language, CancellationToken ct);
}
