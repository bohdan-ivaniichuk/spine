using System.Net;
using System.Text.Json;
using Spine.Api.Endpoints.Review;
using Spine.Application.Review;

namespace Spine.Api.Tests.Review;

[Collection("api")]
[Trait("Category", "Integration")]
public sealed class ActivityEndpointTests(ApiFixture fixture) : IAsyncLifetime
{
    private const string Activity = "/api/review/activity";

    public async Task InitializeAsync()
    {
        await fixture.ImportAsync();
        await fixture.ResetProgressAsync();
    }

    public Task DisposeAsync() => fixture.ResetProgressAsync();

    /// <summary>The URL for a request built from the real request type.</summary>
    private static string Url(GetActivityRequest request) =>
        string.IsNullOrEmpty(request.Tz) ? Activity : $"{Activity}?tz={Uri.EscapeDataString(request.Tz)}";

    private async Task AnswerOneAsync()
    {
        var session = await ReviewApi.StartAsync(fixture.Client, ReviewApi.TopicScope(1, 1));
        await ReviewApi.AnswerAsync(fixture.Client, session.Id, session.Questions[0].Id, 0);
    }

    [Fact]
    [Trait("Requirement", "FR-001")]
    public async Task Without_a_zone_the_days_are_utc_and_the_payload_is_camelCase()
    {
        await AnswerOneAsync();

        var response = await fixture.Client.GetAsync(Url(new GetActivityRequest()));

        var raw = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.OK, raw);
        using var body = JsonDocument.Parse(raw);
        foreach (var name in new[] { "timeZone", "today", "currentStreak", "activeToday", "longestStreak", "days" })
        {
            body.RootElement.TryGetProperty(name, out _).ShouldBeTrue(name);
        }

        var activity = JsonSerializer.Deserialize<ActivityDto>(raw, ApiFixture.Json).ShouldNotBeNull();
        activity.TimeZone.ShouldBe("UTC");
        activity.Today.ShouldBe(DateOnly.FromDateTime(DateTime.UtcNow), "the test does not run across UTC midnight");
        (activity.CurrentStreak, activity.ActiveToday, activity.LongestStreak).ShouldBe((1, true, 1));
    }

    [Fact]
    [Trait("Requirement", "FR-001")]
    public async Task A_known_zone_is_used_and_echoed()
    {
        await AnswerOneAsync();

        var response = await fixture.Client.GetAsync(Url(new GetActivityRequest { Tz = "Europe/Kyiv" }));

        var raw = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.OK, raw);
        var activity = JsonSerializer.Deserialize<ActivityDto>(raw, ApiFixture.Json).ShouldNotBeNull();
        activity.TimeZone.ShouldBe("Europe/Kyiv");
        (activity.CurrentStreak, activity.ActiveToday).ShouldBe((1, true));
    }

    [Fact]
    [Trait("Requirement", "FR-004")]
    public async Task Days_are_fourteen_entries_ending_today_with_the_answer()
    {
        await AnswerOneAsync();

        var response = await fixture.Client.GetAsync(Url(new GetActivityRequest { Tz = "Europe/Kyiv" }));

        var raw = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.OK, raw);
        var activity = JsonSerializer.Deserialize<ActivityDto>(raw, ApiFixture.Json).ShouldNotBeNull();
        activity.Days.Count.ShouldBe(14);
        activity.Days[^1].ShouldBe(new ActivityDayDto(activity.Today, 1));
        activity.Days[0].Date.ShouldBe(activity.Today.AddDays(-13));
    }

    [Fact]
    [Trait("Requirement", "FR-001")]
    public async Task A_legacy_alias_resolves_and_is_echoed_as_sent()
    {
        // Older browsers report Europe/Kiev; tzdata keeps it as a link to Europe/Kyiv.
        var response = await fixture.Client.GetAsync(Url(new GetActivityRequest { Tz = "Europe/Kiev" }));

        var raw = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.OK, raw);
        JsonSerializer.Deserialize<ActivityDto>(raw, ApiFixture.Json)!.TimeZone.ShouldBe("Europe/Kiev");
    }

    [Theory]
    [Trait("Requirement", "FR-001")]
    [InlineData("FLE Standard Time")]
    [InlineData("posix/Europe/Kyiv")]
    [InlineData("Mars/Olympus_Mons")]
    [InlineData("not a zone")]
    public async Task An_unknown_zone_is_a_400_problem_on_tz(string zone)
    {
        var response = await fixture.Client.GetAsync(Url(new GetActivityRequest { Tz = zone }));

        using var problem = await ReviewApi.ShouldBeProblemAsync(response, HttpStatusCode.BadRequest);
        problem.RootElement.GetProperty("errors").EnumerateArray()
            .ShouldContain(e => e.GetProperty("name").GetString() == "tz");
    }
}
