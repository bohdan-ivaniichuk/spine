using FluentValidation;
using Spine.Domain.Content;

namespace Spine.Api.Endpoints.Shared;

/// <summary>The <c>lang</c> rule shared by every validator that accepts it.</summary>
public static class LanguageRules
{
    public static IRuleBuilderOptions<T, string> MustBeSupportedLanguage<T>(this IRuleBuilder<T, string> rule) =>
        rule.Must(v => LanguageExtensions.TryParseLanguage(v, out _)).WithMessage("lang must be 'en' or 'uk'");
}
