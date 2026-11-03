using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Spine.Application.Content.Import;
using Spine.Application.Content.Summary;
using Spine.Domain.Content;
using Spine.Domain.Progress;
using Spine.Infrastructure.Content;
using Spine.Infrastructure.Persistence.Startup;

namespace Spine.Infrastructure.Tests.Content;

[Collection(PostgresCollection.Name)]
[Trait("Category", "Integration")]
public sealed class ContentImporterTests(PostgresFixture fixture) : IAsyncLifetime
{
    private const string UnreachableConnectionString = "Host=127.0.0.1;Port=1;Database=nope;Username=nope;Password=nope;Timeout=1";

    private ContentDirBuilder _content = null!;

    public async Task InitializeAsync()
    {
        await fixture.ResetAsync();
        _content = new ContentDirBuilder().WithDefaultContent();
    }

    public Task DisposeAsync()
    {
        _content.Dispose();
        return Task.CompletedTask;
    }

    // (a)
    [Fact]
    public async Task Import_creates_every_entity_of_the_fixture()
    {
        var result = await ImportAsync();

        result.ExitCode.ShouldBe(ImportExitCode.Success, Describe(result));
        Total(result, EntityKinds.Domain).Created.ShouldBe(1);
        Total(result, EntityKinds.Topic).Created.ShouldBe(1);
        Total(result, EntityKinds.Concept).Created.ShouldBe(TopicTemplate.ConceptCount);
        Total(result, EntityKinds.Question).Created.ShouldBe(TopicTemplate.QuestionCount);
        Total(result, EntityKinds.Kata).Created.ShouldBe(TopicTemplate.KataCount);
        Total(result, EntityKinds.Translation).Created.ShouldBe(TopicTemplate.TranslationCount);

        await using var db = fixture.CreateContext();
        (await db.Domains.Select(d => d.Slug).ToListAsync()).ShouldBe([TopicTemplate.Domain]);
        (await db.Topics.Select(t => t.Slug).ToListAsync()).ShouldBe([TopicTemplate.Topic]);
        (await db.Concepts.OrderBy(c => c.SortOrder).Select(c => c.Slug).ToListAsync()).ShouldBe([TopicTemplate.StackVsHeap, TopicTemplate.SpanMemory]);
        (await db.Questions.Select(q => q.Id).ToListAsync()).ShouldBe(TopicTemplate.QuestionIds, ignoreOrder: true);
        (await db.Katas.Select(k => k.Id).ToListAsync()).ShouldBe([TopicTemplate.KataId]);
        (await db.ConceptTranslations.CountAsync()).ShouldBe(3);
        (await db.QuestionTranslations.CountAsync()).ShouldBe(7);
        (await db.KataTranslations.CountAsync()).ShouldBe(2);
        (await db.Questions.AnyAsync(q => q.DeprecatedAt != null)).ShouldBeFalse();
    }

    // (b)
    [Fact]
    public async Task Reimport_of_identical_content_changes_nothing()
    {
        await ImportAsync();
        var before = await UpdatedAtSnapshotAsync();

        var result = await ImportAsync();

        result.ExitCode.ShouldBe(ImportExitCode.Success, Describe(result));
        foreach (var kind in EntityKinds.All)
        {
            var counts = Total(result, kind);
            (counts.Created, counts.Updated, counts.Deprecated).ShouldBe((0, 0, 0), kind);
        }

        Total(result, EntityKinds.Translation).Unchanged.ShouldBe(TopicTemplate.TranslationCount);
        (await UpdatedAtSnapshotAsync()).ShouldBe(before);
    }

    // (c)
    [Fact]
    public async Task Editing_a_note_and_an_option_text_updates_exactly_two_translations_in_place()
    {
        await ImportAsync();
        var idsBefore = await IdSnapshotAsync();

        var topic = TopicTemplate.ClrMemory();
        StackVsHeap(topic)["translations"]!["en"]!["note"] = TopicTemplate.Note("Stack vs Heap, revised");
        Question(topic, 0)["translations"]!["en"]!["options"]![0]!["text"] = "Option A, reworded";
        _content.WriteTopic(topic);

        var result = await ImportAsync();

        result.ExitCode.ShouldBe(ImportExitCode.Success, Describe(result));
        Total(result, EntityKinds.Translation).Updated.ShouldBe(2);
        Total(result, EntityKinds.Translation).Created.ShouldBe(0);
        Total(result, EntityKinds.Concept).Updated.ShouldBe(0);
        Total(result, EntityKinds.Question).Updated.ShouldBe(0);
        (await IdSnapshotAsync()).ShouldBe(idsBefore);

        await using var db = fixture.CreateContext();
        var note = await db.ConceptTranslations.Where(t => t.Language == Language.En && t.Concept!.Slug == TopicTemplate.StackVsHeap).Select(t => t.Note).SingleAsync();
        note.ShouldStartWith("## Stack vs Heap, revised");
        var options = await db.QuestionTranslations.Where(t => t.QuestionId == TopicTemplate.Mcq1 && t.Language == Language.En).Select(t => t.Options).SingleAsync();
        options![0].Text.ShouldBe("Option A, reworded");
    }

    // (d)
    [Fact]
    public async Task Reimport_after_changing_a_question_leaves_its_progress_rows_byte_identical()
    {
        await ImportAsync();
        await SeedProgressAsync(TopicTemplate.Mcq1);
        var before = await ProgressRowsAsync();
        before.Count.ShouldBe(2);

        var topic = TopicTemplate.ClrMemory();
        Question(topic, 0)["translations"]!["en"]!["prompt"] = "A completely new prompt";
        Question(topic, 0)["level"] = "L";
        _content.WriteTopic(topic);

        var result = await ImportAsync();

        result.ExitCode.ShouldBe(ImportExitCode.Success, Describe(result));
        Total(result, EntityKinds.Question).Updated.ShouldBe(1);
        Total(result, EntityKinds.Translation).Updated.ShouldBe(1);
        (await ProgressRowsAsync()).ShouldBe(before);
    }

    // (e)
    [Fact]
    public async Task Removing_a_question_deprecates_it_and_keeps_its_translations_and_progress()
    {
        await ImportAsync();
        await SeedProgressAsync(TopicTemplate.Open1);
        var progressBefore = await ProgressRowsAsync();

        var topic = TopicTemplate.ClrMemory();
        Questions(topic).RemoveAt(2); // open.1 is last, so the other questions keep their sort order
        _content.WriteTopic(topic);

        var result = await ImportAsync();

        result.ExitCode.ShouldBe(ImportExitCode.Success, Describe(result));
        Total(result, EntityKinds.Question).Deprecated.ShouldBe(1);
        Total(result, EntityKinds.Question).Updated.ShouldBe(0);
        Total(result, EntityKinds.Concept).Deprecated.ShouldBe(0);

        await using var db = fixture.CreateContext();
        var question = await db.Questions.Include(q => q.Translations).SingleAsync(q => q.Id == TopicTemplate.Open1);
        question.DeprecatedAt.ShouldNotBeNull();
        question.Translations.Count.ShouldBe(1);
        (await db.Questions.CountAsync()).ShouldBe(TopicTemplate.QuestionCount);
        (await ProgressRowsAsync()).ShouldBe(progressBefore);
    }

    // (f)
    [Fact]
    public async Task Re_adding_a_deprecated_question_reactivates_it_as_an_update()
    {
        await ImportAsync();
        var removed = TopicTemplate.ClrMemory();
        Questions(removed).RemoveAt(2);
        _content.WriteTopic(removed);
        await ImportAsync();
        _content.WriteTopic(TopicTemplate.ClrMemory());

        var result = await ImportAsync();

        result.ExitCode.ShouldBe(ImportExitCode.Success, Describe(result));
        Total(result, EntityKinds.Question).Updated.ShouldBe(1);
        Total(result, EntityKinds.Question).Created.ShouldBe(0);
        Total(result, EntityKinds.Question).Deprecated.ShouldBe(0);

        await using var db = fixture.CreateContext();
        (await db.Questions.Where(q => q.Id == TopicTemplate.Open1).Select(q => q.DeprecatedAt).SingleAsync()).ShouldBeNull();
        (await db.Questions.CountAsync()).ShouldBe(TopicTemplate.QuestionCount);
    }

    // (g)
    [Fact]
    public async Task Removing_a_uk_translation_from_the_file_leaves_the_existing_uk_row_untouched()
    {
        await ImportAsync();
        var before = await UpdatedAtSnapshotAsync();

        var topic = TopicTemplate.ClrMemory();
        ((JsonObject)StackVsHeap(topic)["translations"]!).Remove("uk");
        ((JsonObject)Question(topic, 0)["translations"]!).Remove("uk");
        _content.WriteTopic(topic);

        var result = await ImportAsync();

        result.ExitCode.ShouldBe(ImportExitCode.Success, Describe(result));
        Total(result, EntityKinds.Translation).Updated.ShouldBe(0);
        Total(result, EntityKinds.Translation).Deprecated.ShouldBe(0);

        await using var db = fixture.CreateContext();
        var ukNote = await db.ConceptTranslations.SingleAsync(t => t.Language == Language.Uk && t.Concept!.Slug == TopicTemplate.StackVsHeap);
        ukNote.Title.ShouldBe("Stack vs Heap (uk)");
        var ukQuestion = await db.QuestionTranslations.SingleAsync(t => t.QuestionId == TopicTemplate.Mcq1 && t.Language == Language.Uk);
        ukQuestion.Prompt.ShouldEndWith("(uk)");
        (await UpdatedAtSnapshotAsync()).ShouldBe(before);
    }

    // (h)
    [Fact]
    public async Task An_invalid_file_among_valid_ones_fails_validation_and_writes_nothing()
    {
        await ImportAsync();
        var idsBefore = await IdSnapshotAsync();
        var updatedBefore = await UpdatedAtSnapshotAsync();

        var manifest = TopicTemplate.Manifest();
        ((JsonArray)manifest["domains"]![0]!["topics"]!).Add(new JsonObject { ["slug"] = "async", ["title"] = "Async" });
        _content.WriteManifest(manifest);
        var edited = TopicTemplate.ClrMemory();
        Question(edited, 0)["translations"]!["en"]!["prompt"] = "Edited prompt that must not be persisted";
        _content.WriteTopic(edited);
        var invalid = TopicTemplate.ClrMemory();
        invalid["topic"] = "async";
        var concept = StackVsHeap(invalid);
        concept["slug"] = "tasks";
        concept["katas"] = new JsonArray();
        var questions = Questions(invalid);
        questions.Clear();
        var twoCorrect = TopicTemplate.McqTranslation("Which two are right?");
        twoCorrect["options"]![0]!["correct"] = true;
        questions.Add(new JsonObject
        {
            ["id"] = "dotnet.async.tasks.mcq.1",
            ["type"] = "mcq",
            ["level"] = "M",
            ["reviewed"] = true,
            ["translations"] = new JsonObject { ["en"] = twoCorrect },
            ["tags"] = new JsonArray(),
        });
        ((JsonArray)invalid["concepts"]!).RemoveAt(1);
        _content.WriteTopic(invalid);

        var result = await ImportAsync();

        result.ExitCode.ShouldBe(ImportExitCode.ValidationFailed, Describe(result));
        result.Summary.ShouldBeNull();
        var error = result.Errors.ShouldHaveSingleItem();
        error.Rule.ShouldBe("mcq-one-correct");
        error.File.ShouldBe("dotnet/async.json");

        await using var db = fixture.CreateContext();
        (await IdSnapshotAsync()).ShouldBe(idsBefore);
        (await UpdatedAtSnapshotAsync()).ShouldBe(updatedBefore);
        (await db.Topics.CountAsync()).ShouldBe(1);
        (await db.QuestionTranslations.Where(t => t.QuestionId == TopicTemplate.Mcq1 && t.Language == Language.En).Select(t => t.Prompt).SingleAsync())
            .ShouldNotContain("Edited prompt");
    }

    // (i)
    [Fact]
    public async Task Import_exits_with_AlreadyInProgress_while_the_advisory_lock_is_held_elsewhere()
    {
        await using var other = new NpgsqlConnection(fixture.ConnectionString);
        await other.OpenAsync();
        await using (var tx = await other.BeginTransactionAsync())
        {
            await using (var lockCmd = new NpgsqlCommand($"SELECT pg_advisory_lock({ContentImporter.AdvisoryLockKey})", other, tx))
            {
                await lockCmd.ExecuteNonQueryAsync();
            }

            try
            {
                var result = await ImportAsync();

                result.ExitCode.ShouldBe(ImportExitCode.AlreadyInProgress, Describe(result));
                result.Summary.ShouldBeNull();
                await using var db = fixture.CreateContext();
                (await db.Domains.CountAsync()).ShouldBe(0);
            }
            finally
            {
                await using var unlock = new NpgsqlCommand($"SELECT pg_advisory_unlock({ContentImporter.AdvisoryLockKey})", other, tx);
                await unlock.ExecuteNonQueryAsync();
                await tx.RollbackAsync();
            }
        }

        (await ImportAsync()).ExitCode.ShouldBe(ImportExitCode.Success);
    }

    // (j)
    [Fact]
    public async Task Dry_run_reports_the_changes_but_leaves_the_database_untouched()
    {
        await ImportAsync();
        var updatedBefore = await UpdatedAtSnapshotAsync();
        var topic = TopicTemplate.ClrMemory();
        StackVsHeap(topic)["translations"]!["en"]!["note"] = TopicTemplate.Note("Dry-run note");
        Questions(topic).RemoveAt(2);
        _content.WriteTopic(topic);

        var result = await ImportAsync(dryRun: true);

        result.ExitCode.ShouldBe(ImportExitCode.Success, Describe(result));
        result.Summary.ShouldNotBeNull();
        result.Summary.HasChanges.ShouldBeTrue();
        Total(result, EntityKinds.Translation).Updated.ShouldBe(1);
        Total(result, EntityKinds.Question).Deprecated.ShouldBe(1);

        await using var db = fixture.CreateContext();
        (await UpdatedAtSnapshotAsync()).ShouldBe(updatedBefore);
        (await db.Questions.AnyAsync(q => q.DeprecatedAt != null)).ShouldBeFalse();
        (await db.ConceptTranslations.AnyAsync(t => t.Note.Contains("Dry-run note"))).ShouldBeFalse();
    }

    // (j) on an empty database
    [Fact]
    public async Task Dry_run_on_an_empty_database_writes_no_rows()
    {
        var result = await ImportAsync(dryRun: true);

        result.ExitCode.ShouldBe(ImportExitCode.Success, Describe(result));
        Total(result, EntityKinds.Question).Created.ShouldBe(TopicTemplate.QuestionCount);

        await using var db = fixture.CreateContext();
        (await db.Domains.CountAsync()).ShouldBe(0);
        (await db.Questions.CountAsync()).ShouldBe(0);
    }

    // (k)
    [Fact]
    public async Task Validate_only_needs_no_database()
    {
        await using var db = PostgresFixture.CreateContext(UnreachableConnectionString);
        var importer = new ContentImporter(db, NullLogger<ContentImporter>.Instance);

        var result = await importer.ImportAsync(new ImportOptions(_content.Dir, ValidateOnly: true));

        result.ExitCode.ShouldBe(ImportExitCode.Success, Describe(result));
        result.Summary.ShouldBeNull();
    }

    [Fact]
    public async Task Validate_only_reports_rule_violations_without_a_database()
    {
        var topic = TopicTemplate.ClrMemory();
        Question(topic, 0)["translations"]!["en"]!["options"]![0]!["correct"] = true;
        _content.WriteTopic(topic);
        await using var db = PostgresFixture.CreateContext(UnreachableConnectionString);
        var importer = new ContentImporter(db, NullLogger<ContentImporter>.Instance);

        var result = await importer.ImportAsync(new ImportOptions(_content.Dir, ValidateOnly: true));

        result.ExitCode.ShouldBe(ImportExitCode.ValidationFailed);
        result.Errors.ShouldContain(e => e.Rule == "mcq-one-correct" && e.File == TopicTemplate.File);
    }

    [Fact]
    public async Task Uk_titles_are_stored_and_reimporting_them_unchanged_writes_nothing()
    {
        _content.WithFixtureManifestSchema().WriteManifest(TopicTemplate.ManifestWithUkTitles("Середовище .NET", "Пам'ять і GC"));

        (await ImportAsync()).ExitCode.ShouldBe(ImportExitCode.Success);
        var (domain, topic) = await DomainAndTopicAsync();
        domain.TitleUk.ShouldBe("Середовище .NET");
        topic.TitleUk.ShouldBe("Пам'ять і GC");
        domain.Title.ShouldBe("C# & .NET Runtime");

        var again = await ImportAsync();

        again.ExitCode.ShouldBe(ImportExitCode.Success, Describe(again));
        foreach (var kind in EntityKinds.All)
        {
            var counts = Total(again, kind);
            (counts.Created, counts.Updated, counts.Deprecated).ShouldBe((0, 0, 0), kind);
        }

        (await DomainAndTopicAsync()).Domain.UpdatedAt.ShouldBe(domain.UpdatedAt);
    }

    [Fact]
    public async Task Adding_changing_and_removing_uk_titles_updates_only_the_affected_rows()
    {
        _content.WithFixtureManifestSchema();
        (await ImportAsync()).ExitCode.ShouldBe(ImportExitCode.Success);
        var ids = await IdSnapshotAsync();

        _content.WriteManifest(TopicTemplate.ManifestWithUkTitles("Середовище .NET", null));
        var added = await ImportAsync();
        added.ExitCode.ShouldBe(ImportExitCode.Success, Describe(added));
        (Total(added, EntityKinds.Domain).Updated, Total(added, EntityKinds.Topic).Updated).ShouldBe((1, 0));
        (await DomainAndTopicAsync()).Domain.TitleUk.ShouldBe("Середовище .NET");

        _content.WriteManifest(TopicTemplate.ManifestWithUkTitles("Середовище виконання .NET", "Пам'ять і GC"));
        var changed = await ImportAsync();
        (Total(changed, EntityKinds.Domain).Updated, Total(changed, EntityKinds.Topic).Updated).ShouldBe((1, 1));
        var (domain, topic) = await DomainAndTopicAsync();
        (domain.TitleUk, topic.TitleUk).ShouldBe(("Середовище виконання .NET", "Пам'ять і GC"));

        _content.WriteManifest(TopicTemplate.Manifest());
        var removed = await ImportAsync();
        (Total(removed, EntityKinds.Domain).Updated, Total(removed, EntityKinds.Topic).Updated).ShouldBe((1, 1));
        (domain, topic) = await DomainAndTopicAsync();
        domain.TitleUk.ShouldBeNull();
        topic.TitleUk.ShouldBeNull();
        (await IdSnapshotAsync()).ShouldBe(ids, "titles are updated in place");
    }

    // ---- helpers ----

    private async Task<(Domain.Content.Domain Domain, Topic Topic)> DomainAndTopicAsync()
    {
        await using var db = fixture.CreateContext();
        var domain = await db.Domains.AsNoTracking().SingleAsync(d => d.Slug == TopicTemplate.Domain);
        var topic = await db.Topics.AsNoTracking().SingleAsync(t => t.Slug == TopicTemplate.Topic);
        return (domain, topic);
    }

    private async Task<ImportResult> ImportAsync(bool dryRun = false)
    {
        await using var db = fixture.CreateContext();
        var importer = new ContentImporter(db, NullLogger<ContentImporter>.Instance);
        return await importer.ImportAsync(new ImportOptions(_content.Dir, DryRun: dryRun));
    }

    private static EntityCounts Total(ImportResult result, string kind)
    {
        result.Summary.ShouldNotBeNull();
        return result.Summary.Totals()[kind];
    }

    private static string Describe(ImportResult result) =>
        $"{result.ExitCode}: {result.Message}\n{string.Join("\n", result.Errors)}";

    private static JsonObject StackVsHeap(JsonObject topic) => (JsonObject)topic["concepts"]![0]!;

    private static JsonArray Questions(JsonObject topic) => (JsonArray)StackVsHeap(topic)["questions"]!;

    private static JsonObject Question(JsonObject topic, int index) => (JsonObject)Questions(topic)[index]!;

    private async Task<Dictionary<string, DateTimeOffset>> UpdatedAtSnapshotAsync()
    {
        await using var db = fixture.CreateContext();
        var snapshot = new Dictionary<string, DateTimeOffset>(StringComparer.Ordinal);
        foreach (var c in await db.Concepts.AsNoTracking().ToListAsync())
        {
            snapshot[$"concept:{c.Id}"] = c.UpdatedAt;
        }

        foreach (var c in await db.ConceptTranslations.AsNoTracking().ToListAsync())
        {
            snapshot[$"concept-translation:{c.ConceptId}:{c.Language}"] = c.UpdatedAt;
        }

        foreach (var q in await db.Questions.AsNoTracking().ToListAsync())
        {
            snapshot[$"question:{q.Id}"] = q.UpdatedAt;
        }

        foreach (var t in await db.QuestionTranslations.AsNoTracking().ToListAsync())
        {
            snapshot[$"question-translation:{t.QuestionId}:{t.Language}"] = t.UpdatedAt;
        }

        return snapshot;
    }

    /// <summary>Row identities of every content table, to prove edits update in place instead of re-creating rows.</summary>
    private async Task<List<string>> IdSnapshotAsync()
    {
        await using var db = fixture.CreateContext();
        var ids = new List<string>();
        ids.AddRange((await db.Domains.Select(d => d.Id).ToListAsync()).Select(id => $"domain:{id}"));
        ids.AddRange((await db.Topics.Select(t => t.Id).ToListAsync()).Select(id => $"topic:{id}"));
        ids.AddRange((await db.Concepts.Select(c => c.Id).ToListAsync()).Select(id => $"concept:{id}"));
        ids.AddRange((await db.ConceptTranslations.Select(t => new { t.ConceptId, t.Language }).ToListAsync()).Select(t => $"concept-translation:{t.ConceptId}:{t.Language}"));
        ids.AddRange((await db.Questions.Select(q => q.Id).ToListAsync()).Select(id => $"question:{id}"));
        ids.AddRange((await db.QuestionTranslations.Select(t => new { t.QuestionId, t.Language }).ToListAsync()).Select(t => $"question-translation:{t.QuestionId}:{t.Language}"));
        ids.AddRange((await db.Katas.Select(k => k.Id).ToListAsync()).Select(id => $"kata:{id}"));
        ids.AddRange((await db.KataTranslations.Select(t => new { t.KataId, t.Language }).ToListAsync()).Select(t => $"kata-translation:{t.KataId}:{t.Language}"));
        ids.Sort(StringComparer.Ordinal);
        return ids;
    }

    private async Task SeedProgressAsync(string questionId)
    {
        await using var db = fixture.CreateContext();
        await new LearnerSeeder(db).SeedAsync();
        var now = DateTimeOffset.UtcNow;
        var state = """{"streak":2}""";
        await db.Database.ExecuteSqlAsync(
            $"""
             INSERT INTO review_states (id, user_id, item_type, question_id, kata_id, algorithm, interval_days, ease, repetitions, last_reviewed_at, due_at, algorithm_state, created_at, updated_at)
             VALUES ({Guid.NewGuid()}, {LocalLearner.Id}, 'question', {questionId}, NULL, 'sm2', 6, 2.6, 2, {now}, {now.AddDays(6)}, {state}::jsonb, {now}, {now})
             """);
        var answer = """{"option":1}""";
        await db.Database.ExecuteSqlAsync(
            $"""
             INSERT INTO attempts (id, user_id, item_type, question_id, kata_id, session_id, answered_at, answer, is_correct, score, grading, created_at)
             VALUES ({Guid.NewGuid()}, {LocalLearner.Id}, 'question', {questionId}, NULL, NULL, {now}, {answer}::jsonb, true, NULL, NULL, {now})
             """);
    }

    /// <summary>Every column of every progress row, serialized by PostgreSQL itself.</summary>
    private async Task<List<string>> ProgressRowsAsync()
    {
        await using var db = fixture.CreateContext();
        var states = await db.Database.SqlQueryRaw<string>("SELECT row_to_json(r)::text AS \"Value\" FROM review_states r ORDER BY id").ToListAsync();
        var attempts = await db.Database.SqlQueryRaw<string>("SELECT row_to_json(a)::text AS \"Value\" FROM attempts a ORDER BY id").ToListAsync();
        return [.. states, .. attempts];
    }
}
