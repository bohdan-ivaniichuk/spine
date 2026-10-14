using System.Text.Json.Nodes;
using Spine.Application.Content;
using Spine.Domain.Content;
using DomainEntity = Spine.Domain.Content.Domain;

namespace Spine.Application.Tests.Content;

public class ImportDiffTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset T1 = T0.AddDays(1);
    private static readonly DateTimeOffset T2 = T0.AddDays(2);

    private const string StackVsHeapMcq1 = "dotnet.clr-memory.stack-vs-heap.mcq.1";
    private const string StackVsHeapMcq2 = "dotnet.clr-memory.stack-vs-heap.mcq.2";
    private const string SpanMemoryOpen1 = "dotnet.clr-memory.span-memory.open.1";

    private static ImportPlan ValidPlan() => ImportPlanBuilder.Build(ContentSetLoader.LoadValid());
    private static ImportPlan PlanWithTopic(Action<JsonNode> mutate) => ImportPlanBuilder.Build(FixtureJson.WithTopic(mutate));

    /// <summary>Removes the last question of span-memory; no sort orders shift and the derived reviewed flag stays false.</summary>
    private static ImportPlan PlanWithoutSpanMemoryOpen1() => PlanWithTopic(t => ((JsonArray)FixtureJson.Concept(t, 1)["questions"]!).RemoveAt(2));

    /// <summary>A fresh import of the valid fixture; the returned entities are the graph the next Compute sees.</summary>
    private static List<DomainEntity> ImportFresh() => ImportDiff.Compute([], ValidPlan(), T0).NewDomains;

    private static (int Created, int Updated, int Unchanged, int Deprecated) Counts(ImportChangeSet change, string kind)
    {
        var c = change.Summary.Totals()[kind];
        return (c.Created, c.Updated, c.Unchanged, c.Deprecated);
    }

    private static IEnumerable<Concept> Concepts(IEnumerable<DomainEntity> domains) => domains.SelectMany(d => d.Topics).SelectMany(t => t.Concepts);
    private static Concept ConceptOf(IEnumerable<DomainEntity> domains, string slug) => Concepts(domains).Single(c => c.Slug == slug);
    private static Question QuestionOf(IEnumerable<DomainEntity> domains, string id) => Concepts(domains).SelectMany(c => c.Questions).Single(q => q.Id == id);

    /// <summary>Hash, UpdatedAt and DeprecatedAt of every entity and translation, in graph order.</summary>
    private static List<string> Snapshot(IEnumerable<DomainEntity> domains)
    {
        var lines = new List<string>();
        static string Line(string kind, string key, string hash, DateTimeOffset updated, DateTimeOffset? deprecated) =>
            $"{kind} {key} hash={hash} updated={updated:O} deprecated={deprecated?.ToString("O") ?? "-"}";

        foreach (var d in domains)
        {
            lines.Add(Line("domain", d.Slug, d.ContentHash, d.UpdatedAt, d.DeprecatedAt));
            foreach (var t in d.Topics)
            {
                lines.Add(Line("topic", $"{d.Slug}/{t.Slug}", t.ContentHash, t.UpdatedAt, t.DeprecatedAt));
                foreach (var c in t.Concepts)
                {
                    lines.Add(Line("concept", c.Slug, c.ContentHash, c.UpdatedAt, c.DeprecatedAt));
                    lines.AddRange(c.Translations.Select(x => Line("concept-translation", $"{c.Slug}/{x.Language}", x.ContentHash, x.UpdatedAt, null)));
                    foreach (var q in c.Questions)
                    {
                        lines.Add(Line("question", q.Id, q.ContentHash, q.UpdatedAt, q.DeprecatedAt));
                        lines.AddRange(q.Translations.Select(x => Line("question-translation", $"{q.Id}/{x.Language}", x.ContentHash, x.UpdatedAt, null)));
                    }

                    foreach (var k in c.Katas)
                    {
                        lines.Add(Line("kata", k.Id, k.ContentHash, k.UpdatedAt, k.DeprecatedAt));
                        lines.AddRange(k.Translations.Select(x => Line("kata-translation", $"{k.Id}/{x.Language}", x.ContentHash, x.UpdatedAt, null)));
                    }
                }
            }
        }

        return lines;
    }

    [Fact]
    public void Compute_EmptyGraph_CreatesEverything()
    {
        var change = ImportDiff.Compute([], ValidPlan(), T0);

        change.Errors.ShouldBeEmpty();
        change.NewDomains.Select(d => d.Slug).ShouldBe(["dotnet", "angular"]);
        Counts(change, EntityKinds.Domain).ShouldBe((2, 0, 0, 0));
        Counts(change, EntityKinds.Topic).ShouldBe((3, 0, 0, 0));
        Counts(change, EntityKinds.Concept).ShouldBe((2, 0, 0, 0));
        Counts(change, EntityKinds.Question).ShouldBe((6, 0, 0, 0));
        Counts(change, EntityKinds.Kata).ShouldBe((1, 0, 0, 0));
        Counts(change, EntityKinds.Translation).ShouldBe((11, 0, 0, 0));
        change.Summary.HasChanges.ShouldBeTrue();
    }

    [Fact]
    public void Compute_EmptyGraph_SetsTimestampsAndDerivedFlags()
    {
        var change = ImportDiff.Compute([], ValidPlan(), T0);

        var question = QuestionOf(change.NewDomains, StackVsHeapMcq1);
        question.CreatedAt.ShouldBe(T0);
        question.UpdatedAt.ShouldBe(T0);
        question.DeprecatedAt.ShouldBeNull();
        question.Translations.Select(t => t.Language).ShouldBe([Language.En, Language.Uk]);
        ConceptOf(change.NewDomains, "stack-vs-heap").Reviewed.ShouldBeTrue();
        ConceptOf(change.NewDomains, "span-memory").Reviewed.ShouldBeFalse();
        change.Summary.Files.Keys.ShouldBe([FixtureJson.TopicPath, ImportPlan.ManifestFile]);
    }

    [Fact]
    public void Compute_SamePlanAgain_LeavesEveryEntityUntouched()
    {
        var existing = ImportFresh();
        var before = Snapshot(existing);

        var change = ImportDiff.Compute(existing, ValidPlan(), T1);

        change.Errors.ShouldBeEmpty();
        change.NewDomains.ShouldBeEmpty();
        change.Summary.HasChanges.ShouldBeFalse();
        Counts(change, EntityKinds.Domain).ShouldBe((0, 0, 2, 0));
        Counts(change, EntityKinds.Topic).ShouldBe((0, 0, 3, 0));
        Counts(change, EntityKinds.Concept).ShouldBe((0, 0, 2, 0));
        Counts(change, EntityKinds.Question).ShouldBe((0, 0, 6, 0));
        Counts(change, EntityKinds.Kata).ShouldBe((0, 0, 1, 0));
        Counts(change, EntityKinds.Translation).ShouldBe((0, 0, 11, 0));
        Snapshot(existing).ShouldBe(before);
    }

    [Fact]
    public void Compute_EditedNote_UpdatesExactlyOneTranslation()
    {
        var existing = ImportFresh();
        var before = Snapshot(existing);
        var plan = PlanWithTopic(t =>
        {
            var en = FixtureJson.Concept(t, 0)["translations"]!["en"]!;
            en["note"] = en["note"]!.GetValue<string>() + "\n\nEdited paragraph.";
        });

        var change = ImportDiff.Compute(existing, plan, T1);

        Counts(change, EntityKinds.Translation).ShouldBe((0, 1, 10, 0));
        Counts(change, EntityKinds.Concept).ShouldBe((0, 0, 2, 0));
        Counts(change, EntityKinds.Question).ShouldBe((0, 0, 6, 0));
        var concept = ConceptOf(existing, "stack-vs-heap");
        var en = concept.Translations.Single(x => x.Language == Language.En);
        en.Note.ShouldEndWith("Edited paragraph.");
        en.UpdatedAt.ShouldBe(T1);
        concept.Translations.Single(x => x.Language == Language.Uk).UpdatedAt.ShouldBe(T0);
        concept.UpdatedAt.ShouldBe(T0);
        Snapshot(existing).Zip(before).Count(p => p.First != p.Second).ShouldBe(1);
    }

    [Fact]
    public void Compute_QuestionRemoved_DeprecatesItAndKeepsItsTranslations()
    {
        var existing = ImportFresh();

        var change = ImportDiff.Compute(existing, PlanWithoutSpanMemoryOpen1(), T1);

        Counts(change, EntityKinds.Question).ShouldBe((0, 0, 5, 1));
        Counts(change, EntityKinds.Concept).ShouldBe((0, 0, 2, 0));
        Counts(change, EntityKinds.Translation).ShouldBe((0, 0, 10, 0));
        var question = QuestionOf(existing, SpanMemoryOpen1);
        question.DeprecatedAt.ShouldBe(T1);
        question.UpdatedAt.ShouldBe(T1);
        var translation = question.Translations.ShouldHaveSingleItem();
        translation.UpdatedAt.ShouldBe(T0);
        translation.Rubric.ShouldNotBeNull().Count.ShouldBe(4);
    }

    [Fact]
    public void Compute_QuestionRemovedTwice_IsNotDeprecatedAgain()
    {
        var existing = ImportFresh();
        ImportDiff.Compute(existing, PlanWithoutSpanMemoryOpen1(), T1);

        var change = ImportDiff.Compute(existing, PlanWithoutSpanMemoryOpen1(), T2);

        Counts(change, EntityKinds.Question).ShouldBe((0, 0, 5, 0));
        change.Summary.HasChanges.ShouldBeFalse();
        QuestionOf(existing, SpanMemoryOpen1).DeprecatedAt.ShouldBe(T1);
    }

    [Fact]
    public void Compute_QuestionReAdded_ReactivatesAndCountsUpdated()
    {
        var existing = ImportFresh();
        ImportDiff.Compute(existing, PlanWithoutSpanMemoryOpen1(), T1);

        var change = ImportDiff.Compute(existing, ValidPlan(), T2);

        Counts(change, EntityKinds.Question).ShouldBe((0, 1, 5, 0));
        Counts(change, EntityKinds.Translation).ShouldBe((0, 0, 11, 0));
        var question = QuestionOf(existing, SpanMemoryOpen1);
        question.DeprecatedAt.ShouldBeNull();
        question.UpdatedAt.ShouldBe(T2);
        question.Translations.ShouldHaveSingleItem().UpdatedAt.ShouldBe(T0);
    }

    [Fact]
    public void Compute_UkTranslationOmitted_KeepsExistingUk()
    {
        var existing = ImportFresh();
        var before = Snapshot(existing);
        var plan = PlanWithTopic(t =>
        {
            ((JsonObject)FixtureJson.Concept(t, 0)["translations"]!).Remove("uk");
            ((JsonObject)FixtureJson.Question(t, 0, 0)["translations"]!).Remove("uk");
        });

        var change = ImportDiff.Compute(existing, plan, T1);

        change.Summary.HasChanges.ShouldBeFalse();
        Counts(change, EntityKinds.Translation).ShouldBe((0, 0, 9, 0));
        ConceptOf(existing, "stack-vs-heap").Translations.Select(x => x.Language).ShouldBe([Language.En, Language.Uk]);
        QuestionOf(existing, StackVsHeapMcq1).Translations.Select(x => x.Language).ShouldBe([Language.En, Language.Uk]);
        Snapshot(existing).ShouldBe(before);
    }

    [Fact]
    public void Compute_ExistingQuestionWithDifferentType_ReportsErrorAndLeavesEntityUntouched()
    {
        var existing = ImportFresh();
        var before = Snapshot(existing);
        var plan = PlanWithTopic(t => FixtureJson.Question(t, 0, 1)["type"] = "open");

        var change = ImportDiff.Compute(existing, plan, T1);

        var error = change.Errors.ShouldHaveSingleItem();
        error.Rule.ShouldBe("type-change");
        error.File.ShouldBe(FixtureJson.TopicPath);
        error.JsonPointer.ShouldBe("/concepts/0/questions/1/type");
        error.Message.ShouldContain(StackVsHeapMcq2);
        var question = QuestionOf(existing, StackVsHeapMcq2);
        question.Type.ShouldBe(QuestionType.Mcq);
        question.UpdatedAt.ShouldBe(T0);
        Counts(change, EntityKinds.Question).ShouldBe((0, 0, 5, 0));
        Snapshot(existing).ShouldBe(before);
    }

    [Fact]
    public void Compute_TopicInManifestWithoutFile_DeprecatesItsConcepts()
    {
        // FR-024: a topic that stays in the manifest but has no seed file is "absent from the seed set".
        var existing = ImportFresh();
        var plan = ImportPlanBuilder.Build(ContentSetLoader.WithValidManifest());

        var change = ImportDiff.Compute(existing, plan, T1);

        change.Summary.HasChanges.ShouldBeTrue();
        Counts(change, EntityKinds.Topic).ShouldBe((0, 0, 3, 0));
        Counts(change, EntityKinds.Concept).ShouldBe((0, 0, 0, 2));
        Counts(change, EntityKinds.Question).ShouldBe((0, 0, 0, 6));
        Concepts(existing).Count().ShouldBe(2);
        Concepts(existing).ShouldAllBe(c => c.DeprecatedAt == T1);
        Concepts(existing).SelectMany(c => c.Questions).ShouldAllBe(q => q.DeprecatedAt == T1);
        Concepts(existing).SelectMany(c => c.Translations).Count().ShouldBeGreaterThan(0); // translations kept
    }

    [Fact]
    public void Compute_DomainRemovedFromManifest_DeprecatesWholeSubtree()
    {
        var existing = ImportFresh();
        var manifest = FixtureJson.ManifestNode();
        ((JsonArray)manifest["domains"]!).RemoveAt(0);
        var plan = ImportPlanBuilder.Build(ContentSetLoader.FromFiles(ContentSetLoader.ValidContentDir, FixtureJson.Text(manifest)));

        var change = ImportDiff.Compute(existing, plan, T1);

        // angular moves from sort order 1 to 0, so it is legitimately updated; its topic keeps sort order 0.
        Counts(change, EntityKinds.Domain).ShouldBe((0, 1, 0, 1));
        Counts(change, EntityKinds.Topic).ShouldBe((0, 0, 1, 2));
        Counts(change, EntityKinds.Concept).ShouldBe((0, 0, 0, 2));
        Counts(change, EntityKinds.Question).ShouldBe((0, 0, 0, 6));
        Counts(change, EntityKinds.Kata).ShouldBe((0, 0, 0, 1));
        Counts(change, EntityKinds.Translation).ShouldBe((0, 0, 0, 0));
        var dotnet = existing.Single(d => d.Slug == "dotnet");
        dotnet.DeprecatedAt.ShouldBe(T1);
        dotnet.Topics.ShouldAllBe(t => t.DeprecatedAt == T1);
        Concepts([dotnet]).ShouldAllBe(c => c.DeprecatedAt == T1);
        Concepts([dotnet]).SelectMany(c => c.Questions).ShouldAllBe(q => q.DeprecatedAt == T1);
        Concepts([dotnet]).SelectMany(c => c.Katas).ShouldAllBe(k => k.DeprecatedAt == T1);
        Concepts([dotnet]).SelectMany(c => c.Translations).ShouldAllBe(x => x.UpdatedAt == T0);
        existing.Single(d => d.Slug == "angular").DeprecatedAt.ShouldBeNull();
    }

    [Fact]
    public void Compute_DomainRestoredToManifest_ReactivatesSubtreeAsUpdated()
    {
        var existing = ImportFresh();
        var manifest = FixtureJson.ManifestNode();
        ((JsonArray)manifest["domains"]!).RemoveAt(0);
        ImportDiff.Compute(existing, ImportPlanBuilder.Build(ContentSetLoader.FromFiles(ContentSetLoader.ValidContentDir, FixtureJson.Text(manifest))), T1);

        var change = ImportDiff.Compute(existing, ValidPlan(), T2);

        Counts(change, EntityKinds.Domain).ShouldBe((0, 2, 0, 0));
        Counts(change, EntityKinds.Topic).ShouldBe((0, 2, 1, 0));
        Counts(change, EntityKinds.Concept).ShouldBe((0, 2, 0, 0));
        Counts(change, EntityKinds.Question).ShouldBe((0, 6, 0, 0));
        Counts(change, EntityKinds.Kata).ShouldBe((0, 1, 0, 0));
        Counts(change, EntityKinds.Translation).ShouldBe((0, 0, 11, 0));
        Concepts(existing).SelectMany(c => c.Questions).ShouldAllBe(q => q.DeprecatedAt == null && q.UpdatedAt == T2);
    }
}
