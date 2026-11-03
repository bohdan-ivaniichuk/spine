using Spine.Application.Review.GetReviewCoverage;
using Spine.Application.Review.Shared;
using Spine.Application.Review;
using Spine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Spine.Infrastructure.Review.GetReviewCoverage;

/// <summary>EF implementation of <see cref="IGetReviewCoverageUseCase"/>: topics in manifest order plus one grouped count.</summary>
public sealed class GetReviewCoverageUseCase(AppDbContext db, TimeProvider clock) : IGetReviewCoverageUseCase
{
    private static readonly ReviewCoverageMapper Mapper = new();

    public async Task<ReviewCoverageDto> ExecuteAsync(CancellationToken ct)
    {
        var topics = await LoadTopicsAsync(ct);
        var counts = await CountByTopicAsync(clock.GetUtcNow(), ct);
        return Mapper.ToCoverage(topics, counts);
    }

    private Task<List<TopicCoverageRow>> LoadTopicsAsync(CancellationToken ct) =>
        db.Topics.AsNoTracking()
            .Where(t => t.DeprecatedAt == null && t.Domain!.DeprecatedAt == null)
            .OrderBy(t => t.Domain!.SortOrder).ThenBy(t => t.Domain!.Id).ThenBy(t => t.SortOrder).ThenBy(t => t.Id)
            .Select(t => new TopicCoverageRow(t.Id, t.Domain!.Slug, t.Slug))
            .ToListAsync(ct);

    /// <summary>One grouped query: eligible questions left-joined to the learner's review state (unique per item).</summary>
    private Task<Dictionary<int, TopicCoverageCounts>> CountByTopicAsync(DateTimeOffset now, CancellationToken ct) =>
        (from q in db.Eligible(ReviewScope.All)
         join r in db.QuestionStates() on q.Id equals r.QuestionId into states
         from r in states.DefaultIfEmpty()
         select new { q.Concept!.TopicId, Seen = r != null, Due = r != null && r.DueAt <= now })
        .GroupBy(x => x.TopicId)
        .Select(g => new TopicCoverageCounts(g.Key, g.Count(), g.Count(x => x.Seen), g.Count(x => x.Due)))
        .ToDictionaryAsync(x => x.TopicId, ct);
}
