using System.Net;
using System.Text.Json;
using Spine.Application.Review;

namespace Spine.Api.Tests.Review;

[Collection("api")]
[Trait("Category", "Integration")]
public sealed class WeakSpotsEndpointTests(ApiFixture fixture) : IAsyncLifetime
{
    private const string WeakSpots = "/api/review/weak-spots";

    public async Task InitializeAsync()
    {
        await fixture.ImportAsync();
        await fixture.ResetProgressAsync();
    }

    public Task DisposeAsync() => fixture.ResetProgressAsync();

    [Fact]
    [Trait("Requirement", "FR-007")]
    public async Task Serves_the_window_and_the_answered_concept_in_camelCase()
    {
        // The first four new clr-memory questions in taxonomy order are all of stack-vs-heap.
        var session = await ReviewApi.StartAsync(fixture.Client, ReviewApi.TopicScope(4, 4));
        foreach (var question in session.Questions)
        {
            await ReviewApi.AnswerAsync(fixture.Client, session.Id, question.Id, 0);
        }

        var response = await fixture.Client.GetAsync(WeakSpots);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var raw = await response.Content.ReadAsStringAsync();
        using var body = JsonDocument.Parse(raw);
        foreach (var name in new[] { "since", "windowDays", "minAnswers", "concepts", "topics" })
        {
            body.RootElement.TryGetProperty(name, out _).ShouldBeTrue(name);
        }

        var spots = JsonSerializer.Deserialize<WeakSpotsDto>(raw, ApiFixture.Json).ShouldNotBeNull();
        (spots.WindowDays, spots.MinAnswers).ShouldBe((30, 3));
        var concept = spots.Concepts.Single();
        concept.Concept.ShouldBe("stack-vs-heap");
        concept.Answers.ShouldBe(4);
        concept.Readiness.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Requirement", "FR-012")]
    public async Task Language_does_not_change_the_payload_and_unknown_languages_are_rejected()
    {
        var session = await ReviewApi.StartAsync(fixture.Client, ReviewApi.TopicScope(4, 4));
        foreach (var question in session.Questions)
        {
            await ReviewApi.AnswerAsync(fixture.Client, session.Id, question.Id, 0);
        }

        var en = await fixture.Client.GetAsync(WeakSpots);
        Assert.Equal(HttpStatusCode.OK, en.StatusCode);

        var uk = await fixture.Client.GetStringAsync(WeakSpots + "?lang=uk");

        // `since` follows the clock, not the language; everything else must be identical.
        WithoutSince(uk).ShouldBe(WithoutSince(await en.Content.ReadAsStringAsync()));
        (await fixture.Client.GetAsync(WeakSpots + "?lang=fr")).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        JsonSerializer.Deserialize<WeakSpotsDto>(uk, ApiFixture.Json).ShouldNotBeNull().Concepts.ShouldNotBeEmpty();
    }

    private static string WithoutSince(string json)
    {
        var node = System.Text.Json.Nodes.JsonNode.Parse(json)!.AsObject();
        node.Remove("since");
        return node.ToJsonString();
    }
}
