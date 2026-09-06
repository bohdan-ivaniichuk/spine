using System.Text.Json;
using System.Text.Json.Nodes;
using InterviewPrep.Application.Content.Seed;

namespace InterviewPrep.Application.Tests.Content;

/// <summary>
/// Builds a <see cref="ContentSet"/> the way the real importer will: schemas from the repo's
/// <c>content/schema</c>, <c>manifest.json</c> from the content dir, topic files discovered as
/// <c>&lt;dir&gt;/*.json</c> (excluding <c>schema/</c>), each parsed to a JsonNode and a DTO.
/// </summary>
internal static class ContentSetLoader
{
    private const string SchemaMarker = "content/schema/content.schema.json";

    public static string FixturesDir => Path.Combine(AppContext.BaseDirectory, "Fixtures");
    public static string ValidContentDir => Path.Combine(FixturesDir, "content");
    public static string InvalidFixturesDir => Path.Combine(FixturesDir, "content-invalid");

    /// <summary>Walks up from the test output directory until a folder containing content/schema/content.schema.json is found.</summary>
    public static string? TryFindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, SchemaMarker)))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return null;
    }

    public static string RepoRoot =>
        TryFindRepoRoot() ?? throw new InvalidOperationException($"Repository root not found: no ancestor of '{AppContext.BaseDirectory}' contains '{SchemaMarker}'.");

    public static string RepoContentDir => Path.Combine(RepoRoot, "content");

    public static string ContentSchemaJson => File.ReadAllText(Path.Combine(RepoContentDir, "schema", "content.schema.json"));
    public static string ManifestSchemaJson => File.ReadAllText(Path.Combine(RepoContentDir, "schema", "manifest.schema.json"));

    public static string ReadValidManifest() => File.ReadAllText(Path.Combine(ValidContentDir, "manifest.json"));
    public static string ReadValidTopic() => File.ReadAllText(Path.Combine(ValidContentDir, "dotnet", "clr-memory.json"));
    public static string ReadInvalid(string fileName) => File.ReadAllText(Path.Combine(InvalidFixturesDir, fileName));

    public static ContentSet LoadValid() => Load(ValidContentDir);

    /// <summary>Loads a content directory from disk.</summary>
    public static ContentSet Load(string contentDir)
    {
        var manifestPath = Path.Combine(contentDir, "manifest.json");
        var manifestJson = File.Exists(manifestPath) ? File.ReadAllText(manifestPath) : null;

        var files = new List<(string RelativePath, string Json)>();
        foreach (var sub in Directory.GetDirectories(contentDir).OrderBy(d => d, StringComparer.Ordinal))
        {
            var name = Path.GetFileName(sub);
            if (string.Equals(name, "schema", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var file in Directory.GetFiles(sub, "*.json").OrderBy(f => f, StringComparer.Ordinal))
            {
                files.Add(($"{name}/{Path.GetFileName(file)}", File.ReadAllText(file)));
            }
        }

        return FromFiles(contentDir, manifestJson, files.ToArray());
    }

    /// <summary>Builds a set from explicit (relativePath, json) pairs so a file can be placed at any path.</summary>
    public static ContentSet FromFiles(string contentDir, string? manifestJson, params (string RelativePath, string Json)[] files)
    {
        ManifestSource? manifest = null;
        if (manifestJson is not null)
        {
            var node = ParseNode(manifestJson, "manifest.json");
            manifest = new ManifestSource("manifest.json", node, TryDeserialize<ManifestFile>(node));
        }

        var topics = files
            .Select(f =>
            {
                var node = ParseNode(f.Json, f.RelativePath);
                return new TopicFileSource(f.RelativePath, node, TryDeserialize<TopicFile>(node));
            })
            .ToList();

        return new ContentSet(contentDir, ContentSchemaJson, ManifestSchemaJson, manifest, topics);
    }

    /// <summary>Builds a set from the valid fixture manifest plus explicit topic files.</summary>
    public static ContentSet WithValidManifest(params (string RelativePath, string Json)[] files) =>
        FromFiles(ValidContentDir, ReadValidManifest(), files);

    /// <summary>Builds a set from already-parsed DTOs (serialized back to nodes), for rule tests that do not care about raw JSON.</summary>
    public static ContentSet FromParsed(ManifestFile? manifest, params (string RelativePath, TopicFile File)[] files)
    {
        var manifestSource = manifest is null
            ? null
            : new ManifestSource("manifest.json", JsonSerializer.SerializeToNode(manifest, SeedJson.Options)!, manifest);

        var topics = files
            .Select(f => new TopicFileSource(f.RelativePath, JsonSerializer.SerializeToNode(f.File, SeedJson.Options)!, f.File))
            .ToList();

        return new ContentSet("<in-memory>", ContentSchemaJson, ManifestSchemaJson, manifestSource, topics);
    }

    private static JsonNode ParseNode(string json, string relativePath) =>
        JsonNode.Parse(json) ?? throw new InvalidOperationException($"'{relativePath}' parsed to a JSON null.");

    private static T? TryDeserialize<T>(JsonNode node) where T : class
    {
        try
        {
            return node.Deserialize<T>(SeedJson.Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
