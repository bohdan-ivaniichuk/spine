namespace Spine.Infrastructure.Review.Sessions;

/// <summary>Stored in <c>attempts.answer</c>: the option index plus its text at answer time.</summary>
public sealed record McqAnswer(int SelectedIndex, string SelectedText);
