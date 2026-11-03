namespace Spine.Application.Review.Shared;

public sealed class ReviewException(ReviewError kind, string detail) : Exception(detail)
{
    public ReviewError Kind { get; } = kind;

    public string Detail { get; } = detail;
}
