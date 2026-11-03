using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Spine.Application.Review.Sessions;
using Spine.Application.Review.Shared;

namespace Spine.Api.Tests.Review;

[Collection("api")]
[Trait("Category", "Integration")]
public sealed class SessionsEndpointTests(ApiFixture fixture) : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await fixture.ImportAsync();
        await fixture.ResetProgressAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Start_returns_201_with_location_and_the_session_shape()
    {
        var response = await fixture.Client.PostAsJsonAsync(ReviewApi.Sessions, ReviewApi.TopicScope(5, 5));

        var raw = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.Created, raw);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/json");
        using var body = JsonDocument.Parse(raw);
        var root = body.RootElement;
        var id = root.GetProperty("id").GetGuid();
        response.Headers.Location.ShouldNotBeNull().ToString().ShouldEndWith($"{ReviewApi.Sessions}/{id}");

        root.GetProperty("scope").GetProperty("type").GetString().ShouldBe("topic");
        root.GetProperty("startedAt").ValueKind.ShouldBe(JsonValueKind.String);
        root.GetProperty("finishedAt").ValueKind.ShouldBe(JsonValueKind.Null);
        root.GetProperty("totalLimit").GetInt32().ShouldBe(5);
        root.GetProperty("newLimit").GetInt32().ShouldBe(5);
        root.GetProperty("answers").GetArrayLength().ShouldBe(0);
        root.GetProperty("skippedQuestionIds").GetArrayLength().ShouldBe(0);

        var questions = root.GetProperty("questions");
        questions.GetArrayLength().ShouldBe(5);
        var first = questions[0];
        first.GetProperty("id").GetString().ShouldBe("dotnet.clr-memory.stack-vs-heap.mcq.1");
        first.GetProperty("level").GetString().ShouldBeOneOf("M", "S", "L");
        first.GetProperty("reviewed").GetBoolean().ShouldBeFalse("the fixture clr-memory topic is unreviewed");
        first.GetProperty("isNew").GetBoolean().ShouldBeTrue();
        first.GetProperty("prompt").GetString().ShouldNotBeNullOrWhiteSpace();
        first.GetProperty("domain").GetProperty("slug").GetString().ShouldBe("dotnet");
        first.GetProperty("topic").GetProperty("title").GetString().ShouldBe("Memory & GC");
        first.GetProperty("concept").GetProperty("slug").GetString().ShouldBe("stack-vs-heap");
        first.GetProperty("concept").GetProperty("title").GetString().ShouldNotBeNullOrWhiteSpace();

        var options = first.GetProperty("options");
        options.GetArrayLength().ShouldBe(4);
        foreach (var option in options.EnumerateArray())
        {
            option.ValueKind.ShouldBe(JsonValueKind.String, "options are text only");
        }

        raw.ShouldNotContain("\"correct\"", customMessage: "the correct option must never leave the server");
        raw.ShouldNotContain("explanation");
    }

    [Fact]
    public async Task Start_uses_the_configured_defaults_when_limits_are_omitted()
    {
        var session = await ReviewApi.StartAsync(fixture.Client, new { scope = "all" });

        session.TotalLimit.ShouldBe(20);
        session.NewLimit.ShouldBe(10);
        session.Questions.Count.ShouldBe(10, "all questions are new, so the default new limit caps the session");
        session.Scope.Type.ShouldBe("all");
    }

    [Fact]
    public async Task Questions_follow_taxonomy_order()
    {
        var session = await ReviewApi.StartAsync(fixture.Client, ReviewApi.TopicScope(ReviewApi.ClrMemoryMcqCount, ReviewApi.ClrMemoryMcqCount));

        session.Questions.Count.ShouldBe(ReviewApi.ClrMemoryMcqCount);
        session.Questions.Select(q => q.Concept.Slug).Distinct().ShouldBe(
            ["stack-vs-heap", "gc-modes", "span-memory"]);
        session.Questions.Select(q => q.Id).ShouldBeUnique();
    }

    [Theory]
    [InlineData("""{"scope":"topic","domain":"dotnet","topic":"clr-memory","totalLimit":0}""")]
    [InlineData("""{"scope":"topic","domain":"dotnet","topic":"clr-memory","newLimit":101}""")]
    [InlineData("""{"scope":"nope"}""")]
    [InlineData("""{"scope":"domain"}""")]
    [InlineData("""{"scope":"all","topic":"clr-memory"}""")]
    public async Task Invalid_start_request_is_a_400_problem(string json)
    {
        var response = await fixture.Client.PostAsync(ReviewApi.Sessions, new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        await ReviewApi.ShouldBeProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    [Trait("Requirement", "FR-012")]
    public async Task Concept_scope_starts_a_session_of_that_concept_only()
    {
        var response = await fixture.Client.PostAsJsonAsync(ReviewApi.Sessions,
            new { scope = "concept", domain = "dotnet", topic = "clr-memory", concept = "gc-modes", totalLimit = 20, newLimit = 20 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var session = (await response.Content.ReadFromJsonAsync<SessionDto>(ApiFixture.Json)).ShouldNotBeNull();
        session.Questions.Count.ShouldBe(4);
        session.Questions.ShouldAllBe(q => q.Concept.Slug == "gc-modes");
        session.Scope.ShouldBe(new ScopeDto("concept", "dotnet", "clr-memory", "C# & .NET Runtime", "Memory & GC", "gc-modes", "GC modes"));
    }

    [Theory]
    [Trait("Requirement", "FR-012")]
    [InlineData("""{"scope":"concept","domain":"dotnet","topic":"clr-memory"}""")]
    [InlineData("""{"scope":"concept","domain":"dotnet","concept":"gc-modes"}""")]
    [InlineData("""{"scope":"topic","domain":"dotnet","topic":"clr-memory","concept":"gc-modes"}""")]
    [InlineData("""{"scope":"concept","domain":"dotnet","topic":"clr-memory","concept":"GC Modes"}""")]
    [InlineData("""{"scope":"concept","domain":"dotnet","topic":"clr-memory","concept":"gc-modes-"}""")]
    [InlineData("""{"scope":"concept","domain":"dotnet","topic":"clr-memory","concept":" "}""")]
    [InlineData("""{"scope":"all","concept":"gc-modes"}""")]
    [InlineData("""{"scope":"domain","domain":"dotnet","concept":"gc-modes"}""")]
    public async Task Concept_slug_is_validated_against_the_scope(string json)
    {
        var response = await fixture.Client.PostAsync(ReviewApi.Sessions, new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        await ReviewApi.ShouldBeProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    [Trait("Requirement", "FR-012")]
    public async Task Unknown_concept_is_a_404_problem()
    {
        var response = await fixture.Client.PostAsJsonAsync(ReviewApi.Sessions, new { scope = "concept", domain = "dotnet", topic = "clr-memory", concept = "nope" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_scope_is_a_404_problem()
    {
        var response = await fixture.Client.PostAsJsonAsync(ReviewApi.Sessions, new { scope = "domain", domain = "nope" });

        await ReviewApi.ShouldBeProblemAsync(response, HttpStatusCode.NotFound, "Scope not found");
    }

    [Fact]
    public async Task Nothing_to_review_is_a_409_problem()
    {
        var response = await fixture.Client.PostAsJsonAsync(ReviewApi.Sessions, new { scope = "topic", domain = "dotnet", topic = ReviewApi.EmptyTopic });

        await ReviewApi.ShouldBeProblemAsync(response, HttpStatusCode.Conflict, "Nothing to review");
    }

    [Fact]
    public async Task Nothing_to_review_leaves_the_open_session_open()
    {
        var open = await ReviewApi.StartAsync(fixture.Client);

        var response = await fixture.Client.PostAsJsonAsync(ReviewApi.Sessions, new { scope = "topic", domain = "dotnet", topic = ReviewApi.EmptyTopic });

        await ReviewApi.ShouldBeProblemAsync(response, HttpStatusCode.Conflict, "Nothing to review");
        var current = (await fixture.Client.GetFromJsonAsync<SessionDto>(ReviewApi.Current, ApiFixture.Json)).ShouldNotBeNull();
        current.Id.ShouldBe(open.Id);
        current.FinishedAt.ShouldBeNull();
    }

    [Fact]
    public async Task Current_resumes_with_the_same_question_order_and_the_given_answers()
    {
        // SC-005: start, answer k = 2, come back → same membership in the same order, both answers recorded.
        var started = await ReviewApi.StartAsync(fixture.Client);
        var first = await ReviewApi.AnswerAsync(fixture.Client, started.Id, started.Questions[0].Id, 0);
        var second = await ReviewApi.AnswerAsync(fixture.Client, started.Id, started.Questions[1].Id, 3);

        var resumed = (await fixture.Client.GetFromJsonAsync<SessionDto>(ReviewApi.Current, ApiFixture.Json)).ShouldNotBeNull();

        resumed.Id.ShouldBe(started.Id);
        resumed.Questions.Select(q => q.Id).ShouldBe(started.Questions.Select(q => q.Id));
        resumed.Answers.ShouldBe(
        [
            new SessionAnswerDto(started.Questions[0].Id, 0, first.IsCorrect),
            new SessionAnswerDto(started.Questions[1].Id, 3, second.IsCorrect),
        ]);
        resumed.SkippedQuestionIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task Current_is_204_without_an_open_session_and_200_with_one()
    {
        var none = await fixture.Client.GetAsync(ReviewApi.Current);
        none.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var started = await ReviewApi.StartAsync(fixture.Client);

        var current = await fixture.Client.GetAsync(ReviewApi.Current);
        current.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await current.Content.ReadFromJsonAsync<SessionDto>(ApiFixture.Json))!.Id.ShouldBe(started.Id);
    }

    [Fact]
    public async Task Get_by_id_returns_the_session_and_404_or_400_otherwise()
    {
        var started = await ReviewApi.StartAsync(fixture.Client);

        var found = await fixture.Client.GetAsync($"{ReviewApi.Sessions}/{started.Id}");
        found.StatusCode.ShouldBe(HttpStatusCode.OK);
        var session = (await found.Content.ReadFromJsonAsync<SessionDto>(ApiFixture.Json)).ShouldNotBeNull();
        session.Id.ShouldBe(started.Id);
        session.Questions.Select(q => q.Id).ShouldBe(started.Questions.Select(q => q.Id));

        await ReviewApi.ShouldBeProblemAsync(await fixture.Client.GetAsync($"{ReviewApi.Sessions}/{Guid.NewGuid()}"), HttpStatusCode.NotFound, "Session not found");
        await ReviewApi.ShouldBeProblemAsync(await fixture.Client.GetAsync($"{ReviewApi.Sessions}/not-a-guid"), HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Starting_again_finishes_the_open_session()
    {
        var first = await ReviewApi.StartAsync(fixture.Client);
        await ReviewApi.AnswerAsync(fixture.Client, first.Id, first.Questions[0].Id, 0);

        var second = await ReviewApi.StartAsync(fixture.Client);

        second.Id.ShouldNotBe(first.Id);
        var closed = await fixture.Client.GetFromJsonAsync<SessionDto>($"{ReviewApi.Sessions}/{first.Id}", ApiFixture.Json);
        closed!.FinishedAt.ShouldNotBeNull();
        closed.Answers.ShouldHaveSingleItem();
        (await fixture.Client.GetFromJsonAsync<SessionDto>(ReviewApi.Current, ApiFixture.Json))!.Id.ShouldBe(second.Id);
    }
}
