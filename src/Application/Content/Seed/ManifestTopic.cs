using System.Text.Json;
using System.Text.Json.Serialization;

namespace Spine.Application.Content.Seed;

public sealed record ManifestTopic(string Slug, string Title, ManifestTitles? Titles = null);
