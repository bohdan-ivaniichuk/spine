namespace Spine.Application.Review.WeakSpots;

public sealed record WeakSpotsDto(
    DateTimeOffset Since,
    int WindowDays,
    int MinAnswers,
    IReadOnlyList<ConceptWeaknessDto> Concepts,
    IReadOnlyList<TopicWeaknessDto> Topics);
