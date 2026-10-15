using Spine.Application.Review;
using Spine.Domain.Content;
using Spine.Domain.Progress;
using Spine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Spine.Infrastructure.Review;

/// <summary>
/// The queries sessions and readiness must agree on. Eligible (schedulable) = non-deprecated MCQ whose concept, topic
/// and domain are not deprecated; when later features schedule more question types, both widen together.
/// </summary>
internal static class ReviewQueryables
{
    public static IQueryable<Question> Eligible(this AppDbContext db, ReviewScope scope)
    {
        var query = db.Questions.AsNoTracking().Where(q =>
            q.Type == QuestionType.Mcq
            && q.DeprecatedAt == null
            && q.Concept!.DeprecatedAt == null
            && q.Concept.Topic!.DeprecatedAt == null
            && q.Concept.Topic.Domain!.DeprecatedAt == null);
        if (scope.Domain is { } domain)
        {
            query = query.Where(q => q.Concept!.Topic!.Domain!.Slug == domain);
        }

        if (scope.Topic is { } topic)
        {
            query = query.Where(q => q.Concept!.Topic!.Slug == topic);
        }

        if (scope.Concept is { } concept)
        {
            query = query.Where(q => q.Concept!.Slug == concept);
        }

        return query;
    }

    /// <summary>The local learner's review states of questions.</summary>
    public static IQueryable<ReviewState> QuestionStates(this AppDbContext db)
    {
        var userId = LocalLearner.Id;
        return db.ReviewStates.AsNoTracking().Where(r => r.UserId == userId && r.ItemType == ItemType.Question);
    }
}
