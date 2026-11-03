using System.Text.Json;
using System.Text.Json.Serialization;

namespace Spine.Application.Content.Seed;

/// <summary>Everything the importer reads from disk before validation.</summary>
public sealed record ContentSet(
    string ContentDir,
    string ContentSchemaJson,
    string ManifestSchemaJson,
    ManifestSource? Manifest,
    IReadOnlyList<TopicFileSource> TopicFiles);
