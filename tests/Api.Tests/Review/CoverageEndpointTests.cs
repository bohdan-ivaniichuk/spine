using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Spine.Api.Endpoints;
using Spine.Application.Review;
using Spine.Application.Review.GetReviewCoverage;

namespace Spine.Api.Tests.Review;

[Collection("api")]
[Trait("Category", "Integration")]
public sealed class CoverageEndpointTests(ApiFixture fixture) : IAsyncLifetime
{
    private const string Coverage = ApiUrl.Prefix + ApiRoutes.Review.Coverage;

    public async Task InitializeAsync()
    {
        await fixture.ImportAsync();
        await fixture.ResetProgressAsync();
    }

    public Task DisposeAsync() => fixture.ResetProgressAsync();

    [Fact]
    public async Task Lists_every_manifest_topic_in_order_including_empty_ones_in_camelCase()
    {
        var response = await fixture.Client.GetAsync(Coverage);

        var raw = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.OK, raw);
        using var body = JsonDocument.Parse(raw);
        var first = body.RootElement.GetProperty("topics")[0];
        foreach (var name in new[] { "domain", "topic", "eligible", "seen", "due" })
        {
            first.TryGetProperty(name, out _).ShouldBeTrue(name);
        }

        var coverage = JsonSerializer.Deserialize<ReviewCoverageDto>(raw, ApiFixture.Json).ShouldNotBeNull();
        coverage.Topics.Select(t => $"{t.Domain}/{t.Topic}").ShouldBe(ManifestTopics());

        var clr = coverage.Topics.Single(t => t.Topic == "clr-memory");
        clr.ShouldBe(new TopicCoverageDto("dotnet", "clr-memory", ReviewApi.ClrMemoryMcqCount, 0, 0));
        coverage.Topics.Single(t => t.Topic == "async").ShouldBe(new TopicCoverageDto("dotnet", "async", ReviewApi.AsyncMcqCount, 0, 0));
        coverage.Topics.Single(t => t.Topic == ReviewApi.EmptyTopic).ShouldBe(new TopicCoverageDto("dotnet", ReviewApi.EmptyTopic, 0, 0, 0));
    }

    [Fact]
    public async Task Seen_and_due_follow_the_learners_review_states()
    {
        var session = await ReviewApi.StartAsync(fixture.Client, ReviewApi.TopicScope(3, 3));
        foreach (var question in session.Questions)
        {
            await ReviewApi.AnswerAsync(fixture.Client, session.Id, question.Id, 0);
        }

        var afterAnswers = await GetClrAsync();
        afterAnswers.ShouldBe(new TopicCoverageDto("dotnet", "clr-memory", ReviewApi.ClrMemoryMcqCount, 3, 0));

        await fixture.ExecuteSqlAsync(
            $"UPDATE review_states SET due_at = now() - interval '1 hour' WHERE question_id = '{session.Questions[0].Id}'");

        (await GetClrAsync()).Due.ShouldBe(1);
    }

    [Fact]
    public async Task Accepts_lang_and_rejects_unknown_languages()
    {
        (await fixture.Client.GetAsync(Coverage + "?lang=uk")).StatusCode.ShouldBe(HttpStatusCode.OK);
        await ReviewApi.ShouldBeProblemAsync(await fixture.Client.GetAsync(Coverage + "?lang=fr"), HttpStatusCode.BadRequest);
    }

    private async Task<TopicCoverageDto> GetClrAsync()
    {
        var coverage = (await fixture.Client.GetFromJsonAsync<ReviewCoverageDto>(Coverage, ApiFixture.Json)).ShouldNotBeNull();
        return coverage.Topics.Single(t => t.Topic == "clr-memory");
    }

    /// <summary>Every (domain, topic) of the fixture manifest in file order; it has nothing deprecated.</summary>
    private static List<string> ManifestTopics()
    {
        var manifest = JsonNode.Parse(File.ReadAllText(Path.Combine(ApiFixture.ContentDir, "manifest.json")))!;
        return manifest["domains"]!.AsArray()
            .SelectMany(d => d!["topics"]!.AsArray().Select(t => $"{d["slug"]!.GetValue<string>()}/{t!["slug"]!.GetValue<string>()}"))
            .ToList();
    }
}
