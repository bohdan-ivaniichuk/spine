namespace Spine.Application.Review.Shared;

/// <summary>
/// Session-size defaults, bound from <c>REVIEW_DEFAULT_TOTAL_LIMIT</c> / <c>REVIEW_DEFAULT_NEW_LIMIT</c>. The overview
/// reports them so the client never hardcodes a number. The hard bounds are fixed.
/// </summary>
public sealed class ReviewOptions
{
    public const string TotalLimitKey = "REVIEW_DEFAULT_TOTAL_LIMIT";
    public const string NewLimitKey = "REVIEW_DEFAULT_NEW_LIMIT";
    public const int MinLimit = 1;
    public const int MaxLimit = 100;

    public int DefaultTotalLimit { get; set; } = 20;

    public int DefaultNewLimit { get; set; } = 10;

    public static bool IsWithinBounds(int limit) => limit is >= MinLimit and <= MaxLimit;
}
