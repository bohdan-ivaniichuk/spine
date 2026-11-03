using Spine.Application.Review;
using Spine.Application.Review.AnswerQuestion;
using Spine.Domain.Content;
using Spine.Infrastructure.Persistence;

namespace Spine.Infrastructure.Review.AnswerQuestion;

/// <summary>EF implementation of <see cref="IAnswerQuestionUseCase"/>.</summary>
public sealed class AnswerQuestionUseCase(AppDbContext db) : IAnswerQuestionUseCase
{
    public Task<AnswerFeedbackDto> ExecuteAsync(Guid sessionId, string questionId, int selectedIndex, Language language, CancellationToken ct)
    {
        _ = db;
        throw new NotImplementedException();
    }
}
