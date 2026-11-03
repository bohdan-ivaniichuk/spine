namespace Spine.Application.Content.Plan;

/// <summary>The desired state of the content graph, built from the manifest and the topic files.</summary>
public sealed class ImportPlan
{
    public const string ManifestFile = "manifest.json";

    public List<PlanDomain> Domains { get; } = [];
}
