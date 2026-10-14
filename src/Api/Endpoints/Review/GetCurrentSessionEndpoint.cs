using FastEndpoints;
using Spine.Application.Review;

namespace Spine.Api.Endpoints.Review;

public sealed class GetCurrentSessionRequest : LanguageQuery;

public sealed class GetCurrentSessionValidator : LanguageQueryValidator<GetCurrentSessionRequest>;

/// <summary>GET /api/review/sessions/current — the open session for resume, or 204 when there is none.</summary>
public sealed class GetCurrentSessionEndpoint(IReviewSessions sessions) : Endpoint<GetCurrentSessionRequest, SessionDto>
{
    public override void Configure()
    {
        Get("/review/sessions/current");
        AllowAnonymous();
        Description(b => b.Produces<SessionDto>(200).Produces(204).ProducesProblemDetails(400));
        Summary(s =>
        {
            s.Summary = "Current session";
            s.Description = "The open session with its questions and recorded answers, so a reload can resume at the first unanswered question. 204 when no session is open.";
        });
    }

    public override async Task HandleAsync(GetCurrentSessionRequest req, CancellationToken ct)
    {
        var session = await sessions.GetCurrentAsync(req.ParsedLanguage, ct);
        if (session is null)
        {
            await Send.NoContentAsync(ct);
            return;
        }

        await Send.OkAsync(session, ct);
    }
}
