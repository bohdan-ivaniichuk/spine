namespace Spine.Application.Knowledge.Tree;

/// <summary>One topic as the tree query loads it.</summary>
public sealed record TreeTopicRow(
    int Id, int DomainId, string Slug, string Title, string? TitleUk, int SortOrder, DateTimeOffset? DeprecatedAt);
