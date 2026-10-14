using FastEndpoints;
using InterviewPrep.Application.Review;

namespace InterviewPrep.Api.Endpoints.Review;

public sealed class FinishSessionRequest
{
    public Guid Id { get; set; }
}

/// <summary>POST /api/review/sessions/{id}/finish — closes the session (idempotent) and returns its summary.</summary>
public sealed class FinishSessionEndpoint(IReviewSessions sessions) : Endpoint<FinishSessionRequest, SessionSummaryDto>
{
    public override void Configure()
    {
        Post("/review/sessions/{id}/finish");
        AllowAnonymous();
        Description(b => b.Produces<SessionSummaryDto>(200).ProducesProblemDetails(400).ProducesProblemDetails(404));
        Summary(s =>
        {
            s.Summary = "Finish a session";
            s.Description = "Stores finish time and counts (once), then returns answered/correct, accuracy, weak concepts ranked by misses and how many questions are due within 24 hours in the same scope. Repeatable.";
        });
    }

    public override async Task HandleAsync(FinishSessionRequest req, CancellationToken ct)
    {
        await Send.OkAsync(await sessions.FinishAsync(req.Id, ct), ct);
    }
}
