namespace Spine.Application.Review.AnswerQuestion;

public sealed record AnswerFeedbackDto(
    string QuestionId,
    int SelectedIndex,
    bool IsCorrect,
    int CorrectIndex,
    string Explanation,
    DateTimeOffset NextDueAt,
    int IntervalDays,
    string Language,
    bool Fallback);
