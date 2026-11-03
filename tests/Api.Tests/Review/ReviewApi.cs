using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Spine.Api.Endpoints;
using Spine.Application.Review.AnswerQuestion;
using Spine.Application.Review.FinishSession;
using Spine.Application.Review.Sessions;

namespace Spine.Api.Tests.Review;

/// <summary>Thin helpers over the review endpoints shared by the four endpoint test classes.</summary>
internal static class ReviewApi
{
    public const string Overview = ApiUrl.Prefix + ApiRoutes.Review.Overview;
    public const string Sessions = ApiUrl.Prefix + ApiRoutes.Review.Sessions;
    public const string Current = ApiUrl.Prefix + ApiRoutes.Review.CurrentSession;

    /// <summary>
    /// The topic every session in these tests draws from: <c>tests/Fixtures/api-content/dotnet/clr-memory.json</c>, three
    /// concepts (stack-vs-heap, gc-modes, span-memory) with 4 MCQs + 1 open question each, all unreviewed and English only
    /// (tests that need <c>uk</c> on it add rows in SQL).
    /// </summary>
    public const int ClrMemoryMcqCount = 12;

    /// <summary>
    /// <c>dotnet/async.json</c>: reviewed, 3 MCQs; <c>async-await</c> has a <c>uk</c> concept translation and <c>uk</c> on
    /// <c>mcq.1</c> only, <c>configure-await</c> is English only.
    /// </summary>
    public const int AsyncMcqCount = 3;

    /// <summary>Every eligible MCQ in the fixture; <c>dotnet</c> is the only domain with content.</summary>
    public const int AllMcqCount = ClrMemoryMcqCount + AsyncMcqCount;

    public const string AsyncUkQuestion = "dotnet.async.async-await.mcq.1";
    public const string AsyncEnOnlyQuestion = "dotnet.async.async-await.mcq.2";

    /// <summary>A topic that is in the fixture manifest but has no content file (and so nothing to review).</summary>
    public const string EmptyTopic = "csharp-lang";

    public static object TopicScope(int? totalLimit = null, int? newLimit = null) =>
        new { scope = "topic", domain = "dotnet", topic = "clr-memory", totalLimit, newLimit };

    public static async Task<SessionDto> StartAsync(HttpClient client, object? body = null)
    {
        var response = await client.PostAsJsonAsync(Sessions, body ?? TopicScope(5, 5));
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<SessionDto>(ApiFixture.Json)).ShouldNotBeNull();
    }

    public static async Task<AnswerFeedbackDto> AnswerAsync(HttpClient client, Guid sessionId, string questionId, int selectedIndex)
    {
        var response = await client.PostAsJsonAsync($"{Sessions}/{sessionId}/answers", new { questionId, selectedIndex });
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<AnswerFeedbackDto>(ApiFixture.Json)).ShouldNotBeNull();
    }

    public static async Task<SessionSummaryDto> FinishAsync(HttpClient client, Guid sessionId)
    {
        var response = await client.PostAsync($"{Sessions}/{sessionId}/finish", content: null);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<SessionSummaryDto>(ApiFixture.Json)).ShouldNotBeNull();
    }

    public static async Task<JsonDocument> ShouldBeProblemAsync(HttpResponseMessage response, HttpStatusCode status, string? title = null)
    {
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(status, body);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        var problem = JsonDocument.Parse(body);
        problem.RootElement.GetProperty("status").GetInt32().ShouldBe((int)status);
        if (title is not null)
        {
            problem.RootElement.GetProperty("title").GetString().ShouldBe(title);
            problem.RootElement.GetProperty("instance").GetString().ShouldNotBeNullOrWhiteSpace();
        }

        return problem;
    }
}
