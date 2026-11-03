using Spine.Domain.Content;
using Spine.Domain.Progress;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Spine.Infrastructure.Persistence.Configurations.Progress;

internal sealed class LearnerConfiguration : IEntityTypeConfiguration<Learner>
{
    public void Configure(EntityTypeBuilder<Learner> e)
    {
        e.ToTable("users", t => t.HasCheckConstraint("ck_users_preferred_language", "preferred_language IN ('en','uk')"));
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).ValueGeneratedNever();
        e.Property(x => x.DisplayName).IsRequired();
        e.Property(x => x.PreferredLanguage).Language();
        e.Timestamps(withUpdated: false);
    }
}
