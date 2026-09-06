using InterviewPrep.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DomainEntity = InterviewPrep.Domain.Content.Domain;

namespace InterviewPrep.Infrastructure.Persistence.Configurations;

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
