using Spine.Application.Content.Plan;
using Spine.Application.Content.Summary;
using Spine.Domain.Content;
using DomainEntity = Spine.Domain.Content.Domain;

namespace Spine.Application.Content.Diff;

/// <summary>Soft-deprecation of an absent entity and everything under it; already deprecated ones are not counted again.</summary>
internal static class ImportDeprecation
{
    public static void Deprecate(DomainEntity domain, DateTimeOffset now, ImportChangeSet change)
    {
        if (domain.DeprecatedAt is null)
        {
            domain.DeprecatedAt = now;
            domain.UpdatedAt = now;
            change.Summary.For(ImportPlan.ManifestFile, EntityKinds.Domain).Deprecated++;
        }

        foreach (var topic in domain.Topics)
        {
            Deprecate(topic, now, change);
        }
    }

    public static void Deprecate(Topic topic, DateTimeOffset now, ImportChangeSet change)
    {
        if (topic.DeprecatedAt is null)
        {
            topic.DeprecatedAt = now;
            topic.UpdatedAt = now;
            change.Summary.For(ImportPlan.ManifestFile, EntityKinds.Topic).Deprecated++;
        }

        var file = $"{topic.Domain?.Slug ?? "?"}/{topic.Slug}.json";
        foreach (var concept in topic.Concepts)
        {
            Deprecate(concept, file, now, change);
        }
    }

    public static void Deprecate(Concept concept, string file, DateTimeOffset now, ImportChangeSet change)
    {
        if (concept.DeprecatedAt is null)
        {
            concept.DeprecatedAt = now;
            concept.UpdatedAt = now;
            change.Summary.For(file, EntityKinds.Concept).Deprecated++;
        }

        foreach (var question in concept.Questions.Where(q => q.DeprecatedAt is null))
        {
            question.DeprecatedAt = now;
            question.UpdatedAt = now;
            change.Summary.For(file, EntityKinds.Question).Deprecated++;
        }

        foreach (var kata in concept.Katas.Where(k => k.DeprecatedAt is null))
        {
            kata.DeprecatedAt = now;
            kata.UpdatedAt = now;
            change.Summary.For(file, EntityKinds.Kata).Deprecated++;
        }
    }
}
