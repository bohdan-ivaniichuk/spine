namespace InterviewPrep.Domain.Content;

/// <summary>Target interview level of a concept or question.</summary>
public enum Level
{
    Mid,
    Senior,
    Lead,
}

public static class LevelExtensions
{
    public static string ToDbValue(this Level level) => level switch
    {
        Level.Mid => "M",
        Level.Senior => "S",
        Level.Lead => "L",
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, null),
    };

    public static Level ParseLevel(string value) => value switch
    {
        "M" => Level.Mid,
        "S" => Level.Senior,
        "L" => Level.Lead,
        _ => throw new ArgumentException($"Unknown level '{value}'.", nameof(value)),
    };
}
