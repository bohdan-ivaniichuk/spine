namespace Spine.Domain.Content;

public enum KataLanguage
{
    CSharp,
    TypeScript,
    Sql,
}

public static class KataLanguageExtensions
{
    public static string ToDbValue(this KataLanguage language) => language switch
    {
        KataLanguage.CSharp => "csharp",
        KataLanguage.TypeScript => "typescript",
        KataLanguage.Sql => "sql",
        _ => throw new ArgumentOutOfRangeException(nameof(language), language, null),
    };

    public static KataLanguage ParseKataLanguage(string value) => value switch
    {
        "csharp" => KataLanguage.CSharp,
        "typescript" => KataLanguage.TypeScript,
        "sql" => KataLanguage.Sql,
        _ => throw new ArgumentException($"Unknown kata language '{value}'.", nameof(value)),
    };
}
