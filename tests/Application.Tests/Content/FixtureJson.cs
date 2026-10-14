using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Spine.Application.Content.Seed;

namespace Spine.Application.Tests.Content;

/// <summary>Small helpers to mutate the valid fixture JSON in memory and turn it back into a <see cref="ContentSet"/>.</summary>
internal static class FixtureJson
{
    public const string TopicPath = "dotnet/clr-memory.json";

    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    private static readonly JsonSerializerOptions Compact = new() { WriteIndented = false, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static JsonNode TopicNode() => JsonNode.Parse(ContentSetLoader.ReadValidTopic())!;
    public static JsonNode ManifestNode() => JsonNode.Parse(ContentSetLoader.ReadValidManifest())!;

    public static JsonObject Concept(JsonNode topic, int index) => (JsonObject)topic["concepts"]![index]!;
    public static JsonObject Question(JsonNode topic, int concept, int index) => (JsonObject)Concept(topic, concept)["questions"]![index]!;

    public static string Text(JsonNode node, bool indented = true) => node.ToJsonString(indented ? Indented : Compact);

    /// <summary>The valid manifest plus the valid topic file after <paramref name="mutate"/> has been applied.</summary>
    public static ContentSet WithTopic(Action<JsonNode> mutate)
    {
        var node = TopicNode();
        mutate(node);
        return ContentSetLoader.WithValidManifest((TopicPath, Text(node)));
    }

    /// <summary>Recursively rebuilds objects with their properties in reverse order; arrays keep their order.</summary>
    public static JsonNode ReverseKeys(JsonNode node) => node switch
    {
        JsonObject obj => new JsonObject(obj.Reverse().Select(p => KeyValuePair.Create(p.Key, p.Value is null ? null : ReverseKeys(p.Value)))),
        JsonArray arr => new JsonArray(arr.Select(a => a is null ? null : ReverseKeys(a)).ToArray()),
        _ => node.DeepClone(),
    };
}
