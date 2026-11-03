using Spine.Domain.Content;

namespace Spine.Application.Knowledge.Tree;

/// <summary>One concept as the tree query loads it: its title in the requested language and in English, and counts.</summary>
public sealed record TreeConceptRow(
    int Id,
    int TopicId,
    string Slug,
    Level Level,
    bool Reviewed,
    int SortOrder,
    DateTimeOffset? DeprecatedAt,
    string? TitleRequested,
    string? TitleEn,
    int QuestionCount,
    int KataCount);
