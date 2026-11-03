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
