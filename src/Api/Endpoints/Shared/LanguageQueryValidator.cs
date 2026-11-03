using FastEndpoints;
using FluentValidation;

namespace Spine.Api.Endpoints.Shared;

/// <summary>Validator for requests whose only rule is the language.</summary>
public abstract class LanguageQueryValidator<T> : Validator<T>
    where T : LanguageQuery
{
    protected LanguageQueryValidator()
    {
        RuleFor(r => r.Lang).MustBeSupportedLanguage();
    }
}
