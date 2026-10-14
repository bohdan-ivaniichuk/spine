using Spine.Domain.Scheduling;

namespace Spine.Domain.Tests.Scheduling;

public sealed class Sm2AlgorithmTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 6, 10, 0, 0, TimeSpan.Zero);
    private readonly Sm2Algorithm _sut = new();

    [Theory]
    // new item answered right: 1 day, ease untouched
    [InlineData(0, 2.5, 0, ReviewGrade.Good, 1, 2.5, 1)]
    // second right answer: 6 days
    [InlineData(1, 2.5, 1, ReviewGrade.Good, 6, 2.5, 2)]
    // third: 6 × 2.5 = 15
    [InlineData(6, 2.5, 2, ReviewGrade.Good, 15, 2.5, 3)]
    // fourth: 15 × 2.5 = 37.5 → 38 (away from zero, not banker's)
    [InlineData(15, 2.5, 3, ReviewGrade.Good, 38, 2.5, 4)]
    // wrong resets the streak and costs 0.2 ease
    [InlineData(6, 2.5, 2, ReviewGrade.Again, 1, 2.3, 0)]
    // ease floor at 1.3
    [InlineData(1, 1.4, 0, ReviewGrade.Again, 1, 1.3, 0)]
    [InlineData(1, 1.3, 0, ReviewGrade.Again, 1, 1.3, 0)]
    // right after wrong starts over at 1 day with the reduced ease
    [InlineData(1, 2.3, 0, ReviewGrade.Good, 1, 2.3, 1)]
    // degenerate stored interval 0 with a long streak still yields at least 1 day
    [InlineData(0, 2.5, 5, ReviewGrade.Good, 1, 2.5, 6)]
    // interval capped at 36500 days so now.AddDays cannot overflow, even from an absurd stored interval
    [InlineData(20000, 2.5, 10, ReviewGrade.Good, Sm2Algorithm.MaxIntervalDays, 2.5, 11)]
    [InlineData(int.MaxValue, 2.5, 10, ReviewGrade.Good, Sm2Algorithm.MaxIntervalDays, 2.5, 11)]
    public void Next_transitions(int interval, double ease, int reps, ReviewGrade grade, int expectedInterval, double expectedEase, int expectedReps)
    {
        var current = new ReviewSnapshot(interval, ease, reps, null, Now, null);

        var result = _sut.Next(current, grade, Now);

        result.IntervalDays.ShouldBe(expectedInterval);
        result.Ease.ShouldBe(expectedEase, tolerance: 1e-9);
        result.Repetitions.ShouldBe(expectedReps);
    }

    [Fact]
    public void Three_correct_answers_grow_the_interval_1_6_15()
    {
        var snapshot = ReviewSnapshot.New(Now);
        var intervals = new List<int>();
        var clock = Now;
        for (var i = 0; i < 3; i++)
        {
            var result = _sut.Next(snapshot, ReviewGrade.Good, clock);
            intervals.Add(result.IntervalDays);
            clock = result.DueAt;
            snapshot = new ReviewSnapshot(result.IntervalDays, result.Ease, result.Repetitions, result.LastReviewedAt, result.DueAt, result.AlgorithmState);
        }

        intervals.ShouldBe([1, 6, 15]);
    }

    [Fact]
    public void Due_and_last_reviewed_are_derived_from_now()
    {
        var result = _sut.Next(new ReviewSnapshot(6, 2.5, 2, Now.AddDays(-6), Now, null), ReviewGrade.Good, Now);

        result.LastReviewedAt.ShouldBe(Now);
        result.DueAt.ShouldBe(Now.AddDays(15));
    }

    [Fact]
    public void Wrong_answer_is_due_tomorrow()
    {
        var result = _sut.Next(new ReviewSnapshot(15, 2.5, 3, Now.AddDays(-15), Now, null), ReviewGrade.Again, Now);

        result.DueAt.ShouldBe(Now.AddDays(1));
        result.Repetitions.ShouldBe(0);
    }

    [Theory]
    [InlineData(ReviewGrade.Good, """{"lastQuality":4}""")]
    [InlineData(ReviewGrade.Again, """{"lastQuality":1}""")]
    public void Algorithm_state_records_the_last_quality(ReviewGrade grade, string expected)
    {
        var result = _sut.Next(ReviewSnapshot.New(Now), grade, Now);

        result.AlgorithmState.ShouldBe(expected);
    }

    [Fact]
    public void Name_is_sm2()
    {
        _sut.Name.ShouldBe("sm2");
        Sm2Algorithm.AlgorithmName.ShouldBe("sm2");
    }

    [Fact]
    public void Ease_never_drops_below_the_floor()
    {
        var snapshot = ReviewSnapshot.New(Now);
        for (var i = 0; i < 10; i++)
        {
            var result = _sut.Next(snapshot, ReviewGrade.Again, Now);
            result.Ease.ShouldBeGreaterThanOrEqualTo(Sm2Algorithm.EaseFloor);
            snapshot = new ReviewSnapshot(result.IntervalDays, result.Ease, result.Repetitions, result.LastReviewedAt, result.DueAt, result.AlgorithmState);
        }

        snapshot.Ease.ShouldBe(1.3, tolerance: 1e-9);
    }

    [Fact]
    public void New_snapshot_is_due_now_with_default_ease()
    {
        var snapshot = ReviewSnapshot.New(Now);

        snapshot.ShouldBe(new ReviewSnapshot(0, 2.5, 0, null, Now, null));
    }

    [Fact]
    public void Unknown_grade_is_rejected()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => _sut.Next(ReviewSnapshot.New(Now), (ReviewGrade)3, Now));
    }
}
