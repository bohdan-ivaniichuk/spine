using Spine.Application.Review.WeakSpots;
using Spine.Domain.Progress;

namespace Spine.Application.Tests.Review;

public sealed class WeaknessRankerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);

    private static ConceptAnswerStats Stats(string concept, int answers, int wrong, int daysAgo = 1, string topic = "async", int weak = 0) =>
        new("dotnet", topic, concept, answers, wrong, Now.AddDays(-daysAgo), weak);

    [Theory]
    [Trait("Requirement", "FR-007")]
    [InlineData(4, 1, 0.75)]
    [InlineData(3, 3, 0.0)]
    [InlineData(5, 0, 1.0)]
    public void Accuracy_is_the_share_of_right_answers(int answers, int wrong, double expected)
    {
        Assert.Equal(expected, WeaknessRanker.Accuracy(answers, wrong), 6);
    }

    [Fact]
    [Trait("Requirement", "FR-008")]
    public void Concepts_with_fewer_than_three_answers_are_not_ranked()
    {
        var ranking = WeaknessRanker.Rank([Stats("two", 2, 2), Stats("three", 3, 1)]);

        Assert.Equal(["three"], ranking.Concepts.Select(c => c.Concept));
    }

    [Fact]
    [Trait("Requirement", "FR-008")]
    public void Lowest_accuracy_first_then_more_wrong_then_most_recent()
    {
        var ranking = WeaknessRanker.Rank(
        [
            Stats("good", 4, 0),
            Stats("half-of-four", 4, 2, daysAgo: 1),
            Stats("half-of-six", 6, 3, daysAgo: 9),
            Stats("worst", 3, 3),
            Stats("half-of-six-older", 6, 3, daysAgo: 20),
        ]);

        Assert.Equal(["worst", "half-of-six", "half-of-six-older", "half-of-four", "good"], ranking.Concepts.Select(c => c.Concept));
    }

    [Fact]
    [Trait("Requirement", "FR-008")]
    public void At_most_twenty_concepts_are_ranked()
    {
        var ranking = WeaknessRanker.Rank([.. Enumerable.Range(0, 25).Select(i => Stats($"c{i:00}", 4, i % 4))]);

        Assert.Equal(WeaknessRule.MaxConcepts, ranking.Concepts.Count);
    }

    [Fact]
    [Trait("Requirement", "FR-009")]
    public void Topics_combine_their_concepts_and_qualify_on_the_combined_answers()
    {
        var ranking = WeaknessRanker.Rank(
        [
            Stats("a1", 2, 1, daysAgo: 3, topic: "async", weak: 1),
            Stats("a2", 2, 2, daysAgo: 1, topic: "async", weak: 2),
            Stats("g1", 2, 0, daysAgo: 1, topic: "generics"),
        ]);

        Assert.Empty(ranking.Concepts);
        var topic = Assert.Single(ranking.Topics);
        Assert.Equal(new TopicAnswerStats("dotnet", "async", 4, 3, Now.AddDays(-1), 3), topic);
    }

    [Fact]
    [Trait("Requirement", "FR-009")]
    public void Topics_are_ordered_like_concepts_and_capped_at_ten()
    {
        var ranking = WeaknessRanker.Rank([.. Enumerable.Range(0, 12).Select(i => Stats($"c{i}", 4, i % 5, topic: $"t{i:00}"))]);

        Assert.Equal(WeaknessRule.MaxTopics, ranking.Topics.Count);
        Assert.Equal(4, ranking.Topics[0].Wrong);
        Assert.True(ranking.Topics.Zip(ranking.Topics.Skip(1)).All(p => p.First.Wrong >= p.Second.Wrong));
    }
}
