using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Spine.Application.Review;
using Npgsql;

namespace Spine.Api.Tests.Review;

/// <summary>
/// <c>?lang=</c> on the review endpoints. The repo content has no <c>uk</c> yet, so each test copies the English row of
/// one question (options included, so they stay index-aligned) and its concept into a <c>uk</c> row, and removes it
/// again afterwards.
/// </summary>
[Collection("api")]
[Trait("Category", "Integration")]
public sealed class ReviewI18nEndpointTests(ApiFixture fixture) : IAsyncLifetime
{
    private const string UkPrompt = "Українське формулювання";
    private const string UkExplanation = "Українське пояснення";
    private const string UkConceptTitle = "Українська назва концепту";

    private readonly List<string> _ukQuestions = [];
    private readonly List<string> _ukConcepts = [];

    public async Task InitializeAsync()
    {
        await fixture.ImportAsync();
        await fixture.ResetProgressAsync();
    }

    public async Task DisposeAsync()
    {
        foreach (var id in _ukQuestions)
        {
            await fixture.ExecuteSqlAsync("DELETE FROM question_translations WHERE question_id = @id AND language = 'uk'", new NpgsqlParameter("id", id));
        }

        foreach (var slug in _ukConcepts)
        {
            await fixture.ExecuteSqlAsync(
                "DELETE FROM concept_translations WHERE language = 'uk' AND concept_id = (SELECT id FROM concepts WHERE slug = @slug)",
                new NpgsqlParameter("slug", slug));
        }

        await fixture.ResetProgressAsync();
    }

    [Fact]
    public async Task Session_in_uk_serves_translated_questions_and_falls_back_per_question()
    {
        var (started, translated, untranslated) = await StartAcrossConceptsAsync();
        await AddUkAsync(translated.Id, translated.Concept.Slug);

        var session = await GetAsync<SessionDto>($"{ReviewApi.Sessions}/{started.Id}?lang=uk");

        session.Questions.Select(q => q.Id).ShouldBe(started.Questions.Select(q => q.Id), "membership is language-independent");
        var uk = session.Questions.Single(q => q.Id == translated.Id);
        uk.Prompt.ShouldBe(UkPrompt);
        uk.Options.ShouldBe(translated.Options, "the uk row copies the en options, so they are index-aligned");
        uk.Language.ShouldBe("uk");
        uk.Fallback.ShouldBeFalse();
        uk.Concept.Title.ShouldBe(UkConceptTitle);

        var en = session.Questions.Single(q => q.Id == untranslated.Id);
        en.Prompt.ShouldBe(untranslated.Prompt);
        en.Language.ShouldBe("en");
        en.Fallback.ShouldBeTrue();
        en.Concept.Title.ShouldBe(untranslated.Concept.Title);

        started.Questions.ShouldAllBe(q => q.Language == "en" && !q.Fallback, "lang defaults to en");
    }

    [Fact]
    public async Task Lang_is_a_query_parameter_on_start_and_current_and_serializes_camelCase()
    {
        var probe = await ReviewApi.StartAsync(fixture.Client);
        await AddUkAsync(probe.Questions[0].Id, probe.Questions[0].Concept.Slug);
        await fixture.ResetProgressAsync();

        var response = await fixture.Client.PostAsJsonAsync(ReviewApi.Sessions + "?lang=uk", ReviewApi.TopicScope(5, 5));
        var raw = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.Created, raw);
        using (var body = JsonDocument.Parse(raw))
        {
            var first = body.RootElement.GetProperty("questions")[0];
            first.GetProperty("prompt").GetString().ShouldBe(UkPrompt);
            first.GetProperty("language").GetString().ShouldBe("uk");
            first.GetProperty("fallback").GetBoolean().ShouldBeFalse();
        }

        var current = await GetAsync<SessionDto>(ReviewApi.Current + "?lang=uk");
        current.Questions[0].Prompt.ShouldBe(UkPrompt);
        (await GetAsync<SessionDto>(ReviewApi.Current)).Questions[0].Language.ShouldBe("en");
    }

    [Fact]
    public async Task Answer_and_finish_follow_lang()
    {
        var (session, translated, untranslated) = await StartAcrossConceptsAsync();
        await AddUkAsync(translated.Id, translated.Concept.Slug);

        var ukFeedback = await AnswerAsync(session.Id, translated.Id, 0, "uk");
        ukFeedback.Explanation.ShouldBe(UkExplanation);
        ukFeedback.Language.ShouldBe("uk");
        ukFeedback.Fallback.ShouldBeFalse();

        var enFallback = await AnswerAsync(session.Id, untranslated.Id, 0, "uk");
        enFallback.Language.ShouldBe("en");
        enFallback.Fallback.ShouldBeTrue();
        enFallback.Explanation.ShouldNotBe(UkExplanation);

        // Weak-concept titles per language are asserted in ReviewSessionsLanguageTests (fixed correct index there).
        var summaryResponse = await fixture.Client.PostAsync($"{ReviewApi.Sessions}/{session.Id}/finish?lang=uk", content: null);
        summaryResponse.StatusCode.ShouldBe(HttpStatusCode.OK, await summaryResponse.Content.ReadAsStringAsync());
        var summary = (await summaryResponse.Content.ReadFromJsonAsync<SessionSummaryDto>(ApiFixture.Json)).ShouldNotBeNull();
        summary.Answered.ShouldBe(2);
        summary.WeakConcepts.Where(w => w.Concept == translated.Concept.Slug).ShouldAllBe(w => w.Title == UkConceptTitle);
        summary.WeakConcepts.Where(w => w.Concept == untranslated.Concept.Slug).ShouldAllBe(w => w.Title == untranslated.Concept.Title);
    }

    [Fact]
    public async Task Answer_with_lang_en_reports_en_without_fallback()
    {
        var session = await ReviewApi.StartAsync(fixture.Client);

        var feedback = await ReviewApi.AnswerAsync(fixture.Client, session.Id, session.Questions[0].Id, 0);

        feedback.Language.ShouldBe("en");
        feedback.Fallback.ShouldBeFalse();
    }

    [Theory]
    [InlineData("GET", "/api/review/overview?lang=fr")]
    [InlineData("GET", "/api/review/coverage?lang=fr")]
    [InlineData("GET", "/api/review/sessions/current?lang=fr")]
    [InlineData("GET", "/api/review/sessions/{id}?lang=fr")]
    [InlineData("POST", "/api/review/sessions?lang=fr")]
    [InlineData("POST", "/api/review/sessions/{id}/answers?lang=fr")]
    [InlineData("POST", "/api/review/sessions/{id}/finish?lang=fr")]
    [InlineData("GET", "/api/review/sessions/{id}?lang=")]
    public async Task Unsupported_lang_is_400(string method, string url)
    {
        var session = await ReviewApi.StartAsync(fixture.Client);
        url = url.Replace("{id}", session.Id.ToString(), StringComparison.Ordinal);

        var response = method == "GET"
            ? await fixture.Client.GetAsync(url)
            : url.EndsWith("/finish?lang=fr", StringComparison.Ordinal)
                ? await fixture.Client.PostAsync(url, content: null)
                : await fixture.Client.PostAsJsonAsync(url, url.Contains("/answers", StringComparison.Ordinal)
                    ? new { questionId = session.Questions[0].Id, selectedIndex = 0 }
                    : ReviewApi.TopicScope(5, 5));

        var problem = await ReviewApi.ShouldBeProblemAsync(response, HttpStatusCode.BadRequest);
        problem.RootElement.GetRawText().ShouldContain("lang must be 'en' or 'uk'");
    }

    // ---- helpers ----

    /// <summary>
    /// Adds <c>uk</c> rows for one question and its concept. <c>ON CONFLICT DO NOTHING</c> leaves any existing <c>uk</c>
    /// row alone (the content is shared by the whole collection), and only rows this call actually created are
    /// recorded for removal in <see cref="DisposeAsync"/>.
    /// </summary>
    private async Task AddUkAsync(string questionId, string conceptSlug)
    {
        var questionRows = await fixture.ExecuteSqlAsync(
            """
            INSERT INTO question_translations (question_id, language, prompt, explanation, options, rubric, content_hash, created_at, updated_at)
            SELECT question_id, 'uk', @prompt, @explanation, options, rubric, content_hash, now(), now()
            FROM question_translations WHERE question_id = @id AND language = 'en'
            ON CONFLICT DO NOTHING
            """,
            new NpgsqlParameter("prompt", UkPrompt),
            new NpgsqlParameter("explanation", UkExplanation),
            new NpgsqlParameter("id", questionId));
        if (questionRows == 1)
        {
            _ukQuestions.Add(questionId);
        }

        var conceptRows = await fixture.ExecuteSqlAsync(
            """
            INSERT INTO concept_translations (concept_id, language, title, note, content_hash, created_at, updated_at)
            SELECT concept_id, 'uk', @title, note, content_hash, now(), now()
            FROM concept_translations WHERE language = 'en' AND concept_id = (SELECT id FROM concepts WHERE slug = @slug)
            ON CONFLICT DO NOTHING
            """,
            new NpgsqlParameter("title", UkConceptTitle),
            new NpgsqlParameter("slug", conceptSlug));
        if (conceptRows == 1)
        {
            _ukConcepts.Add(conceptSlug);
        }
    }

    /// <summary>
    /// Starts a session over the whole clr-memory topic, so it is guaranteed to span more than one concept, and returns
    /// one question to translate plus one from a different concept that stays untranslated.
    /// </summary>
    private async Task<(SessionDto Session, SessionQuestionDto Translated, SessionQuestionDto Untranslated)> StartAcrossConceptsAsync()
    {
        var session = await ReviewApi.StartAsync(
            fixture.Client, ReviewApi.TopicScope(ReviewApi.ClrMemoryMcqCount, ReviewApi.ClrMemoryMcqCount));
        var translated = session.Questions[0];
        var untranslated = session.Questions.FirstOrDefault(q => q.Concept.Slug != translated.Concept.Slug);
        untranslated.ShouldNotBeNull("the clr-memory topic must have questions in at least two concepts");
        return (session, translated, untranslated);
    }

    private async Task<T> GetAsync<T>(string url) where T : class
    {
        var response = await fixture.Client.GetAsync(url);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<T>(ApiFixture.Json)).ShouldNotBeNull();
    }

    private async Task<AnswerFeedbackDto> AnswerAsync(Guid sessionId, string questionId, int selectedIndex, string lang)
    {
        var response = await fixture.Client.PostAsJsonAsync($"{ReviewApi.Sessions}/{sessionId}/answers?lang={lang}", new { questionId, selectedIndex });
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<AnswerFeedbackDto>(ApiFixture.Json)).ShouldNotBeNull();
    }
}
