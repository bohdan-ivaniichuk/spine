namespace Spine.Application.Knowledge.Concept;

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
