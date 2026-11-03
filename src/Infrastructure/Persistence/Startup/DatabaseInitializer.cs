using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Spine.Infrastructure.Persistence.Startup;

/// <summary>
/// Migrates (waiting for the database within <see cref="DatabaseStartupOptions.Timeout"/>) and seeds the local learner.
/// Any failure, cancellation included, is logged as critical and reported as false, so the host refuses to serve.
/// </summary>
public sealed class DatabaseInitializer(
    StartupMigrator migrator,
    LearnerSeeder seeder,
    IOptions<DatabaseStartupOptions> options,
    ILogger<DatabaseInitializer> logger) : IDatabaseInitializer
{
    public async Task<bool> InitializeAsync(CancellationToken ct)
    {
        try
        {
            await migrator.MigrateAsync(options.Value.Timeout, ct);
            await seeder.SeedAsync(ct);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Database start-up failed; refusing to serve");
            return false;
        }
    }
}
