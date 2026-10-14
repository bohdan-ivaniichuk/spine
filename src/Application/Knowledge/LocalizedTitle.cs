using Spine.Domain.Content;

namespace Spine.Application.Knowledge;

/// <summary>
/// Language choice for domain and topic titles, which come from the manifest (<c>title</c> + optional
/// <c>titles.uk</c>) rather than from translation rows. Per item: the requested language when that title exists,
/// otherwise English. <c>Fallback</c> is true only when a non-English language was requested and English is served.
/// </summary>
public static class LocalizedTitle
{
    public static (string Title, bool Fallback) Pick(Language requested, string en, string? uk)
    {
        if (requested == Language.En)
        {
            return (en, false);
        }

        return requested == Language.Uk && !string.IsNullOrWhiteSpace(uk) ? (uk, false) : (en, true);
    }

    /// <summary>The title only, for shapes that carry no fallback flag (refs, scopes).</summary>
    public static string Of(Language requested, string en, string? uk) => Pick(requested, en, uk).Title;
}
