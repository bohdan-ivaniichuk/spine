using FastEndpoints;
using FluentValidation;

namespace Spine.Api.Endpoints.Review.GetActivity;

public sealed class GetActivityValidator : Validator<GetActivityRequest>
{
    public GetActivityValidator()
    {
        RuleFor(r => r.Tz)
            .Must(GetActivityRequest.IsKnownZone)
            .WithMessage("tz must be a known IANA time zone id, e.g. Europe/Kyiv");
    }
}
