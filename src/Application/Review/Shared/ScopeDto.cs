namespace Spine.Application.Review.Shared;

// Shapes are the Angular contract (specs/002-review-session/contracts/review-api.md, extended by
// specs/003-atlas-redesign-i18n/contracts/review-api-i18n.md); property names serialize camelCase.

/// <summary><c>Concept</c> and <c>ConceptTitle</c> are set only for the <c>concept</c> scope (feature 004).</summary>
public sealed record ScopeDto(
    string Type,
    string? Domain,
    string? Topic,
    string? DomainTitle,
    string? TopicTitle,
    string? Concept = null,
    string? ConceptTitle = null);
