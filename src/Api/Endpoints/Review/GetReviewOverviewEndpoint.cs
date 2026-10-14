using FastEndpoints;
using Spine.Application.Review;

namespace Spine.Api.Endpoints.Review;

public sealed class GetReviewOverviewRequest : ScopeQuery;

public sealed class GetReviewOverviewValidator : Validator<GetReviewOverviewRequest>
{
    public GetReviewOverviewValidator()
    {
        this.AddScopeRules();
    }
}

/// <summary>GET /api/review/overview — due/new counts for a scope plus the session-size defaults.</summary>
public sealed class GetReviewOverviewEndpoint(IReviewSessions sessions) : Endpoint<GetReviewOverviewRequest, ReviewOverviewDto>
{
    public override void Configure()
    {
        Get("/review/overview");
        AllowAnonymous();
        Description(b => b.Produces<ReviewOverviewDto>(200).ProducesProblemDetails(400).ProducesProblemDetails(404));
        Summary(s =>
        {
            s.Summary = "Review overview";
            s.Description = "How many questions are due and new in the scope (all / domain / topic), the next due time when nothing is due, and the session-size defaults and bounds. lang is validated; scope titles are English (the manifest has no translations).";
        });
    }

    public override async Task HandleAsync(GetReviewOverviewRequest req, CancellationToken ct)
    {
        await Send.OkAsync(await sessions.GetOverviewAsync(req.ToReviewScope(), ct), ct);
    }
}
