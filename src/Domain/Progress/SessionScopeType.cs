namespace InterviewPrep.Domain.Progress;

public enum SessionScopeType
{
    All,
    Domain,
    Topic,
}

public static class SessionScopeTypeExtensions
{
    public static string ToDbValue(this SessionScopeType type) => type switch
    {
        SessionScopeType.All => "all",
        SessionScopeType.Domain => "domain",
        SessionScopeType.Topic => "topic",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };

    public static SessionScopeType ParseSessionScopeType(string value) => value switch
    {
        "all" => SessionScopeType.All,
        "domain" => SessionScopeType.Domain,
        "topic" => SessionScopeType.Topic,
        _ => throw new ArgumentException($"Unknown session scope type '{value}'.", nameof(value)),
    };
}
