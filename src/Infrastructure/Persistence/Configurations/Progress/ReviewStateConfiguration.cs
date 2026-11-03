using Spine.Domain.Progress;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Spine.Infrastructure.Persistence.Configurations.Progress;

internal sealed class ReviewStateConfiguration : IEntityTypeConfiguration<ReviewState>
{
    public void Configure(EntityTypeBuilder<ReviewState> e)
    {
        e.ToTable("review_states", t =>
        {
            t.HasCheckConstraint("ck_review_states_item", ItemColumns.ItemCheck);
            t.HasCheckConstraint("ck_review_states_item_type", "item_type IN ('question','kata')");
        });
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).ValueGeneratedNever();
        e.HasOne<Learner>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        ItemColumns.ConfigureItem(e);
        e.Property(x => x.Algorithm).IsRequired().HasDefaultValue("sm2");
        e.Property(x => x.IntervalDays).HasDefaultValue(0);
        e.Property(x => x.Ease).HasDefaultValue(2.5);
        e.Property(x => x.Repetitions).HasDefaultValue(0);
        e.Property(x => x.AlgorithmState).HasColumnType("jsonb");
        e.HasIndex(x => new { x.UserId, x.ItemId }).IsUnique();
        e.HasIndex(x => new { x.UserId, x.DueAt });
        e.Timestamps();
    }
}
