using System.Text.Json;
using System.Text.Json.Serialization;

namespace Spine.Application.Content.Seed;

public sealed record ConceptSeed(
    string Slug,
    string Title,
    string Level,
    IReadOnlyDictionary<string, ConceptTranslationSeed> Translations,
    IReadOnlyList<QuestionSeed> Questions,
    IReadOnlyList<KataSeed>? Katas);
