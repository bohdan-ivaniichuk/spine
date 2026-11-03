namespace Spine.Application.Knowledge.Tree;

public sealed record TreeTopicDto(string Slug, string Title, bool TitleFallback, int SortOrder, DateTimeOffset? DeprecatedAt, IReadOnlyList<TreeConceptDto> Concepts);
