namespace Spine.Application.Review.WeakSpots;

/// <summary>
/// The local learner's weakest concepts and topics over the last <see cref="Spine.Domain.Progress.WeaknessRule.WindowDays"/>
/// days. No language: the payload has no text.
/// </summary>
public interface IWeakSpotsQueries
{
    Task<WeakSpotsDto> GetAsync(CancellationToken ct = default);
}
