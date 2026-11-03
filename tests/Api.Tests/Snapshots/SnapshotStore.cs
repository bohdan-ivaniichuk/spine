using System.Text;
using System.Text.RegularExpressions;

namespace Spine.Api.Tests.Snapshots;

/// <summary>
/// Reads and writes snapshot cases under <c>tests/Api.Tests/Snapshots/cases</c>: a header (status, location, SQL command
/// count), a blank line, then the raw body. GUIDs are replaced by <c>&lt;guid-n&gt;</c> in order of first appearance and
/// ProblemDetails trace ids are masked, so a case only changes when the API's observable output changes.
/// <c>SPINE_SNAPSHOTS=record</c> (or a missing file) re-records.
/// </summary>
public sealed partial class SnapshotStore
{
    private readonly Dictionary<string, string> _guids = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _failures = [];

    public static string CasesDir => Path.Combine(ApiFixture.RepoRoot, "tests", "Api.Tests", "Snapshots", "cases");

    public static bool Recording => Environment.GetEnvironmentVariable("SPINE_SNAPSHOTS") == "record";

    public IReadOnlyList<string> Failures => _failures;

    public void Check(string name, int status, string? location, int sql, string body)
    {
        var actualBody = Normalize(body);
        var actualLocation = location is null ? "-" : Normalize(location);
        var path = Path.Combine(CasesDir, name + ".txt");

        if (Recording || !File.Exists(path))
        {
            Directory.CreateDirectory(CasesDir);
            File.WriteAllText(path, Format(status, actualLocation, sql, actualBody), new UTF8Encoding(false));
            _failures.Add($"{name}: recorded");
            return;
        }

        var (expectedStatus, expectedLocation, expectedSql, expectedBody) = Parse(File.ReadAllText(path));
        if (expectedStatus != status)
        {
            _failures.Add($"{name}: status {status}, expected {expectedStatus}");
        }

        if (expectedLocation != actualLocation)
        {
            _failures.Add($"{name}: location {actualLocation}, expected {expectedLocation}");
        }

        if (sql > expectedSql)
        {
            _failures.Add($"{name}: {sql} SQL commands, recorded {expectedSql}");
        }

        if (expectedBody != actualBody)
        {
            _failures.Add($"{name}: body differs\n  expected: {expectedBody}\n  actual:   {actualBody}");
        }
    }

    private string Normalize(string text)
    {
        text = TraceId().Replace(text, "\"traceId\":\"<trace>\"");
        return Guid().Replace(text, m =>
        {
            if (!_guids.TryGetValue(m.Value, out var token))
            {
                token = $"<guid-{_guids.Count + 1}>";
                _guids[m.Value] = token;
            }

            return token;
        });
    }

    private static string Format(int status, string location, int sql, string body) =>
        $"status: {status}\nlocation: {location}\nsql: {sql}\n\n{body}\n";

    private static (int Status, string Location, int Sql, string Body) Parse(string text)
    {
        var split = text.Replace("\r\n", "\n").Split("\n\n", 2);
        var header = split[0].Split('\n').Select(l => l.Split(": ", 2)).ToDictionary(p => p[0], p => p[1]);
        var body = split.Length > 1 ? split[1] : string.Empty;
        if (body.EndsWith('\n'))
        {
            body = body[..^1];
        }

        return (int.Parse(header["status"]), header["location"], int.Parse(header["sql"]), body);
    }

    [GeneratedRegex("[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}")]
    private static partial Regex Guid();

    [GeneratedRegex("\"traceId\":\"[^\"]*\"")]
    private static partial Regex TraceId();
}
