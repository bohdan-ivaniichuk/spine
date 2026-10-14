using FastEndpoints;
using FluentValidation;
using InterviewPrep.Application.Review;
using Microsoft.Extensions.Options;

namespace InterviewPrep.Api.Endpoints.Review;

public sealed class StartSessionRequest : ScopeQuery
{
    /// <summary>Session size, 1..100; defaults to REVIEW_DEFAULT_TOTAL_LIMIT.</summary>
    public int? TotalLimit { get; set; }

    /// <summary>How many of those may be new questions, 1..100; defaults to REVIEW_DEFAULT_NEW_LIMIT.</summary>
    public int? NewLimit { get; set; }
}

public sealed class StartSessionValidator : Validator<StartSessionRequest>
{
    public StartSessionValidator()
    {
        this.AddScopeRules();
        RuleFor(r => r.TotalLimit)
            .InclusiveBetween(ReviewOptions.MinLimit, ReviewOptions.MaxLimit)
            .When(r => r.TotalLimit.HasValue);
        RuleFor(r => r.NewLimit)
            .InclusiveBetween(ReviewOptions.MinLimit, ReviewOptions.MaxLimit)
            .When(r => r.NewLimit.HasValue);
    }
}

/// <summary>POST /api/review/sessions — finishes any open session, builds a new one and returns it (201 + Location).</summary>
public sealed class StartSessionEndpoint(IReviewSessions sessions, IOptions<ReviewOptions> options) : Endpoint<StartSessionRequest, SessionDto>
{
    public override void Configure()
    {
        Post("/review/sessions");
        AllowAnonymous();
        Description(b => b.Produces<SessionDto>(201).ProducesProblemDetails(400).ProducesProblemDetails(404).ProducesProblemDetails(409));
        Summary(s =>
        {
            s.Summary = "Start a review session";
            s.Description = "Due questions first (most overdue first), then new ones in taxonomy order, bounded by totalLimit and newLimit. Any open session is finished first. 404 for an unknown scope, 409 when nothing is due or new.";
        });
    }

    public override async Task HandleAsync(StartSessionRequest req, CancellationToken ct)
    {
        var session = await sessions.StartAsync(
            req.ToReviewScope(),
            req.TotalLimit ?? options.Value.DefaultTotalLimit,
            req.NewLimit ?? options.Value.DefaultNewLimit,
            ct);
        await Send.CreatedAtAsync<GetSessionEndpoint>(new { id = session.Id }, session, cancellation: ct);
    }
}
