using Spine.Application.Review;
using Spine.Application.Review.Shared;
using Spine.Domain.Progress;

namespace Spine.Application.Tests.Review;

public sealed class ReviewScopeTests
{
    [Fact]
    [Trait("Requirement", "FR-012")]
    public void Concept_scope_carries_domain_topic_and_concept()
    {
        var scope = ReviewScope.ForConcept("dotnet", "async", "tasks");

        Assert.Equal(new ReviewScope(SessionScopeType.Concept, "dotnet", "async", "tasks"), scope);
        Assert.True(scope.IsValid);
        Assert.False((scope with { Concept = null }).IsValid);
        Assert.False((scope with { Topic = null }).IsValid);
        Assert.False((scope with { Domain = null }).IsValid);
    }

    [Theory]
    [Trait("Requirement", "FR-012")]
    [InlineData(SessionScopeType.Topic, "dotnet", "async", "tasks")]
    [InlineData(SessionScopeType.Domain, "dotnet", null, "tasks")]
    [InlineData(SessionScopeType.All, null, null, "tasks")]
    public void Concept_slug_is_forbidden_outside_concept_scope(SessionScopeType type, string? domain, string? topic, string? concept)
    {
        var scope = new ReviewScope(type, domain, topic, concept);

        Assert.False(scope.IsValid);
        Assert.Throws<ArgumentException>(scope.Validate);
    }
}
