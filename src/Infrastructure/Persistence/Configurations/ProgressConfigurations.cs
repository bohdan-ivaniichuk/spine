using Spine.Domain.Content;
using Spine.Domain.Progress;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Spine.Infrastructure.Persistence.Configurations;

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

internal static class ItemColumns
{
    public const string ItemCheck =
        "(item_type = 'question' AND question_id IS NOT NULL AND kata_id IS NULL) OR " +
        "(item_type = 'kata' AND kata_id IS NOT NULL AND question_id IS NULL)";

    public static void ConfigureItem<T>(EntityTypeBuilder<T> e) where T : class
    {
        e.Property<ItemType>("ItemType")
            .HasConversion(v => v.ToDbValue(), v => ItemTypeExtensions.ParseItemType(v))
            .HasColumnType("text").IsRequired();
        e.Property<string?>("QuestionId");
        e.Property<string?>("KataId");
        e.Property<string>("ItemId").HasComputedColumnSql("COALESCE(question_id, kata_id)", stored: true);
        e.HasOne<Question>().WithMany().HasForeignKey("QuestionId").OnDelete(DeleteBehavior.Restrict);
        e.HasOne<Kata>().WithMany().HasForeignKey("KataId").OnDelete(DeleteBehavior.Restrict);
    }
}

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

internal sealed class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> e)
    {
        e.ToTable("sessions", t =>
        {
            t.HasCheckConstraint("ck_sessions_scope_type", "scope_type IN ('all','domain','topic')");
            t.HasCheckConstraint("ck_sessions_scope",
                "(scope_type = 'all' AND scope_domain_slug IS NULL AND scope_topic_slug IS NULL) OR " +
                "(scope_type = 'domain' AND scope_domain_slug IS NOT NULL AND scope_topic_slug IS NULL) OR " +
                "(scope_type = 'topic' AND scope_domain_slug IS NOT NULL AND scope_topic_slug IS NOT NULL)");
        });
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).ValueGeneratedNever();
        e.HasOne<Learner>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
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
