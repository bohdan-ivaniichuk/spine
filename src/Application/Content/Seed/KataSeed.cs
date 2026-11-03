using System.Text.Json;
using System.Text.Json.Serialization;

namespace Spine.Application.Content.Seed;

public sealed record KataSeed(
    string Id,
    string Title,
    string Language,
    bool Reviewed,
    IReadOnlyDictionary<string, KataTranslationSeed> Translations,
    int TimeLimitMinutes);
