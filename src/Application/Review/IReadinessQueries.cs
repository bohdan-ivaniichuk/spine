namespace Spine.Application.Review;

/// <summary>
/// Readiness of the local learner per domain, topic and concept, derived from review states (never stored). No
/// language: the payload has no text, so readiness cannot depend on it.
/// </summary>
public interface IReadinessQueries
{
    /// <summary>Every live domain and topic in manifest order; concepts without schedulable questions are left out.</summary>
    Task<ReadinessDto> GetAsync(CancellationToken ct = default);
}
