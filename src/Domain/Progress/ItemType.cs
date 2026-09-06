namespace InterviewPrep.Domain.Progress;

/// <summary>Which kind of content item a review state or attempt refers to.</summary>
public enum ItemType
{
    Question,
    Kata,
}

public static class ItemTypeExtensions
{
    public static string ToDbValue(this ItemType type) => type switch
    {
        ItemType.Question => "question",
        ItemType.Kata => "kata",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };

    public static ItemType ParseItemType(string value) => value switch
    {
        "question" => ItemType.Question,
        "kata" => ItemType.Kata,
        _ => throw new ArgumentException($"Unknown item type '{value}'.", nameof(value)),
    };
}
