using Spine.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Spine.Infrastructure.Persistence.Configurations.Content;

internal sealed class QuestionTranslationConfiguration : IEntityTypeConfiguration<QuestionTranslation>
{
    public void Configure(EntityTypeBuilder<QuestionTranslation> e)
    {
        e.ToTable("question_translations", t =>
        {
            t.HasCheckConstraint("ck_question_translations_language", "language IN ('en','uk')");
            t.HasCheckConstraint("ck_question_translations_options_xor_rubric", "(options IS NULL) <> (rubric IS NULL)");
        });
        e.HasKey(x => new { x.QuestionId, x.Language });
        e.Property(x => x.Language).Language();
        e.Property(x => x.Prompt).IsRequired();
        e.Property(x => x.Explanation).IsRequired();
        e.Property(x => x.Options).JsonList();
        e.Property(x => x.Rubric).JsonList();
        e.Property(x => x.ContentHash).ContentHash();
        e.Timestamps();
    }
}
