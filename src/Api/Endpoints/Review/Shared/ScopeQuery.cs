using Spine.Api.Endpoints.Shared;
using Spine.Application.Review;
using Spine.Domain.Progress;

namespace Spine.Api.Endpoints.Review.Shared;

/// <summary>
/// Scope parameters shared by the overview (query string) and session start (body), plus the <c>lang</c> query
/// parameter.
/// </summary>
public abstract class ScopeQuery : LanguageQuery
{
    /// <summary>all (default), domain, topic or concept.</summary>
    public string Scope { get; set; } = "all";

    /// <summary>Domain slug; required when scope is domain, topic or concept.</summary>
    public string? Domain { get; set; }

    /// <summary>Topic slug; required when scope is topic or concept.</summary>
    public string? Topic { get; set; }

    /// <summary>Concept slug; required when scope is concept.</summary>
    public string? Concept { get; set; }

    public ReviewScope ToReviewScope() =>
        new(
            SessionScopeTypeExtensions.ParseSessionScopeType(Scope),
            Normalize(Domain),
            Normalize(Topic),
            Normalize(Concept));

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
