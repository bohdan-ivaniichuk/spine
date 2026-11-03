using Spine.Application.Knowledge;
using Spine.Application.Review.Sessions;
using Spine.Application.Review.Shared;
using Spine.Application.Review;
using Spine.Domain.Content;
using Spine.Domain.Progress;
using Spine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Spine.Infrastructure.Review.Sessions;

/// <summary>
/// Builds the session DTO a client sees: questions in session order with text in the requested language (per-question
/// English fallback), recorded answers, and ids skipped because they were deprecated after the session started.
/// </summary>
public sealed class SessionProjector(AppDbContext db, SessionScopeDescriber describer)
{
    /// <summary>The learner's session by id, projected; null when it does not exist.</summary>
    public async Task<SessionDto?> FindAsync(Guid sessionId, Language language, CancellationToken ct)
    {
        var userId = LocalLearner.Id;
        var session = await db.Sessions.AsNoTracking().SingleOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, ct);
        return session is null ? null : await ProjectAsync(session, language, ct);
    }

    public async Task<SessionDto> ProjectAsync(Session session, Language language, CancellationToken ct)
    {
        var userId = LocalLearner.Id;
        var ids = session.QuestionIds;
        var startedAt = session.StartedAt;

        var rows = await db.Questions.AsNoTracking()
            .Where(q => ids.Contains(q.Id))
            .Select(q => new
            {
                q.Id,
                q.Level,
                q.Reviewed,
                Deprecated = q.DeprecatedAt != null
                    || q.Concept!.DeprecatedAt != null
                    || q.Concept.Topic!.DeprecatedAt != null
                    || q.Concept.Topic.Domain!.DeprecatedAt != null,
                En = q.Translations.Where(t => t.Language == Language.En).Select(t => new SessionQuestionText(t.Prompt, t.Options)).FirstOrDefault(),
                Requested = language == Language.En
                    ? null
                    : q.Translations.Where(t => t.Language == language).Select(t => new SessionQuestionText(t.Prompt, t.Options)).FirstOrDefault(),
                ConceptSlug = q.Concept!.Slug,
                ConceptTitle = q.Concept.Translations.Where(t => t.Language == language).Select(t => t.Title).FirstOrDefault()
                    ?? q.Concept.Translations.Where(t => t.Language == Language.En).Select(t => t.Title).FirstOrDefault(),
                TopicSlug = q.Concept.Topic!.Slug,
                TopicTitle = q.Concept.Topic.Title,
                TopicTitleUk = q.Concept.Topic.TitleUk,
                DomainSlug = q.Concept.Topic.Domain!.Slug,
                DomainTitle = q.Concept.Topic.Domain.Title,
                DomainTitleUk = q.Concept.Topic.Domain.TitleUk,
                // "New" = no review state existed when the session started.
                HadState = db.ReviewStates.Any(r => r.UserId == userId && r.QuestionId == q.Id && r.CreatedAt < startedAt),
            })
            .ToListAsync(ct);

        var attempts = await db.Attempts.AsNoTracking()
            .Where(a => a.SessionId == session.Id)
            .OrderBy(a => a.AnsweredAt).ThenBy(a => a.Id)
            .Select(a => new { a.QuestionId, a.Answer, a.IsCorrect })
            .ToListAsync(ct);
        var answers = attempts
            .Select(a => new SessionAnswerDto(a.QuestionId!, McqAnswerJson.Parse(a.Answer).SelectedIndex, a.IsCorrect == true))
            .ToList();
        var answeredIds = answers.Select(a => a.QuestionId).ToHashSet(StringComparer.Ordinal);

        var byId = rows.ToDictionary(r => r.Id, StringComparer.Ordinal);
        var questions = new List<SessionQuestionDto>(ids.Length);
        var skipped = new List<string>();
        foreach (var id in ids)
        {
            // FR-011: deprecated after start → skipped, unless it was already answered (then the counts must line up).
            if (!byId.TryGetValue(id, out var row) || row.En is null || (row.Deprecated && !answeredIds.Contains(id)))
            {
                skipped.Add(id);
                continue;
            }

            var served = TranslationPicker.Pick(language, row.En, row.En.Options, row.Requested, row.Requested?.Options);
            questions.Add(new SessionQuestionDto(
                row.Id,
                row.Level.ToDbValue(),
                row.Reviewed,
                !row.HadState,
                served.Text.Prompt,
                served.Text.Options?.Select(o => o.Text).ToList() ?? [],
                new RefDto(row.DomainSlug, LocalizedTitle.Of(language, row.DomainTitle, row.DomainTitleUk)),
                new RefDto(row.TopicSlug, LocalizedTitle.Of(language, row.TopicTitle, row.TopicTitleUk)),
                new RefDto(row.ConceptSlug, row.ConceptTitle ?? row.ConceptSlug),
                served.Language.ToDbValue(),
                served.Fallback));
        }

        return new SessionDto(
            session.Id,
            await describer.DescribeAsync(session, language, ct),
            session.StartedAt,
            session.FinishedAt,
            session.TotalLimit,
            session.NewLimit,
            questions,
            answers,
            skipped,
            session.Mode.ToDbValue());
    }
}
