using FastEndpoints;
using Spine.Api.Errors;
using Spine.Application.Review;

namespace Spine.Api.Endpoints.Review;

public sealed class GetSessionRequest : LanguageQuery
{
    public Guid Id { get; set; }
}

public sealed class GetSessionValidator : LanguageQueryValidator<GetSessionRequest>;

/// <summary>GET /api/review/sessions/{id} — one session, open or finished.</summary>
public sealed class GetSessionEndpoint(IReviewSessions sessions) : Endpoint<GetSessionRequest, SessionDto>
{
    public override void Configure()
    {
        Get("/review/sessions/{id}");
        AllowAnonymous();
        Description(b => b.Produces<SessionDto>(200).ProducesProblemDetails(400).ProducesProblemDetails(404));
        Summary(s =>
        {
            s.Summary = "Session by id";
            s.Description = "Questions in session order (options as text only), recorded answers and ids skipped because they were deprecated after the session started. Text follows ?lang with per-question fallback to English.";
        });
    }

    public override async Task HandleAsync(GetSessionRequest req, CancellationToken ct)
    {
        var session = await sessions.GetAsync(req.Id, req.ParsedLanguage, ct);
        if (session is null)
        {
            await Send.ResultAsync(ReviewExceptionHandler.Problem(ReviewError.SessionNotFound, $"No session '{req.Id}'.", HttpContext.Request.Path));
            return;
        }

        await Send.OkAsync(session, ct);
    }
}
