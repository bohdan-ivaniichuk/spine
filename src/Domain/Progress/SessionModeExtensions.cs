namespace Spine.Domain.Progress;

public static class SessionModeExtensions
{
    public static string ToDbValue(this SessionMode mode) => mode switch
    {
        SessionMode.Regular => "regular",
        SessionMode.Weak => "weak",
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
    };

    public static SessionMode ParseSessionMode(string value) => value switch
    {
        "regular" => SessionMode.Regular,
        "weak" => SessionMode.Weak,
        _ => throw new ArgumentException($"Unknown session mode '{value}'.", nameof(value)),
    };
}
