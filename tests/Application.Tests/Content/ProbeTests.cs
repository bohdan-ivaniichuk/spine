using InterviewPrep.Application.Content.Validation;
using Xunit.Abstractions;

namespace InterviewPrep.Application.Tests.Content;

public class ProbeTests(ITestOutputHelper output)
{
    [Fact]
    public void Probe()
    {
        foreach (var name in new[] { "five-options", "two-correct", "bad-id", "unknown-topic", "rubric-on-mcq", "short-note", "extra-property" })
        {
            var set = ContentSetLoader.WithValidManifest(("dotnet/clr-memory.json", ContentSetLoader.ReadInvalid(name + ".json")));
            output.WriteLine($"== {name}");
            foreach (var e in new ContentSchemaValidator().Validate(set).Errors) output.WriteLine("  schema " + e);
            foreach (var e in new ContentRuleValidator().Validate(set).Errors) output.WriteLine("  rule   " + e);
        }
        var valid = ContentSetLoader.LoadValid();
        output.WriteLine("== valid");
        foreach (var e in new ContentSchemaValidator().Validate(valid).Errors) output.WriteLine("  schema " + e);
        foreach (var e in new ContentRuleValidator().Validate(valid).Errors) output.WriteLine("  rule   " + e);
    }
}
