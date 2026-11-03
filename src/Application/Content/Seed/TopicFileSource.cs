using System.Text.Json;
using System.Text.Json.Serialization;

namespace Spine.Application.Content.Seed;

/// <summary>A topic file as discovered on disk: its relative path plus the parsed node and DTO.</summary>
public sealed record TopicFileSource(string RelativePath, System.Text.Json.Nodes.JsonNode Node, TopicFile? Parsed);
