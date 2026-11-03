using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Spine.Api.Endpoints;
using Spine.Application.Knowledge.Tree;

namespace Spine.Api.Tests.Knowledge;

/// <summary>Runs against its own container so the database is guaranteed empty.</summary>
[Trait("Category", "Integration")]
public sealed class KnowledgeTreeEmptyDatabaseTests(EmptyApiFixture fixture) : IClassFixture<EmptyApiFixture>
{
    [Fact]
    public async Task Empty_database_returns_en_and_no_domains()
    {
        var response = await fixture.Client.GetAsync(ApiUrl.Prefix + ApiRoutes.Knowledge.Tree);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var tree = await response.Content.ReadFromJsonAsync<KnowledgeTreeDto>(ApiFixture.Json);
        tree.ShouldNotBeNull();
        tree.Language.ShouldBe("en");
        tree.Domains.ShouldBeEmpty();
    }
}

[Collection("api")]
[Trait("Category", "Integration")]
public sealed class KnowledgeTreeEndpointTests(ApiFixture fixture) : IAsyncLifetime
{
    // Mirrors tests/Fixtures/api-content (frozen), not the repo content.
    private static readonly string[] ManifestDomains = ["dotnet", "aspnet-data", "angular"];
    private static readonly string[] DotnetTopics = ["clr-memory", "async", "csharp-lang"];
    private static readonly string[] ClrMemoryConcepts = ["stack-vs-heap", "gc-modes", "span-memory"];

    public Task InitializeAsync() => fixture.ImportAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<KnowledgeTreeDto> GetTreeAsync(string query = "")
    {
        var response = await fixture.Client.GetAsync(ApiUrl.Prefix + ApiRoutes.Knowledge.Tree + query);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var tree = await response.Content.ReadFromJsonAsync<KnowledgeTreeDto>(ApiFixture.Json);
        return tree.ShouldNotBeNull();
    }

    [Fact]
    public async Task Domains_and_topics_follow_manifest_order()
    {
        var tree = await GetTreeAsync();

        tree.Language.ShouldBe("en");
        tree.Domains.Select(d => d.Slug).ShouldBe(ManifestDomains);
        tree.Domains.Select(d => d.SortOrder).ShouldBe(Enumerable.Range(0, ManifestDomains.Length));
        tree.Domains[0].Title.ShouldBe("C# & .NET Runtime");
        tree.Domains[0].DeprecatedAt.ShouldBeNull();

        var dotnet = tree.Domains[0];
        dotnet.Topics.Select(t => t.Slug).ShouldBe(DotnetTopics);
        dotnet.Topics.Select(t => t.SortOrder).ShouldBe(Enumerable.Range(0, DotnetTopics.Length));
        dotnet.Topics[0].Title.ShouldBe("Memory & GC");
    }

    [Fact]
    public async Task Concepts_follow_file_order_with_counts_and_unreviewed_flag()
    {
        var tree = await GetTreeAsync();

        var concepts = tree.Domains[0].Topics[0].Concepts;
        concepts.Select(c => c.Slug).ShouldBe(ClrMemoryConcepts);
        concepts.Select(c => c.SortOrder).ShouldBe(Enumerable.Range(0, ClrMemoryConcepts.Length));
        concepts.ShouldAllBe(c => c.QuestionCount == 5);
        concepts.ShouldAllBe(c => !c.Reviewed);
        concepts.ShouldAllBe(c => !c.TitleFallback);
        concepts.ShouldAllBe(c => c.DeprecatedAt == null);
        concepts.Where(c => c.KataCount == 1).Select(c => c.Slug).ShouldBe(["span-memory"]);
        concepts.Where(c => c.Slug != "span-memory").ShouldAllBe(c => c.KataCount == 0);
        concepts.Single(c => c.Slug == "stack-vs-heap").Level.ShouldBe("M");
        concepts.Single(c => c.Slug == "span-memory").Level.ShouldBe("S");
    }

    [Fact]
    public async Task Topics_without_a_content_file_have_no_concepts()
    {
        var tree = await GetTreeAsync();

        tree.Domains[0].Topics.Single(t => t.Slug == "csharp-lang").Concepts.ShouldBeEmpty();
        tree.Domains[1].Topics.ShouldAllBe(t => t.Concepts.Count == 0);
    }

    [Fact]
    public async Task Uk_without_translations_falls_back_to_english_titles()
    {
        var en = await GetTreeAsync("?lang=en");
        var uk = await GetTreeAsync("?lang=uk");

        uk.Language.ShouldBe("uk");
        var ukConcepts = uk.Domains[0].Topics[0].Concepts;
        var enConcepts = en.Domains[0].Topics[0].Concepts;
        ukConcepts.ShouldAllBe(c => c.TitleFallback);
        ukConcepts.Select(c => c.Title).ShouldBe(enConcepts.Select(c => c.Title));
    }

    [Fact]
    public async Task Reviewed_topic_reports_reviewed_concepts()
    {
        var tree = await GetTreeAsync();

        var async = tree.Domains[0].Topics.Single(t => t.Slug == "async").Concepts;
        async.Select(c => c.Slug).ShouldBe(["async-await", "configure-await"]);
        async.ShouldAllBe(c => c.Reviewed);
        async.Select(c => c.QuestionCount).ShouldBe([3, 2]);
    }

    [Fact]
    public async Task Uk_concept_titles_from_content_are_served_and_untranslated_concepts_fall_back()
    {
        var en = await GetTreeAsync("?lang=en");
        var uk = await GetTreeAsync("?lang=uk");

        var ukAsync = uk.Domains[0].Topics.Single(t => t.Slug == "async").Concepts;
        var enAsync = en.Domains[0].Topics.Single(t => t.Slug == "async").Concepts;
        var translated = ukAsync.Single(c => c.Slug == "async-await");
        (translated.Title, translated.TitleFallback).ShouldBe(("Скінченний автомат async/await", false));
        var untranslated = ukAsync.Single(c => c.Slug == "configure-await");
        (untranslated.Title, untranslated.TitleFallback).ShouldBe((enAsync.Single(c => c.Slug == "configure-await").Title, true));
        enAsync.ShouldAllBe(c => !c.TitleFallback);
    }

    [Fact]
    public async Task Unsupported_language_is_a_400_problem()
    {
        var response = await fixture.Client.GetAsync(ApiUrl.Prefix + ApiRoutes.Knowledge.Tree + "?lang=de");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("status").GetInt32().ShouldBe(400);
        var errors = body.RootElement.GetProperty("errors");
        errors.ValueKind.ShouldBe(JsonValueKind.Array);
        errors.EnumerateArray().ShouldContain(e => e.GetProperty("name").GetString() == "lang");
    }

    [Fact]
    public async Task Deprecated_concept_is_hidden_unless_includeDeprecated()
    {
        const string slug = "gc-modes";
        var deprecatedAt = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        await fixture.SetConceptDeprecatedAsync(slug, deprecatedAt);
        try
        {
            var hidden = await GetTreeAsync();
            var shown = await GetTreeAsync("?includeDeprecated=true");

            hidden.Domains[0].Topics[0].Concepts.Select(c => c.Slug).ShouldBe(ClrMemoryConcepts.Where(s => s != slug));
            shown.Domains[0].Topics[0].Concepts.Select(c => c.Slug).ShouldBe(ClrMemoryConcepts);
            shown.Domains[0].Topics[0].Concepts.Single(c => c.Slug == slug).DeprecatedAt.ShouldBe(deprecatedAt);
            shown.Domains[0].Topics[0].Concepts.Where(c => c.Slug != slug).ShouldAllBe(c => c.DeprecatedAt == null);
        }
        finally
        {
            await fixture.SetConceptDeprecatedAsync(slug, null);
        }

        (await GetTreeAsync()).Domains[0].Topics[0].Concepts.Count.ShouldBe(ClrMemoryConcepts.Length);
    }
}
