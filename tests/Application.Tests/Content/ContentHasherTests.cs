using System.Text.Json.Nodes;
using Spine.Application.Content;
using Spine.Domain.Content;

namespace Spine.Application.Tests.Content;

public class ContentHasherTests
{
    private const string StackVsHeapMcq1 = "dotnet.clr-memory.stack-vs-heap.mcq.1";

    private static ImportPlan PlanWithTopic(Action<JsonNode> mutate) => ImportPlanBuilder.Build(FixtureJson.WithTopic(mutate));

    private static PlanConcept ConceptOf(ImportPlan plan, string slug) =>
        plan.Domains.SelectMany(d => d.Topics).SelectMany(t => t.Concepts).Single(c => c.Slug == slug);

    private static PlanQuestion QuestionOf(ImportPlan plan, string id) =>
        plan.Domains.SelectMany(d => d.Topics).SelectMany(t => t.Concepts).SelectMany(c => c.Questions).Single(q => q.Id == id);

    /// <summary>Every hash in the plan, keyed by a stable path, so two plans can be compared as a whole.</summary>
    private static List<string> AllHashes(ImportPlan plan)
    {
        var lines = new List<string>();
        foreach (var d in plan.Domains)
        {
            lines.Add($"domain {d.Slug} {d.Hash}");
            foreach (var t in d.Topics)
            {
                lines.Add($"topic {d.Slug}/{t.Slug} {t.Hash}");
                foreach (var c in t.Concepts)
                {
                    lines.Add($"concept {c.Slug} {c.Hash}");
                    lines.AddRange(c.Translations.OrderBy(x => x.Key).Select(x => $"concept-translation {c.Slug}/{x.Key} {x.Value.Hash}"));
                    foreach (var q in c.Questions)
                    {
                        lines.Add($"question {q.Id} {q.Hash}");
                        lines.AddRange(q.Translations.OrderBy(x => x.Key).Select(x => $"question-translation {q.Id}/{x.Key} {x.Value.Hash}"));
                    }

                    foreach (var k in c.Katas)
                    {
                        lines.Add($"kata {k.Id} {k.Hash}");
                        lines.AddRange(k.Translations.OrderBy(x => x.Key).Select(x => $"kata-translation {k.Id}/{x.Key} {x.Value.Hash}"));
                    }
                }
            }
        }

        return lines;
    }

    [Fact]
    public void Hash_SameParts_ProducesSameSha256Hex()
    {
        var first = ContentHasher.Hash("concept", "stack-vs-heap", "M", true, 0);
        var second = ContentHasher.Hash("concept", "stack-vs-heap", "M", true, 0);

        first.ShouldBe(second);
        first.Length.ShouldBe(64);
        first.ShouldAllBe(c => Uri.IsHexDigit(c) && !char.IsUpper(c));
    }

    [Fact]
    public void Hash_DifferentArgumentOrder_ProducesDifferentHash()
    {
        ContentHasher.Hash("a", "b").ShouldNotBe(ContentHasher.Hash("b", "a"));
    }

    [Fact]
    public void Hash_WhitespaceInsideString_IsContent()
    {
        ContentHasher.Hash("stack vs heap").ShouldNotBe(ContentHasher.Hash("stack  vs heap"));
    }

    [Fact]
    public void Hash_NullVersusMissingPart_Differs()
    {
        ContentHasher.Hash("a", null).ShouldNotBe(ContentHasher.Hash("a"));
    }

    [Fact]
    public void Plan_NoteEdit_ChangesOnlyThatTranslationHash()
    {
        var original = ImportPlanBuilder.Build(ContentSetLoader.LoadValid());
        var edited = PlanWithTopic(t =>
        {
            var en = FixtureJson.Concept(t, 0)["translations"]!["en"]!;
            en["note"] = en["note"]!.GetValue<string>() + "\n\nOne more paragraph.";
        });

        var before = ConceptOf(original, "stack-vs-heap");
        var after = ConceptOf(edited, "stack-vs-heap");

        after.Hash.ShouldBe(before.Hash);
        after.Translations[Language.En].Hash.ShouldNotBe(before.Translations[Language.En].Hash);
        after.Translations[Language.Uk].Hash.ShouldBe(before.Translations[Language.Uk].Hash);
        AllHashes(edited).Except(AllHashes(original)).ShouldHaveSingleItem().ShouldStartWith("concept-translation stack-vs-heap/En");
    }

    [Fact]
    public void Plan_OmittingUk_LeavesEntityAndEnHashesUnchanged()
    {
        var original = ImportPlanBuilder.Build(ContentSetLoader.LoadValid());
        var withoutUk = PlanWithTopic(t =>
        {
            ((JsonObject)FixtureJson.Concept(t, 0)["translations"]!).Remove("uk");
            ((JsonObject)FixtureJson.Question(t, 0, 0)["translations"]!).Remove("uk");
        });

        var concept = ConceptOf(withoutUk, "stack-vs-heap");
        var question = QuestionOf(withoutUk, StackVsHeapMcq1);

        concept.Translations.Keys.ShouldBe([Language.En]);
        question.Translations.Keys.ShouldBe([Language.En]);
        concept.Hash.ShouldBe(ConceptOf(original, "stack-vs-heap").Hash);
        question.Hash.ShouldBe(QuestionOf(original, StackVsHeapMcq1).Hash);
        concept.Translations[Language.En].Hash.ShouldBe(ConceptOf(original, "stack-vs-heap").Translations[Language.En].Hash);
        question.Translations[Language.En].Hash.ShouldBe(QuestionOf(original, StackVsHeapMcq1).Translations[Language.En].Hash);
        AllHashes(original).Except(AllHashes(withoutUk)).ShouldAllBe(line => line.Contains("/Uk "));
    }

    [Fact]
    public void Plan_ReorderedJsonPropertiesAndWhitespace_ProduceIdenticalHashes()
    {
        var manifest = FixtureJson.ManifestNode();
        var topic = FixtureJson.TopicNode();
        var reorderedSet = ContentSetLoader.FromFiles(
            ContentSetLoader.ValidContentDir,
            FixtureJson.Text(FixtureJson.ReverseKeys(manifest), indented: false),
            (FixtureJson.TopicPath, FixtureJson.Text(FixtureJson.ReverseKeys(topic), indented: false)));
        reorderedSet.TopicFiles[0].Parsed.ShouldNotBeNull();

        var original = ImportPlanBuilder.Build(ContentSetLoader.LoadValid());
        var reordered = ImportPlanBuilder.Build(reorderedSet);

        AllHashes(reordered).ShouldBe(AllHashes(original));
    }

    [Fact]
    public void Plan_WhitespaceInsidePrompt_ChangesQuestionTranslationHashOnly()
    {
        var original = ImportPlanBuilder.Build(ContentSetLoader.LoadValid());
        var edited = PlanWithTopic(t =>
        {
            var en = FixtureJson.Question(t, 0, 0)["translations"]!["en"]!;
            en["prompt"] = en["prompt"]!.GetValue<string>().Replace("stored when", "stored  when");
        });

        var before = QuestionOf(original, StackVsHeapMcq1);
        var after = QuestionOf(edited, StackVsHeapMcq1);

        after.Hash.ShouldBe(before.Hash);
        after.Translations[Language.En].Hash.ShouldNotBe(before.Translations[Language.En].Hash);
        AllHashes(edited).Except(AllHashes(original)).ShouldHaveSingleItem().ShouldStartWith($"question-translation {StackVsHeapMcq1}/En");
    }
}
