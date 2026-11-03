namespace Spine.Application.Knowledge.Tree;

public sealed record KnowledgeTreeDto(string Language, IReadOnlyList<TreeDomainDto> Domains);
