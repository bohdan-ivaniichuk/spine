namespace Spine.ArchitectureTests;

/// <summary>FR-008: one top-level type per <c>src</c> file, named like the file; enum helpers in a sibling
/// <c>&lt;Enum&gt;Extensions.cs</c>; no <c>src</c> file above 250 lines. EF migrations are generated and excluded.</summary>
public sealed class SourceFileRulesTests
{
    private const int MaxLines = 250;

    [Fact]
    [Trait("Requirement", "FR-008")]
    public void Scanner_counts_namespace_level_types_only()
    {
        const string source = """
            using System;

            namespace Spine.Sample;

            /// <summary>class Ignored in a comment</summary>
            public sealed record Row(int Id, string Name);

            public static partial class Helpers
            {
                private const string Text = "public class NotAType {";

                private sealed class Nested
                {
                    public enum Inner { A }
                }
            }

            internal enum Mode
            {
                On,
                Off,
            }

            public interface IThing<T> where T : class;
            """;

        SourceLayout.TopLevelTypes(source).ShouldBe(["Row", "Helpers", "Mode", "IThing"]);
    }

    [Fact]
    [Trait("Requirement", "FR-008")]
    public void Scanner_counts_types_inside_a_block_namespace()
    {
        const string source = """
            namespace Spine.Sample
            {
                public class A { public class B { } }

                public readonly record struct C(int X);
            }
            """;

        SourceLayout.TopLevelTypes(source).ShouldBe(["A", "C"]);
    }

    [Fact]
    [Trait("Requirement", "FR-008")]
    public void Scanner_reads_the_extended_types()
    {
        const string source = """
            namespace Spine.Sample;

            public static class LevelExtensions
            {
                public static string ToDbValue(this Level level) => level.ToString();

                public static bool IsHigh(this Level? level, int bar) => level is not null;

                public static Level Parse(string value) => default; // this Level is not a parameter
            }
            """;

        SourceLayout.ExtendedTypes(source).ShouldBe(["Level", "Level"]);
    }

    [Fact]
    [Trait("Requirement", "FR-008")]
    public void Every_src_file_holds_one_type_named_like_the_file_and_stays_short()
    {
        var violations = new List<string>();
        foreach (var file in SourceFiles.In(RepoPaths.Src).Where(f => Path.GetFileName(f) != "Program.cs"))
        {
            var source = File.ReadAllText(file);
            var types = SourceLayout.TopLevelTypes(source);
            var where = Path.GetRelativePath(RepoPaths.Root, file);
            if (types.Count != 1 || types[0] != Path.GetFileNameWithoutExtension(file))
            {
                violations.Add($"{where}: declares [{string.Join(", ", types)}]");
            }

            var lines = File.ReadAllLines(file).Length;
            if (lines > MaxLines)
            {
                violations.Add($"{where}: {lines} lines (max {MaxLines})");
            }
        }

        violations.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Requirement", "FR-008")]
    public void Enum_helpers_live_in_the_sibling_extensions_file()
    {
        var files = SourceFiles.In(RepoPaths.Src).ToList();
        var enums = files
            .Where(f => File.ReadAllText(f).Contains(" enum " + Path.GetFileNameWithoutExtension(f), StringComparison.Ordinal))
            .ToDictionary(f => Path.GetFileNameWithoutExtension(f), f => Path.GetDirectoryName(f)!, StringComparer.Ordinal);
        enums.ShouldNotBeEmpty();

        var violations = new List<string>();
        foreach (var file in files)
        {
            foreach (var extended in SourceLayout.ExtendedTypes(File.ReadAllText(file)).Distinct())
            {
                if (enums.TryGetValue(extended, out var enumDir)
                    && (Path.GetFileName(file) != extended + "Extensions.cs" || Path.GetDirectoryName(file) != enumDir))
                {
                    violations.Add($"{Path.GetRelativePath(RepoPaths.Root, file)}: extends enum {extended}");
                }
            }
        }

        violations.ShouldBeEmpty();
    }
}
