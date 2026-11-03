using FastEndpoints;
using Spine.Domain.Content;

namespace Spine.Api.Endpoints.Shared;

/// <summary>The <c>?lang=en|uk</c> query parameter shared by the knowledge and review endpoints.</summary>
public abstract class LanguageQuery
{
    /// <summary>Content language: en (default) or uk. Falls back to en per item when uk is missing.</summary>
    [QueryParam]
    public string Lang { get; set; } = "en";

    public Language ParsedLanguage => LanguageExtensions.ParseLanguage(Lang);
}
