using Spine.Application.Content.Plan;
using Spine.Application.Content.Summary;
using Spine.Domain.Content;

namespace Spine.Application.Content.Diff;

/// <summary>Katas of one concept with their translations.</summary>
internal static class KataDiff
{
    public static void Apply(Concept concept, PlanConcept pc, string file, DateTimeOffset now, ImportChangeSet change)
    {
        var byId = concept.Katas.ToDictionary(k => k.Id, StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var counts = change.Summary.For(file, EntityKinds.Kata);

        foreach (var pk in pc.Katas)
        {
            seen.Add(pk.Id);
            if (!byId.TryGetValue(pk.Id, out var kata))
            {
                kata = new Kata { Id = pk.Id, Title = pk.Title, Language = pk.Language, Reviewed = pk.Reviewed, TimeLimitMinutes = pk.TimeLimitMinutes, SortOrder = pk.SortOrder, ContentHash = pk.Hash, CreatedAt = now, UpdatedAt = now };
                concept.Katas.Add(kata);
                counts.Created++;
            }
            else if (kata.ContentHash != pk.Hash || kata.DeprecatedAt is not null)
            {
                kata.Title = pk.Title;
                kata.Language = pk.Language;
                kata.Reviewed = pk.Reviewed;
                kata.TimeLimitMinutes = pk.TimeLimitMinutes;
                kata.SortOrder = pk.SortOrder;
                kata.ContentHash = pk.Hash;
                kata.DeprecatedAt = null;
                kata.UpdatedAt = now;
                counts.Updated++;
            }
            else
            {
                counts.Unchanged++;
            }

            var tcounts = change.Summary.For(file, EntityKinds.Translation);
            foreach (var (language, pt) in pk.Translations)
            {
                var existing = kata.Translations.FirstOrDefault(t => t.Language == language);
                if (existing is null)
                {
                    kata.Translations.Add(new KataTranslation { KataId = kata.Id, Language = language, Task = pt.Task, Rubric = pt.Rubric, ReferenceSolution = pt.ReferenceSolution, ContentHash = pt.Hash, CreatedAt = now, UpdatedAt = now });
                    tcounts.Created++;
                }
                else if (existing.ContentHash != pt.Hash)
                {
                    existing.Task = pt.Task;
                    existing.Rubric = pt.Rubric;
                    existing.ReferenceSolution = pt.ReferenceSolution;
                    existing.ContentHash = pt.Hash;
                    existing.UpdatedAt = now;
                    tcounts.Updated++;
                }
                else
                {
                    tcounts.Unchanged++;
                }
            }
        }

        foreach (var kata in concept.Katas.Where(k => !seen.Contains(k.Id) && k.DeprecatedAt is null))
        {
            kata.DeprecatedAt = now;
            kata.UpdatedAt = now;
            counts.Deprecated++;
        }
    }
}
