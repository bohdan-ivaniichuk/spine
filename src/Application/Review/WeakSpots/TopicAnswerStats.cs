namespace Spine.Application.Review.WeakSpots;

/// <summary>A topic's figures: the sums of its concepts' answers, wrong answers and weak questions.</summary>
public sealed record TopicAnswerStats(
    string Domain,
    string Topic,
    int Answers,
    int Wrong,
    DateTimeOffset LastAnsweredAt,
    int WeakQuestions);
