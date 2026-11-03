namespace Spine.Application.Knowledge.Tree;

public sealed record TreeDomainDto(string Slug, string Title, bool TitleFallback, int SortOrder, DateTimeOffset? DeprecatedAt, IReadOnlyList<TreeTopicDto> Topics);
