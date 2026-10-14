using System.Text.Json;
using System.Text.Json.Serialization;

namespace Spine.Application.Content.Seed;

/// <summary>DTOs mirroring content/schema/manifest.schema.json and content.schema.json one to one.</summary>
public static class SeedJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Disallow,
        AllowTrailingCommas = false,
        PropertyNameCaseInsensitive = false,
    };
}

public sealed record ManifestFile(IReadOnlyList<ManifestDomain> Domains);

public sealed record ManifestDomain(string Slug, string Title, IReadOnlyList<ManifestTopic> Topics);

public sealed record ManifestTopic(string Slug, string Title);

public sealed record TopicFile(string Domain, string Topic, IReadOnlyList<ConceptSeed> Concepts);

public sealed record ConceptSeed(
    string Slug,
    string Title,
    string Level,
    IReadOnlyDictionary<string, ConceptTranslationSeed> Translations,
    IReadOnlyList<QuestionSeed> Questions,
    IReadOnlyList<KataSeed>? Katas);

public sealed record ConceptTranslationSeed(string Title, string Note);

public sealed record QuestionSeed(
    string Id,
    string Type,
    string Level,
    bool Reviewed,
    IReadOnlyDictionary<string, QuestionTranslationSeed> Translations,
    IReadOnlyList<string> Tags);

public sealed record QuestionTranslationSeed(
    string Prompt,
    IReadOnlyList<McqOptionSeed>? Options,
    string Explanation,
    IReadOnlyList<string>? Rubric);

public sealed record McqOptionSeed(string Text, bool Correct);

public sealed record KataSeed(
    string Id,
    string Title,
    string Language,
    bool Reviewed,
    IReadOnlyDictionary<string, KataTranslationSeed> Translations,
    int TimeLimitMinutes);

public sealed record KataTranslationSeed(string Task, IReadOnlyList<string> Rubric, string ReferenceSolution);

/// <summary>A topic file as discovered on disk: its relative path plus the parsed node and DTO.</summary>
public sealed record TopicFileSource(string RelativePath, System.Text.Json.Nodes.JsonNode Node, TopicFile? Parsed);

public sealed record ManifestSource(string RelativePath, System.Text.Json.Nodes.JsonNode Node, ManifestFile? Parsed);

/// <summary>Everything the importer reads from disk before validation.</summary>
public sealed record ContentSet(
    string ContentDir,
    string ContentSchemaJson,
    string ManifestSchemaJson,
    ManifestSource? Manifest,
    IReadOnlyList<TopicFileSource> TopicFiles);
