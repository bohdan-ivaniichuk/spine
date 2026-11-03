using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Spine.Api.Endpoints;
using Spine.Application.Review.Readiness;

namespace Spine.Api.Tests.Review;

[Collection("api")]
[Trait("Category", "Integration")]
public sealed class ReadinessEndpointTests(ApiFixture fixture) : IAsyncLifetime
{
    private const string Readiness = ApiUrl.Prefix + ApiRoutes.Review.Readiness;

    public async Task InitializeAsync()
    {
        await fixture.ImportAsync();
        await fixture.ResetProgressAsync();
    }

    public Task DisposeAsync() => fixture.ResetProgressAsync();

    [Fact]
    [Trait("Requirement", "FR-009")]
    public async Task Serves_every_domain_and_topic_in_camelCase_with_a_null_percent_where_nothing_counts()
    {
        var response = await fixture.Client.GetAsync(Readiness);

        var raw = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(raw);
        var domain = body.RootElement.GetProperty("domains")[0];
        foreach (var name in new[] { "domain", "ready", "counted", "percent", "topics" })
        {
            domain.TryGetProperty(name, out _).ShouldBeTrue(name);
        }

        var readiness = JsonSerializer.Deserialize<ReadinessDto>(raw, ApiFixture.Json).ShouldNotBeNull();
        var dotnet = readiness.Domains.Single(d => d.Domain == "dotnet");
        var empty = dotnet.Topics.Single(t => t.Topic == ReviewApi.EmptyTopic);
        (empty.Counted, empty.Percent).ShouldBe((0, (int?)null));
        empty.Concepts.ShouldBeEmpty();

        var clr = dotnet.Topics.Single(t => t.Topic == "clr-memory");
        (clr.Ready, clr.Counted, clr.Percent).ShouldBe((0, 3, 0));
        clr.Concepts.ShouldAllBe(c => c.State == "not-started" && c.Questions == 4);
    }

    [Fact]
    [Trait("Requirement", "FR-008")]
    public async Task Concepts_become_ready_once_every_question_reaches_seven_days()
    {
        // The first four new clr-memory questions in taxonomy order are all of stack-vs-heap, which has exactly 4 MCQs.
        var session = await ReviewApi.StartAsync(fixture.Client, ReviewApi.TopicScope(4, 4));
        foreach (var question in session.Questions)
        {
            await ReviewApi.AnswerAsync(fixture.Client, session.Id, question.Id, 0);
        }

        var concept = session.Questions[0].Concept.Slug;
        (await ClrConceptAsync(concept)).State.ShouldBe("in-progress");

        await fixture.ExecuteSqlAsync("UPDATE review_states SET interval_days = 7");

        var ready = await ClrConceptAsync(concept);
        ready.ShouldBe(new ConceptReadinessDto(concept, "ready", 4, 4));
    }

    [Fact]
    [Trait("Requirement", "FR-010")]
    public async Task Language_does_not_change_readiness()
    {
        var session = await ReviewApi.StartAsync(fixture.Client, ReviewApi.TopicScope(2, 2));
        await ReviewApi.AnswerAsync(fixture.Client, session.Id, session.Questions[0].Id, 0);

        var en = await fixture.Client.GetStringAsync(Readiness);
        var uk = await fixture.Client.GetStringAsync(Readiness + "?lang=uk");

        uk.ShouldBe(en);
        await ReviewApi.ShouldBeProblemAsync(await fixture.Client.GetAsync(Readiness + "?lang=fr"), HttpStatusCode.BadRequest);
    }

    private async Task<ConceptReadinessDto> ClrConceptAsync(string concept)
    {
        var readiness = (await fixture.Client.GetFromJsonAsync<ReadinessDto>(Readiness, ApiFixture.Json)).ShouldNotBeNull();
        return readiness.Domains.Single(d => d.Domain == "dotnet").Topics.Single(t => t.Topic == "clr-memory").Concepts.Single(c => c.Concept == concept);
    }
}
