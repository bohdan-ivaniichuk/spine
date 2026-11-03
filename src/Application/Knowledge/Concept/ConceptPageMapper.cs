using Spine.Domain.Content;

namespace Spine.Application.Knowledge.Concept;

/// <summary>Builds the concept page DTO; text in <c>language</c>, English as the fallback, the slug as a last resort.</summary>
public sealed class ConceptPageMapper(Language language)
{
    public ConceptPageDto ToPage(ConceptPageRow row)
    {
        _ = (language, row);
        throw new NotImplementedException();
    }
}
