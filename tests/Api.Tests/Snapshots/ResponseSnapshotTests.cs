using System.Net.Http.Json;
using System.Text.Json;

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
        await GetAsync("tree-en", "/api/knowledge/tree");
        await GetAsync("tree-uk", "/api/knowledge/tree?lang=uk");
        await GetAsync("tree-bad-lang", "/api/knowledge/tree?lang=fr");
        await GetAsync("concept-en", "/api/knowledge/concepts/dotnet/async/async-await");
        await GetAsync("concept-uk", "/api/knowledge/concepts/dotnet/async/async-await?lang=uk");
        await GetAsync("concept-uk-fallback", "/api/knowledge/concepts/dotnet/clr-memory/gc-modes?lang=uk");
        await GetAsync("concept-unknown", "/api/knowledge/concepts/dotnet/clr-memory/nope");
        await GetAsync("concept-bad-lang", "/api/knowledge/concepts/dotnet/clr-memory/gc-modes?lang=fr");
        await GetAsync("overview-all", "/api/review/overview");
        await GetAsync("overview-domain-uk", "/api/review/overview?scope=domain&domain=dotnet&lang=uk");
        await GetAsync("overview-topic", "/api/review/overview?scope=topic&domain=dotnet&topic=clr-memory");
        await GetAsync("overview-concept", "/api/review/overview?scope=concept&domain=dotnet&topic=async&concept=async-await");
        await GetAsync("overview-unknown-topic", "/api/review/overview?scope=topic&domain=dotnet&topic=nope");
        await GetAsync("overview-bad-scope", "/api/review/overview?scope=planet");
        await GetAsync("coverage", "/api/review/coverage");
        await GetAsync("readiness", "/api/review/readiness");
        await GetAsync("weak-spots-empty", "/api/review/weak-spots");
        await GetAsync("activity-empty", "/api/review/activity");
        await GetAsync("current-none", "/api/review/sessions/current");
        await PostAsync("weak-nothing-to-drill", "/api/review/sessions", new { mode = "weak", totalLimit = 5 });

        // A topic session: two answers (one per option), a duplicate, a foreign question, then finish twice.
        var session = await PostAsync("start-topic", "/api/review/sessions",
            new { scope = "topic", domain = "dotnet", topic = "clr-memory", totalLimit = 4, newLimit = 4 });
        var id = session.RootElement.GetProperty("id").GetString();
        var questions = session.RootElement.GetProperty("questions").EnumerateArray()
            .Select(q => q.GetProperty("id").GetString()!).ToArray();
        fixture.Clock.Advance(TimeSpan.FromMinutes(1));
        await PostAsync("answer-option-0", $"/api/review/sessions/{id}/answers", new { questionId = questions[0], selectedIndex = 0 });
        fixture.Clock.Advance(TimeSpan.FromMinutes(1));
        await PostAsync("answer-option-1", $"/api/review/sessions/{id}/answers", new { questionId = questions[1], selectedIndex = 1 });
        await PostAsync("answer-duplicate", $"/api/review/sessions/{id}/answers", new { questionId = questions[1], selectedIndex = 1 });
        await PostAsync("answer-foreign", $"/api/review/sessions/{id}/answers", new { questionId = "dotnet.async.async-await.mcq.1", selectedIndex = 0 });
        await PostAsync("answer-bad-body", $"/api/review/sessions/{id}/answers", new { questionId = "", selectedIndex = -1 });
        await GetAsync("current-open", "/api/review/sessions/current");
        await GetAsync("session-by-id-uk", $"/api/review/sessions/{id}?lang=uk");
        await GetAsync("session-unknown", $"/api/review/sessions/{Guid.Empty}");
        fixture.Clock.Advance(TimeSpan.FromMinutes(2));
        await PostAsync("finish", $"/api/review/sessions/{id}/finish", null);
        await PostAsync("finish-again", $"/api/review/sessions/{id}/finish", null);
        await PostAsync("answer-after-finish", $"/api/review/sessions/{id}/answers", new { questionId = questions[2], selectedIndex = 0 });

        // A whole-domain session with Ukrainian text, closed by a weak drill that replaces it.
        await PostAsync("start-domain-uk", "/api/review/sessions?lang=uk", new { scope = "domain", domain = "dotnet", totalLimit = 3, newLimit = 3 });
        await PostAsync("start-weak", "/api/review/sessions", new { mode = "weak", totalLimit = 5 });

        // The next day: statistics and schedules move.
        fixture.Clock.Advance(TimeSpan.FromDays(1));
        await GetAsync("overview-next-day", "/api/review/overview");
        await GetAsync("overview-weak-count-topic", "/api/review/overview?scope=topic&domain=dotnet&topic=clr-memory");
        await GetAsync("coverage-after", "/api/review/coverage");
        await GetAsync("readiness-after", "/api/review/readiness");
        await GetAsync("weak-spots-after", "/api/review/weak-spots");
        await GetAsync("activity-utc", "/api/review/activity");
        await GetAsync("activity-kyiv", "/api/review/activity?tz=Europe/Kyiv");
        await GetAsync("activity-bad-tz", "/api/review/activity?tz=Mars/Base");

        // Deprecation is visible only on request.
        await fixture.SetConceptDeprecatedAsync("gc-modes", SnapshotFixture.Start);
        await GetAsync("tree-deprecated-hidden", "/api/knowledge/tree");
        await GetAsync("tree-include-deprecated", "/api/knowledge/tree?includeDeprecated=true");
        await GetAsync("concept-deprecated", "/api/knowledge/concepts/dotnet/clr-memory/gc-modes");
        await GetAsync("concept-deprecated-included", "/api/knowledge/concepts/dotnet/clr-memory/gc-modes?includeDeprecated=true");
        await GetAsync("overview-after-deprecation", "/api/review/overview?scope=topic&domain=dotnet&topic=clr-memory");

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
