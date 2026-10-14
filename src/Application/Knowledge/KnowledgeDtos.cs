namespace Spine.Application.Knowledge;

public sealed record KnowledgeTreeDto(string Language, IReadOnlyList<TreeDomainDto> Domains);

public sealed record TreeDomainDto(string Slug, string Title, int SortOrder, DateTimeOffset? DeprecatedAt, IReadOnlyList<TreeTopicDto> Topics);

public sealed record TreeTopicDto(string Slug, string Title, int SortOrder, DateTimeOffset? DeprecatedAt, IReadOnlyList<TreeConceptDto> Concepts);

public sealed record TreeConceptDto(
    string Slug,
    string Title,
    bool TitleFallback,
    string Level,
    bool Reviewed,
    int SortOrder,
    int QuestionCount,
    int KataCount,
    DateTimeOffset? DeprecatedAt);

public sealed record ConceptRefDto(string Slug, string Title);

public sealed record ConceptPageDto(
    ConceptRefDto Domain,
    ConceptRefDto Topic,
    string Slug,
    string Level,
    bool Reviewed,
    int SortOrder,
    string RequestedLanguage,
    string Language,
    bool Fallback,
    string Title,
    string Note,
    int QuestionCount,
    int KataCount,
    DateTimeOffset? DeprecatedAt);
