namespace Spine.Domain.Content;

/// <summary>Content language. English is canonical and mandatory; Ukrainian is optional.</summary>
public enum Language
{
    En,
    Uk,
}

public static class LanguageExtensions
{
    public static string ToDbValue(this Language language) => language switch
    {
        Language.En => "en",
        Language.Uk => "uk",
        _ => throw new ArgumentOutOfRangeException(nameof(language), language, null),
    };

    public static Language ParseLanguage(string value) => value switch
    {
        "en" => Language.En,
        "uk" => Language.Uk,
        _ => throw new ArgumentException($"Unknown language '{value}'.", nameof(value)),
    };

    public static bool TryParseLanguage(string? value, out Language language)
    {
        switch (value)
        {
            case "en": language = Language.En; return true;
            case "uk": language = Language.Uk; return true;
            default: language = Language.En; return false;
        }
    }
}
