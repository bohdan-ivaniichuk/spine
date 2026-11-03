using System.Text.Json;
using System.Text.Json.Serialization;

namespace Spine.Application.Content.Seed;

public sealed record McqOptionSeed(string Text, bool Correct);
