using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Spine.Api.Endpoints.Review.StartSession;
using Spine.Application.Review.Sessions;

namespace Spine.Api.Tests.Review;

[Collection("api")]
[Trait("Category", "Integration")]
public sealed class WeakSessionEndpointTests(ApiFixture fixture) : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await fixture.ImportAsync();
        await fixture.ResetProgressAsync();
    }

    public Task DisposeAsync() => fixture.ResetProgressAsync();

    [Fact]
    [Trait("Requirement", "FR-001")]
    public async Task Weak_mode_starts_a_session_of_the_missed_questions()
    {
        var missed = await MissSomeAsync();

        var response = await fixture.Client.PostAsJsonAsync(ReviewApi.Sessions, new { mode = "weak", scope = "topic", domain = "dotnet", topic = "clr-memory" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var session = (await response.Content.ReadFromJsonAsync<SessionDto>(ApiFixture.Json)).ShouldNotBeNull();
        session.Mode.ShouldBe("weak");
        session.Questions.Select(q => q.Id).ShouldBe(missed, ignoreOrder: true);
    }

    [Fact]
    [Trait("Requirement", "FR-005")]
    public async Task Nothing_to_drill_is_a_409_problem()
    {
        var response = await fixture.Client.PostAsJsonAsync(ReviewApi.Sessions, new StartSessionRequest { Mode = "weak", Scope = "all" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await ReviewApi.ShouldBeProblemAsync(response, HttpStatusCode.Conflict, "Nothing to drill");
    }

    [Fact]
    [Trait("Requirement", "FR-001")]
    public async Task Unknown_mode_is_a_400_problem()
    {
        var response = await fixture.Client.PostAsJsonAsync(ReviewApi.Sessions, new StartSessionRequest { Mode = "hard", Scope = "all" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [Trait("Requirement", "FR-001")]
    public async Task Weak_mode_ignores_the_new_limit()
    {
        await MissSomeAsync();

        var response = await fixture.Client.PostAsJsonAsync(ReviewApi.Sessions, new StartSessionRequest { Mode = "weak", Scope = "all", NewLimit = 0 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    [Trait("Requirement", "FR-006")]
    public async Task Overview_reports_the_weak_count_of_the_scope()
    {
        var missed = await MissSomeAsync();

        using var body = JsonDocument.Parse(await fixture.Client.GetStringAsync(ReviewApi.Overview + "?scope=topic&domain=dotnet&topic=clr-memory"));

        Assert.Equal(missed.Count, body.RootElement.GetProperty("weakCount").GetInt32());
    }

    /// <summary>Answers five clr-memory questions with option 0 and returns the ones that were wrong (at least one).</summary>
    private async Task<List<string>> MissSomeAsync()
    {
        var session = await ReviewApi.StartAsync(fixture.Client, ReviewApi.TopicScope(5, 5));
        var missed = new List<string>();
        foreach (var question in session.Questions)
        {
            var feedback = await ReviewApi.AnswerAsync(fixture.Client, session.Id, question.Id, 0);
            if (!feedback.IsCorrect)
            {
                missed.Add(question.Id);
            }
        }

        await ReviewApi.FinishAsync(fixture.Client, session.Id);
        missed.ShouldNotBeEmpty("the fixture must have some wrong option 0 among the first five questions");
        return missed;
    }
}
