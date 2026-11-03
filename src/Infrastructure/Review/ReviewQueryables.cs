using Spine.Application.Review.Shared;
using Spine.Application.Review;
using Spine.Domain.Content;
using Spine.Domain.Progress;
using Spine.Infrastructure.Persistence;
using Spine.Infrastructure.Review.Readiness;
using Spine.Infrastructure.Review.WeakSpots;
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

    /// <summary>
    /// One grouped query: per concept, its schedulable questions left-joined to the learner's review state (unique per
    /// item) — how many there are, how many have a state, how many are ready under <see cref="ReadinessRule"/>.
    /// </summary>
    public static IQueryable<ConceptReadinessRow> ConceptReadinessCounts(this AppDbContext db) =>
        (from q in db.Eligible(ReviewScope.All)
         join r in db.QuestionStates() on q.Id equals r.QuestionId into states
         from r in states.DefaultIfEmpty()
         select new { q.ConceptId, Stated = r != null, Ready = r != null && r.IntervalDays >= ReadinessRule.MinIntervalDays })
        .GroupBy(x => x.ConceptId)
        .Select(g => new ConceptReadinessRow(g.Key, g.Count(), g.Count(x => x.Stated), g.Count(x => x.Ready)));

    /// <summary>
    /// Schedulable questions in scope whose most recent answer by the local learner was wrong (feature 006), with the
    /// time of that wrong answer. "Most recent" = no later attempt on the same question; a correct answer at the very
    /// same instant would not clear it, which a single learner cannot produce.
    /// </summary>
    public static IQueryable<WeakQuestionRow> WeakQuestions(this AppDbContext db, ReviewScope scope)
    {
        var userId = LocalLearner.Id;
        return (from q in db.Eligible(scope)
                join a in db.Attempts.AsNoTracking() on q.Id equals a.QuestionId
                where a.UserId == userId && a.ItemType == ItemType.Question && a.IsCorrect == false
                      // item_id (non-null, generated) lets the probe use ix_attempts_user_id_item_id_answered_at.
                      && !db.Attempts.Any(b => b.UserId == userId && b.ItemType == ItemType.Question && b.ItemId == a.ItemId && b.AnsweredAt > a.AnsweredAt)
                select new { q.Id, q.ConceptId, a.AnsweredAt })
            .GroupBy(x => new { x.Id, x.ConceptId })
            .Select(g => new WeakQuestionRow { QuestionId = g.Key.Id, ConceptId = g.Key.ConceptId, MissedAt = g.Max(x => x.AnsweredAt) });
    }

    /// <summary>Due dates of every eligible question in scope that has a review state.</summary>
    public static IQueryable<DateTimeOffset> DueAtOf(this AppDbContext db, ReviewScope scope) =>
        from q in db.Eligible(scope)
        join r in db.QuestionStates() on q.Id equals r.QuestionId
        select r.DueAt;

    /// <summary>Due now, most overdue first, id as a deterministic tie-break.</summary>
    public static IQueryable<string> DueIds(this AppDbContext db, ReviewScope scope, DateTimeOffset now) =>
        from q in db.Eligible(scope)
        join r in db.QuestionStates() on q.Id equals r.QuestionId
        where r.DueAt <= now
        orderby r.DueAt, q.Id
        select q.Id;

    /// <summary>Never answered, in taxonomy order.</summary>
    public static IQueryable<string> NewIds(this AppDbContext db, ReviewScope scope)
    {
        var userId = LocalLearner.Id;
        return db.Eligible(scope)
            .Where(q => !db.ReviewStates.Any(r => r.UserId == userId && r.QuestionId == q.Id))
            .OrderBy(q => q.Concept!.Topic!.Domain!.SortOrder)
            .ThenBy(q => q.Concept!.Topic!.SortOrder)
            .ThenBy(q => q.Concept!.SortOrder)
            .ThenBy(q => q.SortOrder)
            .ThenBy(q => q.Id)
            .Select(q => q.Id);
    }

    /// <summary>The local learner's review states of questions.</summary>
    public static IQueryable<ReviewState> QuestionStates(this AppDbContext db)
    {
        var userId = LocalLearner.Id;
        return db.ReviewStates.AsNoTracking().Where(r => r.UserId == userId && r.ItemType == ItemType.Question);
    }
}
