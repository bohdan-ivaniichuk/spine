using FastEndpoints;
using InterviewPrep.Application.Review;

namespace InterviewPrep.Api.Endpoints.Review;

/// <summary>GET /api/review/sessions/current — the open session for resume, or 204 when there is none.</summary>
public sealed class GetCurrentSessionEndpoint(IReviewSessions sessions) : EndpointWithoutRequest<SessionDto>
{
    public override void Configure()
    {
        Get("/review/sessions/current");
        AllowAnonymous();
        Description(b => b.Produces<SessionDto>(200).Produces(204));
        Summary(s =>
        {
            s.Summary = "Current session";
            s.Description = "The open session with its questions and recorded answers, so a reload can resume at the first unanswered question. 204 when no session is open.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var session = await sessions.GetCurrentAsync(ct);
        if (session is null)
        {
            await Send.NoContentAsync(ct);
            return;
        }

        await Send.OkAsync(session, ct);
    }
}
