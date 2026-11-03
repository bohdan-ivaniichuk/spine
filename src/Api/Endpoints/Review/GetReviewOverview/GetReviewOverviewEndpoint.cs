using FastEndpoints;
using Spine.Application.Review;
using Spine.Application.Review.GetReviewOverview;

namespace Spine.Api.Endpoints.Review.GetReviewOverview;

/// <summary>GET /api/review/overview — due/new counts for a scope plus the session-size defaults.</summary>
public sealed class GetReviewOverviewEndpoint(IGetReviewOverviewUseCase overview)
    : Endpoint<GetReviewOverviewRequest, ReviewOverviewDto>
{
    public override void Configure()
    {
        Get(ApiRoutes.Review.Overview);
        AllowAnonymous();
        Description(b => b
            .Produces<ReviewOverviewDto>(200)
            .ProducesProblemDetails(400)
            .ProducesProblemDetails(404));
        Summary(s =>
        {
            s.Summary = "Review overview";
            s.Description = "How many questions are due and new in the scope (all / domain / topic), the next due "
                + "time when nothing is due, and the session-size defaults and bounds. Scope titles are in "
                + "lang when the manifest has that title (titles.uk), otherwise English.";
        });
    }

    public override async Task HandleAsync(GetReviewOverviewRequest req, CancellationToken ct)
    {
        var result = await overview.ExecuteAsync(req.ToReviewScope(), req.ParsedLanguage, ct);
        await Send.OkAsync(result, ct);
    }
}
