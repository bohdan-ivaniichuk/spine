using System.Text.Json;
using System.Text.Json.Serialization;

namespace Spine.Application.Content.Seed;

public sealed record ManifestDomain(string Slug, string Title, IReadOnlyList<ManifestTopic> Topics, ManifestTitles? Titles = null);
