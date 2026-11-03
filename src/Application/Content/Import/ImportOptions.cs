namespace Spine.Application.Content.Import;

public sealed record ImportOptions(string ContentDir, bool ValidateOnly = false, bool DryRun = false);
