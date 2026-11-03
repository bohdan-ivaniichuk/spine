using Spine.Application.Review.Shared;
using Spine.Domain.Content;

namespace Spine.Application.Review.AnswerQuestion;

/// <summary>Grades one answer, records the attempt and schedules the next review.</summary>
public interface IAnswerQuestionUseCase
{
    /// <exception cref="ReviewException">Session not found or finished, question not in the session, deprecated or already answered.</exception>
    Task<AnswerFeedbackDto> ExecuteAsync(Guid sessionId, string questionId, int selectedIndex, Language language, CancellationToken ct);
}
