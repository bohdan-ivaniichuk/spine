using Spine.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Spine.Infrastructure.Persistence.Configurations.Content;

internal sealed class ConceptTranslationConfiguration : IEntityTypeConfiguration<ConceptTranslation>
{
    public void Configure(EntityTypeBuilder<ConceptTranslation> e)
    {
        e.ToTable("concept_translations", t => t.HasCheckConstraint("ck_concept_translations_language", "language IN ('en','uk')"));
        e.HasKey(x => new { x.ConceptId, x.Language });
        e.Property(x => x.Language).Language();
        e.Property(x => x.Title).IsRequired();
        e.Property(x => x.Note).IsRequired();
        e.Property(x => x.ContentHash).ContentHash();
        e.Timestamps();
    }
}
