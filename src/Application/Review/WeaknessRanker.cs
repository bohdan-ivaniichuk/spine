using Spine.Domain.Progress;

namespace Spine.Application.Review;

/// <summary>One concept's answers in the window, plus how many of its questions are weak right now.</summary>
public sealed record ConceptAnswerStats(
    string Domain,
    string Topic,
    string Concept,
    int Answers,
    int Wrong,
    DateTimeOffset LastAnsweredAt,
    int WeakQuestions);

/// <summary>A topic's figures: the sums of its concepts' answers, wrong answers and weak questions.</summary>
public sealed record TopicAnswerStats(
    string Domain,
    string Topic,
    int Answers,
    int Wrong,
    DateTimeOffset LastAnsweredAt,
    int WeakQuestions);

public sealed record WeaknessRanking(IReadOnlyList<ConceptAnswerStats> Concepts, IReadOnlyList<TopicAnswerStats> Topics);

/// <summary>
/// Pure weak-spots ranking (feature 006): entries with at least <see cref="WeaknessRule.MinAnswers"/>
/// answers, lowest accuracy first, then more wrong answers, then the most recent answer, then slug; capped at the
/// rule's maxima. Topics are ranked on their concepts' combined figures.
/// </summary>
public static class WeaknessRanker
{
    /// <summary>Share of right answers, 0..1.</summary>
    public static double Accuracy(int answers, int wrong) => answers == 0 ? 0 : (answers - wrong) / (double)answers;

    public static WeaknessRanking Rank(IReadOnlyList<ConceptAnswerStats> concepts)
    {
        ArgumentNullException.ThrowIfNull(concepts);

        var rankedConcepts = concepts
            .Where(c => c.Answers >= WeaknessRule.MinAnswers)
            .Order(Comparer<ConceptAnswerStats>.Create((a, b) =>
                Compare(a.Answers, a.Wrong, a.LastAnsweredAt, b.Answers, b.Wrong, b.LastAnsweredAt) is var c and not 0
                    ? c
                    : string.CompareOrdinal($"{a.Domain}/{a.Topic}/{a.Concept}", $"{b.Domain}/{b.Topic}/{b.Concept}")))
            .Take(WeaknessRule.MaxConcepts)
            .ToList();

        var rankedTopics = concepts
            .GroupBy(c => (c.Domain, c.Topic))
            .Select(g => new TopicAnswerStats(
                g.Key.Domain,
                g.Key.Topic,
                g.Sum(c => c.Answers),
                g.Sum(c => c.Wrong),
                g.Max(c => c.LastAnsweredAt),
                g.Sum(c => c.WeakQuestions)))
            .Where(t => t.Answers >= WeaknessRule.MinAnswers)
            .Order(Comparer<TopicAnswerStats>.Create((a, b) =>
                Compare(a.Answers, a.Wrong, a.LastAnsweredAt, b.Answers, b.Wrong, b.LastAnsweredAt) is var c and not 0
                    ? c
                    : string.CompareOrdinal($"{a.Domain}/{a.Topic}", $"{b.Domain}/{b.Topic}")))
            .Take(WeaknessRule.MaxTopics)
            .ToList();

        return new WeaknessRanking(rankedConcepts, rankedTopics);
    }

    /// <summary>
    /// Lower accuracy first (compared by cross-multiplication, so floating point never decides a tie), then more wrong
    /// answers, then the most recent answer.
    /// </summary>
    private static int Compare(int answersA, int wrongA, DateTimeOffset lastA, int answersB, int wrongB, DateTimeOffset lastB)
    {
        var byAccuracy = ((long)(answersA - wrongA) * answersB).CompareTo((long)(answersB - wrongB) * answersA);
        if (byAccuracy != 0)
        {
            return byAccuracy;
        }

        var byWrong = wrongB.CompareTo(wrongA);
        return byWrong != 0 ? byWrong : lastB.CompareTo(lastA);
    }
}
