using Spine.Application.Knowledge;
using Spine.Domain.Content;

namespace Spine.Application.Tests.Knowledge;

public sealed class LocalizedTitleTests
{
    [Fact]
    public void English_request_serves_english_without_fallback()
    {
        LocalizedTitle.Pick(Language.En, "Memory & GC", "Пам'ять і GC").ShouldBe(("Memory & GC", false));
    }

    [Fact]
    public void Uk_request_with_uk_title_serves_uk()
    {
        LocalizedTitle.Pick(Language.Uk, "Memory & GC", "Пам'ять і GC").ShouldBe(("Пам'ять і GC", false));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Uk_request_without_uk_title_falls_back_to_english(string? uk)
    {
        LocalizedTitle.Pick(Language.Uk, "Memory & GC", uk).ShouldBe(("Memory & GC", true));
        LocalizedTitle.Of(Language.Uk, "Memory & GC", uk).ShouldBe("Memory & GC");
    }
}
