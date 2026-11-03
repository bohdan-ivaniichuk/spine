using Spine.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Spine.Infrastructure.Persistence.Configurations.Content;

internal sealed class KataConfiguration : IEntityTypeConfiguration<Kata>
{
    public void Configure(EntityTypeBuilder<Kata> e)
    {
        e.ToTable("katas", t =>
        {
            t.HasCheckConstraint("ck_katas_id", $"id ~ '{Conventions.KataIdPattern}'");
            t.HasCheckConstraint("ck_katas_language", "language IN ('csharp','typescript','sql')");
            t.HasCheckConstraint("ck_katas_time_limit", "time_limit_minutes BETWEEN 5 AND 120");
        });
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).ValueGeneratedNever();
        e.Property(x => x.Title).IsRequired();
        e.Property(x => x.Language).KataLanguage();
        e.Property(x => x.ContentHash).ContentHash();
        e.HasIndex(x => x.ConceptId);
        e.Timestamps();
        e.HasMany(x => x.Translations).WithOne(x => x.Kata).HasForeignKey(x => x.KataId).OnDelete(DeleteBehavior.Cascade);
    }
}
