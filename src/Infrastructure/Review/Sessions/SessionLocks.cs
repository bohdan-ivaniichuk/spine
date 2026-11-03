using Spine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Spine.Infrastructure.Review.Sessions;

/// <summary>The row and advisory locks that serialise starting, answering and finishing sessions.</summary>
public static class SessionLocks
{
    /// <summary>Application-wide advisory lock key for "start a session"; one learner, so one fixed key suffices.</summary>
    private const long StartSessionLockKey = 2002_0001;

    /// <summary>
    /// FR-003: a learner never has two open sessions. Two concurrent starts would each see none and both insert under
    /// READ COMMITTED, so serialise them on a transaction-scoped advisory lock (released at commit/rollback).
    /// </summary>
    public static Task LockStartAsync(this AppDbContext db, CancellationToken ct) =>
        db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({StartSessionLockKey})", ct);

    /// <summary>
    /// Row-locks the open sessions before planning, so an in-flight answer on them commits first and its review-state
    /// change is visible to the planning queries (and Answer/Finish wait for the close). On a refusal the transaction
    /// rolls back and the locks are released with nothing closed.
    /// </summary>
    public static Task LockOpenSessionsAsync(this AppDbContext db, Guid userId, CancellationToken ct) =>
        db.Database.ExecuteSqlAsync($"SELECT id FROM sessions WHERE user_id = {userId} AND finished_at IS NULL FOR UPDATE", ct);

    /// <summary>
    /// <c>SELECT ... FOR UPDATE</c> on one session row, held until the surrounding transaction ends. Answer, Finish and
    /// the auto-close in Start all take it, so an answer can never land in a session that is being finished.
    /// </summary>
    public static Task LockSessionAsync(this AppDbContext db, Guid sessionId, CancellationToken ct) =>
        db.Database.ExecuteSqlAsync($"SELECT id FROM sessions WHERE id = {sessionId} FOR UPDATE", ct);
}
