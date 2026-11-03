using FastEndpoints;
using Spine.Application.Review;
using Spine.Application.Review.AnswerQuestion;

namespace Spine.Api.Endpoints.Review.AnswerQuestion;

/// <summary>
/// POST /api/review/sessions/{id}/answers — records exactly one answer per question and schedules the next review.
/// </summary>
public sealed class AnswerQuestionEndpoint(IAnswerQuestionUseCase answer)
    : Endpoint<AnswerQuestionRequest, AnswerFeedbackDto>
{
    public override void Configure()
    {
        Post(ApiRoutes.Review.Answers);
        AllowAnonymous();
        Description(b => b
            .Produces<AnswerFeedbackDto>(200)
            .ProducesProblemDetails(400)
            .ProducesProblemDetails(404)
            .ProducesProblemDetails(409)
            .ProducesProblemDetails(422));
        Summary(s =>
        {
            s.Summary = "Answer a question";
            s.Description = "Grades the selected option deterministically, stores the attempt, updates the SM-2 "
                + "review state and returns the feedback (correct index, explanation in ?lang with "
                + "per-question fallback to English, next due). 409 when the session is finished or the "
                + "question was already answered; 422 when the question is not part of the session.";
        });
    }

    public override async Task HandleAsync(AnswerQuestionRequest req, CancellationToken ct)
    {
        var feedback = await answer.ExecuteAsync(req.Id, req.QuestionId, req.SelectedIndex, req.ParsedLanguage, ct);
        await Send.OkAsync(feedback, ct);
    }
}
