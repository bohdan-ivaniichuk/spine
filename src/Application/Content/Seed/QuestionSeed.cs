using System.Text.Json;
using System.Text.Json.Serialization;

namespace Spine.Application.Content.Seed;

public sealed record QuestionSeed(
    string Id,
    string Type,
    string Level,
    bool Reviewed,
    IReadOnlyDictionary<string, QuestionTranslationSeed> Translations,
    IReadOnlyList<string> Tags);
