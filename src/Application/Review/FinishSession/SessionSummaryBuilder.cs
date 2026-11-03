using Spine.Application.Review.Shared;
using Spine.Domain.Progress;

namespace Spine.Application.Review.FinishSession;

/// <summary>Pure summary math (FR-012): counts, accuracy and weak concepts ranked by misses.</summary>
public static class SessionSummaryBuilder
{
    public static SessionSummaryDto Build(Session session, ScopeDto scope, IReadOnlyList<AnsweredItem> attempts, int dueTomorrow)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(attempts);
        ArgumentOutOfRangeException.ThrowIfNegative(dueTomorrow);

        var answered = attempts.Count;
        var correct = attempts.Count(a => a.IsCorrect);
        var accuracy = answered == 0 ? (double?)null : correct / (double)answered;

        var weak = attempts
            .GroupBy(a => a.Concept)
            .Select(g => new WeakConceptDto(
                g.Key.DomainSlug,
                g.Key.TopicSlug,
                g.Key.ConceptSlug,
                g.Key.ConceptTitle,
                g.Count(a => !a.IsCorrect),
                g.Count()))
            .Where(w => w.Wrong >= 1)
            .OrderByDescending(w => w.Wrong)
            .ThenBy(w => w.Domain, StringComparer.Ordinal)
            .ThenBy(w => w.Topic, StringComparer.Ordinal)
            .ThenBy(w => w.Concept, StringComparer.Ordinal)
            .ToList();

        return new SessionSummaryDto(
            session.Id,
            scope,
            session.StartedAt,
            session.FinishedAt,
            session.QuestionIds.Length,
            answered,
            correct,
            accuracy,
            weak,
            dueTomorrow,
            session.Mode.ToDbValue());
    }
}
