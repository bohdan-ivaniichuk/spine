using Spine.Domain.Progress;

namespace Spine.Application.Review.Readiness;

/// <summary>
/// Per concept: how many schedulable questions it has, how many have a review state, and how many of those are
/// ready under <see cref="ReadinessRule"/>.
/// </summary>
public sealed record ConceptCounts(string Concept, int Questions, int Stated, int Ready);
