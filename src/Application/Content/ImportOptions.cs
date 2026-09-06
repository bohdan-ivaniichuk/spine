namespace InterviewPrep.Application.Content;

public sealed record ImportOptions(string ContentDir, bool ValidateOnly = false, bool DryRun = false);

public enum ImportExitCode
{
    Success = 0,
    ValidationFailed = 1,
    AlreadyInProgress = 2,
    Failure = 3,
}

public sealed record ImportResult(
    ImportExitCode ExitCode,
    IReadOnlyList<Validation.ContentValidationError> Errors,
    ImportSummary? Summary,
    string? Message = null);

public interface IContentImporter
{
    Task<ImportResult> ImportAsync(ImportOptions options, CancellationToken ct = default);
}
