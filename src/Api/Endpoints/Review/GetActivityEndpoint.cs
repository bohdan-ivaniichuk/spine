using FastEndpoints;
using FluentValidation;
using Spine.Application.Review;

namespace Spine.Api.Endpoints.Review;

/// <summary>No <c>lang</c>: the payload has no text.</summary>
public sealed class GetActivityRequest
{
    /// <summary>IANA time zone id, e.g. Europe/Kyiv. Absent or empty: UTC.</summary>
    [QueryParam]
    public string? Tz { get; set; }

    /// <summary>The zone the request names; UTC when it names none. Only valid after validation.</summary>
    public TimeZoneInfo ResolveZone() => throw new NotImplementedException();
}

public sealed class GetActivityValidator : Validator<GetActivityRequest>
{
    public GetActivityValidator()
    {
        RuleFor(r => r.Tz)
            .Must(_ => throw new NotImplementedException())
            .WithMessage("tz must be a known IANA time zone id, e.g. Europe/Kyiv");
    }
}

/// <summary>GET /api/review/activity — current and longest streak and the last 14 days of answers in a time zone.</summary>
public sealed class GetActivityEndpoint(IActivityQueries activity) : Endpoint<GetActivityRequest, ActivityDto>
{
    public override void Configure()
    {
        Get("/review/activity");
        AllowAnonymous();
        Description(b => b.Produces<ActivityDto>(200).ProducesProblemDetails(400));
        Summary(s =>
        {
            s.Summary = "Answer activity and streaks";
            s.Description = "Current streak (alive through yesterday while today has no answer), longest streak and the answers on each of the last 14 days, with days in the requested IANA time zone (UTC by default).";
        });
    }

    public override Task HandleAsync(GetActivityRequest req, CancellationToken ct)
    {
        _ = activity;
        throw new NotImplementedException();
    }
}
