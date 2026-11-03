using Spine.Domain.Content;
using Spine.Domain.Progress;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Spine.Infrastructure.Persistence.Configurations.Progress;

internal static class ItemColumns
{
    public const string ItemCheck =
        "(item_type = 'question' AND question_id IS NOT NULL AND kata_id IS NULL) OR " +
        "(item_type = 'kata' AND kata_id IS NOT NULL AND question_id IS NULL)";

    public static void ConfigureItem<T>(EntityTypeBuilder<T> e) where T : class
    {
        e.Property<ItemType>("ItemType")
            .HasConversion(v => v.ToDbValue(), v => ItemTypeExtensions.ParseItemType(v))
            .HasColumnType("text").IsRequired();
        e.Property<string?>("QuestionId");
        e.Property<string?>("KataId");
        e.Property<string>("ItemId").HasComputedColumnSql("COALESCE(question_id, kata_id)", stored: true);
        e.HasOne<Question>().WithMany().HasForeignKey("QuestionId").OnDelete(DeleteBehavior.Restrict);
        e.HasOne<Kata>().WithMany().HasForeignKey("KataId").OnDelete(DeleteBehavior.Restrict);
    }
}
