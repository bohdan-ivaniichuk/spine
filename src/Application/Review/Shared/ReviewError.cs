namespace Spine.Application.Review.Shared;

/// <summary>Why a review use case refused to proceed. The API maps each kind to an HTTP status; Application knows no HTTP.</summary>
public enum ReviewError
{
    ScopeNotFound,
    NothingToReview,
    NothingToDrill,
    SessionNotFound,
    SessionFinished,
    QuestionNotInSession,
    DuplicateAnswer,
    QuestionDeprecated,
}
