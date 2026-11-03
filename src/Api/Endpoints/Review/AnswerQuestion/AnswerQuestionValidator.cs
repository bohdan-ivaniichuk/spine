using FastEndpoints;
using FluentValidation;
using Spine.Api.Endpoints.Shared;

namespace Spine.Api.Endpoints.Review.AnswerQuestion;

public sealed class AnswerQuestionValidator : Validator<AnswerQuestionRequest>
{
    public const int MaxOptionIndex = 3;

    public AnswerQuestionValidator()
    {
        RuleFor(r => r.Lang).MustBeSupportedLanguage();
        RuleFor(r => r.QuestionId).NotEmpty();
        RuleFor(r => r.SelectedIndex).InclusiveBetween(0, MaxOptionIndex);
    }
}
