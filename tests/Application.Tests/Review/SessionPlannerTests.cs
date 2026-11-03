using Spine.Application.Review.Sessions;

namespace Spine.Application.Tests.Review;

public sealed class SessionPlannerTests
{
    private static string[] Ids(string prefix, int count) => Enumerable.Range(1, count).Select(i => $"{prefix}{i}").ToArray();

    [Fact]
    public void Due_questions_come_before_new_ones_in_the_given_order()
    {
        var plan = SessionPlanner.Plan(["d2", "d1"], ["n1", "n2"], 20, 10);

        plan.ShouldBe(["d2", "d1", "n1", "n2"]);
    }

    [Fact]
    public void Limits_10_total_3_new_with_4_due_and_20_new_yield_7()
    {
        var plan = SessionPlanner.Plan(Ids("d", 4), Ids("n", 20), 10, 3);

        plan.Length.ShouldBe(7);
        plan.Take(4).ShouldBe(Ids("d", 4));
        plan.Skip(4).ShouldBe(["n1", "n2", "n3"]);
    }

    [Fact]
    public void More_due_than_the_session_holds_means_no_new_questions()
    {
        var plan = SessionPlanner.Plan(Ids("d", 25), Ids("n", 10), 20, 10);

        plan.Length.ShouldBe(20);
        plan.ShouldBe(Ids("d", 20));
        plan.ShouldNotContain(id => id.StartsWith('n'));
    }

    [Fact]
    public void New_limit_above_total_limit_is_clamped()
    {
        var plan = SessionPlanner.Plan([], Ids("n", 50), 5, 100);

        plan.ShouldBe(Ids("n", 5));
    }

    [Fact]
    public void Due_fills_part_and_new_fills_the_rest_up_to_the_new_cap()
    {
        var plan = SessionPlanner.Plan(Ids("d", 15), Ids("n", 50), 20, 10);

        plan.Length.ShouldBe(20);
        plan.Skip(15).ShouldBe(Ids("n", 5));
    }

    [Fact]
    public void Nothing_available_gives_an_empty_plan()
    {
        SessionPlanner.Plan([], [], 20, 10).ShouldBeEmpty();
    }

    [Fact]
    public void Fewer_available_than_limits_takes_everything()
    {
        var plan = SessionPlanner.Plan(["d1"], ["n1", "n2"], 20, 10);

        plan.ShouldBe(["d1", "n1", "n2"]);
    }

    [Fact]
    public void Negative_limits_are_rejected()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => SessionPlanner.Plan([], [], -1, 0));
        Should.Throw<ArgumentOutOfRangeException>(() => SessionPlanner.Plan([], [], 1, -1));
    }

    [Fact]
    [Trait("Requirement", "FR-002")]
    public void PlanWeak_keeps_the_given_order_and_caps_at_the_total()
    {
        Assert.Equal(["c", "a"], SessionPlanner.PlanWeak(["c", "a", "b"], 2));
        Assert.Equal(["c", "a", "b"], SessionPlanner.PlanWeak(["c", "a", "b"], 20));
    }

    [Fact]
    [Trait("Requirement", "FR-005")]
    public void PlanWeak_with_nothing_weak_is_empty()
    {
        Assert.Empty(SessionPlanner.PlanWeak([], 20));
    }
}
