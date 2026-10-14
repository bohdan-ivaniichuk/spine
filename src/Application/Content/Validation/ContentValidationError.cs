namespace Spine.Application.Content.Validation;

/// <summary>One contract violation: file, JSON pointer inside it, the rule (schema keyword or rule id) and a message.</summary>
public sealed record ContentValidationError(string File, string JsonPointer, string Rule, string Message)
{
    public override string ToString() => $"{File}:{JsonPointer} [{Rule}] {Message}";
}

public sealed class ContentValidationResult
{
    private readonly List<ContentValidationError> _errors = [];

    public IReadOnlyList<ContentValidationError> Errors => _errors;
    public bool IsValid => _errors.Count == 0;

    public void Add(ContentValidationError error) => _errors.Add(error);
    public void Add(string file, string pointer, string rule, string message) => _errors.Add(new ContentValidationError(file, pointer, rule, message));
    public void AddRange(IEnumerable<ContentValidationError> errors) => _errors.AddRange(errors);
}
