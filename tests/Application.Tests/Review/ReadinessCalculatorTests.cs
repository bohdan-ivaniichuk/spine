using Spine.Application.Review.Readiness;
using Spine.Domain.Progress;

namespace Spine.Application.Tests.Review;

public sealed class ReadinessCalculatorTests
{
    private static ConceptCounts Ready(string slug, int questions = 2) => new(slug, questions, questions, questions);

    private static ConceptCounts InProgress(string slug) => new(slug, 3, 2, 1);

    private static ConceptCounts NotStarted(string slug) => new(slug, 2, 0, 0);

    private static ConceptCounts NoQuestions(string slug) => new(slug, 0, 0, 0);

    [Theory]
    [Trait("Requirement", "FR-008")]
    [InlineData(4, 4, 4, ConceptReadiness.Ready)]
    [InlineData(4, 4, 3, ConceptReadiness.InProgress)]
    [InlineData(4, 1, 0, ConceptReadiness.InProgress)]
    [InlineData(4, 1, 1, ConceptReadiness.InProgress)]
    [InlineData(4, 0, 0, ConceptReadiness.NotStarted)]
    public void Concept_state_needs_every_schedulable_question_ready(int questions, int stated, int ready, ConceptReadiness expected)
    {
        ReadinessCalculator.StateOf(new ConceptCounts("c", questions, stated, ready)).ShouldBe(expected);
    }

    [Fact]
    [Trait("Requirement", "FR-008")]
    public void Concept_without_schedulable_questions_has_no_state()
    {
        ReadinessCalculator.StateOf(NoQuestions("c")).ShouldBeNull();
    }

    [Theory]
    [Trait("Requirement", "FR-009")]
    [InlineData(1, 4, 25)]
    [InlineData(2, 3, 66)]
    [InlineData(0, 5, 0)]
    [InlineData(3, 3, 100)]
    [InlineData(99, 100, 99)]
    public void Percent_is_rounded_down(int ready, int counted, int expected)
    {
        ReadinessCalculator.Percent(ready, counted).ShouldBe(expected);
    }

    [Fact]
    [Trait("Requirement", "FR-009")]
    public void Percent_is_null_when_nothing_counts()
    {
        ReadinessCalculator.Percent(0, 0).ShouldBeNull();
    }

    [Fact]
    [Trait("Requirement", "FR-009")]
    public void Topic_with_one_of_four_ready_concepts_is_25_percent_and_concepts_without_questions_are_left_out()
    {
        var result = ReadinessCalculator.Build(
        [
            new DomainCounts("dotnet",
            [
                new TopicCounts("async", [Ready("a"), InProgress("b"), NotStarted("c"), NotStarted("d"), NoQuestions("e")]),
            ]),
        ]);

        var topic = result.Domains.ShouldHaveSingleItem().Topics.ShouldHaveSingleItem();
        topic.ShouldBe(topic with { Topic = "async", Ready = 1, Counted = 4, Percent = 25 });
        topic.Concepts.ShouldBe(
        [
            new ConceptReadinessDto("a", "ready", 2, 2),
            new ConceptReadinessDto("b", "in-progress", 3, 1),
            new ConceptReadinessDto("c", "not-started", 2, 0),
            new ConceptReadinessDto("d", "not-started", 2, 0),
        ]);
    }

    [Fact]
    [Trait("Requirement", "FR-009")]
    public void Domain_aggregates_its_concepts_not_the_average_of_topic_percentages()
    {
        // topic A: 1 of 1 ready (100%); topic B: 0 of 3 (0%). Average of topics would be 50; concepts give 1 of 4 = 25.
        var result = ReadinessCalculator.Build(
        [
            new DomainCounts("dotnet",
            [
                new TopicCounts("a", [Ready("a1")]),
                new TopicCounts("b", [NotStarted("b1"), InProgress("b2"), NotStarted("b3")]),
            ]),
        ]);

        var domain = result.Domains.ShouldHaveSingleItem();
        (domain.Ready, domain.Counted, domain.Percent).ShouldBe((1, 4, 25));
        domain.Topics.Select(t => t.Percent).ShouldBe([100, 0]);
    }

    [Fact]
    [Trait("Requirement", "FR-009")]
    public void Domains_and_topics_where_nothing_counts_stay_in_order_with_a_null_percent()
    {
        var result = ReadinessCalculator.Build(
        [
            new DomainCounts("dotnet", [new TopicCounts("empty", []), new TopicCounts("only-open", [NoQuestions("x")])]),
            new DomainCounts("angular", []),
        ]);

        result.Domains.Select(d => d.Domain).ShouldBe(["dotnet", "angular"]);
        result.Domains.ShouldAllBe(d => d.Percent == null && d.Counted == 0 && d.Ready == 0);
        var topics = result.Domains[0].Topics;
        topics.Select(t => t.Topic).ShouldBe(["empty", "only-open"]);
        topics.ShouldAllBe(t => t.Percent == null && t.Concepts.Count == 0);
    }
}
