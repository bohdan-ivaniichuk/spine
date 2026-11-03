using Spine.Application.Content.Plan;
using Spine.Application.Content.Summary;
using Spine.Application.Content.Validation;
using Spine.Domain.Content;

namespace Spine.Application.Content.Diff;

/// <summary>Questions of one concept with their translations; a type change is an error, not an update.</summary>
internal static class QuestionDiff
{
    public static void Apply(Concept concept, PlanConcept pc, string file, DateTimeOffset now, ImportChangeSet change)
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
}
