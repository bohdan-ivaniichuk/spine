using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Spine.Application.Knowledge;

namespace Spine.Api.Tests.Knowledge;

[Collection("api")]
[Trait("Category", "Integration")]
public sealed class ConceptEndpointTests(ApiFixture fixture) : IAsyncLifetime
{
    private const string SpanMemory = "/api/knowledge/concepts/dotnet/clr-memory/span-memory";

    public Task InitializeAsync() => fixture.ImportAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<ConceptPageDto> GetConceptAsync(string path)
    {
        var response = await fixture.Client.GetAsync(path);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<ConceptPageDto>(ApiFixture.Json);
        return page.ShouldNotBeNull();
    }

    private static async Task ShouldBeNotFoundProblemAsync(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("status").GetInt32().ShouldBe(404);
    }

    [Fact]
    public async Task English_concept_page_has_note_ancestors_and_counts()
    {
        var page = await GetConceptAsync(SpanMemory + "?lang=en");

        page.Domain.Slug.ShouldBe("dotnet");
        page.Domain.Title.ShouldBe("C# & .NET Runtime");
        page.Topic.Slug.ShouldBe("clr-memory");
        page.Topic.Title.ShouldBe("Memory & GC");
        page.Slug.ShouldBe("span-memory");
        page.Level.ShouldBe("S");
        page.Reviewed.ShouldBeFalse();
        page.SortOrder.ShouldBe(2);
        page.RequestedLanguage.ShouldBe("en");
        page.Language.ShouldBe("en");
        page.Fallback.ShouldBeFalse();
        page.Title.ShouldBe("Span<T>, Memory<T> and zero-allocation code");
        page.Note.ShouldNotBeNullOrWhiteSpace();
        page.Note.ShouldContain("#");
        page.QuestionCount.ShouldBe(5);
        page.KataCount.ShouldBe(1);
        page.DeprecatedAt.ShouldBeNull();
    }

    [Fact]
    public async Task Default_language_is_english()
    {
        var page = await GetConceptAsync(SpanMemory);

        page.RequestedLanguage.ShouldBe("en");
        page.Fallback.ShouldBeFalse();
    }

    [Fact]
    public async Task Uk_without_translation_falls_back_to_english()
    {
        var en = await GetConceptAsync(SpanMemory + "?lang=en");
        var uk = await GetConceptAsync(SpanMemory + "?lang=uk");

        uk.RequestedLanguage.ShouldBe("uk");
        uk.Language.ShouldBe("en");
        uk.Fallback.ShouldBeTrue();
        uk.Title.ShouldBe(en.Title);
        uk.Note.ShouldBe(en.Note);
    }

    [Fact]
    public async Task Uk_translation_from_content_is_served_without_fallback()
    {
        const string path = "/api/knowledge/concepts/dotnet/async/async-await";
        var en = await GetConceptAsync(path + "?lang=en");
        var uk = await GetConceptAsync(path + "?lang=uk");

        uk.Reviewed.ShouldBeTrue();
        uk.RequestedLanguage.ShouldBe("uk");
        uk.Language.ShouldBe("uk");
        uk.Fallback.ShouldBeFalse();
        uk.Title.ShouldBe("Скінченний автомат async/await");
        uk.Note.ShouldNotBe(en.Note);
        uk.QuestionCount.ShouldBe(en.QuestionCount);
    }

    [Fact]
    public async Task Unsupported_language_is_a_400_problem()
    {
        var response = await fixture.Client.GetAsync(SpanMemory + "?lang=de");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
    }

    [Theory]
    [InlineData("/api/knowledge/concepts/nope/clr-memory/span-memory")]
    [InlineData("/api/knowledge/concepts/dotnet/nope/span-memory")]
    [InlineData("/api/knowledge/concepts/dotnet/clr-memory/nope")]
    [InlineData("/api/knowledge/concepts/dotnet/async/span-memory")]
    public async Task Unknown_path_is_a_404_problem(string path)
    {
        var response = await fixture.Client.GetAsync(path);

        await ShouldBeNotFoundProblemAsync(response);
    }

    [Fact]
    public async Task Deprecated_concept_is_404_unless_includeDeprecated()
    {
        var deprecatedAt = new DateTimeOffset(2026, 5, 6, 7, 8, 9, TimeSpan.Zero);
        await fixture.SetConceptDeprecatedAsync("span-memory", deprecatedAt);
        try
        {
            var hidden = await fixture.Client.GetAsync(SpanMemory);
            await ShouldBeNotFoundProblemAsync(hidden);

            var shown = await GetConceptAsync(SpanMemory + "?includeDeprecated=true");
            shown.Slug.ShouldBe("span-memory");
            shown.DeprecatedAt.ShouldBe(deprecatedAt);
        }
        finally
        {
            await fixture.SetConceptDeprecatedAsync("span-memory", null);
        }

        (await GetConceptAsync(SpanMemory)).DeprecatedAt.ShouldBeNull();
    }
}
