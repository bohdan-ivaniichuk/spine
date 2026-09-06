namespace InterviewPrep.Domain.Progress;

/// <summary>The single seeded local learner. Stable id so later features and tests can rely on it.</summary>
public static class LocalLearner
{
    public static readonly Guid Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public const string DisplayName = "Local learner";
}
