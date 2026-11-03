using Spine.Infrastructure.Persistence.Startup;

namespace Spine.Api.Startup;

/// <summary>Runs <see cref="IDatabaseInitializer"/> once before the API serves requests.</summary>
public static class DatabaseStartup
{
    /// <summary>True when the database is ready; false after a critical log (the caller exits with code 1).</summary>
    public static async Task<bool> InitializeDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var initializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();
        return await initializer.InitializeAsync(app.Lifetime.ApplicationStopping);
    }
}
