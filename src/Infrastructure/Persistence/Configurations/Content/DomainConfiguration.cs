using Spine.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DomainEntity = Spine.Domain.Content.Domain;

namespace Spine.Infrastructure.Persistence.Configurations.Content;

internal sealed class DomainConfiguration : IEntityTypeConfiguration<DomainEntity>
{
    public void Configure(EntityTypeBuilder<DomainEntity> e)
    {
        e.ToTable("domains", t => t.HasCheckConstraint("ck_domains_slug", $"slug ~ '{Conventions.SlugPattern}'"));
        e.HasKey(x => x.Id);
        e.Property(x => x.Slug).IsRequired();
        e.Property(x => x.Title).IsRequired();
        e.Property(x => x.ContentHash).ContentHash();
        e.HasIndex(x => x.Slug).IsUnique();
        e.HasIndex(x => x.SortOrder);
        e.Timestamps();
        e.HasMany(x => x.Topics).WithOne(x => x.Domain).HasForeignKey(x => x.DomainId).OnDelete(DeleteBehavior.Restrict);
    }
}
