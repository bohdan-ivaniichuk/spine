using Spine.Application.Review;
using Spine.Application.Review.GetReviewCoverage;
using Spine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Spine.Infrastructure.Review.GetReviewCoverage;

/// <summary>EF implementation of <see cref="IGetReviewCoverageUseCase"/>: topics in manifest order plus one grouped count.</summary>
public sealed class GetReviewCoverageUseCase(AppDbContext db, TimeProvider clock) : IGetReviewCoverageUseCase
{
    public async Task<ReviewCoverageDto> ExecuteAsync(CancellationToken ct)
    {
        var now = clock.GetUtcNow();

        var topics = await db.Topics.AsNoTracking()
            .Where(t => t.DeprecatedAt == null && t.Domain!.DeprecatedAt == null)
            .OrderBy(t => t.Domain!.SortOrder).ThenBy(t => t.Domain!.Id).ThenBy(t => t.SortOrder).ThenBy(t => t.Id)
            .Select(t => new { t.Id, DomainSlug = t.Domain!.Slug, t.Slug })
            .ToListAsync(ct);

        // One grouped query: eligible questions left-joined to the learner's review state (unique per item).
        var counts = await (
            from q in db.Eligible(ReviewScope.All)
            join r in db.QuestionStates() on q.Id equals r.QuestionId into states
            from r in states.DefaultIfEmpty()
            select new { q.Concept!.TopicId, Seen = r != null, Due = r != null && r.DueAt <= now })
            .GroupBy(x => x.TopicId)
            .Select(g => new { TopicId = g.Key, Eligible = g.Count(), Seen = g.Count(x => x.Seen), Due = g.Count(x => x.Due) })
            .ToDictionaryAsync(x => x.TopicId, ct);

        return new ReviewCoverageDto(topics
            .Select(t => counts.TryGetValue(t.Id, out var c)
                ? new TopicCoverageDto(t.DomainSlug, t.Slug, c.Eligible, c.Seen, c.Due)
                : new TopicCoverageDto(t.DomainSlug, t.Slug, 0, 0, 0))
            .ToList());
    }
}
