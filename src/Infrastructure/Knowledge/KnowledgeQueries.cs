using Spine.Application.Knowledge;
using Spine.Application.Knowledge.Concept;
using Spine.Application.Knowledge.Tree;
using Spine.Domain.Content;
using Spine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Spine.Infrastructure.Knowledge;

/// <summary>
/// Tree = three flat no-tracking queries into rows, nested by <see cref="KnowledgeTreeMapper"/> (no cartesian product,
/// no N+1); concept page = one query into a row, shaped by <see cref="ConceptPageMapper"/>. English is the fallback.
/// </summary>
public sealed class KnowledgeQueries(AppDbContext db) : IKnowledgeQueries
{
    public async Task<KnowledgeTreeDto> GetTreeAsync(
        Language language, bool includeDeprecated, CancellationToken ct = default)
    {
        var domains = await LoadDomainsAsync(includeDeprecated, ct);
        var topics = await LoadTopicsAsync(includeDeprecated, ct);
        var concepts = await LoadConceptsAsync(language, includeDeprecated, ct);
        return new KnowledgeTreeMapper(language).ToTree(domains, topics, concepts);
    }

    public async Task<ConceptPageDto?> GetConceptAsync(
        string domainSlug,
        string topicSlug,
        string conceptSlug,
        Language language,
        bool includeDeprecated,
        CancellationToken ct = default)
    {
        var row = await db.Concepts.AsNoTracking()
            .Where(c => c.Slug == conceptSlug && c.Topic!.Slug == topicSlug && c.Topic.Domain!.Slug == domainSlug)
            .Where(c => includeDeprecated
                        || (c.DeprecatedAt == null
                            && c.Topic!.DeprecatedAt == null
                            && c.Topic.Domain!.DeprecatedAt == null))
            .Select(c => new ConceptPageRow(
                c.Topic!.Domain!.Slug,
                c.Topic.Domain.Title,
                c.Topic.Domain.TitleUk,
                c.Topic.Slug,
                c.Topic.Title,
                c.Topic.TitleUk,
                c.Slug,
                c.Level,
                c.Reviewed,
                c.SortOrder,
                c.DeprecatedAt,
                c.Translations
                    .Where(t => t.Language == language)
                    .Select(t => new ConceptTextRow(t.Title, t.Note))
                    .FirstOrDefault(),
                c.Translations
                    .Where(t => t.Language == Language.En)
                    .Select(t => new ConceptTextRow(t.Title, t.Note))
                    .FirstOrDefault(),
                c.Questions.Count(q => includeDeprecated || q.DeprecatedAt == null),
                c.Katas.Count(k => includeDeprecated || k.DeprecatedAt == null)))
            .SingleOrDefaultAsync(ct);

        return row is null ? null : new ConceptPageMapper(language).ToPage(row);
    }

    private Task<List<TreeDomainRow>> LoadDomainsAsync(bool includeDeprecated, CancellationToken ct) =>
        db.Domains.AsNoTracking()
            .Where(d => includeDeprecated || d.DeprecatedAt == null)
            .OrderBy(d => d.SortOrder).ThenBy(d => d.Id)
            .Select(d => new TreeDomainRow(d.Id, d.Slug, d.Title, d.TitleUk, d.SortOrder, d.DeprecatedAt))
            .ToListAsync(ct);

    private Task<List<TreeTopicRow>> LoadTopicsAsync(bool includeDeprecated, CancellationToken ct) =>
        db.Topics.AsNoTracking()
            .Where(t => includeDeprecated || t.DeprecatedAt == null)
            .OrderBy(t => t.SortOrder).ThenBy(t => t.Id)
            .Select(t => new TreeTopicRow(t.Id, t.DomainId, t.Slug, t.Title, t.TitleUk, t.SortOrder, t.DeprecatedAt))
            .ToListAsync(ct);

    private Task<List<TreeConceptRow>> LoadConceptsAsync(
        Language language, bool includeDeprecated, CancellationToken ct) =>
        db.Concepts.AsNoTracking()
            .Where(c => includeDeprecated || c.DeprecatedAt == null)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Id)
            .Select(c => new TreeConceptRow(
                c.Id,
                c.TopicId,
                c.Slug,
                c.Level,
                c.Reviewed,
                c.SortOrder,
                c.DeprecatedAt,
                c.Translations.Where(t => t.Language == language).Select(t => t.Title).FirstOrDefault(),
                c.Translations.Where(t => t.Language == Language.En).Select(t => t.Title).FirstOrDefault(),
                c.Questions.Count(q => includeDeprecated || q.DeprecatedAt == null),
                c.Katas.Count(k => includeDeprecated || k.DeprecatedAt == null)))
            .ToListAsync(ct);
}
