using InterviewPrep.Domain.Content;
using InterviewPrep.Domain.Progress;
using Microsoft.EntityFrameworkCore;
using DomainEntity = InterviewPrep.Domain.Content.Domain;

namespace InterviewPrep.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<DomainEntity> Domains => Set<DomainEntity>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<Concept> Concepts => Set<Concept>();
    public DbSet<ConceptTranslation> ConceptTranslations => Set<ConceptTranslation>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<QuestionTranslation> QuestionTranslations => Set<QuestionTranslation>();
    public DbSet<Kata> Katas => Set<Kata>();
    public DbSet<KataTranslation> KataTranslations => Set<KataTranslation>();

    public DbSet<Learner> Users => Set<Learner>();
    public DbSet<ReviewState> ReviewStates => Set<ReviewState>();
    public DbSet<Attempt> Attempts => Set<Attempt>();
    public DbSet<Session> Sessions => Set<Session>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
