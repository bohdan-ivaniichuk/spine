using Spine.Api.Endpoints.Shared;

namespace Spine.Api.Endpoints.Review.AnswerQuestion;

public sealed class AnswerQuestionRequest : LanguageQuery
{
    /// <summary>Session id (route).</summary>
    public Guid Id { get; set; }

    public string QuestionId { get; set; } = string.Empty;

    /// <summary>Index of the chosen option, 0..3.</summary>
    public int SelectedIndex { get; set; }
}
