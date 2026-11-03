namespace Spine.Application.Review.Sessions;

public sealed record SessionAnswerDto(string QuestionId, int SelectedIndex, bool IsCorrect);
