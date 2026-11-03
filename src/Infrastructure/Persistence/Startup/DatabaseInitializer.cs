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
    public Task<bool> InitializeAsync(CancellationToken ct)
    {
        _ = (migrator, seeder, options, logger);
        throw new NotImplementedException();
    }
}
