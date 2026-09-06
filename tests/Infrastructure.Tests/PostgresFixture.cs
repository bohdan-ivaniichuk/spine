using InterviewPrep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace InterviewPrep.Infrastructure.Tests;

/// <summary>
/// One PostgreSQL 16 container per test collection. Each test class resets the schema in <c>InitializeAsync</c>
/// so tests start from an empty (or freshly migrated) database.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    /// <summary>A fresh context configured exactly like production (<see cref="DependencyInjection.ConfigureDbContext"/>).</summary>
    public AppDbContext CreateContext() => CreateContext(ConnectionString);

    public static AppDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>();
        DependencyInjection.ConfigureDbContext(options, connectionString);
        return new AppDbContext(options.Options);
    }

    /// <summary>Drops and recreates the <c>public</c> schema; applies migrations unless <paramref name="migrate"/> is false.</summary>
    public async Task ResetAsync(bool migrate = true)
    {
        await using var db = CreateContext();
        await db.Database.ExecuteSqlRawAsync("DROP SCHEMA public CASCADE; CREATE SCHEMA public;");
        if (migrate)
        {
            await db.Database.MigrateAsync();
        }
    }

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
