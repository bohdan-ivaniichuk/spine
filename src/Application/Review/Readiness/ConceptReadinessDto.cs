namespace Spine.Application.Review.Readiness;

/// <summary>
/// Readiness of the local learner (specs/004-knowledge-atlas-notes/contracts/readiness-api.md). <c>State</c> is
/// <c>not-started</c> | <c>in-progress</c> | <c>ready</c>; <c>Percent</c> is null when no concept counts.
/// </summary>
public sealed record ConceptReadinessDto(string Concept, string State, int Questions, int ReadyQuestions);
