using FastEndpoints;
using FluentValidation;
using Spine.Api.Endpoints.Shared;

namespace Spine.Api.Endpoints.Knowledge.GetKnowledgeTree;

public sealed class GetKnowledgeTreeValidator : Validator<GetKnowledgeTreeRequest>
{
    public GetKnowledgeTreeValidator()
    {
        RuleFor(r => r.Lang).MustBeSupportedLanguage();
    }
}
