using FastEndpoints;
using FluentValidation;
using Spine.Domain.Content;

namespace Spine.Api.Endpoints;

/// <summary>The <c>?lang=en|uk</c> query parameter shared by the knowledge and review endpoints.</summary>
public abstract class LanguageQuery
{
    /// <summary>Content language: en (default) or uk. Falls back to en per item when uk is missing.</summary>
    [QueryParam]
    public string Lang { get; set; } = "en";

    public Language ParsedLanguage => LanguageExtensions.ParseLanguage(Lang);
}

public static class LanguageRules
{
    public static IRuleBuilderOptions<T, string> MustBeSupportedLanguage<T>(this IRuleBuilder<T, string> rule) =>
        rule.Must(v => LanguageExtensions.TryParseLanguage(v, out _)).WithMessage("lang must be 'en' or 'uk'");
}

/// <summary>Validator for requests whose only rule is the language.</summary>
public abstract class LanguageQueryValidator<T> : Validator<T> where T : LanguageQuery
{
    protected LanguageQueryValidator()
    {
        RuleFor(r => r.Lang).MustBeSupportedLanguage();
    }
}
