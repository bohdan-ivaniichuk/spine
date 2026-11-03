using Spine.Application.Review.Readiness;
using Spine.Application.Review.Shared;
using Spine.Application.Review.WeakSpots;
using Spine.Application.Review;
using Spine.Domain.Progress;
using Spine.Infrastructure.Persistence;
using Spine.Infrastructure.Review.Readiness;
using Microsoft.EntityFrameworkCore;

namespace Spine.Infrastructure.Review.WeakSpots;

/// <summary>
/// Constant number of queries: answers in the window per concept, current weak questions per concept, the slugs of the
/// answered concepts, and readiness counts. The ranking lives in <see cref="WeaknessRanker"/>; only schedulable
/// (non-deprecated) questions count, through the shared <see cref="ReviewQueryables.Eligible"/> filter.
/// </summary>
public sealed class WeakSpotsQueries(AppDbContext db, TimeProvider clock) : IWeakSpotsQueries
{
    public async Task<WeakSpotsDto> GetAsync(CancellationToken ct = default)
    {
        // Rounded up to a whole minute, so the same request within a minute returns the same payload and no answer older
        // than the window ever counts (the window is at most a minute shorter).
        var now = clock.GetUtcNow();
        var minute = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, now.Offset);
        var since = (minute == now ? minute : minute.AddMinutes(1)).AddDays(-WeaknessRule.WindowDays);
        var userId = LocalLearner.Id;

        var answered = await (
            from a in db.Attempts.AsNoTracking()
            join q in db.Eligible(ReviewScope.All) on a.QuestionId equals q.Id
            where a.UserId == userId && a.ItemType == ItemType.Question && a.AnsweredAt >= since
            select new { q.ConceptId, a.IsCorrect, a.AnsweredAt })
            .GroupBy(x => x.ConceptId)
            .Select(g => new { ConceptId = g.Key, Answers = g.Count(), Wrong = g.Count(x => x.IsCorrect == false), Last = g.Max(x => x.AnsweredAt) })
            .ToListAsync(ct);

        var weak = await db.WeakQuestions(ReviewScope.All)
            .GroupBy(w => w.ConceptId)
            .Select(g => new { ConceptId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ConceptId, x => x.Count, ct);

        // Concepts with a weak question but no answer in the window still add to their topic's weak count.
        var ids = answered.Select(a => a.ConceptId).Union(weak.Keys).ToList();
        var slugs = await db.Concepts.AsNoTracking()
            .Where(c => ids.Contains(c.Id))
            .Select(c => new { c.Id, Concept = c.Slug, Topic = c.Topic!.Slug, Domain = c.Topic.Domain!.Slug })
            .ToDictionaryAsync(c => c.Id, ct);

        var answeredById = answered.ToDictionary(a => a.ConceptId);
        var stats = ids
            .Where(slugs.ContainsKey)
            .Select(id => new ConceptAnswerStats(
                slugs[id].Domain,
                slugs[id].Topic,
                slugs[id].Concept,
                answeredById.TryGetValue(id, out var a) ? a.Answers : 0,
                a?.Wrong ?? 0,
                a?.Last ?? DateTimeOffset.MinValue,
                weak.GetValueOrDefault(id)))
            .ToList();
        var idOf = slugs.Values.ToDictionary(s => (s.Domain, s.Topic, s.Concept), s => s.Id);
        var ranking = WeaknessRanker.Rank(stats);

        var readiness = await db.ConceptReadinessCounts().ToDictionaryAsync(r => r.ConceptId, ct);

        return new WeakSpotsDto(
            since,
            WeaknessRule.WindowDays,
            WeaknessRule.MinAnswers,
            [.. ranking.Concepts.Select(c => new ConceptWeaknessDto(
                c.Domain,
                c.Topic,
                c.Concept,
                c.Answers,
                c.Wrong,
                WeaknessRanker.Accuracy(c.Answers, c.Wrong),
                c.LastAnsweredAt,
                c.WeakQuestions,
                ReadinessOf(c.Concept, readiness.GetValueOrDefault(idOf[(c.Domain, c.Topic, c.Concept)]))))],
            [.. ranking.Topics.Select(t => new TopicWeaknessDto(
                t.Domain,
                t.Topic,
                t.Answers,
                t.Wrong,
                WeaknessRanker.Accuracy(t.Answers, t.Wrong),
                t.LastAnsweredAt,
                t.WeakQuestions))]);
    }

    private static ConceptReadinessDto? ReadinessOf(string concept, ConceptReadinessRow? row)
    {
        var counts = new ConceptCounts(concept, row?.Questions ?? 0, row?.Stated ?? 0, row?.Ready ?? 0);
        return ReadinessCalculator.StateOf(counts) is { } state
            ? new ConceptReadinessDto(concept, state.ToWireValue(), counts.Questions, counts.Ready)
            : null;
    }
}
