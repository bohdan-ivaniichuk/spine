using Spine.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Spine.Infrastructure.Persistence.Configurations.Content;

internal sealed class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> e)
    {
        e.ToTable("questions", t =>
        {
            t.HasCheckConstraint("ck_questions_id", $"id ~ '{Conventions.QuestionIdPattern}'");
            t.HasCheckConstraint("ck_questions_type", "type IN ('mcq','open')");
            t.HasCheckConstraint("ck_questions_level", "level IN ('M','S','L')");
        });
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).ValueGeneratedNever();
        e.Property(x => x.Type).QuestionType();
        e.Property(x => x.Level).Level();
        e.Property(x => x.Tags).TextArray();
        e.Property(x => x.ContentHash).ContentHash();
        e.HasIndex(x => x.ConceptId);
        e.Timestamps();
        e.HasMany(x => x.Translations).WithOne(x => x.Question).HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Cascade);
    }
}
