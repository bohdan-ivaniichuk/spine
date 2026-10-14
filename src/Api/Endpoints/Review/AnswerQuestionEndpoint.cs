using FastEndpoints;
using FluentValidation;
using InterviewPrep.Application.Review;

namespace InterviewPrep.Api.Endpoints.Review;

public sealed class AnswerQuestionRequest
{
    /// <summary>Session id (route).</summary>
    public Guid Id { get; set; }

    public string QuestionId { get; set; } = string.Empty;

    /// <summary>Index of the chosen option, 0..3.</summary>
    public int SelectedIndex { get; set; }
}

public sealed class AnswerQuestionValidator : Validator<AnswerQuestionRequest>
{
    public const int MaxOptionIndex = 3;

    public AnswerQuestionValidator()
    {
        RuleFor(r => r.QuestionId).NotEmpty();
        RuleFor(r => r.SelectedIndex).InclusiveBetween(0, MaxOptionIndex);
    }
}

/// <summary>POST /api/review/sessions/{id}/answers — records exactly one answer per question and schedules the next review.</summary>
public sealed class AnswerQuestionEndpoint(IReviewSessions sessions) : Endpoint<AnswerQuestionRequest, AnswerFeedbackDto>
{
    public override void Configure()
    {
        Post("/review/sessions/{id}/answers");
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
            s.Description = "Grades the selected option deterministically, stores the attempt, updates the SM-2 review state and returns the feedback (correct index, explanation, next due). 409 when the session is finished or the question was already answered; 422 when the question is not part of the session.";
        });
    }

    public override async Task HandleAsync(AnswerQuestionRequest req, CancellationToken ct)
    {
        await Send.OkAsync(await sessions.AnswerAsync(req.Id, req.QuestionId, req.SelectedIndex, ct), ct);
    }
}
