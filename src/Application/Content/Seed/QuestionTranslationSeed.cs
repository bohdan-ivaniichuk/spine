using System.Text.Json;
using System.Text.Json.Serialization;

namespace Spine.Application.Content.Seed;

public sealed record QuestionTranslationSeed(
    string Prompt,
    IReadOnlyList<McqOptionSeed>? Options,
    string Explanation,
    IReadOnlyList<string>? Rubric);
