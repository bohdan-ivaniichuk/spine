namespace Spine.Application.Review.FinishSession;

/// <summary>Where a question lives, for grouping wrong answers by concept.</summary>
public sealed record ConceptRef(string DomainSlug, string TopicSlug, string ConceptSlug, string ConceptTitle);
