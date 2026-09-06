using FastEndpoints;
using FluentValidation;
using InterviewPrep.Application.Knowledge;

namespace InterviewPrep.Api.Endpoints.Knowledge;

public sealed class GetKnowledgeTreeRequest : LanguageQuery;

public sealed class GetKnowledgeTreeValidator : Validator<GetKnowledgeTreeRequest>
{
    public GetKnowledgeTreeValidator()
    {
        RuleFor(r => r.Lang).MustBeSupportedLanguage();
    }
}

/// <summary>GET /api/knowledge/tree — every domain → topic → concept in taxonomy order with counts.</summary>
public sealed class GetKnowledgeTreeEndpoint(IKnowledgeQueries queries) : Endpoint<GetKnowledgeTreeRequest, KnowledgeTreeDto>
{
    public override void Configure()
    {
        Get("/knowledge/tree");
        AllowAnonymous();
        Description(b => b.Produces<KnowledgeTreeDto>(200).ProducesProblemDetails(400));
        Summary(s =>
        {
            s.Summary = "Knowledge tree";
            s.Description = "All domains, topics and concepts in taxonomy order with question/kata counts. Deprecated items are hidden unless includeDeprecated=true.";
        });
    }

    public override async Task HandleAsync(GetKnowledgeTreeRequest req, CancellationToken ct)
    {
        var tree = await queries.GetTreeAsync(req.ParsedLanguage, req.IncludeDeprecated, ct);
        await Send.OkAsync(tree, ct);
    }
}
