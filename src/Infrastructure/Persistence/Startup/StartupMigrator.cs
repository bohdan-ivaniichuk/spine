using Spine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Spine.Infrastructure.Persistence.Startup;

/// <summary>
/// Waits for the database (bounded), then applies pending migrations once. Throws on failure so the host
/// exits non-zero and never serves on a half-migrated schema.
/// </summary>
public sealed class StartupMigrator(AppDbContext db, ILogger<StartupMigrator> logger)
{
    public static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(3);

    public async Task MigrateAsync(TimeSpan timeout, CancellationToken ct = default)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        var attempt = 0;
        while (true)
        {
            attempt++;
            try
            {
                if (await db.Database.CanConnectAsync(ct))
                {
                    break;
                }

                logger.LogWarning("Database not reachable yet (attempt {Attempt})", attempt);
            }
            catch (Exception ex) when (DateTimeOffset.UtcNow + RetryDelay < deadline)
            {
                logger.LogWarning(ex, "Database not reachable yet (attempt {Attempt})", attempt);
            }

            if (DateTimeOffset.UtcNow + RetryDelay >= deadline)
            {
                throw new TimeoutException($"Database not reachable after {timeout.TotalSeconds:F0}s.");
            }

            await Task.Delay(RetryDelay, ct);
        }

        var pending = (await db.Database.GetPendingMigrationsAsync(ct)).ToList();
        if (pending.Count == 0)
        {
            logger.LogInformation("Database schema is up to date");
            return;
        }

        logger.LogInformation("Applying {Count} migration(s): {Migrations}", pending.Count, string.Join(", ", pending));
        await db.Database.MigrateAsync(ct);
        logger.LogInformation("Migrations applied");
    }
}
