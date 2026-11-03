using System.Text.Json;
using System.Text.Json.Serialization;

namespace Spine.Application.Content.Seed;

/// <summary>Optional non-English titles of a manifest domain or topic (<c>titles</c>); English stays in <c>title</c>.</summary>
public sealed record ManifestTitles(string? Uk);
