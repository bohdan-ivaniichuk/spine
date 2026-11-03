namespace Spine.Infrastructure.Persistence;

/// <summary>
/// A clock rounded down to PostgreSQL's <c>timestamptz</c> precision (1 µs). A value handed to a client before
/// <c>SaveChanges</c> then equals the one read back later: on Linux the system clock ticks every 100 ns, so an
/// unrounded "finished at" changed between the first and a repeated finish of the same session.
/// </summary>
public sealed class PostgresTimeProvider(TimeProvider inner) : TimeProvider
{
    public static PostgresTimeProvider SystemTruncated { get; } = new(System);

    public override DateTimeOffset GetUtcNow()
    {
        var now = inner.GetUtcNow();
        return now.AddTicks(-(now.Ticks % TimeSpan.TicksPerMicrosecond));
    }

    public override TimeZoneInfo LocalTimeZone => inner.LocalTimeZone;

    public override long TimestampFrequency => inner.TimestampFrequency;

    public override long GetTimestamp() => inner.GetTimestamp();

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) =>
        inner.CreateTimer(callback, state, dueTime, period);
}
