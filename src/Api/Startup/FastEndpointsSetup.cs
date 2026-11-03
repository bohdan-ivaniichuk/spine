using System.Text.Json;
using FastEndpoints;

namespace Spine.Api.Startup;

/// <summary>FastEndpoints under the <c>/api</c> prefix with camelCase JSON and ProblemDetails errors.</summary>
public static class FastEndpointsSetup
{
    public static WebApplication UseSpineEndpoints(this WebApplication app)
    {
        app.UseFastEndpoints(c =>
        {
            c.Endpoints.RoutePrefix = "api";
            c.Serializer.Options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            c.Errors.UseProblemDetails();
        });
        return app;
    }
}
