using Spine.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Spine.Infrastructure.Persistence.Configurations.Content;

internal sealed class KataTranslationConfiguration : IEntityTypeConfiguration<KataTranslation>
{
    public void Configure(EntityTypeBuilder<KataTranslation> e)
    {
        e.ToTable("kata_translations", t => t.HasCheckConstraint("ck_kata_translations_language", "language IN ('en','uk')"));
        e.HasKey(x => new { x.KataId, x.Language });
        e.Property(x => x.Language).Language();
        e.Property(x => x.Task).IsRequired();
        e.Property(x => x.Rubric).JsonListRequired();
        e.Property(x => x.ReferenceSolution).IsRequired();
        e.Property(x => x.ContentHash).ContentHash();
        e.Timestamps();
    }
}
