using FastEndpoints;
using Microsoft.Extensions.Options;
using Spine.Api.Endpoints.Review.GetSession;
using Spine.Application.Review;

namespace Spine.Api.Endpoints.Review.StartSession;

/// <summary>
/// POST /api/review/sessions — finishes any open session, builds a new one and returns it (201 + Location).
/// </summary>
public sealed class StartSessionEndpoint(IReviewSessions sessions, IOptions<ReviewOptions> options)
    : Endpoint<StartSessionRequest, SessionDto>
{
    public override void Configure()
    {
        Post(ApiRoutes.Review.Sessions);
        AllowAnonymous();
        Description(b => b
            .Produces<SessionDto>(201)
            .ProducesProblemDetails(400)
            .ProducesProblemDetails(404)
            .ProducesProblemDetails(409));
        Summary(s =>
        {
            s.Summary = "Start a review session";
            s.Description = "Due questions first (most overdue first), then new ones in taxonomy order, bounded by "
                + "totalLimit and newLimit. mode=weak instead takes the questions whose most recent answer "
                + "was wrong (newest miss first, newLimit ignored). Any open session is finished first. "
                + "?lang=en|uk selects the text of the returned session (per-question fallback to English). "
                + "404 for an unknown scope, 409 when nothing is due or new (regular) or nothing is weak "
                + "(weak).";
        });
    }

    public override async Task HandleAsync(StartSessionRequest req, CancellationToken ct)
    {
        var total = req.TotalLimit ?? options.Value.DefaultTotalLimit;
        var session = req.Mode == "weak"
            ? await sessions.StartWeakAsync(req.ToReviewScope(), total, req.ParsedLanguage, ct)
            : await sessions.StartAsync(
                req.ToReviewScope(),
                total,
                req.NewLimit ?? options.Value.DefaultNewLimit,
                req.ParsedLanguage,
                ct);
        await Send.CreatedAtAsync<GetSessionEndpoint>(new { id = session.Id }, session, cancellation: ct);
    }
}
