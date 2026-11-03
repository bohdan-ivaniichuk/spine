namespace Spine.Application.Knowledge.Tree;

/// <summary>One domain as the tree query loads it; titles in both manifest languages.</summary>
public sealed record TreeDomainRow(int Id, string Slug, string Title, string? TitleUk, int SortOrder, DateTimeOffset? DeprecatedAt);
