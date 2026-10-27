namespace Spine.Domain.Progress;

/// <summary>Fixed parameters of the weak-spots ranking (feature 006): a 30-day window, at least 3 answers, top 20 / top 10.</summary>
public static class WeaknessRule
{
    public const int WindowDays = 30;
    public const int MinAnswers = 3;
    public const int MaxConcepts = 20;
    public const int MaxTopics = 10;
}
