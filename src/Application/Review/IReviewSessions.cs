namespace InterviewPrep.Application.Review;

/// <summary>
/// The review-session use cases for the single local learner. "Now" is never a parameter: the implementation reads
/// its clock once per call and hands it to the pure scheduling code. Failures are <see cref="ReviewException"/>s.
/// </summary>
public interface IReviewSessions
{
    /// <exception cref="ReviewException"><see cref="ReviewError.ScopeNotFound"/>.</exception>
    Task<ReviewOverviewDto> GetOverviewAsync(ReviewScope scope, CancellationToken ct = default);

    /// <summary>Finishes any open session first, then builds and stores a new one.</summary>
    /// <exception cref="ReviewException"><see cref="ReviewError.ScopeNotFound"/>, <see cref="ReviewError.NothingToReview"/>.</exception>
    Task<SessionDto> StartAsync(ReviewScope scope, int totalLimit, int newLimit, CancellationToken ct = default);

    /// <summary>The open session, or null when there is none.</summary>
    Task<SessionDto?> GetCurrentAsync(CancellationToken ct = default);

    /// <summary>Open or finished session by id; null when unknown.</summary>
    Task<SessionDto?> GetAsync(Guid sessionId, CancellationToken ct = default);

    /// <exception cref="ReviewException">
    /// <see cref="ReviewError.SessionNotFound"/>, <see cref="ReviewError.SessionFinished"/>,
    /// <see cref="ReviewError.QuestionNotInSession"/>, <see cref="ReviewError.DuplicateAnswer"/>.
    /// </exception>
    Task<AnswerFeedbackDto> AnswerAsync(Guid sessionId, string questionId, int selectedIndex, CancellationToken ct = default);

    /// <summary>Idempotent: an already finished session returns its summary unchanged.</summary>
    /// <exception cref="ReviewException"><see cref="ReviewError.SessionNotFound"/>.</exception>
    Task<SessionSummaryDto> FinishAsync(Guid sessionId, CancellationToken ct = default);
}
