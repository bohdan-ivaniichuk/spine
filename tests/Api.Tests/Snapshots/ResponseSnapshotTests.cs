using System.Net.Http.Json;
using System.Text.Json;
using Spine.Api.Endpoints;

namespace Spine.Api.Tests.Snapshots;

/// <summary>
/// Characterisation of every endpoint before and after the 008 refactor (FR-007): one scripted run through the API with a
/// pinned clock, each response compared with its recorded case (status, Location, body byte for byte) and its SQL
/// commands never above the recorded count. Not part of a red/green cycle: it is green on the pre-refactor code by
/// construction and must stay green.
/// </summary>
[Collection("snapshots")]
[Trait("Category", "Integration")]
public sealed class ResponseSnapshotTests(SnapshotFixture fixture) : IAsyncLifetime
{
    private readonly SnapshotStore _store = new();
    private int _step;

    public Task InitializeAsync() => fixture.ImportAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Every_endpoint_answers_exactly_as_recorded()
    {
        // Fresh learner: nothing answered yet.
        await GetAsync("tree-en", ApiUrl.Prefix + ApiRoutes.Knowledge.Tree);
        await GetAsync("tree-uk", ApiUrl.Prefix + ApiRoutes.Knowledge.Tree + "?lang=uk");
        await GetAsync("tree-bad-lang", ApiUrl.Prefix + ApiRoutes.Knowledge.Tree + "?lang=fr");
        await GetAsync("concept-en", ApiUrl.For(ApiRoutes.Knowledge.Concept, ("domain", "dotnet"), ("topic", "async"), ("concept", "async-await")));
        await GetAsync("concept-uk", ApiUrl.For(ApiRoutes.Knowledge.Concept, ("domain", "dotnet"), ("topic", "async"), ("concept", "async-await")) + "?lang=uk");
        await GetAsync("concept-uk-fallback", ApiUrl.For(ApiRoutes.Knowledge.Concept, ("domain", "dotnet"), ("topic", "clr-memory"), ("concept", "gc-modes")) + "?lang=uk");
        await GetAsync("concept-unknown", ApiUrl.For(ApiRoutes.Knowledge.Concept, ("domain", "dotnet"), ("topic", "clr-memory"), ("concept", "nope")));
        await GetAsync("concept-bad-lang", ApiUrl.For(ApiRoutes.Knowledge.Concept, ("domain", "dotnet"), ("topic", "clr-memory"), ("concept", "gc-modes")) + "?lang=fr");
        await GetAsync("overview-all", ApiUrl.Prefix + ApiRoutes.Review.Overview);
        await GetAsync("overview-domain-uk", ApiUrl.Prefix + ApiRoutes.Review.Overview + "?scope=domain&domain=dotnet&lang=uk");
        await GetAsync("overview-topic", ApiUrl.Prefix + ApiRoutes.Review.Overview + "?scope=topic&domain=dotnet&topic=clr-memory");
        await GetAsync("overview-concept", ApiUrl.Prefix + ApiRoutes.Review.Overview + "?scope=concept&domain=dotnet&topic=async&concept=async-await");
        await GetAsync("overview-unknown-topic", ApiUrl.Prefix + ApiRoutes.Review.Overview + "?scope=topic&domain=dotnet&topic=nope");
        await GetAsync("overview-bad-scope", ApiUrl.Prefix + ApiRoutes.Review.Overview + "?scope=planet");
        await GetAsync("coverage", ApiUrl.Prefix + ApiRoutes.Review.Coverage);
        await GetAsync("readiness", ApiUrl.Prefix + ApiRoutes.Review.Readiness);
        await GetAsync("weak-spots-empty", ApiUrl.Prefix + ApiRoutes.Review.WeakSpots);
        await GetAsync("activity-empty", ApiUrl.Prefix + ApiRoutes.Review.Activity);
        await GetAsync("current-none", ApiUrl.Prefix + ApiRoutes.Review.CurrentSession);
        await PostAsync("weak-nothing-to-drill", ApiUrl.Prefix + ApiRoutes.Review.Sessions, new { mode = "weak", totalLimit = 5 });

        // A topic session: two answers (one per option), a duplicate, a foreign question, then finish twice.
        var session = await PostAsync("start-topic", ApiUrl.Prefix + ApiRoutes.Review.Sessions,
            new { scope = "topic", domain = "dotnet", topic = "clr-memory", totalLimit = 4, newLimit = 4 });
        var id = session.RootElement.GetProperty("id").GetString();
        var questions = session.RootElement.GetProperty("questions").EnumerateArray()
            .Select(q => q.GetProperty("id").GetString()!).ToArray();
        fixture.Clock.Advance(TimeSpan.FromMinutes(1));
        await PostAsync("answer-option-0", ApiUrl.For(ApiRoutes.Review.Answers, ("id", id!)), new { questionId = questions[0], selectedIndex = 0 });
        fixture.Clock.Advance(TimeSpan.FromMinutes(1));
        await PostAsync("answer-option-1", ApiUrl.For(ApiRoutes.Review.Answers, ("id", id!)), new { questionId = questions[1], selectedIndex = 1 });
        await PostAsync("answer-duplicate", ApiUrl.For(ApiRoutes.Review.Answers, ("id", id!)), new { questionId = questions[1], selectedIndex = 1 });
        await PostAsync("answer-foreign", ApiUrl.For(ApiRoutes.Review.Answers, ("id", id!)), new { questionId = "dotnet.async.async-await.mcq.1", selectedIndex = 0 });
        await PostAsync("answer-bad-body", ApiUrl.For(ApiRoutes.Review.Answers, ("id", id!)), new { questionId = "", selectedIndex = -1 });
        await GetAsync("current-open", ApiUrl.Prefix + ApiRoutes.Review.CurrentSession);
        await GetAsync("session-by-id-uk", ApiUrl.For(ApiRoutes.Review.Session, ("id", id!)) + "?lang=uk");
        await GetAsync("session-unknown", ApiUrl.For(ApiRoutes.Review.Session, ("id", Guid.Empty)));
        fixture.Clock.Advance(TimeSpan.FromMinutes(2));
        await PostAsync("finish", ApiUrl.For(ApiRoutes.Review.Finish, ("id", id!)), null);
        await PostAsync("finish-again", ApiUrl.For(ApiRoutes.Review.Finish, ("id", id!)), null);
        await PostAsync("answer-after-finish", ApiUrl.For(ApiRoutes.Review.Answers, ("id", id!)), new { questionId = questions[2], selectedIndex = 0 });

        // A whole-domain session with Ukrainian text, closed by a weak drill that replaces it.
        await PostAsync("start-domain-uk", ApiUrl.Prefix + ApiRoutes.Review.Sessions + "?lang=uk", new { scope = "domain", domain = "dotnet", totalLimit = 3, newLimit = 3 });
        await PostAsync("start-weak", ApiUrl.Prefix + ApiRoutes.Review.Sessions, new { mode = "weak", totalLimit = 5 });

        // The next day: statistics and schedules move.
        fixture.Clock.Advance(TimeSpan.FromDays(1));
        await GetAsync("overview-next-day", ApiUrl.Prefix + ApiRoutes.Review.Overview);
        await GetAsync("overview-weak-count-topic", ApiUrl.Prefix + ApiRoutes.Review.Overview + "?scope=topic&domain=dotnet&topic=clr-memory");
        await GetAsync("coverage-after", ApiUrl.Prefix + ApiRoutes.Review.Coverage);
        await GetAsync("readiness-after", ApiUrl.Prefix + ApiRoutes.Review.Readiness);
        await GetAsync("weak-spots-after", ApiUrl.Prefix + ApiRoutes.Review.WeakSpots);
        await GetAsync("activity-utc", ApiUrl.Prefix + ApiRoutes.Review.Activity);
        await GetAsync("activity-kyiv", ApiUrl.Prefix + ApiRoutes.Review.Activity + "?tz=Europe/Kyiv");
        await GetAsync("activity-bad-tz", ApiUrl.Prefix + ApiRoutes.Review.Activity + "?tz=Mars/Base");

        // Deprecation is visible only on request.
        await fixture.SetConceptDeprecatedAsync("gc-modes", SnapshotFixture.Start);
        await GetAsync("tree-deprecated-hidden", ApiUrl.Prefix + ApiRoutes.Knowledge.Tree);
        await GetAsync("tree-include-deprecated", ApiUrl.Prefix + ApiRoutes.Knowledge.Tree + "?includeDeprecated=true");
        await GetAsync("concept-deprecated", ApiUrl.For(ApiRoutes.Knowledge.Concept, ("domain", "dotnet"), ("topic", "clr-memory"), ("concept", "gc-modes")));
        await GetAsync("concept-deprecated-included", ApiUrl.For(ApiRoutes.Knowledge.Concept, ("domain", "dotnet"), ("topic", "clr-memory"), ("concept", "gc-modes")) + "?includeDeprecated=true");
        await GetAsync("overview-after-deprecation", ApiUrl.Prefix + ApiRoutes.Review.Overview + "?scope=topic&domain=dotnet&topic=clr-memory");

        _store.Failures.ShouldBeEmpty(string.Join("\n", _store.Failures));
    }

    private async Task GetAsync(string name, string url)
    {
        fixture.Commands.Reset();
        using var response = await fixture.Client.GetAsync(url);
        await RecordAsync(name, response);
    }

    private async Task<JsonDocument> PostAsync(string name, string url, object? body)
    {
        fixture.Commands.Reset();
        using var response = body is null
            ? await fixture.Client.PostAsync(url, content: null)
            : await fixture.Client.PostAsJsonAsync(url, body);
        var raw = await RecordAsync(name, response);
        return JsonDocument.Parse(string.IsNullOrEmpty(raw) ? "{}" : raw);
    }

    private async Task<string> RecordAsync(string name, HttpResponseMessage response)
    {
        var sql = fixture.Commands.Count;
        var raw = await response.Content.ReadAsStringAsync();
        _step++;
        _store.Check($"{_step:D2}-{name}", (int)response.StatusCode, response.Headers.Location?.ToString(), sql, raw);
        return raw;
    }
}
