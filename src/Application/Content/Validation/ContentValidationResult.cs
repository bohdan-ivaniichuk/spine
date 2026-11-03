namespace Spine.Application.Content.Validation;

public sealed class ContentValidationResult
{
    private readonly List<ContentValidationError> _errors = [];

    public IReadOnlyList<ContentValidationError> Errors => _errors;
    public bool IsValid => _errors.Count == 0;

    public void Add(ContentValidationError error) => _errors.Add(error);
    public void Add(string file, string pointer, string rule, string message) => _errors.Add(new ContentValidationError(file, pointer, rule, message));
    public void AddRange(IEnumerable<ContentValidationError> errors) => _errors.AddRange(errors);
}
