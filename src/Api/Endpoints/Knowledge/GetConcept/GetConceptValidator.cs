using FastEndpoints;
using FluentValidation;
using Spine.Api.Endpoints.Shared;

namespace Spine.Api.Endpoints.Knowledge.GetConcept;

public sealed class GetConceptValidator : Validator<GetConceptRequest>
{
    public GetConceptValidator()
    {
        RuleFor(r => r.Lang).MustBeSupportedLanguage();
        RuleFor(r => r.Domain).NotEmpty();
        RuleFor(r => r.Topic).NotEmpty();
        RuleFor(r => r.Concept).NotEmpty();
    }
}
