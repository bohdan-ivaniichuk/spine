using Spine.Api.Endpoints.Shared;

namespace Spine.Api.Endpoints.Review.FinishSession;

public sealed class FinishSessionRequest : LanguageQuery
{
    public Guid Id { get; set; }
}
