using System.Reflection;
using System.Xml.Linq;

namespace Spine.ArchitectureTests;

/// <summary>
/// Layering (constitution VII, FR-009): Domain ← Application ← Infrastructure ← Api; no layer references a higher one,
/// checked both on the compiled assemblies and on the project files.
/// </summary>
public sealed class LayerDependencyTests
{
    private static readonly string[] Layers = ["Spine.Domain", "Spine.Application", "Spine.Infrastructure", "Spine.Api"];

    public static TheoryData<string> LayerNames() => new(Layers);

    [Theory]
    [MemberData(nameof(LayerNames))]
    public void Compiled_assemblies_reference_only_lower_layers(string layer)
    {
        var higher = Layers.SkipWhile(l => l != layer).Skip(1).ToHashSet(StringComparer.Ordinal);

        var referenced = Assembly.Load(layer).GetReferencedAssemblies().Select(a => a.Name!).ToList();

        referenced.Where(higher.Contains).ShouldBeEmpty($"{layer} must not reference {string.Join(", ", higher)}");
    }

    [Theory]
    [MemberData(nameof(LayerNames))]
    public void Project_files_reference_only_lower_layers(string layer)
    {
        var higher = Layers.SkipWhile(l => l != layer).Skip(1).ToHashSet(StringComparer.Ordinal);
        var project = Path.Combine(RepoPaths.Src, layer["Spine.".Length..], layer + ".csproj");

        var references = XDocument.Load(project).Descendants("ProjectReference")
            .Select(r => Path.GetFileNameWithoutExtension((string)r.Attribute("Include")!))
            .ToList();

        references.Where(higher.Contains).ShouldBeEmpty($"{project} must not reference {string.Join(", ", higher)}");
    }
}
