using Spine.Domain.Content;

namespace Spine.Application.Review.Shared;

/// <summary>
/// Per-item language choice for review text. A question is served in the requested language only when that
/// translation exists and its options are index-aligned with English (same count, correct option at the same index),
/// so a <c>selectedIndex</c> means the same option in every language. Otherwise prompt, options and explanation all
/// fall back to English together; they are never mixed.
/// </summary>
public static class TranslationPicker
{
    /// <summary>True when <paramref name="requestedOptions"/> can replace <paramref name="enOptions"/> without changing grading.</summary>
    public static bool IsAligned(IReadOnlyList<McqOption>? enOptions, IReadOnlyList<McqOption>? requestedOptions)
    {
        if (enOptions is null || requestedOptions is null || enOptions.Count != requestedOptions.Count)
        {
            return false;
        }

        for (var i = 0; i < enOptions.Count; i++)
        {
            if (enOptions[i].Correct != requestedOptions[i].Correct)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// The translation to serve: <paramref name="requested"/> when it exists and is aligned, else <paramref name="en"/>.
    /// <c>Fallback</c> is true only when a non-English language was requested and English is served.
    /// </summary>
    public static (T Text, Language Language, bool Fallback) Pick<T>(
        Language requestedLanguage,
        T en,
        IReadOnlyList<McqOption>? enOptions,
        T? requested,
        IReadOnlyList<McqOption>? requestedOptions)
        where T : class
    {
        if (requestedLanguage == Language.En)
        {
            return (en, Language.En, false);
        }

        return requested is not null && IsAligned(enOptions, requestedOptions)
            ? (requested, requestedLanguage, false)
            : (en, Language.En, true);
    }
}
