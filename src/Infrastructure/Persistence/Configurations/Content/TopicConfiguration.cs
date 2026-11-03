using Spine.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Spine.Infrastructure.Persistence.Configurations.Content;

internal sealed class TopicConfiguration : IEntityTypeConfiguration<Topic>
{
    public void Configure(EntityTypeBuilder<Topic> e)
    {
        e.ToTable("topics", t => t.HasCheckConstraint("ck_topics_slug", $"slug ~ '{Conventions.SlugPattern}'"));
        e.HasKey(x => x.Id);
        e.Property(x => x.Slug).IsRequired();
        e.Property(x => x.Title).IsRequired();
        e.Property(x => x.ContentHash).ContentHash();
        e.HasIndex(x => new { x.DomainId, x.Slug }).IsUnique();
        e.Timestamps();
        e.HasMany(x => x.Concepts).WithOne(x => x.Topic).HasForeignKey(x => x.TopicId).OnDelete(DeleteBehavior.Restrict);
    }
}
