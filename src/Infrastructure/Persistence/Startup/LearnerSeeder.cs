using Spine.Domain.Progress;
using Spine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Spine.Infrastructure.Persistence.Startup;

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
