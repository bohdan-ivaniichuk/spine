namespace Spine.ArchitectureTests;

/// <summary>Locates the repository from the test binaries (walks up until <c>Spine.slnx</c> is found).</summary>
internal static class RepoPaths
{
    public static string Root { get; } = Find();

    public static string Src => Path.Combine(Root, "src");

    public static string Tests => Path.Combine(Root, "tests");

    private static string Find()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Spine.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new DirectoryNotFoundException("Spine.slnx not found above " + AppContext.BaseDirectory);
    }
}
