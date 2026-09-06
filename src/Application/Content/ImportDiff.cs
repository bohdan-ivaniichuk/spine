using InterviewPrep.Application.Content.Validation;
using InterviewPrep.Domain.Content;
using DomainEntity = InterviewPrep.Domain.Content.Domain;

namespace InterviewPrep.Application.Content;

/// <summary>Root additions and counts produced by <see cref="ImportDiff"/>; existing entities are mutated in place.</summary>
public sealed class ImportChangeSet
{
    public List<DomainEntity> NewDomains { get; } = [];
    public ImportSummary Summary { get; } = new();
    public List<ContentValidationError> Errors { get; } = [];
}

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
                domain = new DomainEntity { Slug = pd.Slug, Title = pd.Title, SortOrder = pd.SortOrder, ContentHash = pd.Hash, CreatedAt = now, UpdatedAt = now };
                change.NewDomains.Add(domain);
                counts.Created++;
            }
            else if (domain.ContentHash != pd.Hash || domain.DeprecatedAt is not null)
            {
                domain.Title = pd.Title;
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
            Deprecate(domain, now, change);
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
                topic = new Topic { Slug = pt.Slug, Title = pt.Title, SortOrder = pt.SortOrder, ContentHash = pt.Hash, CreatedAt = now, UpdatedAt = now };
                domain.Topics.Add(topic);
                counts.Created++;
            }
            else if (topic.ContentHash != pt.Hash || topic.DeprecatedAt is not null)
            {
                topic.Title = pt.Title;
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

            // A topic listed in the manifest but without a file keeps whatever concepts it already has.
            if (pt.File is not null)
            {
                DiffConcepts(topic, pt, now, change);
            }
        }

        foreach (var topic in domain.Topics.Where(t => !seen.Contains(t.Slug)))
        {
            Deprecate(topic, now, change);
        }
    }

    private static void DiffConcepts(Topic topic, PlanTopic pt, DateTimeOffset now, ImportChangeSet change)
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

            DiffConceptTranslations(concept, pc, file, now, change);
            DiffQuestions(concept, pc, file, now, change);
            DiffKatas(concept, pc, file, now, change);
        }

        foreach (var concept in topic.Concepts.Where(c => !seen.Contains(c.Slug)))
        {
            Deprecate(concept, file, now, change);
        }
    }

    private static void DiffConceptTranslations(Concept concept, PlanConcept pc, string file, DateTimeOffset now, ImportChangeSet change)
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

    private static void DiffQuestions(Concept concept, PlanConcept pc, string file, DateTimeOffset now, ImportChangeSet change)
    {
        var byId = concept.Questions.ToDictionary(q => q.Id, StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var counts = change.Summary.For(file, EntityKinds.Question);

        foreach (var pq in pc.Questions)
        {
            seen.Add(pq.Id);
            if (!byId.TryGetValue(pq.Id, out var question))
            {
                question = new Question { Id = pq.Id, Type = pq.Type, Level = pq.Level, Reviewed = pq.Reviewed, Tags = pq.Tags, SortOrder = pq.SortOrder, ContentHash = pq.Hash, CreatedAt = now, UpdatedAt = now };
                concept.Questions.Add(question);
                counts.Created++;
            }
            else if (question.Type != pq.Type)
            {
                change.Errors.Add(new ContentValidationError(file, $"/concepts/{pc.SortOrder}/questions/{pq.SortOrder}/type", "type-change",
                    $"question '{pq.Id}' already exists as '{question.Type.ToDbValue()}'; a type change requires a new id"));
                continue;
            }
            else if (question.ContentHash != pq.Hash || question.DeprecatedAt is not null)
            {
                question.Level = pq.Level;
                question.Reviewed = pq.Reviewed;
                question.Tags = pq.Tags;
                question.SortOrder = pq.SortOrder;
                question.ContentHash = pq.Hash;
                question.DeprecatedAt = null;
                question.UpdatedAt = now;
                counts.Updated++;
            }
            else
            {
                counts.Unchanged++;
            }

            var tcounts = change.Summary.For(file, EntityKinds.Translation);
            foreach (var (language, pt) in pq.Translations)
            {
                var existing = question.Translations.FirstOrDefault(t => t.Language == language);
                if (existing is null)
                {
                    question.Translations.Add(new QuestionTranslation { QuestionId = question.Id, Language = language, Prompt = pt.Prompt, Explanation = pt.Explanation, Options = pt.Options, Rubric = pt.Rubric, ContentHash = pt.Hash, CreatedAt = now, UpdatedAt = now });
                    tcounts.Created++;
                }
                else if (existing.ContentHash != pt.Hash)
                {
                    existing.Prompt = pt.Prompt;
                    existing.Explanation = pt.Explanation;
                    existing.Options = pt.Options;
                    existing.Rubric = pt.Rubric;
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

        foreach (var question in concept.Questions.Where(q => !seen.Contains(q.Id) && q.DeprecatedAt is null))
        {
            question.DeprecatedAt = now;
            question.UpdatedAt = now;
            counts.Deprecated++;
        }
    }

    private static void DiffKatas(Concept concept, PlanConcept pc, string file, DateTimeOffset now, ImportChangeSet change)
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

    private static void Deprecate(DomainEntity domain, DateTimeOffset now, ImportChangeSet change)
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

    private static void Deprecate(Topic topic, DateTimeOffset now, ImportChangeSet change)
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

    private static void Deprecate(Concept concept, string file, DateTimeOffset now, ImportChangeSet change)
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
