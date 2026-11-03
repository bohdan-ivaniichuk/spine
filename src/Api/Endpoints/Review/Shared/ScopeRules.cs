using System.Text.RegularExpressions;
using FluentValidation;
using Spine.Api.Endpoints.Shared;

namespace Spine.Api.Endpoints.Review.Shared;

/// <summary>Scope validation shared by the overview and session start: slugs and the scope/slug combinations.</summary>
public static partial class ScopeRules
{
    private static readonly string[] ScopeTypes = ["all", "domain", "topic", "concept"];

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex SlugRegex();

    public static bool IsSlug(string? value) => value is not null && SlugRegex().IsMatch(value);

    /// <summary>
    /// The same rule as <c>ck_sessions_scope</c>: domain iff scope ≠ all, topic iff scope is topic or concept, concept
    /// iff scope is concept.
    /// </summary>
    public static void AddScopeRules<T>(this AbstractValidator<T> validator) where T : ScopeQuery
    {
        validator.RuleFor(r => r.Lang).MustBeSupportedLanguage();
        validator.RuleFor(r => r.Scope)
            .Must(s => ScopeTypes.Contains(s, StringComparer.Ordinal))
            .WithMessage("scope must be 'all', 'domain', 'topic' or 'concept'");

        validator.RuleFor(r => r.Domain)
            .Must(d => string.IsNullOrEmpty(d) || IsSlug(d))
            .WithMessage("domain must be a slug (lowercase letters, digits, single hyphens)");
        validator.RuleFor(r => r.Topic)
            .Must(t => string.IsNullOrEmpty(t) || IsSlug(t))
            .WithMessage("topic must be a slug (lowercase letters, digits, single hyphens)");
        validator.RuleFor(r => r.Concept)
            .Must(c => string.IsNullOrEmpty(c) || IsSlug(c))
            .WithMessage("concept must be a slug (lowercase letters, digits, single hyphens)");

        validator.RuleFor(r => r.Domain)
            .NotEmpty().When(r => r.Scope is "domain" or "topic" or "concept")
            .WithMessage("domain is required unless scope is 'all'");
        validator.RuleFor(r => r.Domain)
            .Empty().When(r => r.Scope == "all")
            .WithMessage("domain must be empty when scope is 'all'");
        validator.RuleFor(r => r.Topic)
            .NotEmpty().When(r => r.Scope is "topic" or "concept")
            .WithMessage("topic is required when scope is 'topic' or 'concept'");
        validator.RuleFor(r => r.Topic)
            .Empty().When(r => r.Scope is not ("topic" or "concept"))
            .WithMessage("topic must be empty unless scope is 'topic' or 'concept'");
        validator.RuleFor(r => r.Concept)
            .NotEmpty().When(r => r.Scope == "concept")
            .WithMessage("concept is required when scope is 'concept'");
        validator.RuleFor(r => r.Concept)
            .Empty().When(r => r.Scope != "concept")
            .WithMessage("concept must be empty unless scope is 'concept'");
    }
}
