using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Spine.Api.Tests.Review;
using Spine.Application.Knowledge;
using Spine.Application.Review;
using Npgsql;

namespace Spine.Api.Tests.Knowledge;

/// <summary>
/// Domain and topic titles honour <c>?lang=uk</c> when <c>title_uk</c> is set and fall back to English per item.
/// Each test sets <c>title_uk</c> on dotnet and dotnet/clr-memory and clears it on dotnet/async directly in the
/// database, so the outcome does not depend on the fixture manifest's <c>titles.uk</c>; the previous values are restored
/// afterwards. The fixture manifest's own <c>titles.uk</c> (on aspnet-data) is asserted end-to-end separately.
/// </summary>
[Collection("api")]
[Trait("Category", "Integration")]
public sealed class UkTitlesEndpointTests(ApiFixture fixture) : IAsyncLifetime
{
    private const string DomainEn = "C# & .NET Runtime";
    private const string DomainUk = "Тестова назва домену";
    private const string ClrMemoryEn = "Memory & GC";
    private const string ClrMemoryUk = "Тестова назва теми";
    private const string AsyncEn = "Async & Threading";

    private readonly List<(string Sql, NpgsqlParameter[] Parameters)> _restore = [];

    public async Task InitializeAsync()
    {
        await fixture.ImportAsync();
        await fixture.ResetProgressAsync();

        await SetDomainTitleUkAsync("dotnet", DomainUk);
        await SetTopicTitleUkAsync("dotnet", "clr-memory", ClrMemoryUk);
        await SetTopicTitleUkAsync("dotnet", "async", null);
    }

    public async Task DisposeAsync()
    {
        foreach (var (sql, parameters) in _restore)
        {
            await fixture.ExecuteSqlAsync(sql, parameters);
        }

        await fixture.ResetProgressAsync();
    }

    [Fact]
    public async Task Tree_in_uk_serves_uk_titles_and_flags_english_fallback_per_item()
    {
        var tree = await GetAsync<KnowledgeTreeDto>("/api/knowledge/tree?lang=uk");

        var dotnet = tree.Domains.Single(d => d.Slug == "dotnet");
        (dotnet.Title, dotnet.TitleFallback).ShouldBe((DomainUk, false));
        var clrMemory = dotnet.Topics.Single(t => t.Slug == "clr-memory");
        (clrMemory.Title, clrMemory.TitleFallback).ShouldBe((ClrMemoryUk, false));
        var async = dotnet.Topics.Single(t => t.Slug == "async");
        (async.Title, async.TitleFallback).ShouldBe((AsyncEn, true));
    }

    [Fact]
    public async Task Tree_in_en_ignores_uk_titles_and_never_reports_fallback()
    {
        var tree = await GetAsync<KnowledgeTreeDto>("/api/knowledge/tree?lang=en");

        var dotnet = tree.Domains.Single(d => d.Slug == "dotnet");
        (dotnet.Title, dotnet.TitleFallback).ShouldBe((DomainEn, false));
        dotnet.Topics.Single(t => t.Slug == "clr-memory").Title.ShouldBe(ClrMemoryEn);
        tree.Domains.ShouldAllBe(d => !d.TitleFallback && d.Topics.All(t => !t.TitleFallback));
    }

    [Fact]
    public async Task Concept_page_ancestors_follow_the_requested_language()
    {
        var uk = await GetAsync<ConceptPageDto>("/api/knowledge/concepts/dotnet/clr-memory/span-memory?lang=uk");
        var en = await GetAsync<ConceptPageDto>("/api/knowledge/concepts/dotnet/clr-memory/span-memory?lang=en");

        uk.Domain.ShouldBe(new ConceptRefDto("dotnet", DomainUk));
        uk.Topic.ShouldBe(new ConceptRefDto("clr-memory", ClrMemoryUk));
        en.Domain.ShouldBe(new ConceptRefDto("dotnet", DomainEn));
        en.Topic.ShouldBe(new ConceptRefDto("clr-memory", ClrMemoryEn));
    }

    [Fact]
    public async Task Overview_scope_titles_follow_the_requested_language_with_fallback()
    {
        var uk = await GetAsync<ReviewOverviewDto>(ReviewApi.Overview + "?scope=topic&domain=dotnet&topic=clr-memory&lang=uk");
        var en = await GetAsync<ReviewOverviewDto>(ReviewApi.Overview + "?scope=topic&domain=dotnet&topic=clr-memory");
        var fallback = await GetAsync<ReviewOverviewDto>(ReviewApi.Overview + "?scope=topic&domain=dotnet&topic=async&lang=uk");

        (uk.Scope.DomainTitle, uk.Scope.TopicTitle).ShouldBe((DomainUk, ClrMemoryUk));
        (en.Scope.DomainTitle, en.Scope.TopicTitle).ShouldBe((DomainEn, ClrMemoryEn));
        (fallback.Scope.DomainTitle, fallback.Scope.TopicTitle).ShouldBe((DomainUk, AsyncEn));
    }

    [Fact]
    public async Task Session_and_summary_titles_follow_the_requested_language()
    {
        var response = await fixture.Client.PostAsJsonAsync(ReviewApi.Sessions + "?lang=uk", ReviewApi.TopicScope(3, 3));
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var started = (await response.Content.ReadFromJsonAsync<SessionDto>(ApiFixture.Json)).ShouldNotBeNull();

        (started.Scope.DomainTitle, started.Scope.TopicTitle).ShouldBe((DomainUk, ClrMemoryUk));
        started.Questions.ShouldNotBeEmpty();
        started.Questions.ShouldAllBe(q => q.Domain.Title == DomainUk && q.Topic.Title == ClrMemoryUk);

        var en = await GetAsync<SessionDto>($"{ReviewApi.Sessions}/{started.Id}?lang=en");
        en.Questions.ShouldAllBe(q => q.Domain.Title == DomainEn && q.Topic.Title == ClrMemoryEn);
        en.Scope.TopicTitle.ShouldBe(ClrMemoryEn);

        var finish = await fixture.Client.PostAsync($"{ReviewApi.Sessions}/{started.Id}/finish?lang=uk", content: null);
        finish.StatusCode.ShouldBe(HttpStatusCode.OK, await finish.Content.ReadAsStringAsync());
        var summary = (await finish.Content.ReadFromJsonAsync<SessionSummaryDto>(ApiFixture.Json)).ShouldNotBeNull();
        (summary.Scope.DomainTitle, summary.Scope.TopicTitle).ShouldBe((DomainUk, ClrMemoryUk));
    }

    [Fact]
    public async Task Uk_titles_from_the_manifest_are_imported_and_served()
    {
        var tree = await GetAsync<KnowledgeTreeDto>("/api/knowledge/tree?lang=uk");

        // tests/Fixtures/api-content/manifest.json: titles.uk on aspnet-data and aspnet-data/pipeline, not on efcore.
        var aspnet = tree.Domains.Single(d => d.Slug == "aspnet-data");
        (aspnet.Title, aspnet.TitleFallback).ShouldBe(("ASP.NET Core, EF Core та PostgreSQL", false));
        var pipeline = aspnet.Topics.Single(t => t.Slug == "pipeline");
        (pipeline.Title, pipeline.TitleFallback).ShouldBe(("Конвеєр запитів", false));
        var efcore = aspnet.Topics.Single(t => t.Slug == "efcore");
        (efcore.Title, efcore.TitleFallback).ShouldBe(("EF Core", true));
    }

    // ---- helpers ----

    private async Task SetDomainTitleUkAsync(string domain, string? titleUk)
    {
        var previous = await fixture.ExecuteScalarAsync(
            "SELECT title_uk FROM domains WHERE slug = @slug", new NpgsqlParameter("slug", domain));
        _restore.Add(("UPDATE domains SET title_uk = @title WHERE slug = @slug",
            [new NpgsqlParameter("title", previous ?? DBNull.Value), new NpgsqlParameter("slug", domain)]));

        var affected = await fixture.ExecuteSqlAsync(
            "UPDATE domains SET title_uk = @title WHERE slug = @slug",
            new NpgsqlParameter("title", (object?)titleUk ?? DBNull.Value),
            new NpgsqlParameter("slug", domain));
        affected.ShouldBe(1, $"domain '{domain}' should exist");
    }

    private async Task SetTopicTitleUkAsync(string domain, string topic, string? titleUk)
    {
        const string where = "slug = @topic AND domain_id = (SELECT id FROM domains WHERE slug = @domain)";
        var previous = await fixture.ExecuteScalarAsync(
            $"SELECT title_uk FROM topics WHERE {where}",
            new NpgsqlParameter("topic", topic), new NpgsqlParameter("domain", domain));
        _restore.Add(($"UPDATE topics SET title_uk = @title WHERE {where}",
            [new NpgsqlParameter("title", previous ?? DBNull.Value), new NpgsqlParameter("topic", topic), new NpgsqlParameter("domain", domain)]));

        var affected = await fixture.ExecuteSqlAsync(
            $"UPDATE topics SET title_uk = @title WHERE {where}",
            new NpgsqlParameter("title", (object?)titleUk ?? DBNull.Value),
            new NpgsqlParameter("topic", topic),
            new NpgsqlParameter("domain", domain));
        affected.ShouldBe(1, $"topic '{domain}/{topic}' should exist");
    }

    private async Task<T> GetAsync<T>(string url) where T : class
    {
        var response = await fixture.Client.GetAsync(url);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<T>(ApiFixture.Json)).ShouldNotBeNull();
    }
}
