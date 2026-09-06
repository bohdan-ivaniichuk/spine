using System.Text.Json.Nodes;
using InterviewPrep.Application.Content.Validation;

namespace InterviewPrep.Application.Tests.Content;

public class ContentSchemaValidatorTests
{
    private static string Dump(ContentValidationResult result) => string.Join(Environment.NewLine, result.Errors);

    private static ContentValidationResult ValidateInvalid(string fixture, string relativePath = FixtureJson.TopicPath) =>
        new ContentSchemaValidator().Validate(ContentSetLoader.WithValidManifest((relativePath, ContentSetLoader.ReadInvalid(fixture))));

    [Fact]
    public void Validate_ValidFixture_HasNoErrors()
    {
        var set = ContentSetLoader.LoadValid();

        var result = new ContentSchemaValidator().Validate(set);

        result.Errors.ShouldBeEmpty(Dump(result));
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_CalledTwiceInOneProcess_DoesNotThrow()
    {
        // The importer (and its integration tests) run Validate many times per process; compiling the schema must be repeatable.
        var validator = new ContentSchemaValidator();
        var set = ContentSetLoader.LoadValid();

        Should.NotThrow(() =>
        {
            validator.Validate(set);
            validator.Validate(set);
        });
    }

    [Fact]
    public void Validate_FiveOptions_ReportsMaxItemsOnOptions()
    {
        var result = ValidateInvalid("five-options.json");

        result.Errors.ShouldContain(
            e => e.File == FixtureJson.TopicPath && e.JsonPointer == "/concepts/0/questions/0/translations/en/options" && e.Rule == "maxItems",
            Dump(result));
    }

    [Fact]
    public void Validate_BadId_ReportsPatternOnId()
    {
        var result = ValidateInvalid("bad-id.json");

        result.Errors.ShouldContain(
            e => e.File == FixtureJson.TopicPath && e.JsonPointer == "/concepts/0/questions/0/id" && e.Rule == "pattern",
            Dump(result));
    }

    [Fact]
    public void Validate_RubricOnMcq_ReportsNotOnEnTranslation()
    {
        var result = ValidateInvalid("rubric-on-mcq.json");

        result.Errors.ShouldContain(
            e => e.File == FixtureJson.TopicPath && e.JsonPointer == "/concepts/0/questions/0/translations/en" && e.Rule == "not",
            Dump(result));
    }

    [Fact]
    public void Validate_ShortNote_ReportsMinLengthOnNote()
    {
        var result = ValidateInvalid("short-note.json");

        result.Errors.ShouldContain(
            e => e.File == FixtureJson.TopicPath && e.JsonPointer == "/concepts/0/translations/en/note" && e.Rule == "minLength",
            Dump(result));
    }

    [Fact]
    public void Validate_ExtraProperty_ReportsAdditionalPropertiesOnConcept()
    {
        var result = ValidateInvalid("extra-property.json");

        result.Errors.ShouldContain(
            e => e.File == FixtureJson.TopicPath && e.JsonPointer == "/concepts/0" && e.Rule == "additionalProperties" && e.Message.Contains("difficulty"),
            Dump(result));
    }

    [Fact]
    public void Validate_MissingRequiredProperty_ReportsRequired()
    {
        var set = FixtureJson.WithTopic(t => FixtureJson.Concept(t, 1).Remove("level"));

        var result = new ContentSchemaValidator().Validate(set);

        result.Errors.ShouldContain(
            e => e.File == FixtureJson.TopicPath && e.JsonPointer == "/concepts/1" && e.Rule == "required" && e.Message.Contains("level"),
            Dump(result));
    }

    [Fact]
    public void Validate_MultipleErrorsInOneFile_AreAllReported()
    {
        var node = JsonNode.Parse(ContentSetLoader.ReadInvalid("short-note.json"))!;
        var question = FixtureJson.Question(node, 0, 0);
        question["id"] = "dotnet.clr-memory.stack-vs-heap.mcq.one";
        ((JsonArray)question["translations"]!["en"]!["options"]!).Add(new JsonObject { ["text"] = "Fifth", ["correct"] = false });
        var set = ContentSetLoader.WithValidManifest((FixtureJson.TopicPath, FixtureJson.Text(node)));

        var result = new ContentSchemaValidator().Validate(set);

        result.Errors.ShouldContain(e => e.JsonPointer == "/concepts/0/translations/en/note" && e.Rule == "minLength", Dump(result));
        result.Errors.ShouldContain(e => e.JsonPointer == "/concepts/0/questions/0/id" && e.Rule == "pattern", Dump(result));
        result.Errors.ShouldContain(e => e.JsonPointer == "/concepts/0/questions/0/translations/en/options" && e.Rule == "maxItems", Dump(result));
    }

    [Fact]
    public void Validate_InvalidManifest_ReportsAgainstManifestFile()
    {
        var manifest = FixtureJson.ManifestNode();
        ((JsonObject)manifest["domains"]![1]!).Remove("topics");
        var set = ContentSetLoader.FromFiles(ContentSetLoader.ValidContentDir, FixtureJson.Text(manifest), (FixtureJson.TopicPath, ContentSetLoader.ReadValidTopic()));

        var result = new ContentSchemaValidator().Validate(set);

        result.Errors.ShouldContain(
            e => e.File == "manifest.json" && e.JsonPointer == "/domains/1" && e.Rule == "required" && e.Message.Contains("topics"),
            Dump(result));
        result.Errors.ShouldAllBe(e => e.File == "manifest.json", Dump(result));
    }

    [Fact]
    public void Validate_TwoCorrectOptions_PassesSchema()
    {
        // The schema cannot count correct options; that is ContentRuleValidator's job (see ContentRuleValidatorTests).
        var result = ValidateInvalid("two-correct.json");

        result.Errors.ShouldBeEmpty(Dump(result));
    }

    [Fact]
    public void Validate_UnknownTopic_PassesSchema()
    {
        var result = ValidateInvalid("unknown-topic.json", "dotnet/nonexistent.json");

        result.Errors.ShouldBeEmpty(Dump(result));
    }

    [Fact]
    public void Validate_FiveOptions_ReportsOnlyTheLeafViolation()
    {
        // Contract (T033): only failing leaf nodes are reported. Results from `if` conditions and from inside `not`
        // subschemas are not violations, and aggregate nodes (properties/items/allOf/then) only repeat their children.
        var result = ValidateInvalid("five-options.json");

        result.Errors.Count.ShouldBe(1, Dump(result));
    }
}
