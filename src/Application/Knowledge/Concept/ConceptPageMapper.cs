using Riok.Mapperly.Abstractions;
using Spine.Domain.Content;

namespace Spine.Application.Knowledge.Concept;

/// <summary>Builds the concept page DTO; text in <c>language</c>, English as the fallback, the slug as a last resort.</summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public sealed partial class ConceptPageMapper(Language language)
{
    [MapPropertyFromSource(nameof(ConceptPageDto.Domain), Use = nameof(DomainRef))]
    [MapPropertyFromSource(nameof(ConceptPageDto.Topic), Use = nameof(TopicRef))]
    [MapPropertyFromSource(nameof(ConceptPageDto.RequestedLanguage), Use = nameof(RequestedLanguage))]
    [MapPropertyFromSource(nameof(ConceptPageDto.Language), Use = nameof(ServedLanguage))]
    [MapPropertyFromSource(nameof(ConceptPageDto.Fallback), Use = nameof(IsFallback))]
    [MapPropertyFromSource(nameof(ConceptPageDto.Title), Use = nameof(Title))]
    [MapPropertyFromSource(nameof(ConceptPageDto.Note), Use = nameof(Note))]
    public partial ConceptPageDto ToPage(ConceptPageRow row);

    private static string LevelValue(Level level) => level.ToDbValue();

    private ConceptRefDto DomainRef(ConceptPageRow row) =>
        new(row.DomainSlug, LocalizedTitle.Of(language, row.DomainTitle, row.DomainTitleUk));

    private ConceptRefDto TopicRef(ConceptPageRow row) =>
        new(row.TopicSlug, LocalizedTitle.Of(language, row.TopicTitle, row.TopicTitleUk));

    private string RequestedLanguage(ConceptPageRow row) => language.ToDbValue();

    private string ServedLanguage(ConceptPageRow row) => (IsFallback(row) ? Language.En : language).ToDbValue();

    private static bool IsFallback(ConceptPageRow row) => row.Requested is null;

    private static string Title(ConceptPageRow row) => Text(row)?.Title ?? row.Slug;

    private static string Note(ConceptPageRow row) => Text(row)?.Note ?? string.Empty;

    private static ConceptTextRow? Text(ConceptPageRow row) => row.Requested ?? row.En;
}
