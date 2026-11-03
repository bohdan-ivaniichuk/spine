using Spine.Domain.Progress;
using Spine.Infrastructure.Persistence;
using Spine.Infrastructure.Persistence.Startup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Spine.Infrastructure.Tests.Persistence;

/// <summary>Database preparation at start-up (FR-004): the block Program.cs used to hold, as an injectable component.</summary>
[Collection(PostgresCollection.Name)]
[Trait("Category", "Integration")]
public sealed class DatabaseInitializerTests(PostgresFixture fixture) : IAsyncLifetime
{
    private const string Critical = "Database start-up failed; refusing to serve";

    public Task InitializeAsync() => fixture.ResetAsync(migrate: false);

    public Task DisposeAsync() => Task.CompletedTask;

    private static (DatabaseInitializer Initializer, RecordingLogger<DatabaseInitializer> Log) Create(AppDbContext db, TimeSpan timeout)
    {
        var log = new RecordingLogger<DatabaseInitializer>();
        var initializer = new DatabaseInitializer(
            new StartupMigrator(db, NullLogger<StartupMigrator>.Instance),
            new LearnerSeeder(db),
            Options.Create(new DatabaseStartupOptions { Timeout = timeout }),
            log);
        return (initializer, log);
    }

    [Fact]
    [Trait("Requirement", "FR-004")]
    public async Task An_empty_database_is_migrated_and_seeded()
    {
        await using (var db = fixture.CreateContext())
        {
            var (initializer, log) = Create(db, TimeSpan.FromSeconds(30));

            (await initializer.InitializeAsync(CancellationToken.None)).ShouldBeTrue();
            log.Entries.ShouldNotContain(e => e.Level == LogLevel.Critical);
        }

        await using var verify = fixture.CreateContext();
        (await verify.Database.GetPendingMigrationsAsync()).ShouldBeEmpty();
        (await verify.Users.AsNoTracking().SingleAsync()).Id.ShouldBe(LocalLearner.Id);
    }

    [Fact]
    [Trait("Requirement", "FR-004")]
    public async Task An_unreachable_database_is_reported_as_critical_and_refused()
    {
        await using var db = PostgresFixture.CreateContext("Host=127.0.0.1;Port=1;Database=none;Username=none;Password=none;Timeout=1");
        var (initializer, log) = Create(db, TimeSpan.FromSeconds(1));

        var ready = await initializer.InitializeAsync(CancellationToken.None);

        ready.ShouldBeFalse();
        log.Entries.ShouldContain(e => e.Level == LogLevel.Critical && e.Message == Critical && e.Exception != null);
    }

    [Fact]
    [Trait("Requirement", "FR-004")]
    public async Task A_stop_signal_cancels_preparation_before_anything_is_applied()
    {
        using var stopping = new CancellationTokenSource();
        await stopping.CancelAsync();

        await using (var db = fixture.CreateContext())
        {
            var (initializer, log) = Create(db, TimeSpan.FromSeconds(30));

            (await initializer.InitializeAsync(stopping.Token)).ShouldBeFalse();
            log.Entries.ShouldContain(e => e.Level == LogLevel.Critical && e.Exception is OperationCanceledException);
        }

        await using var verify = fixture.CreateContext();
        (await verify.Database.GetAppliedMigrationsAsync()).ShouldBeEmpty();
    }
}
