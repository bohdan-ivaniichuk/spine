using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Spine.Application.Content.Import;
using Spine.Application.Review;
using Spine.Application.Review.GetReviewCoverage;
using Spine.Application.Review.Shared;
using Spine.Domain.Content;
using Spine.Domain.Scheduling;
using Spine.Infrastructure.Content;
using Spine.Infrastructure.Persistence;
using Spine.Infrastructure.Persistence.Startup;
using Spine.Infrastructure.Tests.Content;

namespace Spine.Infrastructure.Tests.Review;

/// <summary>
/// Requested-language text and per-item fallback, plus per-topic coverage. Default content: stack-vs-heap has a uk title
/// and mcq.1 a uk translation (prompt "… (uk)", same options, correct index 1); mcq.2 and span-memory are English only.
/// </summary>
[Collection(PostgresCollection.Name)]
[Trait("Category", "Integration")]
public sealed class ReviewSessionsLanguageTests(PostgresFixture fixture) : IAsyncLifetime
{
    private static readonly DateTimeOffset Start = new(2026, 9, 6, 10, 0, 0, TimeSpan.Zero);
    private const string UkExplanation = "Пояснення українською.";
    private const int Correct = 1;
    private const int Wrong = 0;

    private ContentDirBuilder _content = null!;
    private FakeTimeProvider _clock = null!;
    private AppDbContext _db = null!;
    private ReviewSessionsHarness _sut = null!;

    public async Task InitializeAsync()
    {
        await fixture.ResetAsync();
        _content = new ContentDirBuilder().WriteManifest(ManifestWithEmptyTopicFirst()).WriteTopic(ClrMemoryWithUkExplanation());
        await ImportAsync();
        _clock = new FakeTimeProvider(Start);
        _db = fixture.CreateContext();
        _sut = new ReviewSessionsHarness(_db, new Sm2Algorithm(), _clock, Options.Create(new ReviewOptions()));
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        _content.Dispose();
    }

    [Fact]
    public async Task Uk_serves_translated_questions_and_falls_back_to_en_per_question()
    {
        var session = await _sut.StartAsync(ReviewScope.All, 4, 4, Language.Uk);

        var mcq1 = session.Questions.Single(q => q.Id == TopicTemplate.Mcq1);
        mcq1.Prompt.ShouldBe("Where does a struct field of a class live? (uk)");
        mcq1.Options.ShouldBe(["Option A", "Option B", "Option C", "Option D"]);
        mcq1.Language.ShouldBe("uk");
        mcq1.Fallback.ShouldBeFalse();
        mcq1.Concept.ShouldBe(new RefDto(TopicTemplate.StackVsHeap, "Stack vs Heap (uk)"));
        mcq1.Domain.ShouldBe(new RefDto("dotnet", "C# & .NET Runtime"), "the test manifest has no titles.uk, so domain titles fall back to English");

        var mcq2 = session.Questions.Single(q => q.Id == TopicTemplate.Mcq2);
        mcq2.Prompt.ShouldBe("Which statement boxes?");
        mcq2.Language.ShouldBe("en");
        mcq2.Fallback.ShouldBeTrue();
        mcq2.Concept.Title.ShouldBe("Stack vs Heap (uk)", "the concept title falls back independently of the question");

        var span = session.Questions.Single(q => q.Id == TopicTemplate.SpanMcq1);
        span.Fallback.ShouldBeTrue();
        span.Concept.Title.ShouldBe("Span and Memory");
    }

    [Fact]
    public async Task En_never_reports_fallback_and_a_session_reads_in_either_language()
    {
        var en = await _sut.StartAsync(ReviewScope.All, 4, 4);
        en.Questions.ShouldAllBe(q => q.Language == "en" && !q.Fallback);
        en.Questions.Single(q => q.Id == TopicTemplate.Mcq1).Concept.Title.ShouldBe("Stack vs Heap");

        var uk = (await _sut.GetAsync(en.Id, Language.Uk)).ShouldNotBeNull();
        uk.Questions.Select(q => q.Id).ShouldBe(en.Questions.Select(q => q.Id));
        uk.Questions.Single(q => q.Id == TopicTemplate.Mcq1).Language.ShouldBe("uk");

        var current = (await _sut.GetCurrentAsync(Language.Uk)).ShouldNotBeNull();
        current.Questions.Single(q => q.Id == TopicTemplate.Mcq1).Prompt.ShouldEndWith("(uk)");
    }

    [Fact]
    public async Task Uk_options_that_are_not_index_aligned_fall_back_the_whole_question()
    {
        var topic = ClrMemoryWithUkExplanation();
        // uk options with the correct answer at index 2 while English has it at index 1.
        Mcq1(topic)["translations"]!["uk"] = TopicTemplate.McqTranslation("Misaligned (uk)", correctIndex: 2);
        _content.WriteTopic(topic);
        await ImportAsync();

        var session = await _sut.StartAsync(ReviewScope.All, 4, 4, Language.Uk);
        var mcq1 = session.Questions.Single(q => q.Id == TopicTemplate.Mcq1);
        mcq1.Prompt.ShouldBe("Where does a struct field of a class live?");
        mcq1.Language.ShouldBe("en");
        mcq1.Fallback.ShouldBeTrue();

        var feedback = await _sut.AnswerAsync(session.Id, TopicTemplate.Mcq1, Correct, Language.Uk);
        feedback.IsCorrect.ShouldBeTrue("grading always uses the English options");
        feedback.Language.ShouldBe("en");
        feedback.Fallback.ShouldBeTrue();
    }

    [Fact]
    public async Task Answer_explanation_and_finish_titles_follow_the_requested_language()
    {
        var session = await _sut.StartAsync(ReviewScope.All, 4, 4);

        var uk = await _sut.AnswerAsync(session.Id, TopicTemplate.Mcq1, Wrong, Language.Uk);
        uk.IsCorrect.ShouldBeFalse();
        uk.CorrectIndex.ShouldBe(Correct);
        uk.Explanation.ShouldBe(UkExplanation);
        uk.Language.ShouldBe("uk");
        uk.Fallback.ShouldBeFalse();

        var fallback = await _sut.AnswerAsync(session.Id, TopicTemplate.SpanMcq1, Wrong, Language.Uk);
        fallback.Explanation.ShouldBe("B is right because of the layout rules; A, C and D describe other cases.");
        fallback.Language.ShouldBe("en");
        fallback.Fallback.ShouldBeTrue();

        var en = await _sut.AnswerAsync(session.Id, TopicTemplate.Mcq2, Correct);
        en.Language.ShouldBe("en");
        en.Fallback.ShouldBeFalse();

        var summary = await _sut.FinishAsync(session.Id, Language.Uk);
        summary.WeakConcepts.Select(w => (w.Concept, w.Title)).ShouldBe(
            [(TopicTemplate.SpanMemory, "Span and Memory"), (TopicTemplate.StackVsHeap, "Stack vs Heap (uk)")],
            ignoreOrder: true);

        var enSummary = await _sut.FinishAsync(session.Id);
        enSummary.WeakConcepts.Single(w => w.Concept == TopicTemplate.StackVsHeap).Title.ShouldBe("Stack vs Heap");
    }

    // ---- coverage ----

    [Fact]
    public async Task Coverage_lists_live_topics_in_manifest_order_with_eligible_seen_and_due()
    {
        var empty = await _sut.GetCoverageAsync();
        empty.Topics.ShouldBe(
        [
            new TopicCoverageDto("dotnet", "threading", 0, 0, 0),
            new TopicCoverageDto("dotnet", "clr-memory", 4, 0, 0),
        ]);

        var session = await _sut.StartAsync(ReviewScope.All, 4, 4);
        await _sut.AnswerAsync(session.Id, TopicTemplate.Mcq1, Correct);
        await _sut.AnswerAsync(session.Id, TopicTemplate.Mcq2, Wrong);

        (await _sut.GetCoverageAsync()).Topics[1].ShouldBe(new TopicCoverageDto("dotnet", "clr-memory", 4, 2, 0));

        // Both answers schedule the next review one day out.
        _clock.Advance(TimeSpan.FromDays(1));
        (await _sut.GetCoverageAsync()).Topics[1].ShouldBe(new TopicCoverageDto("dotnet", "clr-memory", 4, 2, 2));

        // A deprecated concept takes its questions (and their states) out of every count.
        await using (var db = fixture.CreateContext())
        {
            await db.Database.ExecuteSqlAsync($"UPDATE concepts SET deprecated_at = {Start} WHERE slug = {TopicTemplate.StackVsHeap}");
        }

        (await _sut.GetCoverageAsync()).Topics[1].ShouldBe(new TopicCoverageDto("dotnet", "clr-memory", 2, 0, 0));
    }

    [Fact]
    public async Task Coverage_omits_deprecated_topics()
    {
        await using (var db = fixture.CreateContext())
        {
            await db.Database.ExecuteSqlAsync($"UPDATE topics SET deprecated_at = {Start} WHERE slug = 'threading'");
        }

        (await _sut.GetCoverageAsync()).Topics.Select(t => t.Topic).ShouldBe(["clr-memory"]);
    }

    // ---- helpers ----

    private async Task ImportAsync()
    {
        await using var db = fixture.CreateContext();
        var result = await new ContentImporter(db, NullLogger<ContentImporter>.Instance).ImportAsync(new ImportOptions(_content.Dir));
        result.ExitCode.ShouldBe(ImportExitCode.Success, string.Join("\n", result.Errors));
        await new LearnerSeeder(db).SeedAsync();
    }

    /// <summary>dotnet with an empty "threading" topic listed before clr-memory, so order is the manifest's, not alphabetical.</summary>
    private static JsonNode ManifestWithEmptyTopicFirst()
    {
        var manifest = TopicTemplate.Manifest();
        ((JsonArray)manifest["domains"]![0]!["topics"]!).Insert(0, new JsonObject { ["slug"] = "threading", ["title"] = "Threading" });
        return manifest;
    }

    /// <summary>The default topic with a distinct uk explanation on mcq.1 (the template reuses the English one).</summary>
    private static JsonObject ClrMemoryWithUkExplanation()
    {
        var topic = TopicTemplate.ClrMemory();
        Mcq1(topic)["translations"]!["uk"]!["explanation"] = UkExplanation;
        return topic;
    }

    private static JsonNode Mcq1(JsonObject topic) => topic["concepts"]![0]!["questions"]![0]!;
}
