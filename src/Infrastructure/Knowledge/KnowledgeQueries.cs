using InterviewPrep.Application.Knowledge;
using InterviewPrep.Domain.Content;
using InterviewPrep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InterviewPrep.Infrastructure.Knowledge;

/// <summary>
/// Tree = three flat no-tracking queries stitched in memory (no cartesian product, no N+1);
/// concept page = one query with sub-selects. English is the fallback language.
/// </summary>
public sealed class KnowledgeQueries(AppDbContext db) : IKnowledgeQueries
{
    public async Task<KnowledgeTreeDto> GetTreeAsync(Language language, bool includeDeprecated, CancellationToken ct = default)
    {
        var domains = await db.Domains.AsNoTracking()
            .Where(d => includeDeprecated || d.DeprecatedAt == null)
            .OrderBy(d => d.SortOrder)
            .Select(d => new { d.Id, d.Slug, d.Title, d.SortOrder, d.DeprecatedAt })
            .ToListAsync(ct);

        var topics = await db.Topics.AsNoTracking()
            .Where(t => includeDeprecated || t.DeprecatedAt == null)
            .OrderBy(t => t.SortOrder)
            .Select(t => new { t.Id, t.DomainId, t.Slug, t.Title, t.SortOrder, t.DeprecatedAt })
            .ToListAsync(ct);

        var concepts = await db.Concepts.AsNoTracking()
            .Where(c => includeDeprecated || c.DeprecatedAt == null)
            .OrderBy(c => c.SortOrder)
            .Select(c => new
            {
                c.Id,
                c.TopicId,
                c.Slug,
                c.Level,
                c.Reviewed,
                c.SortOrder,
                c.DeprecatedAt,
                TitleRequested = c.Translations.Where(t => t.Language == language).Select(t => t.Title).FirstOrDefault(),
                TitleEn = c.Translations.Where(t => t.Language == Language.En).Select(t => t.Title).FirstOrDefault(),
                QuestionCount = c.Questions.Count(q => includeDeprecated || q.DeprecatedAt == null),
                KataCount = c.Katas.Count(k => includeDeprecated || k.DeprecatedAt == null),
            })
            .ToListAsync(ct);

        var conceptsByTopic = concepts.ToLookup(c => c.TopicId);
        var topicsByDomain = topics.ToLookup(t => t.DomainId);

        var tree = domains.Select(d => new TreeDomainDto(
            d.Slug, d.Title, d.SortOrder, d.DeprecatedAt,
            topicsByDomain[d.Id].Select(t => new TreeTopicDto(
                t.Slug, t.Title, t.SortOrder, t.DeprecatedAt,
                conceptsByTopic[t.Id].Select(c => new TreeConceptDto(
                    c.Slug,
                    c.TitleRequested ?? c.TitleEn ?? c.Slug,
                    c.TitleRequested is null,
                    c.Level.ToDbValue(),
                    c.Reviewed,
                    c.SortOrder,
                    c.QuestionCount,
                    c.KataCount,
                    c.DeprecatedAt)).ToList())).ToList())).ToList();

        return new KnowledgeTreeDto(language.ToDbValue(), tree);
    }

    public async Task<ConceptPageDto?> GetConceptAsync(string domainSlug, string topicSlug, string conceptSlug, Language language, bool includeDeprecated, CancellationToken ct = default)
    {
        var row = await db.Concepts.AsNoTracking()
            .Where(c => c.Slug == conceptSlug && c.Topic!.Slug == topicSlug && c.Topic.Domain!.Slug == domainSlug)
            .Where(c => includeDeprecated || (c.DeprecatedAt == null && c.Topic!.DeprecatedAt == null && c.Topic.Domain!.DeprecatedAt == null))
            .Select(c => new
            {
                DomainSlug = c.Topic!.Domain!.Slug,
                DomainTitle = c.Topic.Domain.Title,
                TopicSlug = c.Topic.Slug,
                TopicTitle = c.Topic.Title,
                c.Slug,
                c.Level,
                c.Reviewed,
                c.SortOrder,
                c.DeprecatedAt,
                Requested = c.Translations.Where(t => t.Language == language).Select(t => new { t.Title, t.Note }).FirstOrDefault(),
                En = c.Translations.Where(t => t.Language == Language.En).Select(t => new { t.Title, t.Note }).FirstOrDefault(),
                QuestionCount = c.Questions.Count(q => includeDeprecated || q.DeprecatedAt == null),
                KataCount = c.Katas.Count(k => includeDeprecated || k.DeprecatedAt == null),
            })
            .SingleOrDefaultAsync(ct);

        if (row is null)
        {
            return null;
        }

        var fallback = row.Requested is null;
        var text = row.Requested ?? row.En;
        return new ConceptPageDto(
            new ConceptRefDto(row.DomainSlug, row.DomainTitle),
            new ConceptRefDto(row.TopicSlug, row.TopicTitle),
            row.Slug,
            row.Level.ToDbValue(),
            row.Reviewed,
            row.SortOrder,
            language.ToDbValue(),
            fallback ? Language.En.ToDbValue() : language.ToDbValue(),
            fallback,
            text?.Title ?? row.Slug,
            text?.Note ?? string.Empty,
            row.QuestionCount,
            row.KataCount,
            row.DeprecatedAt);
    }
}
