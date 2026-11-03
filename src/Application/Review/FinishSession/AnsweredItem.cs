namespace Spine.Application.Review.FinishSession;

/// <summary>One recorded answer of a session with its concept.</summary>
public sealed record AnsweredItem(string QuestionId, bool IsCorrect, ConceptRef Concept);
