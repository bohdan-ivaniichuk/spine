using Spine.Domain.Content;

namespace Spine.Application.Content;

/// <summary>The desired state of the content graph, built from the manifest and the topic files.</summary>
public sealed class ImportPlan
{
    public const string ManifestFile = "manifest.json";

    public List<PlanDomain> Domains { get; } = [];
}

public sealed class PlanDomain
{
    public required string Slug { get; init; }
    public required string Title { get; init; }
    public int SortOrder { get; init; }
    public required string Hash { get; init; }
    public List<PlanTopic> Topics { get; } = [];
}

public sealed class PlanTopic
{
    public required string Slug { get; init; }
    public required string Title { get; init; }
    public int SortOrder { get; init; }
    public required string Hash { get; init; }
    /// <summary>Relative path of the topic file, or null when the manifest lists a topic that has no file yet.</summary>
    public string? File { get; init; }
    public List<PlanConcept> Concepts { get; } = [];
}

public sealed class PlanConcept
{
    public required string Slug { get; init; }
    public Level Level { get; init; }
    public bool Reviewed { get; init; }
    public int SortOrder { get; init; }
    public required string Hash { get; init; }
    public Dictionary<Language, PlanConceptTranslation> Translations { get; } = [];
    public List<PlanQuestion> Questions { get; } = [];
    public List<PlanKata> Katas { get; } = [];
}

public sealed record PlanConceptTranslation(string Title, string Note, string Hash);

public sealed class PlanQuestion
{
    public required string Id { get; init; }
    public QuestionType Type { get; init; }
    public Level Level { get; init; }
    public bool Reviewed { get; init; }
    public string[] Tags { get; init; } = [];
    public int SortOrder { get; init; }
    public required string Hash { get; init; }
    public Dictionary<Language, PlanQuestionTranslation> Translations { get; } = [];
}

public sealed record PlanQuestionTranslation(
    string Prompt,
    string Explanation,
    IReadOnlyList<McqOption>? Options,
    IReadOnlyList<string>? Rubric,
    string Hash);

public sealed class PlanKata
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public KataLanguage Language { get; init; }
    public bool Reviewed { get; init; }
    public int TimeLimitMinutes { get; init; }
    public int SortOrder { get; init; }
    public required string Hash { get; init; }
    public Dictionary<Language, PlanKataTranslation> Translations { get; } = [];
}

public sealed record PlanKataTranslation(string Task, IReadOnlyList<string> Rubric, string ReferenceSolution, string Hash);
