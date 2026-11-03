using Spine.Infrastructure.Persistence;
using Microsoft.Extensions.Time.Testing;

namespace Spine.Infrastructure.Tests.Persistence;

public sealed class PostgresTimeProviderTests
{
    private static readonly DateTimeOffset Microsecond = new(2026, 11, 3, 14, 3, 59, TimeSpan.Zero);

    [Fact]
    public void Sub_microsecond_ticks_are_dropped_so_the_value_survives_a_timestamptz_round_trip()
    {
        // Linux clocks tick every 100 ns; PostgreSQL keeps 1 µs. 7 ticks = 700 ns.
        var clock = new PostgresTimeProvider(new FakeTimeProvider(Microsecond.AddTicks(12 * TimeSpan.TicksPerMicrosecond + 7)));

        clock.GetUtcNow().ShouldBe(Microsecond.AddTicks(12 * TimeSpan.TicksPerMicrosecond));
    }

    [Fact]
    public void A_whole_microsecond_is_returned_unchanged()
    {
        var clock = new PostgresTimeProvider(new FakeTimeProvider(Microsecond.AddTicks(42 * TimeSpan.TicksPerMicrosecond)));

        clock.GetUtcNow().ShouldBe(Microsecond.AddTicks(42 * TimeSpan.TicksPerMicrosecond));
    }

    [Fact]
    public void Timestamps_and_time_zone_come_from_the_inner_clock()
    {
        var inner = new FakeTimeProvider(Microsecond);
        var clock = new PostgresTimeProvider(inner);

        var before = clock.GetTimestamp();
        inner.Advance(TimeSpan.FromSeconds(2));

        clock.GetElapsedTime(before).ShouldBe(TimeSpan.FromSeconds(2));
        clock.LocalTimeZone.ShouldBe(inner.LocalTimeZone);
    }
}
