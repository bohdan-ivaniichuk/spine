using InterviewPrep.Domain.Progress;
using Microsoft.EntityFrameworkCore;

namespace InterviewPrep.Infrastructure.Persistence;

/// <summary>Seeds the single local learner idempotently (INSERT … ON CONFLICT DO NOTHING).</summary>
public sealed class LearnerSeeder(AppDbContext db)
{
    public Task SeedAsync(CancellationToken ct = default) =>
        db.Database.ExecuteSqlAsync(
            $"""
             INSERT INTO users (id, display_name, preferred_language, created_at)
             VALUES ({LocalLearner.Id}, {LocalLearner.DisplayName}, 'en', {DateTimeOffset.UtcNow})
             ON CONFLICT (id) DO NOTHING
             """, ct);
}
