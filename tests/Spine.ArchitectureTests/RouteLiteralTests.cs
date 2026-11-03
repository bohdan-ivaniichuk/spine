using System.Text.RegularExpressions;

namespace Spine.ArchitectureTests;

/// <summary>FR-002 and FR-003 as source rules: routes come only from ApiRoutes, and Api lines stay readable.</summary>
public sealed partial class RouteLiteralTests
{
    public const int MaxLineLength = 120;

    [Fact]
    public void Endpoints_take_their_routes_from_the_route_table()
    {
        var offenders = SourceFiles.In(Path.Combine(RepoPaths.Src, "Api", "Endpoints"))
            .Where(f => Path.GetFileName(f) != "ApiRoutes.cs")
            .SelectMany(f => SourceFiles.Lines(f).Where(l => RouteLiteral().IsMatch(l.Text)).Select(l => l.Where))
            .ToList();

        offenders.ShouldBeEmpty("route string literals belong in ApiRoutes");
    }

    [Fact]
    public void Api_tests_build_their_urls_from_the_route_table()
    {
        var offenders = SourceFiles.In(Path.Combine(RepoPaths.Tests, "Api.Tests"))
            .SelectMany(f => SourceFiles.Lines(f).Where(l => l.Text.Contains("\"/api/", StringComparison.Ordinal)).Select(l => l.Where))
            .ToList();

        offenders.ShouldBeEmpty("use ApiUrl.For(ApiRoutes…) or ApiUrl.Prefix + ApiRoutes…");
    }

    [Fact]
    public void Api_lines_fit_the_editor_and_chain_one_call_per_line()
    {
        var files = SourceFiles.In(Path.Combine(RepoPaths.Src, "Api")).ToList();

        files.SelectMany(f => SourceFiles.Lines(f).Where(l => l.Text.Length > MaxLineLength).Select(l => l.Where))
            .ShouldBeEmpty($"lines in src/Api must be at most {MaxLineLength} characters");
        files.SelectMany(f => SourceFiles.Lines(f).Where(l => TwoProduces().IsMatch(l.Text)).Select(l => l.Where))
            .ShouldBeEmpty("endpoint metadata: one .Produces… call per line");
    }

    [GeneratedRegex("""\b(Get|Post|Put|Patch|Delete|Routes)\("/""")]
    private static partial Regex RouteLiteral();

    [GeneratedRegex(@"\.Produces\w*\(.*\.Produces")]
    private static partial Regex TwoProduces();
}
