using Spine.Api.Endpoints.Shared;

namespace Spine.Api.Endpoints.Review.GetSession;

public sealed class GetSessionRequest : LanguageQuery
{
    public Guid Id { get; set; }
}
