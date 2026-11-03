using Spine.Application.Content.Summary;
using Spine.Application.Content.Validation;
using DomainEntity = Spine.Domain.Content.Domain;

namespace Spine.Application.Content.Diff;

/// <summary>Root additions and counts produced by <see cref="ImportDiff"/>; existing entities are mutated in place.</summary>
public sealed class ImportChangeSet
{
    public List<DomainEntity> NewDomains { get; } = [];
    public ImportSummary Summary { get; } = new();
    public List<ContentValidationError> Errors { get; } = [];
}
