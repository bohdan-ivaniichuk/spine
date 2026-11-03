using Spine.Application.Content;
using Spine.Application.Knowledge;
using Spine.Application.Review;
using Spine.Domain.Scheduling;
using Spine.Infrastructure.Content;
using Spine.Infrastructure.Knowledge;
using Spine.Infrastructure.Persistence;
using Spine.Infrastructure.Persistence.Startup;
using Spine.Infrastructure.Review;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Spine.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "Default";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? AppDbContextDesignTimeFactory.FallbackConnectionString;

        services.AddDbContext<AppDbContext>(options => ConfigureDbContext(options, connectionString));
        services.AddScoped<StartupMigrator>();
        services.AddScoped<LearnerSeeder>();
        services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();
        services.AddOptions<DatabaseStartupOptions>().Configure(o => o.Timeout = TimeSpan.FromSeconds(
            configuration.GetValue(DatabaseStartupOptions.TimeoutKey, DatabaseStartupOptions.DefaultTimeoutSeconds)));
        services.AddScoped<IContentImporter, ContentImporter>();
        services.AddScoped<IKnowledgeQueries, KnowledgeQueries>();

        // Tests replace the clock with FakeTimeProvider; TryAdd keeps a registration made before this call.
        services.TryAddSingleton<TimeProvider>(PostgresTimeProvider.SystemTruncated);
        services.AddReview(configuration);
        return services;
    }

    public static void ConfigureDbContext(DbContextOptionsBuilder options, string connectionString)
    {
        options
            .UseNpgsql(connectionString, npgsql => npgsql
                .EnableRetryOnFailure(maxRetryCount: 3)
                .MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name))
            .UseSnakeCaseNamingConvention();
    }
}
