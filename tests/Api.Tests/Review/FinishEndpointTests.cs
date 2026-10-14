using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Spine.Application.Review;

namespace Spine.Api.Tests.Review;

[Collection("api")]
[Trait("Category", "Integration")]
public sealed class FinishEndpointTests(ApiFixture fixture) : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await fixture.ImportAsync();
        await fixture.ResetProgressAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Finish_returns_the_summary_in_camelCase_and_is_idempotent()
    {
        var session = await ReviewApi.StartAsync(fixture.Client, ReviewApi.TopicScope(5, 5));
        var feedback = new List<AnswerFeedbackDto>();
        foreach (var question in session.Questions.Take(3))
        {
            // Always pick option 0; whether that is right depends on the content, so expectations derive from the feedback.
            feedback.Add(await ReviewApi.AnswerAsync(fixture.Client, session.Id, question.Id, 0));
        }

        var expectedCorrect = feedback.Count(f => f.IsCorrect);
        var wrongConcepts = session.Questions.Take(3).Zip(feedback)
            .Where(p => !p.Second.IsCorrect)
            .GroupBy(p => p.First.Concept.Slug)
            .ToDictionary(g => g.Key, g => g.Count());

        var response = await fixture.Client.PostAsync($"{ReviewApi.Sessions}/{session.Id}/finish", content: null);

        var raw = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.OK, raw);
        using var body = JsonDocument.Parse(raw);
        var root = body.RootElement;
        root.GetProperty("sessionId").GetGuid().ShouldBe(session.Id);
        root.GetProperty("scope").GetProperty("topicTitle").GetString().ShouldBe("Memory & GC");
        root.GetProperty("startedAt").ValueKind.ShouldBe(JsonValueKind.String);
        root.GetProperty("finishedAt").ValueKind.ShouldBe(JsonValueKind.String);
        root.GetProperty("questionCount").GetInt32().ShouldBe(5);
        root.GetProperty("answered").GetInt32().ShouldBe(3);
        root.GetProperty("correct").GetInt32().ShouldBe(expectedCorrect);
        root.GetProperty("accuracy").GetDouble().ShouldBe(expectedCorrect / 3.0, tolerance: 1e-9);
        root.GetProperty("dueTomorrow").GetInt32().ShouldBe(3, "every first answer is due in one day");

        var weak = root.GetProperty("weakConcepts").EnumerateArray().ToList();
        weak.Count.ShouldBe(wrongConcepts.Count);
        var previousWrong = int.MaxValue;
        foreach (var concept in weak)
        {
            concept.GetProperty("domain").GetString().ShouldBe("dotnet");
            concept.GetProperty("topic").GetString().ShouldBe("clr-memory");
            concept.GetProperty("title").GetString().ShouldNotBeNullOrWhiteSpace();
            var wrong = concept.GetProperty("wrong").GetInt32();
            wrong.ShouldBe(wrongConcepts[concept.GetProperty("concept").GetString()!]);
            wrong.ShouldBeLessThanOrEqualTo(previousWrong, "ranked by misses");
            wrong.ShouldBeLessThanOrEqualTo(concept.GetProperty("total").GetInt32());
            previousWrong = wrong;
        }

        var again = await ReviewApi.FinishAsync(fixture.Client, session.Id);
        again.Answered.ShouldBe(3);
        again.Correct.ShouldBe(expectedCorrect);
        again.FinishedAt.ShouldBe(root.GetProperty("finishedAt").GetDateTimeOffset());

        (await fixture.Client.GetAsync(ReviewApi.Current)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var stored = await fixture.Client.GetFromJsonAsync<SessionDto>($"{ReviewApi.Sessions}/{session.Id}", ApiFixture.Json);
        stored!.FinishedAt.ShouldBe(again.FinishedAt);
        stored.Answers.Count.ShouldBe(3);
    }

    [Fact]
    public async Task Finish_without_answers_has_null_accuracy_and_no_weak_concepts()
    {
        var session = await ReviewApi.StartAsync(fixture.Client);

        var summary = await ReviewApi.FinishAsync(fixture.Client, session.Id);

        summary.Answered.ShouldBe(0);
        summary.Correct.ShouldBe(0);
        summary.Accuracy.ShouldBeNull();
        summary.WeakConcepts.ShouldBeEmpty();
        summary.DueTomorrow.ShouldBe(0);
        summary.QuestionCount.ShouldBe(5);
    }

    [Fact]
    public async Task Unknown_session_is_a_404_problem()
    {
        var response = await fixture.Client.PostAsync($"{ReviewApi.Sessions}/{Guid.NewGuid()}/finish", content: null);

        await ReviewApi.ShouldBeProblemAsync(response, HttpStatusCode.NotFound, "Session not found");
    }

    [Fact]
    public async Task Review_again_after_finishing_excludes_what_was_just_answered()
    {
        var session = await ReviewApi.StartAsync(fixture.Client, ReviewApi.TopicScope(3, 3));
        foreach (var question in session.Questions)
        {
            await ReviewApi.AnswerAsync(fixture.Client, session.Id, question.Id, 0);
        }

        await ReviewApi.FinishAsync(fixture.Client, session.Id);
        var next = await ReviewApi.StartAsync(fixture.Client, ReviewApi.TopicScope(3, 3));

        next.Questions.Select(q => q.Id).ShouldNotContain(id => session.Questions.Any(q => q.Id == id));
        next.Questions.Count.ShouldBe(3);
    }
}
