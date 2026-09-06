using FastEndpoints;
using FluentValidation;
using InterviewPrep.Domain.Content;

namespace InterviewPrep.Api.Endpoints.Knowledge;

/// <summary>Shared query parameters of the knowledge read endpoints.</summary>
public abstract class LanguageQuery
{
    /// <summary>Content language: en (default) or uk. Falls back to en per item when uk is missing.</summary>
    [QueryParam]
    public string Lang { get; set; } = "en";

    /// <summary>Include deprecated domains/topics/concepts (default false).</summary>
    [QueryParam]
    public bool IncludeDeprecated { get; set; }

    public Language ParsedLanguage => LanguageExtensions.ParseLanguage(Lang);
}

public static class LanguageRules
{
    public static IRuleBuilderOptions<T, string> MustBeSupportedLanguage<T>(this IRuleBuilder<T, string> rule) =>
        rule.Must(v => LanguageExtensions.TryParseLanguage(v, out _)).WithMessage("lang must be 'en' or 'uk'");
}
