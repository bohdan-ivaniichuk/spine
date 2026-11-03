using FastEndpoints;

namespace Spine.Api.Endpoints.Review.GetActivity;

/// <summary>No <c>lang</c>: the payload has no text.</summary>
public sealed class GetActivityRequest
{
    /// <summary>IANA time zone id, e.g. Europe/Kyiv. Absent or empty: UTC.</summary>
    [QueryParam]
    public string? Tz { get; set; }

    /// <summary>The zone the request names; UTC when it names none. Only valid after validation.</summary>
    public TimeZoneInfo ResolveZone() =>
        string.IsNullOrEmpty(Tz) ? TimeZoneInfo.Utc : TimeZoneInfo.FindSystemTimeZoneById(Tz);

    /// <summary>
    /// Absent, empty, <c>UTC</c>, or an IANA <c>Area/Location</c> id the time-zone data knows (aliases such as
    /// Europe/Kiev too). Windows ids resolve only where ICU is loaded and the <c>posix/</c> / <c>right/</c> trees are
    /// file-system aliases, so both are refused everywhere and every machine answers alike.
    /// </summary>
    public static bool IsKnownZone(string? tz) =>
        string.IsNullOrEmpty(tz)
        || (tz.Length <= 64
            && (tz == "UTC" || (tz.Contains('/', StringComparison.Ordinal)
                && !tz.StartsWith("posix/", StringComparison.Ordinal)
                && !tz.StartsWith("right/", StringComparison.Ordinal)))
            && TimeZoneInfo.TryFindSystemTimeZoneById(tz, out _));
}
