using Spine.Api.Endpoints;

namespace Spine.Api.Tests;

/// <summary>Builds request URLs from <see cref="ApiRoutes"/>, so tests never hard-code a path (FR-002).</summary>
internal static class ApiUrl
{
    /// <summary>The served prefix, <c>/api</c>; constant so it can appear in attribute arguments.</summary>
    public const string Prefix = "/" + ApiRoutes.Prefix;

    /// <summary>The route with its <c>{name}</c> placeholders filled, under <see cref="Prefix"/>.</summary>
    public static string For(string route, params (string Name, object Value)[] values)
    {
        var path = route;
        foreach (var (name, value) in values)
        {
            path = path.Replace("{" + name + "}", Uri.EscapeDataString(value.ToString()!), StringComparison.Ordinal);
        }

        return Prefix + path;
    }
}
