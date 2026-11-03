using Spine.Application.Review.Readiness;

namespace Spine.Application.Review.WeakSpots;

/// <summary>
/// The local learner's weakest concepts and topics (specs/006-weak-spots/contracts/weak-spots-api.md). Slugs and figures
/// only; accuracy is 0..1.
/// </summary>
public sealed record ConceptWeaknessDto(
    string Domain,
    string Topic,
    string Concept,
    int Answers,
    int Wrong,
    double Accuracy,
    DateTimeOffset LastAnsweredAt,
    int WeakQuestions,
    ConceptReadinessDto? Readiness);
