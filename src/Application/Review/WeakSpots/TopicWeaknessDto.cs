namespace Spine.Application.Review.WeakSpots;

public sealed record TopicWeaknessDto(
    string Domain,
    string Topic,
    int Answers,
    int Wrong,
    double Accuracy,
    DateTimeOffset LastAnsweredAt,
    int WeakQuestions);
