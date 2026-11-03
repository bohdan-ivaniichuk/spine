using FastEndpoints;
using Spine.Application.Review;

namespace Spine.Api.Endpoints.Review.GetActivity;

/// <summary>
/// GET /api/review/activity — current and longest streak and the last 14 days of answers in a time zone.
/// </summary>
public sealed class GetActivityEndpoint(IActivityQueries activity) : Endpoint<GetActivityRequest, ActivityDto>
{
    public override void Configure()
    {
        Get(ApiRoutes.Review.Activity);
        AllowAnonymous();
        Description(b => b
            .Produces<ActivityDto>(200)
            .ProducesProblemDetails(400));
        Summary(s =>
        {
            s.Summary = "Answer activity and streaks";
            s.Description = "Current streak (alive through yesterday while today has no answer), longest streak and "
                + "the answers on each of the last 14 days, with days in the requested IANA time zone (UTC "
                + "by default).";
        });
    }

    public override async Task HandleAsync(GetActivityRequest req, CancellationToken ct)
    {
        await Send.OkAsync(await activity.GetAsync(req.ResolveZone(), ct), ct);
    }
}
