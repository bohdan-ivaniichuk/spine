using System.Text.Json;
using System.Text.Json.Nodes;
using InterviewPrep.Application.Content.Seed;
using Json.Schema;

namespace InterviewPrep.Application.Content.Validation;

/// <summary>Validates the manifest and every topic file against the committed JSON Schemas (draft 2020-12).</summary>
public sealed class ContentSchemaValidator
{
    private static readonly EvaluationOptions Options = new()
    {
        OutputFormat = OutputFormat.List,
        RequireFormatValidation = false,
    };

    public ContentValidationResult Validate(ContentSet set)
    {
        var result = new ContentValidationResult();
        var manifestSchema = JsonSchema.FromText(set.ManifestSchemaJson);
        var contentSchema = JsonSchema.FromText(set.ContentSchemaJson);

        if (set.Manifest is not null)
        {
            Evaluate(manifestSchema, set.Manifest.Node, set.Manifest.RelativePath, result);
        }

        foreach (var file in set.TopicFiles)
        {
            Evaluate(contentSchema, file.Node, file.RelativePath, result);
        }

        return result;
    }

    public static void Evaluate(JsonSchema schema, JsonNode node, string file, ContentValidationResult result)
    {
        var evaluation = schema.Evaluate(JsonSerializer.SerializeToElement(node), Options);
        if (evaluation.IsValid)
        {
            return;
        }

        var any = false;
        foreach (var detail in evaluation.Details ?? [])
        {
            if (detail.IsValid || detail.Errors is null || detail.Errors.Count == 0)
            {
                continue;
            }

            foreach (var (keyword, message) in detail.Errors!)
            {
                any = true;
                result.Add(file, detail.InstanceLocation.ToString(), keyword, message);
            }
        }

        if (!any)
        {
            result.Add(file, "", "schema", "does not match the content schema");
        }
    }
}
