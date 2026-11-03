using System.Data.Common;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Spine.Application.Content.Import;
using Spine.Application.Review.Readiness;
using Spine.Domain.Progress;
using Spine.Infrastructure.Content;
using Spine.Infrastructure.Persistence;
using Spine.Infrastructure.Persistence.Startup;
using Spine.Infrastructure.Review.Readiness;
using Spine.Infrastructure.Tests.Content;

namespace Spine.Infrastructure.Tests.Review;

/// <summary>
/// Readiness against a real PostgreSQL. Content: dotnet/clr-memory (stack-vs-heap: mcq.1, mcq.2, open.1; span-memory:
/// mcq.1, mcq.2, open.1), dotnet/async (tasks: one MCQ) and dotnet/generics (in the manifest, no content file).
/// Review states are inserted directly so each test pins exact intervals.
/// </summary>
[Collection(PostgresCollection.Name)]
[Trait("Category", "Integration")]
public sealed class ReadinessQueriesTests(PostgresFixture fixture) : IAsyncLifetime
{
    private const string AsyncMcq = "dotnet.async.tasks.mcq.1";
    private static readonly DateTimeOffset Now = new(2026, 10, 15, 10, 0, 0, TimeSpan.Zero);

    private ContentDirBuilder _content = null!;

    public async Task InitializeAsync()
    {
        await fixture.ResetAsync();
        _content = new ContentDirBuilder().WithDefaultContent();
        var manifest = TopicTemplate.Manifest();
        var topics = (JsonArray)manifest["domains"]![0]!["topics"]!;
        topics.Add(new JsonObject { ["slug"] = "async", ["title"] = "Async & Threading" });
        topics.Add(new JsonObject { ["slug"] = "generics", ["title"] = "Generics" });
        _content.WriteManifest(manifest);
        _content.WriteTopic(AsyncTopic());

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

    [Fact]
    [Trait("Requirement", "FR-009")]
    public async Task Percentages_match_a_hand_calculation_for_every_domain_and_topic()
    {
        await InsertStateAsync(TopicTemplate.Mcq1, 15);
        await InsertStateAsync(TopicTemplate.Mcq2, 7);
        await InsertStateAsync(TopicTemplate.SpanMcq1, 6);

        var readiness = await GetAsync();

        // stack-vs-heap ready, span-memory in progress, tasks not started: dotnet 1 of 3 = 33 (floor).
        var dotnet = readiness.Domains.ShouldHaveSingleItem();
        (dotnet.Domain, dotnet.Ready, dotnet.Counted, dotnet.Percent).ShouldBe(("dotnet", 1, 3, 33));
        dotnet.Topics.Select(t => (t.Topic, t.Ready, t.Counted, t.Percent)).ShouldBe(
        [
            ("clr-memory", 1, 2, 50),
            ("async", 0, 1, 0),
            ("generics", 0, 0, (int?)null),
        ]);
        dotnet.Topics[0].Concepts.ShouldBe(
        [
            new ConceptReadinessDto(TopicTemplate.StackVsHeap, "ready", 2, 2),
            new ConceptReadinessDto(TopicTemplate.SpanMemory, "in-progress", 2, 0),
        ]);
        dotnet.Topics[1].Concepts.ShouldBe([new ConceptReadinessDto("tasks", "not-started", 1, 0)]);
        dotnet.Topics[2].Concepts.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Requirement", "FR-008")]
    public async Task Open_questions_are_not_counted_until_reviews_schedule_them()
    {
        await InsertStateAsync(TopicTemplate.Mcq1, 7);
        await InsertStateAsync(TopicTemplate.Mcq2, 7);
        await InsertStateAsync(TopicTemplate.Open1, 0);
        await InsertStateAsync(TopicTemplate.SpanOpen1, 30);

        var concepts = (await GetAsync()).Domains[0].Topics[0].Concepts;

        concepts.ShouldBe(
        [
            new ConceptReadinessDto(TopicTemplate.StackVsHeap, "ready", 2, 2),
            new ConceptReadinessDto(TopicTemplate.SpanMemory, "not-started", 2, 0),
        ]);
    }

    [Fact]
    [Trait("Requirement", "FR-008")]
    public async Task Deprecated_questions_and_concepts_do_not_count()
    {
        await InsertStateAsync(TopicTemplate.SpanMcq1, 7);
        await using (var db = fixture.CreateContext())
        {
            await db.Database.ExecuteSqlAsync($"UPDATE questions SET deprecated_at = {Now} WHERE id = {TopicTemplate.SpanMcq2}");
            await db.Database.ExecuteSqlAsync($"UPDATE concepts SET deprecated_at = {Now} WHERE slug = 'tasks'");
        }

        var topics = (await GetAsync()).Domains[0].Topics;

        topics[0].Concepts.Single(c => c.Concept == TopicTemplate.SpanMemory).ShouldBe(new ConceptReadinessDto(TopicTemplate.SpanMemory, "ready", 1, 1));
        topics[1].Concepts.ShouldBeEmpty();
        (topics[1].Counted, topics[1].Percent).ShouldBe((0, (int?)null));
    }

    [Fact]
    [Trait("Requirement", "FR-010")]
    public async Task Only_the_local_learners_review_states_count()
    {
        var other = Guid.Parse("22222222-2222-2222-2222-222222222222");
        await using (var db = fixture.CreateContext())
        {
            db.Users.Add(new Learner { Id = other, DisplayName = "Someone else", CreatedAt = Now });
            await db.SaveChangesAsync();
        }

        await InsertStateAsync(AsyncMcq, 30, other);

        var async = (await GetAsync()).Domains[0].Topics.Single(t => t.Topic == "async");

        async.Concepts.ShouldBe([new ConceptReadinessDto("tasks", "not-started", 1, 0)]);
        async.Percent.ShouldBe(0);
    }

    [Fact]
    [Trait("Requirement", "FR-009")]
    public async Task Query_count_does_not_grow_with_content()
    {
        await InsertStateAsync(TopicTemplate.Mcq1, 7);
        var counter = new CommandCounter();
        var options = new DbContextOptionsBuilder<AppDbContext>();
        DependencyInjection.ConfigureDbContext(options, fixture.ConnectionString);
        options.AddInterceptors(counter);
        await using var db = new AppDbContext(options.Options);

        await new ReadinessQueries(db).GetAsync();

        // domains, topics, concepts, one grouped count — for 3 topics and 3 concepts alike.
        counter.Count.ShouldBe(4);
    }

    private sealed class CommandCounter : DbCommandInterceptor
    {
        public int Count { get; private set; }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Count++;
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }

    private async Task<ReadinessDto> GetAsync()
    {
        await using var db = fixture.CreateContext();
        return await new ReadinessQueries(db).GetAsync();
    }

    private async Task InsertStateAsync(string questionId, int intervalDays, Guid? userId = null)
    {
        await using var db = fixture.CreateContext();
        await db.Database.ExecuteSqlAsync(
            $"""
             INSERT INTO review_states (id, user_id, item_type, question_id, kata_id, algorithm, interval_days, ease, repetitions, last_reviewed_at, due_at, algorithm_state, created_at, updated_at)
             VALUES ({Guid.NewGuid()}, {userId ?? LocalLearner.Id}, 'question', {questionId}, NULL, 'sm2', {intervalDays}, 2.5, 1, {Now}, {Now.AddDays(intervalDays)}, NULL, {Now}, {Now})
             """);
    }

    private static JsonObject AsyncTopic() => new()
    {
        ["domain"] = "dotnet",
        ["topic"] = "async",
        ["concepts"] = new JsonArray(new JsonObject
        {
            ["slug"] = "tasks",
            ["title"] = "Tasks",
            ["level"] = "M",
            ["translations"] = new JsonObject { ["en"] = new JsonObject { ["title"] = "Tasks", ["note"] = TopicTemplate.Note("Tasks") } },
            ["questions"] = new JsonArray(new JsonObject
            {
                ["id"] = AsyncMcq,
                ["type"] = "mcq",
                ["level"] = "M",
                ["reviewed"] = true,
                ["translations"] = new JsonObject { ["en"] = TopicTemplate.McqTranslation("Which type represents a hot task?") },
                ["tags"] = new JsonArray("async"),
            }),
            ["katas"] = new JsonArray(),
        }),
    };
}
