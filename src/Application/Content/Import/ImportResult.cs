using Spine.Application.Content.Summary;

namespace Spine.Application.Content.Import;

public sealed record ImportResult(
    ImportExitCode ExitCode,
    IReadOnlyList<Validation.ContentValidationError> Errors,
    ImportSummary? Summary,
    string? Message = null);
