using FastEndpoints;
using Spine.Application.Review;

namespace Spine.Api.Endpoints.Review;

/// <summary><c>lang</c> is accepted and validated for consistency with the other review endpoints; the payload has no text.</summary>
public sealed class GetReadinessRequest : LanguageQuery;

public sealed class GetReadinessValidator : LanguageQueryValidator<GetReadinessRequest>;

/// <summary>GET /api/review/readiness — readiness per domain, topic and concept for the atlas.</summary>
public sealed class GetReadinessEndpoint(IReadinessQueries readiness) : Endpoint<GetReadinessRequest, ReadinessDto>
{
    public override void Configure()
    {
        Get("/review/readiness");
        AllowAnonymous();
        Description(b => b.Produces<ReadinessDto>(200).ProducesProblemDetails(400));
        Summary(s =>
        {
            s.Summary = "Readiness per domain, topic and concept";
            s.Description = "Every non-deprecated domain and topic in manifest order with ready / counted concepts and a floor percentage (null when nothing counts); concepts with schedulable questions carry not-started / in-progress / ready.";
        });
    }

    public override async Task HandleAsync(GetReadinessRequest req, CancellationToken ct)
    {
        await Send.OkAsync(await readiness.GetAsync(ct), ct);
    }
}
