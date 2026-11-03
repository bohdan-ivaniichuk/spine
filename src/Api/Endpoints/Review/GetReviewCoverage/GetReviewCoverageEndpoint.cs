using FastEndpoints;
using Spine.Application.Review;
using Spine.Application.Review.GetReviewCoverage;

namespace Spine.Api.Endpoints.Review.GetReviewCoverage;

/// <summary>GET /api/review/coverage — eligible / seen / due counts per topic for the knowledge map.</summary>
public sealed class GetReviewCoverageEndpoint(IGetReviewCoverageUseCase coverage)
    : Endpoint<GetReviewCoverageRequest, ReviewCoverageDto>
{
    public override void Configure()
    {
        Get(ApiRoutes.Review.Coverage);
        AllowAnonymous();
        Description(b => b
            .Produces<ReviewCoverageDto>(200)
            .ProducesProblemDetails(400));
        Summary(s =>
        {
            s.Summary = "Review coverage per topic";
            s.Description = "Every non-deprecated topic in manifest order (including ones with nothing eligible) with "
                + "eligible MCQs, how many have been seen (have a review state) and how many are due now.";
        });
    }

    public override async Task HandleAsync(GetReviewCoverageRequest req, CancellationToken ct)
    {
        await Send.OkAsync(await coverage.ExecuteAsync(ct), ct);
    }
}
