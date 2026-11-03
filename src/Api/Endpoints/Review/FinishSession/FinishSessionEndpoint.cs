using FastEndpoints;
using Spine.Application.Review;
using Spine.Application.Review.FinishSession;

namespace Spine.Api.Endpoints.Review.FinishSession;

/// <summary>POST /api/review/sessions/{id}/finish — closes the session (idempotent) and returns its summary.</summary>
public sealed class FinishSessionEndpoint(IFinishSessionUseCase finish) : Endpoint<FinishSessionRequest, SessionSummaryDto>
{
    public override void Configure()
    {
        Post(ApiRoutes.Review.Finish);
        AllowAnonymous();
        Description(b => b
            .Produces<SessionSummaryDto>(200)
            .ProducesProblemDetails(400)
            .ProducesProblemDetails(404));
        Summary(s =>
        {
            s.Summary = "Finish a session";
            s.Description = "Stores finish time and counts (once), then returns answered/correct, accuracy, weak "
                + "concepts ranked by misses and how many questions are due within 24 hours in the same "
                + "scope. Weak-concept titles follow ?lang with fallback to English. Repeatable.";
        });
    }

    public override async Task HandleAsync(FinishSessionRequest req, CancellationToken ct)
    {
        await Send.OkAsync(await finish.ExecuteAsync(req.Id, req.ParsedLanguage, ct), ct);
    }
}
