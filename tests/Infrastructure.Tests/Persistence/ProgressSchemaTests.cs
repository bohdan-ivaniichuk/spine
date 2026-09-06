using InterviewPrep.Application.Content;
using InterviewPrep.Domain.Progress;
using InterviewPrep.Infrastructure.Content;
using InterviewPrep.Infrastructure.Persistence;
using InterviewPrep.Infrastructure.Tests.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace InterviewPrep.Infrastructure.Tests.Persistence;

/// <summary>Principle IV: progress is separate from content and the schema itself enforces item consistency.</summary>
[Collection(PostgresCollection.Name)]
[Trait("Category", "Integration")]
public sealed class ProgressSchemaTests(PostgresFixture fixture) : IAsyncLifetime
{
    private const string NotNullViolation = "23502";
    private const string CheckViolation = "23514";
    private const string UniqueViolation = "23505";

    private static readonly string[] ContentTables =
    [
        "domains", "topics", "concepts", "concept_translations", "questions", "question_translations", "katas", "kata_translations",
    ];

    private ContentDirBuilder _content = null!;

    public async Task InitializeAsync()
    {
        await fixture.ResetAsync();
        _content = new ContentDirBuilder().WithDefaultContent();

        await using var db = fixture.CreateContext();
        var result = await new ContentImporter(db, NullLogger<ContentImporter>.Instance).ImportAsync(new ImportOptions(_content.Dir));
        result.ExitCode.ShouldBe(ImportExitCode.Success, string.Join("\n", result.Errors));
        await new LearnerSeeder(db).SeedAsync();
    }

    public Task DisposeAsync()
    {
        _content.Dispose();
        return Task.CompletedTask;
    }

    // With both FKs null the generated item_id is NULL, and PostgreSQL raises its NOT NULL violation before the CHECK is
    // evaluated; either constraint rejects the row, so both are accepted here.
    [Fact]
    public async Task Review_state_with_both_item_foreign_keys_null_is_rejected()
    {
        var ex = await Should.ThrowAsync<PostgresException>(() => InsertReviewState("question", questionId: null, kataId: null));

        ex.SqlState.ShouldBeOneOf(NotNullViolation, CheckViolation);
        (ex.ColumnName ?? ex.ConstraintName).ShouldBeOneOf("item_id", "ck_review_states_item");
    }

    [Fact]
    public async Task Review_state_with_kata_type_but_question_id_set_fails_the_item_check()
    {
        var ex = await Should.ThrowAsync<PostgresException>(() => InsertReviewState("kata", questionId: TopicTemplate.Mcq1, kataId: null));

        ex.SqlState.ShouldBe(CheckViolation);
        ex.ConstraintName.ShouldBe("ck_review_states_item");
    }

    [Fact]
    public async Task Attempt_with_both_item_foreign_keys_null_is_rejected()
    {
        var ex = await Should.ThrowAsync<PostgresException>(() => InsertAttempt("question", questionId: null, kataId: null));

        ex.SqlState.ShouldBeOneOf(NotNullViolation, CheckViolation);
        (ex.ColumnName ?? ex.ConstraintName).ShouldBeOneOf("item_id", "ck_attempts_item");
    }

    [Fact]
    public async Task Second_review_state_for_the_same_user_and_item_fails_the_unique_index()
    {
        await InsertReviewState("question", TopicTemplate.Mcq1, kataId: null);

        var ex = await Should.ThrowAsync<PostgresException>(() => InsertReviewState("question", TopicTemplate.Mcq1, kataId: null));

        ex.SqlState.ShouldBe(UniqueViolation);
        ex.ConstraintName.ShouldBe("ix_review_states_user_id_item_id");
    }

    [Fact]
    public async Task Item_id_equals_whichever_foreign_key_is_set()
    {
        var questionRow = await InsertReviewState("question", TopicTemplate.Mcq1, kataId: null);
        var kataRow = await InsertReviewState("kata", questionId: null, TopicTemplate.KataId);

        await using var db = fixture.CreateContext();
        var states = await db.ReviewStates.AsNoTracking().ToDictionaryAsync(r => r.Id);
        states[questionRow].ItemId.ShouldBe(TopicTemplate.Mcq1);
        states[kataRow].ItemId.ShouldBe(TopicTemplate.KataId);
    }

    [Fact]
    public async Task Session_with_topic_scope_but_no_topic_slug_fails_the_scope_check()
    {
        await using var db = fixture.CreateContext();
        var now = DateTimeOffset.UtcNow;

        var ex = await Should.ThrowAsync<PostgresException>(() => db.Database.ExecuteSqlAsync(
            $"""
             INSERT INTO sessions (id, user_id, started_at, finished_at, scope_type, scope_domain_slug, scope_topic_slug, answered_count, correct_count, created_at)
             VALUES ({Guid.NewGuid()}, {LocalLearner.Id}, {now}, NULL, 'topic', 'dotnet', NULL, NULL, NULL, {now})
             """));

        ex.SqlState.ShouldBe(CheckViolation);
        ex.ConstraintName.ShouldBe("ck_sessions_scope");
    }

    [Fact]
    public async Task Content_tables_have_no_user_id_column()
    {
        await using var db = fixture.CreateContext();

        var offenders = await db.Database
            .SqlQueryRaw<string>("SELECT table_name AS \"Value\" FROM information_schema.columns WHERE table_schema = 'public' AND column_name = 'user_id'")
            .ToListAsync();

        offenders.ShouldNotBeEmpty("progress tables do reference users");
        offenders.Intersect(ContentTables, StringComparer.Ordinal).ShouldBeEmpty();
    }

    private async Task<Guid> InsertReviewState(string itemType, string? questionId, string? kataId)
    {
        await using var db = fixture.CreateContext();
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        await db.Database.ExecuteSqlAsync(
            $"""
             INSERT INTO review_states (id, user_id, item_type, question_id, kata_id, algorithm, interval_days, ease, repetitions, last_reviewed_at, due_at, algorithm_state, created_at, updated_at)
             VALUES ({id}, {LocalLearner.Id}, {itemType}, {questionId}, {kataId}, 'sm2', 0, 2.5, 0, NULL, {now}, NULL, {now}, {now})
             """);
        return id;
    }

    private async Task InsertAttempt(string itemType, string? questionId, string? kataId)
    {
        await using var db = fixture.CreateContext();
        var now = DateTimeOffset.UtcNow;
        var answer = """{"option":1}""";
        await db.Database.ExecuteSqlAsync(
            $"""
             INSERT INTO attempts (id, user_id, item_type, question_id, kata_id, session_id, answered_at, answer, is_correct, score, grading, created_at)
             VALUES ({Guid.NewGuid()}, {LocalLearner.Id}, {itemType}, {questionId}, {kataId}, NULL, {now}, {answer}::jsonb, true, NULL, NULL, {now})
             """);
    }
}
