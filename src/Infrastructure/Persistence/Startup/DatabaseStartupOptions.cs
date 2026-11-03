namespace Spine.Infrastructure.Persistence.Startup;

/// <summary>Start-up settings for the database; bound from <c>DB_STARTUP_TIMEOUT_SECONDS</c> (default 90).</summary>
public sealed class DatabaseStartupOptions
{
    public const string TimeoutKey = "DB_STARTUP_TIMEOUT_SECONDS";

    public const int DefaultTimeoutSeconds = 90;

    /// <summary>How long to wait for the database to accept connections before giving up.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(DefaultTimeoutSeconds);
}
