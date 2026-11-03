using Spine.Application.Review;
using Spine.Domain.Content;
using Spine.Domain.Progress;
using Spine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Spine.Infrastructure.Review.Sessions;

/// <summary>
/// The transactional part of every start: lock, plan (which throws to refuse), close the open session, insert. A refusal
/// rolls back, so nothing is closed and an open session stays resumable. Shared by the regular and weak starts.
/// </summary>
public sealed class SessionStarter(AppDbContext db, TimeProvider clock, ReviewScopeResolver resolver, SessionProjector projector)
{
    public async Task<SessionDto> StartAsync(
        ReviewScope scope,
        SessionMode mode,
        int totalLimit,
        int newLimit,
        Language language,
        Func<DateTimeOffset, Task<string[]>> plan,
        CancellationToken ct)
    {
        if (!ReviewOptions.IsWithinBounds(totalLimit))
        {
            throw new ArgumentOutOfRangeException(
                nameof(totalLimit), totalLimit, $"Must be {ReviewOptions.MinLimit}..{ReviewOptions.MaxLimit}.");
        }

        await resolver.ResolveAsync(scope, language, ct);

        var strategy = db.Database.CreateExecutionStrategy();
        var sessionId = await strategy.ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var now = clock.GetUtcNow();
            var userId = LocalLearner.Id;

            await db.LockStartAsync(ct);
            await db.LockOpenSessionsAsync(userId, ct);

            var questionIds = await plan(now);

            var open = await db.Sessions.Where(s => s.UserId == userId && s.FinishedAt == null).ToListAsync(ct);
            foreach (var session in open)
            {
                await CloseAsync(session, now, ct);
            }

            var created = new Session
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                StartedAt = now,
                CreatedAt = now,
                Mode = mode,
                ScopeType = scope.Type,
                ScopeDomainSlug = scope.Domain,
                ScopeTopicSlug = scope.Topic,
                ScopeConceptSlug = scope.Concept,
                QuestionIds = questionIds,
                TotalLimit = totalLimit,
                NewLimit = newLimit,
            };
            db.Sessions.Add(created);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return created.Id;
        });

        return await projector.FindAsync(sessionId, language, ct)
            ?? throw new InvalidOperationException($"Session '{sessionId}' was not found right after it was created.");
    }

    private async Task CloseAsync(Session session, DateTimeOffset now, CancellationToken ct)
    {
        var id = session.Id;
        session.FinishedAt = now;
        session.AnsweredCount = await db.Attempts.CountAsync(a => a.SessionId == id, ct);
        session.CorrectCount = await db.Attempts.CountAsync(a => a.SessionId == id && a.IsCorrect == true, ct);
    }
}
