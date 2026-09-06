using InterviewPrep.Application.Content.Seed;
using InterviewPrep.Application.Content.Validation;

namespace InterviewPrep.Application.Tests.Content;

public class ContentRuleValidatorTests
{
    private const string Note = "A note long enough to satisfy the schema is not required here because rule validation works on parsed DTOs, but keep it realistic anyway.";

    private static string Dump(ContentValidationResult result) => string.Join(Environment.NewLine, result.Errors);

    private static ManifestFile Manifest() => new(
    [
        new ManifestDomain("dotnet", "C# & .NET Runtime", [new ManifestTopic("clr-memory", "Memory & GC"), new ManifestTopic("async", "Async & Threading")]),
        new ManifestDomain("angular", "Angular", [new ManifestTopic("core", "Core Mechanics")]),
    ]);

    private static QuestionTranslationSeed McqTranslation(int correctCount = 1) => new(
        "Prompt?",
        Enumerable.Range(0, 4).Select(i => new McqOptionSeed($"Option {i}", i < correctCount)).ToList(),
        "Because.",
        null);

    private static QuestionSeed Mcq(string id, int correctCount = 1, string type = "mcq") =>
        new(id, type, "M", true, new Dictionary<string, QuestionTranslationSeed> { ["en"] = McqTranslation(correctCount) }, ["tag"]);

    private static QuestionSeed Open(string id, string type = "open") =>
        new(id, type, "M", true, new Dictionary<string, QuestionTranslationSeed> { ["en"] = new("Prompt?", null, "Because.", ["a", "b", "c"]) }, []);

    private static KataSeed Kata(string id) =>
        new(id, "Kata", "csharp", true, new Dictionary<string, KataTranslationSeed> { ["en"] = new("Task", ["a", "b", "c"], "solution") }, 20);

    private static ConceptSeed Concept(string slug, IEnumerable<QuestionSeed> questions, IEnumerable<KataSeed>? katas = null, string title = "Title", string? enTitle = null) =>
        new(slug, title, "M", new Dictionary<string, ConceptTranslationSeed> { ["en"] = new(enTitle ?? title, Note) }, questions.ToList(), katas?.ToList());

    private static TopicFile Topic(string domain, string topic, params ConceptSeed[] concepts) => new(domain, topic, concepts);

    private static ContentValidationResult Validate(ManifestFile? manifest, params (string, TopicFile)[] files) =>
        new ContentRuleValidator().Validate(ContentSetLoader.FromParsed(manifest, files));

    private static TopicFile ValidClrMemory() => Topic("dotnet", "clr-memory",
        Concept("stack-vs-heap", [Mcq("dotnet.clr-memory.stack-vs-heap.mcq.1"), Open("dotnet.clr-memory.stack-vs-heap.open.1")]),
        Concept("span-memory", [Mcq("dotnet.clr-memory.span-memory.mcq.1")], [Kata("dotnet.clr-memory.span-memory.kata.1")]));

    [Fact]
    public void Validate_ValidSet_HasNoErrors()
    {
        var result = Validate(Manifest(), ("dotnet/clr-memory.json", ValidClrMemory()));

        result.Errors.ShouldBeEmpty(Dump(result));
    }

    [Fact]
    public void Validate_ValidFixtureFromDisk_HasNoErrors()
    {
        var result = new ContentRuleValidator().Validate(ContentSetLoader.LoadValid());

        result.Errors.ShouldBeEmpty(Dump(result));
    }

    [Fact]
    public void McqOneCorrect_TwoCorrectOptions_ReportsCount()
    {
        var file = Topic("dotnet", "clr-memory", Concept("stack-vs-heap", [Mcq("dotnet.clr-memory.stack-vs-heap.mcq.1", correctCount: 2)]));

        var result = Validate(Manifest(), ("dotnet/clr-memory.json", file));

        var error = result.Errors.ShouldHaveSingleItem(Dump(result));
        error.Rule.ShouldBe(ContentRuleValidator.McqOneCorrect);
        error.File.ShouldBe("dotnet/clr-memory.json");
        error.JsonPointer.ShouldBe("/concepts/0/questions/0/translations/en/options");
        error.Message.ShouldContain("found 2");
    }

    [Fact]
    public void McqOneCorrect_ZeroCorrectOptions_ReportsCount()
    {
        var file = Topic("dotnet", "clr-memory", Concept("stack-vs-heap", [Mcq("dotnet.clr-memory.stack-vs-heap.mcq.1", correctCount: 0)]));

        var result = Validate(Manifest(), ("dotnet/clr-memory.json", file));

        var error = result.Errors.ShouldHaveSingleItem(Dump(result));
        error.Rule.ShouldBe(ContentRuleValidator.McqOneCorrect);
        error.JsonPointer.ShouldBe("/concepts/0/questions/0/translations/en/options");
        error.Message.ShouldContain("found 0");
    }

    [Fact]
    public void McqOneCorrect_TwoCorrectFixture_ReportsRule()
    {
        var set = ContentSetLoader.WithValidManifest(("dotnet/clr-memory.json", ContentSetLoader.ReadInvalid("two-correct.json")));

        var result = new ContentRuleValidator().Validate(set);

        var error = result.Errors.ShouldHaveSingleItem(Dump(result));
        error.Rule.ShouldBe(ContentRuleValidator.McqOneCorrect);
        error.JsonPointer.ShouldBe("/concepts/0/questions/0/translations/en/options");
    }

    [Fact]
    public void IdPrefix_QuestionIdFromAnotherTopic_ReportsExpectedPrefix()
    {
        var file = Topic("dotnet", "clr-memory", Concept("stack-vs-heap", [Mcq("dotnet.async.stack-vs-heap.mcq.1")]));

        var result = Validate(Manifest(), ("dotnet/clr-memory.json", file));

        var error = result.Errors.ShouldHaveSingleItem(Dump(result));
        error.Rule.ShouldBe(ContentRuleValidator.IdPrefix);
        error.JsonPointer.ShouldBe("/concepts/0/questions/0/id");
        error.Message.ShouldContain("dotnet.clr-memory.stack-vs-heap.");
    }

    [Fact]
    public void IdPrefix_KataIdFromAnotherConcept_ReportsOnKataPointer()
    {
        var file = Topic("dotnet", "clr-memory",
            Concept("span-memory", [Mcq("dotnet.clr-memory.span-memory.mcq.1")], [Kata("dotnet.clr-memory.stack-vs-heap.kata.1")]));

        var result = Validate(Manifest(), ("dotnet/clr-memory.json", file));

        var error = result.Errors.ShouldHaveSingleItem(Dump(result));
        error.Rule.ShouldBe(ContentRuleValidator.IdPrefix);
        error.JsonPointer.ShouldBe("/concepts/0/katas/0/id");
    }

    [Fact]
    public void IdType_OpenQuestionWithMcqIdSegment_ReportsMismatch()
    {
        var file = Topic("dotnet", "clr-memory", Concept("stack-vs-heap", [Open("dotnet.clr-memory.stack-vs-heap.mcq.1")]));

        var result = Validate(Manifest(), ("dotnet/clr-memory.json", file));

        var error = result.Errors.ShouldHaveSingleItem(Dump(result));
        error.Rule.ShouldBe(ContentRuleValidator.IdType);
        error.JsonPointer.ShouldBe("/concepts/0/questions/0/type");
        error.Message.ShouldContain("'open'");
        error.Message.ShouldContain("'mcq'");
    }

    [Fact]
    public void ManifestPair_TopicNotInManifest_ReportsPair()
    {
        var file = Topic("dotnet", "gc", Concept("gen0", [Mcq("dotnet.gc.gen0.mcq.1")]));

        var result = Validate(Manifest(), ("dotnet/gc.json", file));

        var error = result.Errors.ShouldHaveSingleItem(Dump(result));
        error.Rule.ShouldBe(ContentRuleValidator.ManifestPair);
        error.File.ShouldBe("dotnet/gc.json");
        error.JsonPointer.ShouldBe("/topic");
        error.Message.ShouldContain("'gc'");
    }

    [Fact]
    public void ManifestPair_UnknownTopicFixture_ReportsOnlyThatRule()
    {
        var set = ContentSetLoader.WithValidManifest(("dotnet/nonexistent.json", ContentSetLoader.ReadInvalid("unknown-topic.json")));

        var result = new ContentRuleValidator().Validate(set);

        var error = result.Errors.ShouldHaveSingleItem(Dump(result));
        error.Rule.ShouldBe(ContentRuleValidator.ManifestPair);
        error.Message.ShouldContain("'nonexistent'");
    }

    [Fact]
    public void FilePath_FileNotAtDomainSlashTopic_ReportsExpectedPath()
    {
        var result = Validate(Manifest(), ("dotnet/memory.json", ValidClrMemory()));

        var error = result.Errors.ShouldHaveSingleItem(Dump(result));
        error.Rule.ShouldBe(ContentRuleValidator.FilePath);
        error.File.ShouldBe("dotnet/memory.json");
        error.JsonPointer.ShouldBe("");
        error.Message.ShouldContain("'dotnet/clr-memory.json'");
    }

    [Fact]
    public void FilePath_BackslashSeparator_IsAccepted()
    {
        var result = Validate(Manifest(), (@"dotnet\clr-memory.json", ValidClrMemory()));

        result.Errors.ShouldNotContain(e => e.Rule == ContentRuleValidator.FilePath, Dump(result));
    }

    [Fact]
    public void DuplicateTopicFile_TwoFilesDeclareSamePair_ReportsOnSecondNamingFirst()
    {
        var second = Topic("dotnet", "clr-memory", Concept("gc-generations", [Mcq("dotnet.clr-memory.gc-generations.mcq.1")]));

        var result = Validate(Manifest(), ("dotnet/clr-memory.json", ValidClrMemory()), ("dotnet/clr-memory-2.json", second));

        var error = result.Errors.Single(e => e.Rule == ContentRuleValidator.DuplicateTopicFile);
        error.File.ShouldBe("dotnet/clr-memory-2.json");
        error.JsonPointer.ShouldBe("/topic");
        error.Message.ShouldContain("'dotnet/clr-memory.json'");
    }

    [Fact]
    public void DuplicateId_AcrossTwoFiles_NamesBothFiles()
    {
        var async = Topic("dotnet", "async", Concept("cancellation", [Mcq("dotnet.clr-memory.stack-vs-heap.mcq.1")]));

        var result = Validate(Manifest(), ("dotnet/clr-memory.json", ValidClrMemory()), ("dotnet/async.json", async));

        var error = result.Errors.Single(e => e.Rule == ContentRuleValidator.DuplicateId);
        error.File.ShouldBe("dotnet/async.json");
        error.JsonPointer.ShouldBe("/concepts/0/questions/0/id");
        error.Message.ShouldContain("'dotnet/clr-memory.json'");
        error.ToString().ShouldContain("dotnet/async.json");
        error.ToString().ShouldContain("dotnet/clr-memory.json");
    }

    [Fact]
    public void DuplicateId_WithinOneFile_SaysThisFile()
    {
        var file = Topic("dotnet", "clr-memory",
            Concept("stack-vs-heap", [Mcq("dotnet.clr-memory.stack-vs-heap.mcq.1"), Mcq("dotnet.clr-memory.stack-vs-heap.mcq.1")]));

        var result = Validate(Manifest(), ("dotnet/clr-memory.json", file));

        var error = result.Errors.ShouldHaveSingleItem(Dump(result));
        error.Rule.ShouldBe(ContentRuleValidator.DuplicateId);
        error.JsonPointer.ShouldBe("/concepts/0/questions/1/id");
        error.Message.ShouldContain("this file");
    }

    [Fact]
    public void DuplicateId_KataReusesQuestionId_ReportsOnKata()
    {
        var file = Topic("dotnet", "clr-memory",
            Concept("span-memory", [Mcq("dotnet.clr-memory.span-memory.mcq.1")], [Kata("dotnet.clr-memory.span-memory.mcq.1")]));

        var result = Validate(Manifest(), ("dotnet/clr-memory.json", file));

        result.Errors.ShouldContain(e => e.Rule == ContentRuleValidator.DuplicateId && e.JsonPointer == "/concepts/0/katas/0/id", Dump(result));
    }

    [Fact]
    public void ConceptSlugUnique_SameSlugTwice_ReportsSecondOccurrence()
    {
        var file = Topic("dotnet", "clr-memory",
            Concept("stack-vs-heap", [Mcq("dotnet.clr-memory.stack-vs-heap.mcq.1")]),
            Concept("stack-vs-heap", [Mcq("dotnet.clr-memory.stack-vs-heap.mcq.2")]));

        var result = Validate(Manifest(), ("dotnet/clr-memory.json", file));

        var error = result.Errors.ShouldHaveSingleItem(Dump(result));
        error.Rule.ShouldBe(ContentRuleValidator.ConceptSlugUnique);
        error.JsonPointer.ShouldBe("/concepts/1/slug");
    }

    [Fact]
    public void ConceptTitle_DriftsFromEnTitle_ReportsRule()
    {
        var file = Topic("dotnet", "clr-memory",
            Concept("stack-vs-heap", [Mcq("dotnet.clr-memory.stack-vs-heap.mcq.1")], title: "Stack vs Heap", enTitle: "Stack versus Heap"));

        var result = Validate(Manifest(), ("dotnet/clr-memory.json", file));

        var error = result.Errors.ShouldHaveSingleItem(Dump(result));
        error.Rule.ShouldBe(ContentRuleValidator.ConceptTitle);
        error.JsonPointer.ShouldBe("/concepts/0/title");
    }

    [Fact]
    public void ManifestDuplicates_DomainListedTwice_ReportsSecondEntry()
    {
        var manifest = new ManifestFile(
        [
            new ManifestDomain("dotnet", "A", [new ManifestTopic("clr-memory", "Memory")]),
            new ManifestDomain("dotnet", "B", [new ManifestTopic("async", "Async")]),
        ]);

        var result = Validate(manifest);

        var error = result.Errors.ShouldHaveSingleItem(Dump(result));
        error.Rule.ShouldBe(ContentRuleValidator.ManifestDuplicates);
        error.File.ShouldBe("manifest.json");
        error.JsonPointer.ShouldBe("/domains/1/slug");
    }

    [Fact]
    public void ManifestDuplicates_TopicListedTwiceInDomain_ReportsSecondEntry()
    {
        var manifest = new ManifestFile(
        [
            new ManifestDomain("dotnet", "A", [new ManifestTopic("clr-memory", "Memory"), new ManifestTopic("clr-memory", "Memory again")]),
        ]);

        var result = Validate(manifest);

        var error = result.Errors.ShouldHaveSingleItem(Dump(result));
        error.Rule.ShouldBe(ContentRuleValidator.ManifestDuplicates);
        error.JsonPointer.ShouldBe("/domains/0/topics/1/slug");
        error.Message.ShouldContain("'dotnet'");
    }

    [Fact]
    public void ManifestMissing_TopicFilesWithoutManifest_ReportsRule()
    {
        var result = Validate(null, ("dotnet/clr-memory.json", ValidClrMemory()));

        var error = result.Errors.ShouldHaveSingleItem(Dump(result));
        error.Rule.ShouldBe(ContentRuleValidator.ManifestMissing);
        error.File.ShouldBe("manifest.json");
    }

    [Fact]
    public void ManifestMissing_NoTopicFiles_IsValid()
    {
        var result = Validate(null);

        result.Errors.ShouldBeEmpty(Dump(result));
    }

    [Fact]
    public void Validate_UnparsedTopicFile_IsSkipped()
    {
        var set = ContentSetLoader.WithValidManifest(("dotnet/clr-memory.json", """{ "domain": "dotnet", "topic": "clr-memory", "concepts": "not-an-array" }"""));
        set.TopicFiles[0].Parsed.ShouldBeNull();

        var result = new ContentRuleValidator().Validate(set);

        result.Errors.ShouldBeEmpty(Dump(result));
    }
}
