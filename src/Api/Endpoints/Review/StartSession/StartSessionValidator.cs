using Spine.Api.Endpoints.Review.Shared;
using Spine.Application.Review.Shared;
using FastEndpoints;
using FluentValidation;

namespace Spine.Api.Endpoints.Review.StartSession;

public sealed class StartSessionValidator : Validator<StartSessionRequest>
{
    public StartSessionValidator()
    {
        this.AddScopeRules();
        RuleFor(r => r.Mode)
            .Must(m => m is "regular" or "weak")
            .WithMessage("mode must be 'regular' or 'weak'");
        RuleFor(r => r.TotalLimit)
            .InclusiveBetween(ReviewOptions.MinLimit, ReviewOptions.MaxLimit)
            .When(r => r.TotalLimit.HasValue);
        RuleFor(r => r.NewLimit)
            .InclusiveBetween(ReviewOptions.MinLimit, ReviewOptions.MaxLimit)
            .When(r => r.NewLimit.HasValue && r.Mode != "weak");
    }
}
