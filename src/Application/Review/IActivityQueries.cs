namespace Spine.Application.Review;

/// <summary>The local learner's answer activity, with days in the given time zone. No language: the payload has no text.</summary>
public interface IActivityQueries
{
    Task<ActivityDto> GetAsync(TimeZoneInfo zone, CancellationToken ct = default);
}
