namespace Spine.ArchitectureTests;

/// <summary>C# sources under a folder, without build output and EF migrations; lines with a "file:line" locator.</summary>
internal static class SourceFiles
{
    public static IEnumerable<string> In(string folder) =>
        Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !f.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal);

    public static IEnumerable<(string Text, string Where)> Lines(string file) =>
        File.ReadAllLines(file).Select((text, i) => (text, $"{Path.GetRelativePath(RepoPaths.Root, file)}:{i + 1}"));
}
