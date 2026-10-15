using Spine.Domain.Progress;

namespace Spine.Domain.Tests.Progress;

public sealed class ReadinessRuleTests
{
    [Theory]
    [Trait("Requirement", "FR-008")]
    // 1 → 6 → 15 under SM-2: the third correct answer in a row crosses the line
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(6, false)]
    [InlineData(7, true)]
    [InlineData(15, true)]
    public void A_question_is_ready_from_an_interval_of_seven_days(int intervalDays, bool ready)
    {
        ReadinessRule.IsReady(intervalDays).ShouldBe(ready);
    }

    [Theory]
    [Trait("Requirement", "FR-008")]
    [InlineData(ConceptReadiness.NotStarted, "not-started")]
    [InlineData(ConceptReadiness.InProgress, "in-progress")]
    [InlineData(ConceptReadiness.Ready, "ready")]
    public void States_have_kebab_case_wire_values(ConceptReadiness readiness, string wire)
    {
        readiness.ToWireValue().ShouldBe(wire);
    }
}
