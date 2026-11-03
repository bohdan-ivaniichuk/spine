namespace Spine.Application.Content.Summary;

/// <summary>Per-file and total counts of created / updated / unchanged / deprecated per entity kind.</summary>
public sealed class ImportSummary
{
    public SortedDictionary<string, Dictionary<string, EntityCounts>> Files { get; } = new(StringComparer.Ordinal);

    public EntityCounts For(string file, string kind)
    {
        if (!Files.TryGetValue(file, out var kinds))
        {
            kinds = new Dictionary<string, EntityCounts>(StringComparer.Ordinal);
            Files[file] = kinds;
        }

        if (!kinds.TryGetValue(kind, out var counts))
        {
            counts = new EntityCounts();
            kinds[kind] = counts;
        }

        return counts;
    }

    public Dictionary<string, EntityCounts> Totals()
    {
        var totals = EntityKinds.All.ToDictionary(k => k, _ => new EntityCounts(), StringComparer.Ordinal);
        foreach (var kinds in Files.Values)
        {
            foreach (var (kind, counts) in kinds)
            {
                totals[kind].Add(counts);
            }
        }

        return totals;
    }

    public bool HasChanges => Files.Values.SelectMany(k => k.Values).Any(c => c.Created + c.Updated + c.Deprecated > 0);

    public string Render()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("file                                   kind          created  updated  unchanged  deprecated");
        foreach (var (file, kinds) in Files)
        {
            foreach (var kind in EntityKinds.All)
            {
                if (!kinds.TryGetValue(kind, out var c))
                {
                    continue;
                }

                sb.AppendLine($"{file,-38} {kind,-12} {c.Created,8} {c.Updated,8} {c.Unchanged,10} {c.Deprecated,11}");
            }
        }

        sb.AppendLine(new string('-', 92));
        foreach (var (kind, c) in Totals())
        {
            sb.AppendLine($"{"TOTAL",-38} {kind,-12} {c.Created,8} {c.Updated,8} {c.Unchanged,10} {c.Deprecated,11}");
        }

        return sb.ToString();
    }
}
