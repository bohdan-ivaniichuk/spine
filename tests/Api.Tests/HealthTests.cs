using System.Net;
using System.Text.Json;

namespace InterviewPrep.Api.Tests;

[Collection("api")]
[Trait("Category", "Integration")]
public sealed class HealthTests(ApiFixture fixture)
{
    [Theory]
    [InlineData("/health")]
    [InlineData("/health/ready")]
    public async Task Health_endpoints_report_healthy(string path)
    {
        var response = await fixture.Client.GetAsync(path);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/json");
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("status").GetString().ShouldBe("Healthy");
    }

    [Fact]
    public async Task Meta_reports_api_name_version_and_environment()
    {
        var response = await fixture.Client.GetAsync("/api/meta");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("name").GetString().ShouldBe("InterviewPrep API");
        body.RootElement.GetProperty("version").GetString().ShouldNotBeNullOrWhiteSpace();
        body.RootElement.GetProperty("environment").GetString().ShouldBe("Development");
    }
}
