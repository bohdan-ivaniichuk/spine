namespace Spine.Domain.Progress;

public static class ConceptReadinessExtensions
{
    /// <summary>Wire value: <c>not-started</c>, <c>in-progress</c>, <c>ready</c>.</summary>
    public static string ToWireValue(this ConceptReadiness readiness) => readiness switch
    {
        ConceptReadiness.NotStarted => "not-started",
        ConceptReadiness.InProgress => "in-progress",
        ConceptReadiness.Ready => "ready",
        _ => throw new ArgumentOutOfRangeException(nameof(readiness), readiness, null),
    };
}
