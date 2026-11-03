using System.Text.Json;
using System.Text.Json.Serialization;

namespace Spine.Application.Content.Seed;

public sealed record ManifestSource(string RelativePath, System.Text.Json.Nodes.JsonNode Node, ManifestFile? Parsed);
