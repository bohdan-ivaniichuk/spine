using System.Text.Json;
using System.Text.Json.Serialization;

namespace Spine.Application.Content.Seed;

public sealed record KataTranslationSeed(string Task, IReadOnlyList<string> Rubric, string ReferenceSolution);
