using System.Text.Json.Nodes;
using Spine.Application.Content;
using Spine.Application.Content.Seed;
using Spine.Application.Content.Validation;
using Spine.Domain.Content;
using DomainEntity = Spine.Domain.Content.Domain;

namespace Spine.Application.Tests.Content;

/// <summary>
/// Optional Ukrainian domain/topic titles (<c>titles.uk</c> in the manifest): parsing, plan, hash and diff behaviour,
/// and the manifest schema. Schema cases use <c>tests/Fixtures/schema/manifest.schema.json</c>, the schema that allows
/// <c>titles</c>; it mirrors <c>content/schema/manifest.schema.json</c>.
/// </summary>
public class ManifestTitlesTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset T1 = T0.AddDays(1);

    private const string DomainUk = "C# та середовище виконання .NET";
    private const string TopicUk = "Пам'ять і GC";

    private static string FixtureManifestSchemaJson =>
        File.ReadAllText(Path.Combine(ContentSetLoader.FixturesDir, "schema", "manifest.schema.json"));

    private static JsonObject Domain(JsonNode manifest, int index) => (JsonObject)manifest["domains"]![index]!;
    private static JsonObject Topic(JsonNode manifest, int domain, int index) => (JsonObject)Domain(manifest, domain)["topics"]![index]!;

    /// <summary>The valid fixture manifest with <paramref name="mutate"/> applied, plus the valid topic file.</summary>
    private static ContentSet SetWithManifest(Action<JsonNode> mutate)
    {
        var manifest = FixtureJson.ManifestNode();
        mutate(manifest);
        var set = ContentSetLoader.FromFiles(
            ContentSetLoader.ValidContentDir,
            FixtureJson.Text(manifest),
            (FixtureJson.TopicPath, ContentSetLoader.ReadValidTopic()));
        return set with { ManifestSchemaJson = FixtureManifestSchemaJson };
    }

    private static void WithUkTitles(JsonNode manifest, string domainUk = DomainUk, string topicUk = TopicUk)
    {
        Domain(manifest, 0)["titles"] = new JsonObject { ["uk"] = domainUk };
        Topic(manifest, 0, 0)["titles"] = new JsonObject { ["uk"] = topicUk };
    }

    private static ImportPlan Plan(Action<JsonNode> mutate) => ImportPlanBuilder.Build(SetWithManifest(mutate));

    private static DomainEntity DotnetOf(IEnumerable<DomainEntity> domains) => domains.Single(d => d.Slug == "dotnet");
    private static Topic ClrMemoryOf(IEnumerable<DomainEntity> domains) => DotnetOf(domains).Topics.Single(t => t.Slug == "clr-memory");

    private static (int Created, int Updated, int Unchanged, int Deprecated) Counts(ImportChangeSet change, string kind)
    {
        var c = change.Summary.Totals()[kind];
        return (c.Created, c.Updated, c.Unchanged, c.Deprecated);
    }

    // ---- parsing ----

    [Fact]
    public void Parse_TitlesUk_OnDomainAndTopic()
    {
        var manifest = SetWithManifest(m => WithUkTitles(m)).Manifest!.Parsed!;

        manifest.Domains[0].Titles.ShouldBe(new ManifestTitles(DomainUk));
        manifest.Domains[0].Topics[0].Titles.ShouldBe(new ManifestTitles(TopicUk));
        manifest.Domains[0].Topics[1].Titles.ShouldBeNull();
        manifest.Domains[1].Titles.ShouldBeNull();
    }

    // ---- plan and hash ----

    [Fact]
    public void Plan_TitlesUk_AreCarriedToDomainAndTopic()
    {
        var plan = Plan(m => WithUkTitles(m));

        plan.Domains[0].TitleUk.ShouldBe(DomainUk);
        plan.Domains[0].Topics[0].TitleUk.ShouldBe(TopicUk);
        plan.Domains[0].Topics[1].TitleUk.ShouldBeNull();
        plan.Domains[1].TitleUk.ShouldBeNull();
    }

    [Fact]
    public void Plan_WithoutTitles_KeepsTheHashOfManifestsWrittenBeforeTitlesExisted()
    {
        // Rows imported before titles.uk existed must stay "unchanged" on the next import.
        var plan = Plan(_ => { });

        plan.Domains[0].Hash.ShouldBe(ContentHasher.Hash("domain", "dotnet", "C# & .NET Runtime", 0));
        plan.Domains[0].Topics[0].Hash.ShouldBe(ContentHasher.Hash("topic", "clr-memory", "Memory & GC", 0));
    }

    [Fact]
    public void Plan_AddingOrChangingUkTitle_ChangesOnlyThatEntityHash()
    {
        var plain = Plan(_ => { });
        var withUk = Plan(m => WithUkTitles(m));
        var changed = Plan(m => WithUkTitles(m, domainUk: "Інша назва"));

        withUk.Domains[0].Hash.ShouldNotBe(plain.Domains[0].Hash);
        withUk.Domains[0].Topics[0].Hash.ShouldNotBe(plain.Domains[0].Topics[0].Hash);
        withUk.Domains[0].Topics[1].Hash.ShouldBe(plain.Domains[0].Topics[1].Hash);
        withUk.Domains[1].Hash.ShouldBe(plain.Domains[1].Hash);

        changed.Domains[0].Hash.ShouldNotBe(withUk.Domains[0].Hash);
        changed.Domains[0].Topics[0].Hash.ShouldBe(withUk.Domains[0].Topics[0].Hash);
    }

    // ---- diff ----

    [Fact]
    public void Compute_NewGraph_StoresUkTitles()
    {
        var change = ImportDiff.Compute([], Plan(m => WithUkTitles(m)), T0);

        DotnetOf(change.NewDomains).TitleUk.ShouldBe(DomainUk);
        ClrMemoryOf(change.NewDomains).TitleUk.ShouldBe(TopicUk);
        change.NewDomains.Single(d => d.Slug == "angular").TitleUk.ShouldBeNull();
    }

    [Fact]
    public void Compute_UkTitleAdded_UpdatesExactlyThatDomainAndTopic()
    {
        var existing = ImportDiff.Compute([], Plan(_ => { }), T0).NewDomains;

        var change = ImportDiff.Compute(existing, Plan(m => WithUkTitles(m)), T1);

        Counts(change, EntityKinds.Domain).ShouldBe((0, 1, 1, 0));
        Counts(change, EntityKinds.Topic).ShouldBe((0, 1, 2, 0));
        DotnetOf(existing).TitleUk.ShouldBe(DomainUk);
        DotnetOf(existing).UpdatedAt.ShouldBe(T1);
        ClrMemoryOf(existing).TitleUk.ShouldBe(TopicUk);
        ClrMemoryOf(existing).UpdatedAt.ShouldBe(T1);
        DotnetOf(existing).Title.ShouldBe("C# & .NET Runtime");
    }

    [Fact]
    public void Compute_SameUkTitlesAgain_ChangesNothing()
    {
        var existing = ImportDiff.Compute([], Plan(m => WithUkTitles(m)), T0).NewDomains;

        var change = ImportDiff.Compute(existing, Plan(m => WithUkTitles(m)), T1);

        change.Summary.HasChanges.ShouldBeFalse();
        Counts(change, EntityKinds.Domain).ShouldBe((0, 0, 2, 0));
        Counts(change, EntityKinds.Topic).ShouldBe((0, 0, 3, 0));
        DotnetOf(existing).UpdatedAt.ShouldBe(T0);
    }

    [Fact]
    public void Compute_UkTitleChanged_UpdatesTheRow()
    {
        var existing = ImportDiff.Compute([], Plan(m => WithUkTitles(m)), T0).NewDomains;

        var change = ImportDiff.Compute(existing, Plan(m => WithUkTitles(m, topicUk: "Пам'ять і збирач сміття")), T1);

        Counts(change, EntityKinds.Domain).ShouldBe((0, 0, 2, 0));
        Counts(change, EntityKinds.Topic).ShouldBe((0, 1, 2, 0));
        ClrMemoryOf(existing).TitleUk.ShouldBe("Пам'ять і збирач сміття");
    }

    [Fact]
    public void Compute_UkTitleRemoved_ClearsTheColumn()
    {
        var existing = ImportDiff.Compute([], Plan(m => WithUkTitles(m)), T0).NewDomains;

        var change = ImportDiff.Compute(existing, Plan(_ => { }), T1);

        Counts(change, EntityKinds.Domain).ShouldBe((0, 1, 1, 0));
        Counts(change, EntityKinds.Topic).ShouldBe((0, 1, 2, 0));
        DotnetOf(existing).TitleUk.ShouldBeNull();
        ClrMemoryOf(existing).TitleUk.ShouldBeNull();
    }

    // ---- schema ----

    [Fact]
    public void Schema_TitlesUkOnDomainAndTopic_IsValid()
    {
        var result = new ContentSchemaValidator().Validate(SetWithManifest(m => WithUkTitles(m)));

        result.Errors.ShouldBeEmpty(string.Join(Environment.NewLine, result.Errors));
    }

    [Fact]
    public void Schema_UnknownLanguageInTitles_IsRejected()
    {
        var result = new ContentSchemaValidator().Validate(SetWithManifest(m =>
            Domain(m, 0)["titles"] = new JsonObject { ["uk"] = DomainUk, ["de"] = "Laufzeit" }));

        result.Errors.ShouldContain(e => e.File == "manifest.json" && e.JsonPointer == "/domains/0/titles" && e.Rule == "additionalProperties",
            string.Join(Environment.NewLine, result.Errors));
    }

    [Fact]
    public void Schema_EmptyUkTitle_IsRejected()
    {
        var result = new ContentSchemaValidator().Validate(SetWithManifest(m =>
            Topic(m, 0, 1)["titles"] = new JsonObject { ["uk"] = "" }));

        result.Errors.ShouldContain(e => e.File == "manifest.json" && e.JsonPointer == "/domains/0/topics/1/titles/uk" && e.Rule == "minLength",
            string.Join(Environment.NewLine, result.Errors));
    }

    [Fact]
    public void Schema_FixtureCopy_MatchesTheRepoSchemaOnceItSupportsTitles()
    {
        // tests/Fixtures/schema/manifest.schema.json is a copy of the repo schema; flag drift once the repo schema has titles.
        var repo = ContentSetLoader.ManifestSchemaJson;
        if (!repo.Contains("\"titles\"", StringComparison.Ordinal))
        {
            return;
        }

        JsonNode.DeepEquals(JsonNode.Parse(repo), JsonNode.Parse(FixtureManifestSchemaJson)).ShouldBeTrue(
            "tests/Fixtures/schema/manifest.schema.json has drifted from content/schema/manifest.schema.json");
    }
}
