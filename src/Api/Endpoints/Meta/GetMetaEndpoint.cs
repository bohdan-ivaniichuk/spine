using System.Reflection;
using FastEndpoints;

namespace Spine.Api.Endpoints.Meta;

public sealed record MetaResponse(string Name, string Version, string Environment);

/// <summary>GET /api/meta — build and environment info for the web shell and smoke checks.</summary>
public sealed class GetMetaEndpoint(IWebHostEnvironment environment) : EndpointWithoutRequest<MetaResponse>
{
    public override void Configure()
    {
        Get("/meta");
        AllowAnonymous();
        Description(b => b.Produces<MetaResponse>(200));
    }

    public override Task HandleAsync(CancellationToken ct)
    {
        var version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                      ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                      ?? "0.0.0";
        return Send.OkAsync(new MetaResponse("Spine API", version, environment.EnvironmentName), ct);
    }
}
