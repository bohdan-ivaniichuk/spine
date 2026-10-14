using Spine.Domain.Content;

namespace Spine.Domain.Progress;

/// <summary>The learner who owns progress. MVP seeds exactly one (<see cref="LocalLearner"/>).</summary>
public sealed class Learner
{
    public Guid Id { get; set; }
    public required string DisplayName { get; set; }
    public Language PreferredLanguage { get; set; } = Language.En;
    public DateTimeOffset CreatedAt { get; set; }
}
