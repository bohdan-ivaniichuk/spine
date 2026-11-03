namespace Spine.Application.Review;

/// <summary>Answers given on one calendar day in the requested time zone.</summary>
public sealed record ActivityDayDto(DateOnly Date, int Answers);

/// <summary>
/// The local learner's answer activity in one time zone (<c>specs/007-activity-streak/contracts/activity-api.md</c>).
/// </summary>
/// <param name="TimeZone">The IANA id as requested (aliases such as Europe/Kiev stay as sent); <c>UTC</c> when none was.</param>
/// <param name="Today">Today's date in that zone.</param>
/// <param name="CurrentStreak">Consecutive active days ending today, or ending yesterday while today is inactive.</param>
/// <param name="ActiveToday">Whether today already has an answer.</param>
/// <param name="LongestStreak">The longest run of active days in the whole history.</param>
/// <param name="Days">The last <see cref="ActivityCalculator.WindowDays"/> days, oldest first, ending today.</param>
public sealed record ActivityDto(
    string TimeZone,
    DateOnly Today,
    int CurrentStreak,
    bool ActiveToday,
    int LongestStreak,
    IReadOnlyList<ActivityDayDto> Days);
