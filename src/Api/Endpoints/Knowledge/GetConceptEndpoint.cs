using FastEndpoints;
using FluentValidation;
using InterviewPrep.Application.Knowledge;

namespace InterviewPrep.Api.Endpoints.Knowledge;

public sealed class GetConceptRequest : LanguageQuery
{
    public string Domain { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Concept { get; set; } = string.Empty;
}

public sealed class GetConceptValidator : Validator<GetConceptRequest>
{
    public GetConceptValidator()
    {
        RuleFor(r => r.Lang).MustBeSupportedLanguage();
        RuleFor(r => r.Domain).NotEmpty();
        RuleFor(r => r.Topic).NotEmpty();
        RuleFor(r => r.Concept).NotEmpty();
    }
}

/// <summary>GET /api/knowledge/concepts/{domain}/{topic}/{concept} — one concept with its note in the requested language.</summary>
public sealed class GetConceptEndpoint(IKnowledgeQueries queries) : Endpoint<GetConceptRequest, ConceptPageDto>
{
    public override void Configure()
    {
        Get("/knowledge/concepts/{domain}/{topic}/{concept}");
        AllowAnonymous();
        Description(b => b.Produces<ConceptPageDto>(200).ProducesProblemDetails(400).ProducesProblemDetails(404));
        Summary(s =>
        {
            s.Summary = "Concept page";
            s.Description = "Title and Markdown note in the requested language; falls back to English with fallback=true. 404 for unknown or deprecated paths.";
        });
    }

    public override async Task HandleAsync(GetConceptRequest req, CancellationToken ct)
    {
        var concept = await queries.GetConceptAsync(req.Domain, req.Topic, req.Concept, req.ParsedLanguage, req.IncludeDeprecated, ct);
        if (concept is null)
        {
            await Send.ResultAsync(TypedResults.Problem(
                title: "Concept not found",
                detail: $"No concept '{req.Domain}/{req.Topic}/{req.Concept}' (deprecated items need includeDeprecated=true).",
                statusCode: StatusCodes.Status404NotFound,
                instance: HttpContext.Request.Path));
            return;
        }

        await Send.OkAsync(concept, ct);
    }
}
