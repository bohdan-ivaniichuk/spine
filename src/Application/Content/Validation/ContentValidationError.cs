namespace Spine.Application.Content.Validation;

/// <summary>One contract violation: file, JSON pointer inside it, the rule (schema keyword or rule id) and a message.</summary>
public sealed record ContentValidationError(string File, string JsonPointer, string Rule, string Message)
{
    public override string ToString() => $"{File}:{JsonPointer} [{Rule}] {Message}";
}
