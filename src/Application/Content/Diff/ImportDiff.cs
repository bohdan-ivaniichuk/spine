using Spine.Application.Content.Plan;
using Spine.Application.Content.Summary;
using Spine.Domain.Content;
using DomainEntity = Spine.Domain.Content.Domain;

namespace Spine.Application.Content.Diff;

/// <summary>
/// Pure diff of the existing content graph against an <see cref="ImportPlan"/>. Unchanged entities are never
/// assigned to (so no UPDATE is emitted and no timestamp moves); changed ones are updated in place; absent ones are
/// soft-deprecated; reappearing ones are reactivated. Progress tables are not part of the graph and are untouched.
/// </summary>
public static class ImportDiff
{
    public static ImportChangeSet Compute(IReadOnlyCollection<DomainEntity> existing, ImportPlan plan, DateTimeOffset now)
    {
        var change = new ImportChangeSet();
        var byDomainSlug = existing.ToDictionary(d => d.Slug, StringComparer.Ordinal);
        var seenDomains = new HashSet<string>(StringComparer.Ordinal);

        foreach (var pd in plan.Domains)
        {
            seenDomains.Add(pd.Slug);
            var counts = change.Summary.For(ImportPlan.ManifestFile, EntityKinds.Domain);
            if (!byDomainSlug.TryGetValue(pd.Slug, out var domain))
            {
                domain = new DomainEntity { Slug = pd.Slug, Title = pd.Title, TitleUk = pd.TitleUk, SortOrder = pd.SortOrder, ContentHash = pd.Hash, CreatedAt = now, UpdatedAt = now };
                change.NewDomains.Add(domain);
                counts.Created++;
            }
            else if (domain.ContentHash != pd.Hash || domain.DeprecatedAt is not null)
            {
                domain.Title = pd.Title;
                domain.TitleUk = pd.TitleUk;
                domain.SortOrder = pd.SortOrder;
                domain.ContentHash = pd.Hash;
                domain.DeprecatedAt = null;
                domain.UpdatedAt = now;
                counts.Updated++;
            }
            else
            {
                counts.Unchanged++;
            }

            DiffTopics(domain, pd, now, change);
        }

        foreach (var domain in existing.Where(d => !seenDomains.Contains(d.Slug)))
        {
            ImportDeprecation.Deprecate(domain, now, change);
        }

        return change;
    }

    private static void DiffTopics(DomainEntity domain, PlanDomain pd, DateTimeOffset now, ImportChangeSet change)
    {
        var bySlug = domain.Topics.ToDictionary(t => t.Slug, StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var pt in pd.Topics)
        {
            seen.Add(pt.Slug);
            var counts = change.Summary.For(ImportPlan.ManifestFile, EntityKinds.Topic);
            if (!bySlug.TryGetValue(pt.Slug, out var topic))
            {
                topic = new Topic { Slug = pt.Slug, Title = pt.Title, TitleUk = pt.TitleUk, SortOrder = pt.SortOrder, ContentHash = pt.Hash, CreatedAt = now, UpdatedAt = now };
                domain.Topics.Add(topic);
                counts.Created++;
            }
            else if (topic.ContentHash != pt.Hash || topic.DeprecatedAt is not null)
            {
                topic.Title = pt.Title;
                topic.TitleUk = pt.TitleUk;
                topic.SortOrder = pt.SortOrder;
                topic.ContentHash = pt.Hash;
                topic.DeprecatedAt = null;
                topic.UpdatedAt = now;
                counts.Updated++;
            }
            else
            {
                counts.Unchanged++;
            }

            if (pt.File is not null)
            {
                ConceptDiff.Apply(topic, pt, now, change);
            }
            else
            {
                // Listed in the manifest but no seed file: its concepts are absent from the seed set (FR-024).
                var file = $"{pd.Slug}/{pt.Slug}.json";
                foreach (var concept in topic.Concepts)
                {
                    ImportDeprecation.Deprecate(concept, file, now, change);
                }
            }
        }

        foreach (var topic in domain.Topics.Where(t => !seen.Contains(t.Slug)))
        {
            ImportDeprecation.Deprecate(topic, now, change);
        }
    }
}
