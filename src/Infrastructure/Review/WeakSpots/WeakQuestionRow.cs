
namespace Spine.Infrastructure.Review.WeakSpots;

/// <summary>A question whose latest answer was wrong, and when that answer was given.</summary>
internal sealed class WeakQuestionRow
{
    public required string QuestionId { get; init; }
    public int ConceptId { get; init; }
    public DateTimeOffset MissedAt { get; init; }
}
