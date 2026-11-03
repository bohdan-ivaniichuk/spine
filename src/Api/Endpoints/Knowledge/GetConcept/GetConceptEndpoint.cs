using Spine.Application.Knowledge.Concept;
using Spine.Application.Knowledge;
using FastEndpoints;

namespace Spine.Api.Endpoints.Knowledge.GetConcept;

/// <summary>
/// GET /api/knowledge/concepts/{domain}/{topic}/{concept} — one concept with its note in the requested language.
/// </summary>
public sealed class GetConceptEndpoint(IKnowledgeQueries queries) : Endpoint<GetConceptRequest, ConceptPageDto>
{
    public override void Configure()
    {
        Get(ApiRoutes.Knowledge.Concept);
        AllowAnonymous();
        Description(b => b
            .Produces<ConceptPageDto>(200)
            .ProducesProblemDetails(400)
            .ProducesProblemDetails(404));
        Summary(s =>
        {
            s.Summary = "Concept page";
            s.Description = "Title and Markdown note in the requested language; falls back to English with "
                + "fallback=true. 404 for unknown or deprecated paths.";
        });
    }

    public override async Task HandleAsync(GetConceptRequest req, CancellationToken ct)
    {
        var concept = await queries.GetConceptAsync(
            req.Domain, req.Topic, req.Concept, req.ParsedLanguage, req.IncludeDeprecated, ct);
        if (concept is null)
        {
            await Send.ResultAsync(TypedResults.Problem(
                title: "Concept not found",
                detail: $"No concept '{req.Domain}/{req.Topic}/{req.Concept}' "
                    + "(deprecated items need includeDeprecated=true).",
                statusCode: StatusCodes.Status404NotFound,
                instance: HttpContext.Request.Path));
            return;
        }

        await Send.OkAsync(concept, ct);
    }
}
