using Spine.Application.Review.FinishSession;
using Spine.Application.Review.Shared;
using Spine.Application.Review;
using Spine.Domain.Content;
using Spine.Domain.Progress;
using Spine.Infrastructure.Persistence;
using Spine.Infrastructure.Review.Sessions;
using Microsoft.EntityFrameworkCore;

namespace Spine.Infrastructure.Review.FinishSession;

/// <summary>
/// EF implementation of <see cref="IFinishSessionUseCase"/>: stores finish time and counts once (idempotent), then
/// summarises the answers and counts what is due within a day in the same scope.
/// </summary>
public sealed class FinishSessionUseCase(AppDbContext db, TimeProvider clock, SessionScopeDescriber describer)
    : IFinishSessionUseCase
{
    public async Task<SessionSummaryDto> ExecuteAsync(Guid sessionId, Language language, CancellationToken ct)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var now = clock.GetUtcNow();
            var userId = LocalLearner.Id;

            // Serialises with Answer and with a concurrent Finish: the second Finish sees FinishedAt set and rewrites nothing.
            await db.LockSessionAsync(sessionId, ct);
            var session = await db.Sessions.SingleOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, ct)
                ?? throw new ReviewException(ReviewError.SessionNotFound, $"No session '{sessionId}'.");

            var rows = await (
                from a in db.Attempts.AsNoTracking()
                join q in db.Questions.AsNoTracking() on a.QuestionId equals q.Id
                where a.SessionId == sessionId
                orderby a.AnsweredAt, a.Id
                select new
                {
                    QuestionId = q.Id,
                    a.IsCorrect,
                    DomainSlug = q.Concept!.Topic!.Domain!.Slug,
                    TopicSlug = q.Concept.Topic.Slug,
                    ConceptSlug = q.Concept.Slug,
                    ConceptTitle = q.Concept.Translations.Where(t => t.Language == language).Select(t => t.Title).FirstOrDefault()
                        ?? q.Concept.Translations.Where(t => t.Language == Language.En).Select(t => t.Title).FirstOrDefault(),
                }).ToListAsync(ct);
            var answered = rows
                .Select(r => new AnsweredItem(r.QuestionId, r.IsCorrect == true, new ConceptRef(r.DomainSlug, r.TopicSlug, r.ConceptSlug, r.ConceptTitle ?? r.ConceptSlug)))
                .ToList();

            if (session.FinishedAt is null)
            {
                session.FinishedAt = now;
                session.AnsweredCount = answered.Count;
                session.CorrectCount = answered.Count(a => a.IsCorrect);
                await db.SaveChangesAsync(ct);
            }

            var scope = SessionScopeDescriber.ScopeOf(session);
            var tomorrow = now.AddDays(1);
            var dueTomorrow = await db.DueAtOf(scope).Where(dueAt => dueAt <= tomorrow).CountAsync(ct);
            await tx.CommitAsync(ct);

            return SessionSummaryBuilder.Build(session, await describer.DescribeAsync(session, language, ct), answered, dueTomorrow);
        });
    }
}
