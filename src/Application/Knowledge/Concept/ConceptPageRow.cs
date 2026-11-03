using Spine.Domain.Content;

namespace Spine.Application.Knowledge.Concept;

/// <summary>One concept with its ancestors' titles and its text in the requested language and in English.</summary>
public sealed record ConceptPageRow(
    string DomainSlug,
    string DomainTitle,
    string? DomainTitleUk,
    string TopicSlug,
    string TopicTitle,
    string? TopicTitleUk,
    string Slug,
    Level Level,
    bool Reviewed,
    int SortOrder,
    DateTimeOffset? DeprecatedAt,
    ConceptTextRow? Requested,
    ConceptTextRow? En,
    int QuestionCount,
    int KataCount);
