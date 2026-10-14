using InterviewPrep.Application.Review;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.HttpResults;

namespace InterviewPrep.Api.Errors;

/// <summary>
/// The one place where <see cref="ReviewError"/> becomes an HTTP status. Writes RFC 7807 <c>application/problem+json</c>
/// with <c>instance</c> = request path; anything that is not a <see cref="ReviewException"/> falls through to the
/// default 500 problem.
/// </summary>
public sealed class ReviewExceptionHandler : IExceptionHandler
{
    public static (int Status, string Title) Map(ReviewError kind) => kind switch
    {
        ReviewError.ScopeNotFound => (StatusCodes.Status404NotFound, "Scope not found"),
        ReviewError.NothingToReview => (StatusCodes.Status409Conflict, "Nothing to review"),
        ReviewError.SessionNotFound => (StatusCodes.Status404NotFound, "Session not found"),
        ReviewError.SessionFinished => (StatusCodes.Status409Conflict, "Session already finished"),
        ReviewError.QuestionNotInSession => (StatusCodes.Status422UnprocessableEntity, "Question not in session"),
        ReviewError.DuplicateAnswer => (StatusCodes.Status409Conflict, "Question already answered"),
        ReviewError.QuestionDeprecated => (StatusCodes.Status422UnprocessableEntity, "Question deprecated"),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    public static ProblemHttpResult Problem(ReviewError kind, string detail, PathString instance)
    {
        var (status, title) = Map(kind);
        return TypedResults.Problem(title: title, detail: detail, statusCode: status, instance: instance);
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not ReviewException review)
        {
            return false;
        }

        await Problem(review.Kind, review.Detail, httpContext.Request.Path).ExecuteAsync(httpContext);
        return true;
    }
}
