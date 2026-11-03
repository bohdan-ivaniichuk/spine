using Spine.Application.Review.Readiness;
using Spine.Domain.Progress;
using Spine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Spine.Infrastructure.Review.Readiness;

/// <summary>
/// Constant number of queries regardless of content size: the live domains, topics and concepts in manifest order,
/// and one grouped count of schedulable questions left-joined to the learner's review states. The math lives in
/// <see cref="ReadinessCalculator"/>; the SQL repeats <see cref="ReadinessRule.MinIntervalDays"/>.
/// </summary>
public sealed class ReadinessQueries(AppDbContext db) : IReadinessQueries
{
    public async Task<ReadinessDto> GetAsync(CancellationToken ct = default)
    {
        var domains = await db.Domains.AsNoTracking()
            .Where(d => d.DeprecatedAt == null)
            .OrderBy(d => d.SortOrder).ThenBy(d => d.Id)
            .Select(d => new { d.Id, d.Slug })
            .ToListAsync(ct);

        var topics = await db.Topics.AsNoTracking()
            .Where(t => t.DeprecatedAt == null && t.Domain!.DeprecatedAt == null)
            .OrderBy(t => t.SortOrder).ThenBy(t => t.Id)
            .Select(t => new { t.Id, t.DomainId, t.Slug })
            .ToListAsync(ct);

        var concepts = await db.Concepts.AsNoTracking()
            .Where(c => c.DeprecatedAt == null && c.Topic!.DeprecatedAt == null && c.Topic.Domain!.DeprecatedAt == null)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Id)
            .Select(c => new { c.Id, c.TopicId, c.Slug })
            .ToListAsync(ct);

        var counts = await db.ConceptReadinessCounts().ToDictionaryAsync(x => x.ConceptId, ct);

        var conceptsByTopic = concepts.ToLookup(c => c.TopicId);
        var topicsByDomain = topics.ToLookup(t => t.DomainId);

        return ReadinessCalculator.Build(domains
            .Select(d => new DomainCounts(d.Slug, topicsByDomain[d.Id]
                .Select(t => new TopicCounts(t.Slug, conceptsByTopic[t.Id]
                    .Select(c => counts.TryGetValue(c.Id, out var n)
                        ? new ConceptCounts(c.Slug, n.Questions, n.Stated, n.Ready)
                        : new ConceptCounts(c.Slug, 0, 0, 0))
                    .ToList()))
                .ToList()))
            .ToList());
    }
}
