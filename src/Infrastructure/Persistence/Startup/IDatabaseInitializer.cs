namespace Spine.Infrastructure.Persistence.Startup;

/// <summary>Prepares the database before the API serves requests: migrate within the start-up timeout, then seed.</summary>
public interface IDatabaseInitializer
{
    /// <summary>True when the database is ready; false after logging a critical error (the host then exits with code 1).</summary>
    Task<bool> InitializeAsync(CancellationToken ct);
}
