using Spine.Application.Content.Plan;
using Spine.Application.Content.Summary;
using Spine.Domain.Content;

namespace Spine.Application.Content.Diff;

/// <summary>Concepts of one topic file and their translations; questions and katas go to their own diffs.</summary>
internal static class ConceptDiff
{
    public static void Apply(Topic topic, PlanTopic pt, DateTimeOffset now, ImportChangeSet change)
    {
        var file = pt.File!;
        var bySlug = topic.Concepts.ToDictionary(c => c.Slug, StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var pc in pt.Concepts)
        {
            seen.Add(pc.Slug);
            var counts = change.Summary.For(file, EntityKinds.Concept);
            if (!bySlug.TryGetValue(pc.Slug, out var concept))
            {
                concept = new Concept { Slug = pc.Slug, Level = pc.Level, Reviewed = pc.Reviewed, SortOrder = pc.SortOrder, ContentHash = pc.Hash, CreatedAt = now, UpdatedAt = now };
                topic.Concepts.Add(concept);
                counts.Created++;
            }
            else if (concept.ContentHash != pc.Hash || concept.DeprecatedAt is not null)
            {
                concept.Level = pc.Level;
                concept.Reviewed = pc.Reviewed;
                concept.SortOrder = pc.SortOrder;
                concept.ContentHash = pc.Hash;
                concept.DeprecatedAt = null;
                concept.UpdatedAt = now;
                counts.Updated++;
            }
            else
            {
                counts.Unchanged++;
            }

            ApplyTranslations(concept, pc, file, now, change);
            QuestionDiff.Apply(concept, pc, file, now, change);
            KataDiff.Apply(concept, pc, file, now, change);
        }

        foreach (var concept in topic.Concepts.Where(c => !seen.Contains(c.Slug)))
        {
            ImportDeprecation.Deprecate(concept, file, now, change);
        }
    }

    private static void ApplyTranslations(Concept concept, PlanConcept pc, string file, DateTimeOffset now, ImportChangeSet change)
    {
        var counts = change.Summary.For(file, EntityKinds.Translation);
        foreach (var (language, pt) in pc.Translations)
        {
            var existing = concept.Translations.FirstOrDefault(t => t.Language == language);
            if (existing is null)
            {
                concept.Translations.Add(new ConceptTranslation { Language = language, Title = pt.Title, Note = pt.Note, ContentHash = pt.Hash, CreatedAt = now, UpdatedAt = now });
                counts.Created++;
            }
            else if (existing.ContentHash != pt.Hash)
            {
                existing.Title = pt.Title;
                existing.Note = pt.Note;
                existing.ContentHash = pt.Hash;
                existing.UpdatedAt = now;
                counts.Updated++;
            }
            else
            {
                counts.Unchanged++;
            }
        }
    }
}
