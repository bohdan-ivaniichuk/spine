using System.Text.Json;
using InterviewPrep.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace InterviewPrep.Infrastructure.Persistence.Configurations;

/// <summary>Shared column conventions: text enums with CHECK constraints, jsonb lists with comparers, timestamps.</summary>
internal static class Conventions
{
    public const string SlugPattern = "^[a-z0-9]+(-[a-z0-9]+)*$";
    public const string QuestionIdPattern = "^[a-z0-9]+(-[a-z0-9]+)*\\.[a-z0-9]+(-[a-z0-9]+)*\\.[a-z0-9]+(-[a-z0-9]+)*\\.(mcq|open)\\.[0-9]+$";
    public const string KataIdPattern = "^[a-z0-9]+(-[a-z0-9]+)*\\.[a-z0-9]+(-[a-z0-9]+)*\\.[a-z0-9]+(-[a-z0-9]+)*\\.kata\\.[0-9]+$";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static PropertyBuilder<Level> Level(this PropertyBuilder<Level> b) =>
        b.HasConversion(v => v.ToDbValue(), v => LevelExtensions.ParseLevel(v)).HasColumnType("text").IsRequired();

    public static PropertyBuilder<Language> Language(this PropertyBuilder<Language> b) =>
        b.HasConversion(v => v.ToDbValue(), v => LanguageExtensions.ParseLanguage(v)).HasColumnType("text").IsRequired();

    public static PropertyBuilder<QuestionType> QuestionType(this PropertyBuilder<QuestionType> b) =>
        b.HasConversion(v => v.ToDbValue(), v => QuestionTypeExtensions.ParseQuestionType(v)).HasColumnType("text").IsRequired();

    public static PropertyBuilder<KataLanguage> KataLanguage(this PropertyBuilder<KataLanguage> b) =>
        b.HasConversion(v => v.ToDbValue(), v => KataLanguageExtensions.ParseKataLanguage(v)).HasColumnType("text").IsRequired();

    public static PropertyBuilder<string> ContentHash(this PropertyBuilder<string> b) =>
        b.HasColumnType("char(64)").IsRequired();

    /// <summary>jsonb column holding a JSON array; equality by element so unchanged lists never produce an UPDATE.</summary>
    public static PropertyBuilder<IReadOnlyList<T>?> JsonList<T>(this PropertyBuilder<IReadOnlyList<T>?> b)
    {
        var converter = new ValueConverter<IReadOnlyList<T>?, string?>(
            v => v == null ? null : JsonSerializer.Serialize(v, JsonOptions),
            v => v == null ? null : JsonSerializer.Deserialize<List<T>>(v, JsonOptions));
        var comparer = new ValueComparer<IReadOnlyList<T>?>(
            (a, c) => (a == null && c == null) || (a != null && c != null && a.SequenceEqual(c)),
            v => v == null ? 0 : v.Aggregate(17, (h, e) => HashCode.Combine(h, e)),
            v => v == null ? null : v.ToList());
        return b.HasConversion(converter, comparer).HasColumnType("jsonb");
    }

    public static PropertyBuilder<IReadOnlyList<T>> JsonListRequired<T>(this PropertyBuilder<IReadOnlyList<T>> b)
    {
        var converter = new ValueConverter<IReadOnlyList<T>, string>(
            v => JsonSerializer.Serialize(v, JsonOptions),
            v => JsonSerializer.Deserialize<List<T>>(v, JsonOptions)!);
        var comparer = new ValueComparer<IReadOnlyList<T>>(
            (a, c) => a != null && c != null && a.SequenceEqual(c),
            v => v.Aggregate(17, (h, e) => HashCode.Combine(h, e)),
            v => v.ToList());
        return b.HasConversion(converter, comparer).HasColumnType("jsonb").IsRequired();
    }

    public static PropertyBuilder<string[]> TextArray(this PropertyBuilder<string[]> b)
    {
        var comparer = new ValueComparer<string[]>(
            (a, c) => a != null && c != null && a.SequenceEqual(c),
            v => v.Aggregate(17, (h, e) => HashCode.Combine(h, e.GetHashCode())),
            v => v.ToArray());
        b.HasColumnType("text[]").IsRequired();
        b.Metadata.SetValueComparer(comparer);
        return b;
    }

    public static void Timestamps<T>(this EntityTypeBuilder<T> e, bool withUpdated = true) where T : class
    {
        e.Property<DateTimeOffset>("CreatedAt").IsRequired();
        if (withUpdated)
        {
            e.Property<DateTimeOffset>("UpdatedAt").IsRequired();
        }
    }
}
