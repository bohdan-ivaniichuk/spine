namespace Spine.Application.Knowledge.Tree;

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
