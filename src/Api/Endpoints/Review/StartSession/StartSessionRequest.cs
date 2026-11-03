using Spine.Api.Endpoints.Review.Shared;

namespace Spine.Api.Endpoints.Review.StartSession;

public sealed class StartSessionRequest : ScopeQuery
{
    /// <summary>regular (default: due, then new) or weak (questions whose last answer was wrong).</summary>
    public string Mode { get; set; } = "regular";

    /// <summary>Session size, 1..100; defaults to REVIEW_DEFAULT_TOTAL_LIMIT.</summary>
    public int? TotalLimit { get; set; }

    /// <summary>How many of those may be new questions, 1..100; defaults to REVIEW_DEFAULT_NEW_LIMIT.</summary>
    public int? NewLimit { get; set; }
}
