using Spine.Domain.Progress;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Spine.Infrastructure.Persistence.Configurations.Progress;

internal sealed class AttemptConfiguration : IEntityTypeConfiguration<Attempt>
{
    public void Configure(EntityTypeBuilder<Attempt> e)
    {
        e.ToTable("attempts", t =>
        {
            t.HasCheckConstraint("ck_attempts_item", ItemColumns.ItemCheck);
            t.HasCheckConstraint("ck_attempts_item_type", "item_type IN ('question','kata')");
            t.HasCheckConstraint("ck_attempts_score", "score IS NULL OR (score >= 0 AND score <= 1)");
        });
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).ValueGeneratedNever();
        e.HasOne<Learner>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        ItemColumns.ConfigureItem(e);
        e.HasOne<Session>().WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.SetNull);
        e.Property(x => x.Answer).HasColumnType("jsonb").IsRequired();
        e.Property(x => x.Grading).HasColumnType("jsonb");
        e.HasIndex(x => new { x.UserId, x.ItemId, x.AnsweredAt }).IsDescending(false, false, true);
        e.HasIndex(x => x.SessionId);
        // One answer per item per session (FR-005); partial so session-less attempts (feature 004) stay unconstrained.
        e.HasIndex(x => new { x.SessionId, x.ItemId }).IsUnique().HasFilter("session_id IS NOT NULL");
        e.Timestamps(withUpdated: false);
    }
}
