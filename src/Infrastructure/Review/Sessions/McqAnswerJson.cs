using System.Text.Json;

namespace Spine.Infrastructure.Review.Sessions;

/// <summary>The JSON form of <see cref="McqAnswer"/> in <c>attempts.answer</c>.</summary>
public static class McqAnswerJson
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static string Serialize(McqAnswer answer) => JsonSerializer.Serialize(answer, Json);

    public static McqAnswer Parse(string json) =>
        JsonSerializer.Deserialize<McqAnswer>(json, Json)
        ?? throw new InvalidOperationException("Attempt answer is not an MCQ answer.");
}
