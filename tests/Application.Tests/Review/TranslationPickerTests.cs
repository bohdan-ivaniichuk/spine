using Spine.Application.Review;
using Spine.Domain.Content;

namespace Spine.Application.Tests.Review;

public sealed class TranslationPickerTests
{
    private static readonly McqOption[] En = [new("A", false), new("B", true), new("C", false), new("D", false)];
    private static readonly McqOption[] UkAligned = [new("А", false), new("Б", true), new("В", false), new("Г", false)];

    [Fact]
    public void Aligned_uk_is_served_without_fallback()
    {
        TranslationPicker.Pick(Language.Uk, "en", En, "uk", UkAligned).ShouldBe(("uk", Language.Uk, false));
    }

    [Fact]
    public void Missing_uk_falls_back_to_en()
    {
        TranslationPicker.Pick<string>(Language.Uk, "en", En, null, null).ShouldBe(("en", Language.En, true));
    }

    [Theory]
    [InlineData(2)] // correct option at a different index
    [InlineData(-1)] // three options only
    public void Misaligned_uk_options_fall_back_to_en(int variant)
    {
        McqOption[] uk = variant == -1
            ? [new("А", false), new("Б", true), new("В", false)]
            : [new("А", false), new("Б", false), new("В", true), new("Г", false)];

        TranslationPicker.IsAligned(En, uk).ShouldBeFalse();
        TranslationPicker.Pick(Language.Uk, "en", En, "uk", uk).ShouldBe(("en", Language.En, true));
    }

    [Fact]
    public void Uk_prompt_without_options_falls_back_to_en()
    {
        TranslationPicker.Pick(Language.Uk, "en", En, "uk", null).ShouldBe(("en", Language.En, true));
    }

    [Fact]
    public void En_request_never_reports_fallback()
    {
        TranslationPicker.Pick<string>(Language.En, "en", En, null, null).ShouldBe(("en", Language.En, false));
        TranslationPicker.Pick(Language.En, "en", En, "uk", UkAligned).ShouldBe(("en", Language.En, false));
    }
}
