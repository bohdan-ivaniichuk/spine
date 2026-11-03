using Spine.Application.Content.Seed;
using Spine.Domain.Content;

namespace Spine.Application.Content.Plan;

/// <summary>Builds the <see cref="ImportPlan"/> from a validated <see cref="ContentSet"/>: order, hashes, derived flags.</summary>
public static class ImportPlanBuilder
{
    public static ImportPlan Build(ContentSet set)
    {
        var manifest = set.Manifest?.Parsed ?? throw new InvalidOperationException("Manifest is required to build an import plan.");
        var filesByPair = set.TopicFiles
            .Where(f => f.Parsed is not null)
            .ToDictionary(f => (f.Parsed!.Domain, f.Parsed.Topic), f => f);

        var plan = new ImportPlan();
        for (var d = 0; d < manifest.Domains.Count; d++)
        {
            var md = manifest.Domains[d];
            var domain = new PlanDomain
            {
                Slug = md.Slug,
                Title = md.Title,
                TitleUk = md.Titles?.Uk,
                SortOrder = d,
                Hash = TitledHash("domain", md.Slug, md.Title, md.Titles?.Uk, d),
            };

            for (var t = 0; t < md.Topics.Count; t++)
            {
                var mt = md.Topics[t];
                filesByPair.TryGetValue((md.Slug, mt.Slug), out var file);
                var topic = new PlanTopic
                {
                    Slug = mt.Slug,
                    Title = mt.Title,
                    TitleUk = mt.Titles?.Uk,
                    SortOrder = t,
                    Hash = TitledHash("topic", mt.Slug, mt.Title, mt.Titles?.Uk, t),
                    File = file?.RelativePath,
                };

                if (file?.Parsed is { } parsed)
                {
                    for (var c = 0; c < parsed.Concepts.Count; c++)
                    {
                        topic.Concepts.Add(BuildConcept(parsed.Concepts[c], c));
                    }
                }

                domain.Topics.Add(topic);
            }

            plan.Domains.Add(domain);
        }

        return plan;
    }

    /// <summary>
    /// Domain/topic hash. The Ukrainian title is appended only when present, so rows imported before
    /// <c>titles.uk</c> existed keep their hash (no spurious updates), while adding, changing or removing it
    /// changes the hash and therefore the row.
    /// </summary>
    private static string TitledHash(string kind, string slug, string title, string? titleUk, int sortOrder) =>
        titleUk is null
            ? ContentHasher.Hash(kind, slug, title, sortOrder)
            : ContentHasher.Hash(kind, slug, title, sortOrder, new { uk = titleUk });

    private static PlanConcept BuildConcept(ConceptSeed seed, int sortOrder)
    {
        var questions = new List<PlanQuestion>();
        for (var q = 0; q < seed.Questions.Count; q++)
        {
            questions.Add(BuildQuestion(seed.Questions[q], q));
        }

        var katas = new List<PlanKata>();
        if (seed.Katas is not null)
        {
            for (var k = 0; k < seed.Katas.Count; k++)
            {
                katas.Add(BuildKata(seed.Katas[k], k));
            }
        }

        var reviewed = questions.All(q => q.Reviewed) && katas.All(k => k.Reviewed);
        var level = LevelExtensions.ParseLevel(seed.Level);
        var concept = new PlanConcept
        {
            Slug = seed.Slug,
            Level = level,
            Reviewed = reviewed,
            SortOrder = sortOrder,
            Hash = ContentHasher.Hash("concept", seed.Slug, seed.Level, reviewed, sortOrder),
        };

        foreach (var (lang, tr) in seed.Translations)
        {
            concept.Translations[LanguageExtensions.ParseLanguage(lang)] =
                new PlanConceptTranslation(tr.Title, tr.Note, ContentHasher.Hash("concept-translation", tr.Title, tr.Note));
        }

        concept.Questions.AddRange(questions);
        concept.Katas.AddRange(katas);
        return concept;
    }

    private static PlanQuestion BuildQuestion(QuestionSeed seed, int sortOrder)
    {
        var tags = seed.Tags.ToArray();
        var question = new PlanQuestion
        {
            Id = seed.Id,
            Type = QuestionTypeExtensions.ParseQuestionType(seed.Type),
            Level = LevelExtensions.ParseLevel(seed.Level),
            Reviewed = seed.Reviewed,
            Tags = tags,
            SortOrder = sortOrder,
            Hash = ContentHasher.Hash("question", seed.Id, seed.Type, seed.Level, seed.Reviewed, tags, sortOrder),
        };

        foreach (var (lang, tr) in seed.Translations)
        {
            var options = tr.Options?.Select(o => new McqOption(o.Text, o.Correct)).ToList();
            var rubric = tr.Rubric?.ToList();
            question.Translations[LanguageExtensions.ParseLanguage(lang)] = new PlanQuestionTranslation(
                tr.Prompt, tr.Explanation, options, rubric,
                ContentHasher.Hash("question-translation", tr.Prompt, tr.Explanation, options, rubric));
        }

        return question;
    }

    private static PlanKata BuildKata(KataSeed seed, int sortOrder)
    {
        var kata = new PlanKata
        {
            Id = seed.Id,
            Title = seed.Title,
            Language = KataLanguageExtensions.ParseKataLanguage(seed.Language),
            Reviewed = seed.Reviewed,
            TimeLimitMinutes = seed.TimeLimitMinutes,
            SortOrder = sortOrder,
            Hash = ContentHasher.Hash("kata", seed.Id, seed.Title, seed.Language, seed.Reviewed, seed.TimeLimitMinutes, sortOrder),
        };

        foreach (var (lang, tr) in seed.Translations)
        {
            var rubric = tr.Rubric.ToList();
            kata.Translations[LanguageExtensions.ParseLanguage(lang)] = new PlanKataTranslation(
                tr.Task, rubric, tr.ReferenceSolution,
                ContentHasher.Hash("kata-translation", tr.Task, rubric, tr.ReferenceSolution));
        }

        return kata;
    }
}
