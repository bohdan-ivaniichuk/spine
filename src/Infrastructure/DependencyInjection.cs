using InterviewPrep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InterviewPrep.Infrastructure;

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
