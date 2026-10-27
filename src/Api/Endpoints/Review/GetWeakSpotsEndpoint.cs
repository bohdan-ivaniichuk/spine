using FastEndpoints;
using Spine.Application.Review;

namespace Spine.Api.Endpoints.Review;

/// <summary><c>lang</c> is accepted and validated for consistency with the other review endpoints; the payload has no text.</summary>
public sealed class GetWeakSpotsRequest : LanguageQuery;

public sealed class GetWeakSpotsValidator : LanguageQueryValidator<GetWeakSpotsRequest>;

/// <summary>GET /api/review/weak-spots — the weakest concepts and topics over the last 30 days.</summary>
public sealed class GetWeakSpotsEndpoint(IWeakSpotsQueries weakSpots) : Endpoint<GetWeakSpotsRequest, WeakSpotsDto>
{
    public override void Configure()
    {
        Get("/review/weak-spots");
        AllowAnonymous();
        Description(b => b.Produces<WeakSpotsDto>(200).ProducesProblemDetails(400));
        Summary(s =>
        {
            s.Summary = "Weakest concepts and topics";
            s.Description = "Concepts (top 20) and topics (top 10) with at least 3 answers in the last 30 days, lowest accuracy first, then more wrong answers, then the most recent answer; each concept carries its current weak questions and readiness.";
        });
    }

    public override async Task HandleAsync(GetWeakSpotsRequest req, CancellationToken ct)
    {
        await Send.OkAsync(await weakSpots.GetAsync(ct), ct);
    }
}
