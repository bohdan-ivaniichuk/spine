using Spine.Api.Errors;
using Spine.Application.Review.GetSession;
using Spine.Application.Review.Sessions;
using Spine.Application.Review.Shared;
using FastEndpoints;

namespace Spine.Api.Endpoints.Review.GetSession;

/// <summary>GET /api/review/sessions/{id} — one session, open or finished.</summary>
public sealed class GetSessionEndpoint(IGetSessionUseCase getSession) : Endpoint<GetSessionRequest, SessionDto>
{
    public override void Configure()
    {
        Get(ApiRoutes.Review.Session);
        AllowAnonymous();
        Description(b => b
            .Produces<SessionDto>(200)
            .ProducesProblemDetails(400)
            .ProducesProblemDetails(404));
        Summary(s =>
        {
            s.Summary = "Session by id";
            s.Description = "Questions in session order (options as text only), recorded answers and ids skipped "
                + "because they were deprecated after the session started. Text follows ?lang with "
                + "per-question fallback to English.";
        });
    }

    public override async Task HandleAsync(GetSessionRequest req, CancellationToken ct)
    {
        var session = await getSession.ExecuteAsync(req.Id, req.ParsedLanguage, ct);
        if (session is null)
        {
            await Send.ResultAsync(ReviewExceptionHandler.Problem(
                ReviewError.SessionNotFound, $"No session '{req.Id}'.", HttpContext.Request.Path));
            return;
        }

        await Send.OkAsync(session, ct);
    }
}
