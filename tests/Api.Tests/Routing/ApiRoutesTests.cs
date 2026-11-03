using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Spine.Api.Endpoints;

namespace Spine.Api.Tests.Routing;

/// <summary>The route table is the single source of every API path (FR-002): nothing served outside it, nothing in it unserved.</summary>
[Collection("api")]
[Trait("Category", "Integration")]
public sealed class ApiRoutesTests(ApiFixture fixture)
{
    [Fact]
    [Trait("Requirement", "FR-002")]
    public void Every_served_endpoint_matches_exactly_one_route_table_entry()
    {
        var served = fixture.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Select(e => "/" + e.RoutePattern.RawText!.TrimStart('/'))
            .Where(path => path.StartsWith(ApiUrl.Prefix + "/", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToList();

        var table = ApiRoutes.All.Select(route => ApiUrl.Prefix + route).Order(StringComparer.Ordinal).ToList();

        table.ShouldBeUnique();
        served.ShouldBe(table);
    }

    [Fact]
    [Trait("Requirement", "FR-002")]
    public void The_route_table_covers_every_feature_group()
    {
        ApiRoutes.All.ShouldContain(ApiRoutes.Knowledge.Tree);
        ApiRoutes.All.ShouldContain(ApiRoutes.Review.Finish);
        ApiRoutes.All.ShouldContain(ApiRoutes.Meta.Info);
        ApiRoutes.All.ShouldNotContain(ApiRoutes.Health.Live, "health checks are mapped outside the API prefix");
    }
}
