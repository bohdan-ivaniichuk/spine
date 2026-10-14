using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Spine.Application.Review;

namespace Spine.Api.Tests.Review;

/// <summary>Thin helpers over the review endpoints shared by the four endpoint test classes.</summary>
internal static class ReviewApi
{
    public const string Overview = "/api/review/overview";
    public const string Sessions = "/api/review/sessions";
    public const string Current = "/api/review/sessions/current";

    /// <summary>
    /// The topic every session in these tests draws from: 28 MCQs in <c>content/dotnet/clr-memory.json</c>, all unreviewed.
    /// Only topic-scoped assertions may use it; "all"/"domain" counts grow with every content batch, so they are asserted
    /// against <c>totalEligible</c> of the same response instead.
    /// </summary>
    public const int ClrMemoryMcqCount = 28;

    /// <summary>A topic that is in the manifest but has no content file (and so nothing to review).</summary>
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
