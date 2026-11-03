namespace Spine.Application.Review.WeakSpots;

/// <summary>One concept's answers in the window, plus how many of its questions are weak right now.</summary>
public sealed record ConceptAnswerStats(
    string Domain,
    string Topic,
    string Concept,
    int Answers,
    int Wrong,
    DateTimeOffset LastAnsweredAt,
    int WeakQuestions);
