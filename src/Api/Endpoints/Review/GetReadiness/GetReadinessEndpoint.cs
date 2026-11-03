using Spine.Application.Review.Readiness;
using Spine.Application.Review;
using FastEndpoints;

namespace Spine.Api.Endpoints.Review.GetReadiness;

/// <summary>GET /api/review/readiness — readiness per domain, topic and concept for the atlas.</summary>
public sealed class GetReadinessEndpoint(IReadinessQueries readiness) : Endpoint<GetReadinessRequest, ReadinessDto>
{
    public override void Configure()
    {
        Get(ApiRoutes.Review.Readiness);
        AllowAnonymous();
        Description(b => b
            .Produces<ReadinessDto>(200)
            .ProducesProblemDetails(400));
        Summary(s =>
        {
            s.Summary = "Readiness per domain, topic and concept";
            s.Description = "Every non-deprecated domain and topic in manifest order with ready / counted concepts "
                + "and a floor percentage (null when nothing counts); concepts with schedulable questions "
                + "carry not-started / in-progress / ready.";
        });
    }

    public override async Task HandleAsync(GetReadinessRequest req, CancellationToken ct)
    {
        await Send.OkAsync(await readiness.GetAsync(ct), ct);
    }
}
