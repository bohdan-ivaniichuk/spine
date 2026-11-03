using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Npgsql;
using Spine.Application.Review.Sessions;

namespace Spine.Api.Tests.Review;

[Collection("api")]
[Trait("Category", "Integration")]
public sealed class AnswersEndpointTests(ApiFixture fixture) : IAsyncLifetime
{
    private SessionDto _session = null!;

    public async Task InitializeAsync()
    {
        await fixture.ImportAsync();
        await fixture.ResetProgressAsync();
        _session = await ReviewApi.StartAsync(fixture.Client);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private string AnswersPath => $"{ReviewApi.Sessions}/{_session.Id}/answers";

    [Fact]
    public async Task Answer_returns_feedback_and_the_session_records_it()
    {
        var questionId = _session.Questions[0].Id;

        var response = await fixture.Client.PostAsJsonAsync(AnswersPath, new { questionId, selectedIndex = 2 });

        var raw = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.OK, raw);
        using var body = JsonDocument.Parse(raw);
        var root = body.RootElement;
        root.GetProperty("questionId").GetString().ShouldBe(questionId);
        root.GetProperty("selectedIndex").GetInt32().ShouldBe(2);
        var correctIndex = root.GetProperty("correctIndex").GetInt32();
        correctIndex.ShouldBeInRange(0, 3);
        root.GetProperty("isCorrect").GetBoolean().ShouldBe(correctIndex == 2);
        root.GetProperty("explanation").GetString().ShouldNotBeNullOrWhiteSpace();
        root.GetProperty("intervalDays").GetInt32().ShouldBe(1);
        var nextDueAt = root.GetProperty("nextDueAt").GetDateTimeOffset();
        nextDueAt.ShouldBeGreaterThan(DateTimeOffset.UtcNow.AddHours(23));
        nextDueAt.ShouldBeLessThan(DateTimeOffset.UtcNow.AddHours(25));

        var session = await fixture.Client.GetFromJsonAsync<SessionDto>($"{ReviewApi.Sessions}/{_session.Id}", ApiFixture.Json);
        session!.Answers.ShouldBe([new SessionAnswerDto(questionId, 2, correctIndex == 2)]);
    }

    [Fact]
    public async Task Second_answer_to_the_same_question_is_a_409_problem()
    {
        var questionId = _session.Questions[1].Id;
        await ReviewApi.AnswerAsync(fixture.Client, _session.Id, questionId, 0);

        var response = await fixture.Client.PostAsJsonAsync(AnswersPath, new { questionId, selectedIndex = 1 });

        await ReviewApi.ShouldBeProblemAsync(response, HttpStatusCode.Conflict, "Question already answered");
        var session = await fixture.Client.GetFromJsonAsync<SessionDto>($"{ReviewApi.Sessions}/{_session.Id}", ApiFixture.Json);
        session!.Answers.ShouldHaveSingleItem().SelectedIndex.ShouldBe(0);
    }

    [Theory]
    [InlineData("dotnet.clr-memory.span-memory.mcq.4")]
    [InlineData("nope.nope.nope.mcq.1")]
    public async Task Question_outside_the_session_is_a_422_problem(string questionId)
    {
        var response = await fixture.Client.PostAsJsonAsync(AnswersPath, new { questionId, selectedIndex = 0 });

        await ReviewApi.ShouldBeProblemAsync(response, HttpStatusCode.UnprocessableEntity, "Question not in session");
    }

    [Theory]
    [InlineData("""{"questionId":"dotnet.clr-memory.stack-vs-heap.mcq.1","selectedIndex":4}""")]
    [InlineData("""{"questionId":"dotnet.clr-memory.stack-vs-heap.mcq.1","selectedIndex":-1}""")]
    [InlineData("""{"selectedIndex":0}""")]
    [InlineData("""{"questionId":"","selectedIndex":0}""")]
    public async Task Invalid_answer_body_is_a_400_problem(string json)
    {
        var response = await fixture.Client.PostAsync(AnswersPath, new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        await ReviewApi.ShouldBeProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Unknown_session_is_a_404_problem()
    {
        var response = await fixture.Client.PostAsJsonAsync($"{ReviewApi.Sessions}/{Guid.NewGuid()}/answers", new { questionId = _session.Questions[0].Id, selectedIndex = 0 });

        await ReviewApi.ShouldBeProblemAsync(response, HttpStatusCode.NotFound, "Session not found");
    }

    [Fact]
    public async Task Question_deprecated_after_start_is_a_422_problem()
    {
        var questionId = _session.Questions[2].Id;
        await SetQuestionDeprecatedAsync(questionId, deprecated: true);
        try
        {
            var response = await fixture.Client.PostAsJsonAsync(AnswersPath, new { questionId, selectedIndex = 0 });

            await ReviewApi.ShouldBeProblemAsync(response, HttpStatusCode.UnprocessableEntity, "Question deprecated");
        }
        finally
        {
            // The imported content is shared by the whole collection; put it back.
            await SetQuestionDeprecatedAsync(questionId, deprecated: false);
        }
    }

    private Task<int> SetQuestionDeprecatedAsync(string questionId, bool deprecated) =>
        fixture.ExecuteSqlAsync(
            "UPDATE questions SET deprecated_at = CASE WHEN @deprecated THEN now() END WHERE id = @id",
            new NpgsqlParameter("deprecated", deprecated),
            new NpgsqlParameter("id", questionId));

    [Fact]
    public async Task Answer_after_finish_is_a_409_problem()
    {
        await ReviewApi.FinishAsync(fixture.Client, _session.Id);

        var response = await fixture.Client.PostAsJsonAsync(AnswersPath, new { questionId = _session.Questions[0].Id, selectedIndex = 0 });

        await ReviewApi.ShouldBeProblemAsync(response, HttpStatusCode.Conflict, "Session already finished");
    }
}
