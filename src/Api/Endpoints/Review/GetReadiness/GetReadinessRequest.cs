using Spine.Api.Endpoints.Shared;

namespace Spine.Api.Endpoints.Review.GetReadiness;

/// <summary>
/// <c>lang</c> is accepted and validated for consistency with the other review endpoints; the payload has no text.
/// </summary>
public sealed class GetReadinessRequest : LanguageQuery;
