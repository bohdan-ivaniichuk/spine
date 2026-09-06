using InterviewPrep.Application.Content.Validation;
using Xunit.Abstractions;

namespace InterviewPrep.Application.Tests.Content;

/// <summary>Replaces the ajv-cli check: the committed content/ directory must pass both validators.</summary>
public class RepoContentValidatesTests(ITestOutputHelper output)
{
    [Fact]
    public void RepoContent_PassesSchemaAndRuleValidation()
    {
        var root = ContentSetLoader.TryFindRepoRoot();
        if (root is null)
        {
            output.WriteLine($"SKIPPED: no ancestor of '{AppContext.BaseDirectory}' contains content/schema/content.schema.json.");
            return;
        }

        var set = ContentSetLoader.Load(Path.Combine(root, "content"));
        set.Manifest.ShouldNotBeNull().Parsed.ShouldNotBeNull();
        set.TopicFiles.ShouldNotBeEmpty();
        set.TopicFiles.ShouldAllBe(f => f.Parsed != null);

        var schema = new ContentSchemaValidator().Validate(set);
        var rules = new ContentRuleValidator().Validate(set);

        var errors = schema.Errors.Concat(rules.Errors).ToList();
        errors.ShouldBeEmpty(string.Join(Environment.NewLine, errors));
    }
}
