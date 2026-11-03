using System.Reflection;
using FastEndpoints;
using Spine.Api.Endpoints;

namespace Spine.ArchitectureTests;

/// <summary>
/// FR-001: every endpoint lives in its own folder (one namespace) with its request and exactly one validator; the folder
/// holds nothing else. Exactly one validator also guards against a silently dropped pairing — FastEndpoints skips
/// validation when none exists.
/// </summary>
public sealed class EndpointLayoutTests
{
    private static readonly Assembly Api = typeof(ApiRoutes).Assembly;

    public static TheoryData<string> Endpoints() =>
        new(Api.GetTypes().Where(IsEndpoint).Select(t => t.FullName!).Order(StringComparer.Ordinal));

    [Theory]
    [MemberData(nameof(Endpoints))]
    public void Request_and_one_validator_share_the_endpoint_namespace(string endpointName)
    {
        var endpoint = Api.GetType(endpointName)!;
        var request = RequestTypeOf(endpoint);
        if (request is null)
        {
            return;
        }

        request.Namespace.ShouldBe(endpoint.Namespace, $"{request.Name} must sit next to {endpoint.Name}");
        var validators = Api.GetTypes()
            .Where(t => !t.IsAbstract && IsValidatorOf(t, request))
            .ToList();
        validators.Count.ShouldBe(1, $"{request.Name} needs exactly one validator");
        validators[0].Namespace.ShouldBe(endpoint.Namespace);
    }

    [Theory]
    [MemberData(nameof(Endpoints))]
    public void The_endpoint_folder_holds_only_its_own_files(string endpointName)
    {
        var endpoint = Api.GetType(endpointName)!;
        endpoint.Name.ShouldEndWith("Endpoint");
        var name = endpoint.Name[..^"Endpoint".Length];
        endpoint.Namespace.ShouldEndWith("." + name, customMessage: $"{endpoint.Name} must live in a folder named {name}");

        var folder = Path.Combine(RepoPaths.Src, "Api", Path.Combine(endpoint.Namespace!.Split('.').Skip(2).ToArray()));
        var files = Directory.GetFiles(folder, "*.cs").Select(Path.GetFileName).ToList();

        string[] allowed = [$"{name}Endpoint.cs", $"{name}Request.cs", $"{name}Validator.cs"];
        files.Where(f => !allowed.Contains(f) && !f!.EndsWith("Response.cs", StringComparison.Ordinal))
            .ShouldBeEmpty($"{folder} may hold only {string.Join(", ", allowed)} and *Response.cs");
    }

    private static bool IsEndpoint(Type type) =>
        type is { IsClass: true, IsAbstract: false } && typeof(BaseEndpoint).IsAssignableFrom(type);

    private static Type? RequestTypeOf(Type endpoint)
    {
        for (var t = endpoint.BaseType; t is not null; t = t.BaseType)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition().Name == "Endpoint`2"
                && t.GetGenericArguments() is [var request, _] && request != typeof(EmptyRequest))
            {
                return request;
            }
        }

        return null;
    }

    private static bool IsValidatorOf(Type type, Type request)
    {
        for (var t = type.BaseType; t is not null; t = t.BaseType)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Validator<>) && t.GetGenericArguments()[0] == request)
            {
                return true;
            }
        }

        return false;
    }
}
