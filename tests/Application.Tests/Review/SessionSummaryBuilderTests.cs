using Spine.Application.Review;
using Spine.Domain.Progress;

namespace Spine.Application.Tests.Review;

public sealed class SessionSummaryBuilderTests
{
    private static readonly DateTimeOffset Started = new(2026, 9, 6, 10, 0, 0, TimeSpan.Zero);
    private static readonly ScopeDto Scope = new("topic", "dotnet", "clr-memory", "C# & .NET Runtime", "Memory & GC");

    private static readonly ConceptRef GcModes = new("dotnet", "clr-memory", "gc-modes", "GC modes");
    private static readonly ConceptRef StackVsHeap = new("dotnet", "clr-memory", "stack-vs-heap", "Stack vs Heap");
    private static readonly ConceptRef Signals = new("angular", "reactivity", "signals", "Signals");

    private static Session Session(int questionCount, DateTimeOffset? finishedAt = null) => new()
    {
        Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        UserId = LocalLearner.Id,
        StartedAt = Started,
        FinishedAt = finishedAt ?? Started.AddMinutes(8),
        ScopeType = SessionScopeType.Topic,
        ScopeDomainSlug = "dotnet",
        ScopeTopicSlug = "clr-memory",
        QuestionIds = Enumerable.Range(1, questionCount).Select(i => $"q{i}").ToArray(),
        TotalLimit = 20,
        NewLimit = 10,
        CreatedAt = Started,
    };

    private static AnsweredItem Item(int n, bool correct, ConceptRef concept) => new($"q{n}", correct, concept);

    [Fact]
    public void Fifteen_of_twenty_is_75_percent()
    {
        var attempts = Enumerable.Range(1, 20).Select(i => Item(i, correct: i <= 15, StackVsHeap)).ToList();

        var summary = SessionSummaryBuilder.Build(Session(20), Scope, attempts, dueTomorrow: 5);

        summary.QuestionCount.ShouldBe(20);
        summary.Answered.ShouldBe(20);
        summary.Correct.ShouldBe(15);
        summary.Accuracy.ShouldBe(0.75);
        summary.DueTomorrow.ShouldBe(5);
        summary.SessionId.ShouldBe(Session(20).Id);
        summary.Scope.ShouldBe(Scope);
        summary.StartedAt.ShouldBe(Started);
        summary.FinishedAt.ShouldBe(Started.AddMinutes(8));
    }

    [Fact]
    public void Weak_concepts_are_ranked_by_wrong_count_then_domain_topic_concept()
    {
        List<AnsweredItem> attempts =
        [
            Item(1, false, StackVsHeap),
            Item(2, false, GcModes),
            Item(3, false, GcModes),
            Item(4, true, GcModes),
            Item(5, false, Signals),
            Item(6, true, StackVsHeap),
        ];

        var summary = SessionSummaryBuilder.Build(Session(6), Scope, attempts, 0);

        summary.WeakConcepts.Select(w => w.Concept).ShouldBe(["gc-modes", "signals", "stack-vs-heap"]);
        summary.WeakConcepts[0].ShouldBe(new WeakConceptDto("dotnet", "clr-memory", "gc-modes", "GC modes", 2, 3));
        summary.WeakConcepts[1].ShouldBe(new WeakConceptDto("angular", "reactivity", "signals", "Signals", 1, 1));
        summary.WeakConcepts[2].ShouldBe(new WeakConceptDto("dotnet", "clr-memory", "stack-vs-heap", "Stack vs Heap", 1, 2));
    }

    [Fact]
    public void Concepts_with_only_correct_answers_are_not_weak()
    {
        List<AnsweredItem> attempts = [Item(1, true, GcModes), Item(2, true, GcModes), Item(3, false, StackVsHeap)];

        var summary = SessionSummaryBuilder.Build(Session(3), Scope, attempts, 0);

        summary.WeakConcepts.ShouldHaveSingleItem().Concept.ShouldBe("stack-vs-heap");
    }

    [Fact]
    public void Zero_answers_have_null_accuracy_and_no_weak_concepts()
    {
        var summary = SessionSummaryBuilder.Build(Session(20), Scope, [], 0);

        summary.Answered.ShouldBe(0);
        summary.Correct.ShouldBe(0);
        summary.Accuracy.ShouldBeNull();
        summary.WeakConcepts.ShouldBeEmpty();
        summary.QuestionCount.ShouldBe(20);
    }

    [Fact]
    public void Ended_early_counts_only_what_was_answered()
    {
        var attempts = Enumerable.Range(1, 5).Select(i => Item(i, correct: i != 3, GcModes)).ToList();

        var summary = SessionSummaryBuilder.Build(Session(20), Scope, attempts, 1);

        summary.QuestionCount.ShouldBe(20);
        summary.Answered.ShouldBe(5);
        summary.Correct.ShouldBe(4);
        summary.Accuracy.ShouldBe(0.8);
    }
}
