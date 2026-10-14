namespace Spine.Domain.Content;

public enum QuestionType
{
    Mcq,
    Open,
}

public static class QuestionTypeExtensions
{
    public static string ToDbValue(this QuestionType type) => type switch
    {
        QuestionType.Mcq => "mcq",
        QuestionType.Open => "open",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };

    public static QuestionType ParseQuestionType(string value) => value switch
    {
        "mcq" => QuestionType.Mcq,
        "open" => QuestionType.Open,
        _ => throw new ArgumentException($"Unknown question type '{value}'.", nameof(value)),
    };
}
