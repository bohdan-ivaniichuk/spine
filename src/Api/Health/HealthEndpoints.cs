using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Spine.Api.Health;

/// <summary>/health = liveness (no checks); /health/ready = database reachable. Both answer {"status": "..."}.</summary>
public static class HealthEndpoints
{
    public static void MapHealth(this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = WriteAsync,
        });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = WriteAsync,
        });
    }

    private static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(new { status = report.Status.ToString() }));
    }
}
