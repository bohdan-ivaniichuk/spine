using Spine.Application.Knowledge;
using Spine.Application.Knowledge.Tree;
using Spine.Domain.Content;

namespace Spine.Application.Tests.Knowledge;

public sealed class KnowledgeTreeMapperTests
{
    private static readonly DateTimeOffset Deprecated = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly TreeDomainRow[] Domains =
    [
        new(1, "dotnet", ".NET", ".NET (укр)", 1, null),
        new(2, "angular", "Angular", null, 2, Deprecated),
    ];

    private static readonly TreeTopicRow[] Topics =
    [
        new(10, 1, "clr-memory", "Memory & GC", "Пам'ять і GC", 1, null),
        new(11, 1, "async", "Async", null, 2, null),
        new(20, 2, "signals", "Signals", null, 1, null),
    ];

    private static readonly TreeConceptRow[] Concepts =
    [
        new(100, 10, "stack-vs-heap", Level.Mid, true, 1, null, "Стек і купа", "Stack vs heap", 3, 1),
        new(101, 10, "span-memory", Level.Senior, false, 2, null, null, "Span and Memory", 2, 0),
        new(102, 10, "orphan", Level.Lead, false, 3, Deprecated, null, null, 0, 0),
        new(110, 11, "task-vs-valuetask", Level.Senior, true, 1, null, null, "Task vs ValueTask", 1, 1),
    ];

    [Fact]
    [Trait("Requirement", "FR-006")]
    public void Uk_tree_uses_uk_titles_and_flags_english_fallbacks()
    {
        var tree = new KnowledgeTreeMapper(Language.Uk).ToTree(Domains, Topics, Concepts);

        tree.Language.ShouldBe("uk");
        tree.Domains.Select(d => (d.Slug, d.Title, d.TitleFallback))
            .ShouldBe([("dotnet", ".NET (укр)", false), ("angular", "Angular", true)]);
        tree.Domains[0].Topics.Select(t => (t.Slug, t.Title, t.TitleFallback))
            .ShouldBe([("clr-memory", "Пам'ять і GC", false), ("async", "Async", true)]);
        tree.Domains[0].Topics[0].Concepts.Select(c => (c.Slug, c.Title, c.TitleFallback))
            .ShouldBe([("stack-vs-heap", "Стек і купа", false), ("span-memory", "Span and Memory", true), ("orphan", "orphan", true)]);
    }

    [Fact]
    [Trait("Requirement", "FR-006")]
    public void English_tree_has_no_fallbacks_and_keeps_every_field()
    {
        var tree = new KnowledgeTreeMapper(Language.En).ToTree(Domains, Topics, Concepts);

        tree.Language.ShouldBe("en");
        tree.Domains[1].ShouldBe(new TreeDomainDto("angular", "Angular", false, 2, Deprecated, tree.Domains[1].Topics));
        tree.Domains[1].Topics.Single().Concepts.ShouldBeEmpty();
        tree.Domains[0].Topics[1].Concepts.ShouldBe(
            [new TreeConceptDto("task-vs-valuetask", "Task vs ValueTask", false, "S", true, 1, 1, 1, null)]);
        tree.Domains[0].Topics[0].Concepts.Select(c => c.Level).ShouldBe(["M", "S", "L"]);
        tree.Domains[0].Topics[0].Concepts[2].DeprecatedAt.ShouldBe(Deprecated);
        tree.Domains[0].Topics[0].Concepts[0].QuestionCount.ShouldBe(3);
    }

    [Fact]
    [Trait("Requirement", "FR-006")]
    public void Children_keep_the_order_of_their_rows()
    {
        var tree = new KnowledgeTreeMapper(Language.En).ToTree(
            [Domains[1], Domains[0]], [Topics[1], Topics[0], Topics[2]], [Concepts[1], Concepts[0]]);

        tree.Domains.Select(d => d.Slug).ShouldBe(["angular", "dotnet"]);
        tree.Domains[1].Topics.Select(t => t.Slug).ShouldBe(["async", "clr-memory"]);
        tree.Domains[1].Topics[1].Concepts.Select(c => c.Slug).ShouldBe(["span-memory", "stack-vs-heap"]);
    }
}
