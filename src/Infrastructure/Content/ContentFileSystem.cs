using System.Text.Json;
using System.Text.Json.Nodes;
using Spine.Application.Content.Seed;
using Spine.Application.Content.Validation;

namespace Spine.Infrastructure.Content;

/// <summary>Reads the content directory: schemas, manifest and every &lt;domain&gt;/&lt;topic&gt;.json topic file.</summary>
public static class ContentFileSystem
{
    public const string SchemaDir = "schema";
    public const string ContentSchemaFile = "content.schema.json";
    public const string ManifestSchemaFile = "manifest.schema.json";
    public const string ManifestFile = "manifest.json";

    /// <summary>Loads everything; JSON syntax errors are reported as validation errors, not exceptions.</summary>
    public static (ContentSet? Set, ContentValidationResult ParseErrors) Load(string contentDir)
    {
        var errors = new ContentValidationResult();
        var schemaDir = Path.Combine(contentDir, SchemaDir);
        foreach (var schemaFile in new[] { ContentSchemaFile, ManifestSchemaFile })
        {
            if (!File.Exists(Path.Combine(schemaDir, schemaFile)))
            {
                errors.Add($"{SchemaDir}/{schemaFile}", "", "schema-missing", "schema file is missing from the content directory");
            }
        }

        if (!errors.IsValid)
        {
            return (null, errors);
        }

        var contentSchema = File.ReadAllText(Path.Combine(schemaDir, ContentSchemaFile));
        var manifestSchema = File.ReadAllText(Path.Combine(schemaDir, ManifestSchemaFile));

        ManifestSource? manifest = null;
        var manifestPath = Path.Combine(contentDir, ManifestFile);
        if (File.Exists(manifestPath))
        {
            var node = ParseNode(manifestPath, ManifestFile, errors);
            if (node is not null)
            {
                manifest = new ManifestSource(ManifestFile, node, TryDeserialize<ManifestFile>(node));
            }
        }

        var topicFiles = new List<TopicFileSource>();
        foreach (var dir in Directory.EnumerateDirectories(contentDir).Order(StringComparer.Ordinal))
        {
            var dirName = Path.GetFileName(dir);
            if (string.Equals(dirName, SchemaDir, StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(dir, "*.json").Order(StringComparer.Ordinal))
            {
                var relative = $"{dirName}/{Path.GetFileName(file)}";
                var node = ParseNode(file, relative, errors);
                if (node is not null)
                {
                    topicFiles.Add(new TopicFileSource(relative, node, TryDeserialize<TopicFile>(node)));
                }
            }
        }

        return (new ContentSet(contentDir, contentSchema, manifestSchema, manifest, topicFiles), errors);
    }

    public static bool HasAnyContent(string contentDir) =>
        File.Exists(Path.Combine(contentDir, ManifestFile)) ||
        Directory.EnumerateDirectories(contentDir)
            .Where(d => !string.Equals(Path.GetFileName(d), SchemaDir, StringComparison.Ordinal))
            .Any(d => Directory.EnumerateFiles(d, "*.json").Any());

    private static JsonNode? ParseNode(string path, string relative, ContentValidationResult errors)
    {
        try
        {
            return JsonNode.Parse(File.ReadAllText(path), documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Disallow });
        }
        catch (JsonException ex)
        {
            errors.Add(relative, ex.Path ?? "", "json", ex.Message);
            return null;
        }
    }

    private static T? TryDeserialize<T>(JsonNode node) where T : class
    {
        try
        {
            return node.Deserialize<T>(SeedJson.Options);
        }
        catch (JsonException)
        {
            return null; // schema validation reports the shape problem with a pointer
        }
    }
}
