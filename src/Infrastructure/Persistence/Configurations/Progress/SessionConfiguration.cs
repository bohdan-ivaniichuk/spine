using Spine.Domain.Content;
using Spine.Domain.Progress;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Spine.Infrastructure.Persistence.Configurations.Progress;

internal sealed class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> e)
    {
        e.ToTable("sessions", t =>
        {
            t.HasCheckConstraint("ck_sessions_mode", "mode IN ('regular','weak')");
            t.HasCheckConstraint("ck_sessions_scope_type", "scope_type IN ('all','domain','topic','concept')");
            t.HasCheckConstraint("ck_sessions_scope",
                "(scope_type = 'all' AND scope_domain_slug IS NULL AND scope_topic_slug IS NULL AND scope_concept_slug IS NULL) OR " +
                "(scope_type = 'domain' AND scope_domain_slug IS NOT NULL AND scope_topic_slug IS NULL AND scope_concept_slug IS NULL) OR " +
                "(scope_type = 'topic' AND scope_domain_slug IS NOT NULL AND scope_topic_slug IS NOT NULL AND scope_concept_slug IS NULL) OR " +
                "(scope_type = 'concept' AND scope_domain_slug IS NOT NULL AND scope_topic_slug IS NOT NULL AND scope_concept_slug IS NOT NULL)");
        });
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).ValueGeneratedNever();
        e.HasOne<Learner>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        // Regular is the CLR default, so EF omits it on insert and the database default applies; keep the two in step.
        e.Property(x => x.Mode)
            .HasConversion(v => v.ToDbValue(), v => SessionModeExtensions.ParseSessionMode(v))
            .HasColumnType("text").IsRequired().HasDefaultValueSql("'regular'");
        e.Property(x => x.ScopeType)
            .HasConversion(v => v.ToDbValue(), v => SessionScopeTypeExtensions.ParseSessionScopeType(v))
            .HasColumnType("text").IsRequired();
        e.Property(x => x.QuestionIds).TextArray().HasDefaultValueSql("'{}'");
        e.Property(x => x.TotalLimit).HasDefaultValue(20);
        e.Property(x => x.NewLimit).HasDefaultValue(10);
        e.HasIndex(x => new { x.UserId, x.StartedAt }).IsDescending(false, true);
        e.Timestamps(withUpdated: false);
    }
}
