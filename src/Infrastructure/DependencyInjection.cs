using InterviewPrep.Application.Content;
using InterviewPrep.Application.Knowledge;
using InterviewPrep.Application.Review;
using InterviewPrep.Domain.Scheduling;
using InterviewPrep.Infrastructure.Content;
using InterviewPrep.Infrastructure.Knowledge;
using InterviewPrep.Infrastructure.Persistence;
using InterviewPrep.Infrastructure.Review;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
        services.AddScoped<IContentImporter, ContentImporter>();
        services.AddScoped<IKnowledgeQueries, KnowledgeQueries>();

        // Tests replace the clock with FakeTimeProvider; TryAdd keeps a registration made before this call.
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<ISchedulingAlgorithm, Sm2Algorithm>();
        services.AddScoped<IReviewSessions, ReviewSessions>();
        services.AddOptions<ReviewOptions>().Configure(o =>
        {
            o.DefaultTotalLimit = configuration.GetValue(ReviewOptions.TotalLimitKey, o.DefaultTotalLimit);
            o.DefaultNewLimit = configuration.GetValue(ReviewOptions.NewLimitKey, o.DefaultNewLimit);
        })
        .Validate(
            o => ReviewOptions.IsWithinBounds(o.DefaultTotalLimit) && ReviewOptions.IsWithinBounds(o.DefaultNewLimit),
            $"{ReviewOptions.TotalLimitKey} and {ReviewOptions.NewLimitKey} must be {ReviewOptions.MinLimit}..{ReviewOptions.MaxLimit}.")
        .ValidateOnStart();
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
