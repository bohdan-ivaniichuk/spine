using Spine.Application.Knowledge;
using Spine.Application.Knowledge.Concept;
using Spine.Domain.Content;

namespace Spine.Application.Tests.Knowledge;

public sealed class ConceptPageMapperTests
{
    private static readonly DateTimeOffset Deprecated = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);

    private static ConceptPageRow Row(ConceptTextRow? requested, ConceptTextRow? en, string? domainUk = ".NET (укр)") => new(
        "dotnet", ".NET", domainUk, "clr-memory", "Memory & GC", null,
        "stack-vs-heap", Level.Lead, true, 4, Deprecated, requested, en, 5, 2);

    [Fact]
    [Trait("Requirement", "FR-006")]
    public void Requested_translation_is_served_without_fallback()
    {
        var page = new ConceptPageMapper(Language.Uk).ToPage(
            Row(new ConceptTextRow("Стек і купа", "# Нотатка"), new ConceptTextRow("Stack vs heap", "# Note")));

        page.ShouldBe(new ConceptPageDto(
            new ConceptRefDto("dotnet", ".NET (укр)"),
            new ConceptRefDto("clr-memory", "Memory & GC"),
            "stack-vs-heap",
            "L",
            true,
            4,
            "uk",
            "uk",
            false,
            "Стек і купа",
            "# Нотатка",
            5,
            2,
            Deprecated));
    }

    [Fact]
    [Trait("Requirement", "FR-006")]
    public void Missing_translation_falls_back_to_english()
    {
        var page = new ConceptPageMapper(Language.Uk).ToPage(Row(null, new ConceptTextRow("Stack vs heap", "# Note"), domainUk: null));

        (page.RequestedLanguage, page.Language, page.Fallback).ShouldBe(("uk", "en", true));
        (page.Title, page.Note).ShouldBe(("Stack vs heap", "# Note"));
        page.Domain.Title.ShouldBe(".NET");
    }

    [Fact]
    [Trait("Requirement", "FR-006")]
    public void No_text_at_all_serves_the_slug_and_an_empty_note()
    {
        var page = new ConceptPageMapper(Language.En).ToPage(Row(null, null));

        (page.Title, page.Note, page.Fallback, page.Language).ShouldBe(("stack-vs-heap", string.Empty, true, "en"));
        page.Domain.Title.ShouldBe(".NET");
    }
}
