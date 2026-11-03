namespace Spine.Api.Endpoints.Meta.GetMeta;

/// <summary>Build and environment info.</summary>
public sealed record MetaResponse(string Name, string Version, string Environment);
