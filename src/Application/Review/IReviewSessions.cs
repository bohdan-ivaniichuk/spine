using Spine.Domain.Content;

namespace Spine.Application.Review;

/// <summary>
/// The review-session use cases for the single local learner. "Now" is never a parameter: the implementation reads
/// its clock once per call and hands it to the pure scheduling code. Failures are <see cref="ReviewException"/>s.
/// <c>language</c> only selects the text that is returned (per-item fallback to English, see
/// <see cref="TranslationPicker"/>); membership, grading and scheduling never depend on it.
/// </summary>
public interface IReviewSessions
{
    /// <exception cref="ReviewException"><see cref="ReviewError.ScopeNotFound"/>.</exception>
    Task<ReviewOverviewDto> GetOverviewAsync(ReviewScope scope, CancellationToken ct = default);

    /// <summary>Finishes any open session first, then builds and stores a new one.</summary>
    /// <exception cref="ReviewException"><see cref="ReviewError.ScopeNotFound"/>, <see cref="ReviewError.NothingToReview"/>.</exception>
    Task<SessionDto> StartAsync(ReviewScope scope, int totalLimit, int newLimit, Language language = Language.En, CancellationToken ct = default);

    /// <summary>The open session, or null when there is none.</summary>
    Task<SessionDto?> GetCurrentAsync(Language language = Language.En, CancellationToken ct = default);

    /// <summary>Open or finished session by id; null when unknown.</summary>
    Task<SessionDto?> GetAsync(Guid sessionId, Language language = Language.En, CancellationToken ct = default);

    /// <exception cref="ReviewException">
    /// <see cref="ReviewError.SessionNotFound"/>, <see cref="ReviewError.SessionFinished"/>,
    /// <see cref="ReviewError.QuestionNotInSession"/>, <see cref="ReviewError.DuplicateAnswer"/>.
    /// </exception>
    Task<AnswerFeedbackDto> AnswerAsync(Guid sessionId, string questionId, int selectedIndex, Language language = Language.En, CancellationToken ct = default);

    /// <summary>Idempotent: an already finished session returns its summary unchanged.</summary>
    /// <exception cref="ReviewException"><see cref="ReviewError.SessionNotFound"/>.</exception>
    Task<SessionSummaryDto> FinishAsync(Guid sessionId, Language language = Language.En, CancellationToken ct = default);

    /// <summary>
    /// Eligible / seen / due counts for every non-deprecated topic of a non-deprecated domain, in manifest order
    /// (topics with nothing eligible included). No text, so no language.
    /// </summary>
    Task<ReviewCoverageDto> GetCoverageAsync(CancellationToken ct = default);
}
