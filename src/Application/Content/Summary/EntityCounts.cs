namespace Spine.Application.Content.Summary;

public sealed class EntityCounts
{
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Unchanged { get; set; }
    public int Deprecated { get; set; }

    public void Add(EntityCounts other)
    {
        Created += other.Created;
        Updated += other.Updated;
        Unchanged += other.Unchanged;
        Deprecated += other.Deprecated;
    }
}
