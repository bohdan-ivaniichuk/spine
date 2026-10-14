using FastEndpoints;
using Spine.Application.Review;

namespace Spine.Api.Endpoints.Review;

/// <summary><c>lang</c> is accepted and validated for consistency with the other review endpoints; the payload has no text.</summary>
public sealed class GetReviewCoverageRequest : LanguageQuery;

public sealed class GetReviewCoverageValidator : LanguageQueryValidator<GetReviewCoverageRequest>;

/// <summary>GET /api/review/coverage — eligible / seen / due counts per topic for the knowledge map.</summary>
public sealed class GetReviewCoverageEndpoint(IReviewSessions sessions) : Endpoint<GetReviewCoverageRequest, ReviewCoverageDto>
{
    public override void Configure()
    {
        Get("/review/coverage");
        AllowAnonymous();
        Description(b => b.Produces<ReviewCoverageDto>(200).ProducesProblemDetails(400));
        Summary(s =>
        {
            s.Summary = "Review coverage per topic";
            s.Description = "Every non-deprecated topic in manifest order (including ones with nothing eligible) with eligible MCQs, how many have been seen (have a review state) and how many are due now.";
        });
    }

    public override async Task HandleAsync(GetReviewCoverageRequest req, CancellationToken ct)
    {
        await Send.OkAsync(await sessions.GetCoverageAsync(ct), ct);
    }
}
