using FastEndpoints;
using Spine.Api.Endpoints.Review.Shared;

namespace Spine.Api.Endpoints.Review.GetReviewOverview;

public sealed class GetReviewOverviewValidator : Validator<GetReviewOverviewRequest>
{
    public GetReviewOverviewValidator()
    {
        this.AddScopeRules();
    }
}
