using InterviewPrep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace InterviewPrep.Infrastructure.Tests.Persistence;

[Collection(PostgresCollection.Name)]
[Trait("Category", "Integration")]
public sealed class MigrationsTests(PostgresFixture fixture) : IAsyncLifetime
{
    private static readonly string[] ExpectedTables =
    [
        "domains", "topics", "concepts", "concept_translations", "questions", "question_translations", "katas", "kata_translations",
        "users", "review_states", "attempts", "sessions",
    ];

    public Task InitializeAsync() => fixture.ResetAsync(migrate: false);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task MigrateAsync_on_an_empty_database_applies_the_initial_migration()
    {
        await using var db = fixture.CreateContext();
        (await db.Database.GetPendingMigrationsAsync()).ShouldNotBeEmpty();

        await Migrate(db);

        (await db.Database.GetPendingMigrationsAsync()).ShouldBeEmpty();
        (await db.Database.GetAppliedMigrationsAsync()).Count().ShouldBe(1);
    }

    [Fact]
    public async Task Second_MigrateAsync_is_a_no_op()
    {
        await using var db = fixture.CreateContext();
        await Migrate(db);
        var applied = (await db.Database.GetAppliedMigrationsAsync()).ToList();

        await Migrate(db);

        (await db.Database.GetAppliedMigrationsAsync()).ShouldBe(applied);
        (await db.Database.GetPendingMigrationsAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task Schema_has_exactly_the_content_and_progress_tables()
    {
        await using var db = fixture.CreateContext();
        await Migrate(db);

        var tables = await Scalars(db,
            "SELECT table_name AS \"Value\" FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE' AND table_name <> '__EFMigrationsHistory'");

        tables.OrderBy(t => t, StringComparer.Ordinal).ShouldBe(ExpectedTables.OrderBy(t => t, StringComparer.Ordinal));
    }

    [Fact]
    public async Task Unique_indexes_exist()
    {
        await using var db = fixture.CreateContext();
        await Migrate(db);

        var unique = await Scalars(db,
            "SELECT indexname AS \"Value\" FROM pg_indexes WHERE schemaname = 'public' AND indexdef LIKE 'CREATE UNIQUE INDEX%'");

        unique.ShouldContain("ix_domains_slug");
        unique.ShouldContain("ix_topics_domain_id_slug");
        unique.ShouldContain("ix_concepts_topic_id_slug");
        unique.ShouldContain("ix_review_states_user_id_item_id");
    }

    [Fact]
    public async Task Check_constraints_exist()
    {
        await using var db = fixture.CreateContext();
        await Migrate(db);

        var checks = await Scalars(db,
            "SELECT constraint_name AS \"Value\" FROM information_schema.table_constraints WHERE table_schema = 'public' AND constraint_type = 'CHECK'");

        checks.ShouldContain("ck_questions_type");
        checks.ShouldContain("ck_question_translations_options_xor_rubric");
        checks.ShouldContain("ck_review_states_item");
        checks.ShouldContain("ck_sessions_scope");
    }

    [Fact]
    public async Task Review_states_item_id_is_a_stored_generated_column()
    {
        await using var db = fixture.CreateContext();
        await Migrate(db);

        var generated = await Scalars(db,
            "SELECT is_generated AS \"Value\" FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'review_states' AND column_name = 'item_id'");

        generated.ShouldBe(["ALWAYS"]);
    }

    private static Task Migrate(AppDbContext db) =>
        new StartupMigrator(db, NullLogger<StartupMigrator>.Instance).MigrateAsync(TimeSpan.FromSeconds(30));

    private static Task<List<string>> Scalars(AppDbContext db, string sql) => db.Database.SqlQueryRaw<string>(sql).ToListAsync();
}
