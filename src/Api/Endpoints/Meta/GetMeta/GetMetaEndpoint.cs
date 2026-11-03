using System.Reflection;
using FastEndpoints;

namespace Spine.Api.Endpoints.Meta.GetMeta;

/// <summary>GET /api/meta — build and environment info for the web shell and smoke checks.</summary>
public sealed class GetMetaEndpoint(IWebHostEnvironment environment) : EndpointWithoutRequest<MetaResponse>
{
    public override void Configure()
    {
        Get(ApiRoutes.Meta.Info);
        AllowAnonymous();
        Description(b => b
            .Produces<MetaResponse>(200));
    }

    public override Task HandleAsync(CancellationToken ct)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                      ?? assembly.GetName().Version?.ToString()
                      ?? "0.0.0";
        return Send.OkAsync(new MetaResponse("Spine API", version, environment.EnvironmentName), ct);
    }
}
