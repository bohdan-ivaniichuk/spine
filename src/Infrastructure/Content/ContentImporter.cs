using InterviewPrep.Application.Content;
using InterviewPrep.Application.Content.Validation;
using InterviewPrep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InterviewPrep.Infrastructure.Content;

/// <summary>
/// The `import-content` use case: validate everything first (no database), then apply the diff in one transaction
/// guarded by a transaction-scoped advisory lock. Never touches progress tables.
/// </summary>
public sealed class ContentImporter(AppDbContext db, ILogger<ContentImporter> logger) : IContentImporter
{
    /// <summary>Arbitrary but fixed key for pg_try_advisory_xact_lock.</summary>
    public const long AdvisoryLockKey = 7411001;

    public async Task<ImportResult> ImportAsync(ImportOptions options, CancellationToken ct = default)
    {
        if (!Directory.Exists(options.ContentDir))
        {
            return new ImportResult(ImportExitCode.Failure, [], null, $"content directory not found: {options.ContentDir}");
        }

        if (!ContentFileSystem.HasAnyContent(options.ContentDir))
        {
            return new ImportResult(ImportExitCode.Success, [], null, $"nothing to import in {options.ContentDir}");
        }

        var (set, parseErrors) = ContentFileSystem.Load(options.ContentDir);
        if (set is null)
        {
            return new ImportResult(ImportExitCode.ValidationFailed, parseErrors.Errors, null);
        }

        var errors = new List<ContentValidationError>(parseErrors.Errors);
        errors.AddRange(new ContentSchemaValidator().Validate(set).Errors);
        errors.AddRange(new ContentRuleValidator().Validate(set).Errors);
        if (errors.Count > 0)
        {
            return new ImportResult(ImportExitCode.ValidationFailed, errors, null);
        }

        if (options.ValidateOnly)
        {
            return new ImportResult(ImportExitCode.Success, [], null, "valid");
        }

        var plan = ImportPlanBuilder.Build(set);
        var now = DateTimeOffset.UtcNow;

        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            // A retried attempt must start from a clean tracker: tracked, already-mutated entities would
            // make the diff report "unchanged" while still carrying Added/Modified state from the failed attempt.
            db.ChangeTracker.Clear();
            await using var tx = await db.Database.BeginTransactionAsync(ct);

            var locked = await db.Database
                .SqlQuery<bool>($"SELECT pg_try_advisory_xact_lock({AdvisoryLockKey}) AS \"Value\"")
                .SingleAsync(ct);
            if (!locked)
            {
                return new ImportResult(ImportExitCode.AlreadyInProgress, [], null, "import already in progress");
            }

            var existing = await db.Domains
                .Include(d => d.Topics).ThenInclude(t => t.Concepts).ThenInclude(c => c.Translations)
                .Include(d => d.Topics).ThenInclude(t => t.Concepts).ThenInclude(c => c.Questions).ThenInclude(q => q.Translations)
                .Include(d => d.Topics).ThenInclude(t => t.Concepts).ThenInclude(c => c.Katas).ThenInclude(k => k.Translations)
                .AsSplitQuery()
                .ToListAsync(ct);

            var change = ImportDiff.Compute(existing, plan, now);
            if (change.Errors.Count > 0)
            {
                await tx.RollbackAsync(ct);
                return new ImportResult(ImportExitCode.ValidationFailed, change.Errors, null);
            }

            db.Domains.AddRange(change.NewDomains);
            var written = await db.SaveChangesAsync(ct);

            if (options.DryRun)
            {
                await tx.RollbackAsync(ct);
                logger.LogInformation("Dry run: rolled back {Rows} row change(s)", written);
                return new ImportResult(ImportExitCode.Success, [], change.Summary, "dry run — nothing written");
            }

            await tx.CommitAsync(ct);
            logger.LogInformation("Import committed: {Rows} row change(s)", written);
            return new ImportResult(ImportExitCode.Success, [], change.Summary);
        });
    }
}
