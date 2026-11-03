namespace Spine.Application.Content.Import;

public interface IContentImporter
{
    Task<ImportResult> ImportAsync(ImportOptions options, CancellationToken ct = default);
}
