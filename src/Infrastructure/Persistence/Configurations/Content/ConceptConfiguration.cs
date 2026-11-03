using Spine.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Spine.Infrastructure.Persistence.Configurations.Content;

internal sealed class ConceptConfiguration : IEntityTypeConfiguration<Concept>
{
    public void Configure(EntityTypeBuilder<Concept> e)
    {
        e.ToTable("concepts", t =>
        {
            t.HasCheckConstraint("ck_concepts_slug", $"slug ~ '{Conventions.SlugPattern}'");
            t.HasCheckConstraint("ck_concepts_level", "level IN ('M','S','L')");
        });
        e.HasKey(x => x.Id);
        e.Property(x => x.Slug).IsRequired();
        e.Property(x => x.Level).Level();
        e.Property(x => x.ContentHash).ContentHash();
        e.HasIndex(x => new { x.TopicId, x.Slug }).IsUnique();
        e.Timestamps();
        e.HasMany(x => x.Translations).WithOne(x => x.Concept).HasForeignKey(x => x.ConceptId).OnDelete(DeleteBehavior.Cascade);
        e.HasMany(x => x.Questions).WithOne(x => x.Concept).HasForeignKey(x => x.ConceptId).OnDelete(DeleteBehavior.Restrict);
        e.HasMany(x => x.Katas).WithOne(x => x.Concept).HasForeignKey(x => x.ConceptId).OnDelete(DeleteBehavior.Restrict);
    }
}
