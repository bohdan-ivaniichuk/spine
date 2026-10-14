namespace Spine.Application.Review;

/// <summary>Why a review use case refused to proceed. The API maps each kind to an HTTP status; Application knows no HTTP.</summary>
public enum ReviewError
{
    ScopeNotFound,
    NothingToReview,
    SessionNotFound,
    SessionFinished,
    QuestionNotInSession,
    DuplicateAnswer,
    QuestionDeprecated,
}

public sealed class ReviewException(ReviewError kind, string detail) : Exception(detail)
{
    public ReviewError Kind { get; } = kind;

    public string Detail { get; } = detail;
}
