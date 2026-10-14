using System.Net;
using System.Text.Json;

namespace Spine.Api.Tests.Review;

[Collection("api")]
[Trait("Category", "Integration")]
public sealed class OverviewEndpointTests(ApiFixture fixture) : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await fixture.ImportAsync();
        await fixture.ResetProgressAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Topic_overview_reports_counts_titles_defaults_and_limits_in_camelCase()
    {
        var response = await fixture.Client.GetAsync(ReviewApi.Overview + "?scope=topic&domain=dotnet&topic=clr-memory");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = body.RootElement;
        var scope = root.GetProperty("scope");
        scope.GetProperty("type").GetString().ShouldBe("topic");
        scope.GetProperty("domain").GetString().ShouldBe("dotnet");
        scope.GetProperty("topic").GetString().ShouldBe("clr-memory");
        scope.GetProperty("domainTitle").GetString().ShouldBe("C# & .NET Runtime");
        scope.GetProperty("topicTitle").GetString().ShouldBe("Memory & GC");
        root.GetProperty("dueCount").GetInt32().ShouldBe(0);
        root.GetProperty("newCount").GetInt32().ShouldBe(ReviewApi.ClrMemoryMcqCount);
        root.GetProperty("totalEligible").GetInt32().ShouldBe(ReviewApi.ClrMemoryMcqCount);
        root.GetProperty("nextDueAt").ValueKind.ShouldBe(JsonValueKind.Null);
        root.GetProperty("defaults").GetProperty("totalLimit").GetInt32().ShouldBe(20);
        root.GetProperty("defaults").GetProperty("newLimit").GetInt32().ShouldBe(10);
        root.GetProperty("limits").GetProperty("min").GetInt32().ShouldBe(1);
        root.GetProperty("limits").GetProperty("max").GetInt32().ShouldBe(100);
    }

    [Fact]
    public async Task Scope_defaults_to_all()
    {
        var response = await fixture.Client.GetAsync(ReviewApi.Overview);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("scope").GetProperty("type").GetString().ShouldBe("all");
        body.RootElement.GetProperty("scope").GetProperty("domain").ValueKind.ShouldBe(JsonValueKind.Null);
        var totalEligible = body.RootElement.GetProperty("totalEligible").GetInt32();
        totalEligible.ShouldBe(ReviewApi.AllMcqCount, "\"all\" spans every imported topic");
        body.RootElement.GetProperty("dueCount").GetInt32().ShouldBe(0);
        body.RootElement.GetProperty("newCount").GetInt32().ShouldBe(totalEligible, "nothing has been answered yet");
    }

    [Fact]
    public async Task Counts_change_after_answers_and_next_due_is_reported()
    {
        var session = await ReviewApi.StartAsync(fixture.Client, ReviewApi.TopicScope(3, 3));
        await ReviewApi.AnswerAsync(fixture.Client, session.Id, session.Questions[0].Id, 0);

        var response = await fixture.Client.GetAsync(ReviewApi.Overview + "?scope=domain&domain=dotnet");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var totalEligible = body.RootElement.GetProperty("totalEligible").GetInt32();
        totalEligible.ShouldBe(ReviewApi.ClrMemoryMcqCount + ReviewApi.AsyncMcqCount);
        body.RootElement.GetProperty("dueCount").GetInt32().ShouldBe(0);
        body.RootElement.GetProperty("newCount").GetInt32().ShouldBe(totalEligible - 1, "one question now has a review state");
        body.RootElement.GetProperty("nextDueAt").ValueKind.ShouldBe(JsonValueKind.String);
    }

    [Theory]
    [InlineData("?scope=bogus")]
    [InlineData("?scope=domain")]
    [InlineData("?scope=topic&domain=dotnet")]
    [InlineData("?scope=all&domain=dotnet")]
    [InlineData("?scope=domain&domain=dotnet&topic=clr-memory")]
    [InlineData("?scope=domain&domain=Not_A_Slug")]
    [InlineData("?scope=topic&domain=dotnet&topic=clr--memory")]
    public async Task Invalid_scope_parameters_are_a_400_problem(string query)
    {
        var response = await fixture.Client.GetAsync(ReviewApi.Overview + query);

        await ReviewApi.ShouldBeProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("?scope=domain&domain=nope")]
    [InlineData("?scope=topic&domain=dotnet&topic=nope")]
    [InlineData("?scope=topic&domain=angular&topic=clr-memory")]
    public async Task Unknown_scope_is_a_404_problem(string query)
    {
        var response = await fixture.Client.GetAsync(ReviewApi.Overview + query);

        await ReviewApi.ShouldBeProblemAsync(response, HttpStatusCode.NotFound, "Scope not found");
    }
}
