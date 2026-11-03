using System.Text.RegularExpressions;

namespace Spine.ArchitectureTests;

/// <summary>A light C# source scanner for the file-layout rules (comments and string literals are ignored).</summary>
internal static partial class SourceLayout
{
    /// <summary>Names of the types declared at namespace level, in source order; nested types are not counted.</summary>
    public static IReadOnlyList<string> TopLevelTypes(string source)
    {
        var code = StripCommentsAndStrings(source);
        var topLevel = TopLevelPositions(code);
        return TypeDeclaration().Matches(code)
            .Where(m => topLevel[m.Index])
            .Select(m => m.Groups["name"].Value)
            .ToList();
    }

    /// <summary>The types extended by the file's extension methods (the type of each <c>this</c> parameter).</summary>
    public static IReadOnlyList<string> ExtendedTypes(string source) =>
        ThisParameter().Matches(StripCommentsAndStrings(source))
            .Select(m => m.Groups["type"].Value.Split('.')[^1])
            .ToList();

    /// <summary>Blanks comments and literals out (keeping offsets), so their text never looks like code.</summary>
    private static string StripCommentsAndStrings(string source) =>
        CommentOrLiteral().Replace(source, m => new string(' ', m.Length));

    /// <summary>For each offset: true when every enclosing brace belongs to a block namespace.</summary>
    private static bool[] TopLevelPositions(string code)
    {
        var result = new bool[code.Length + 1];
        var stack = new Stack<bool>();
        var nonNamespaceDepth = 0;
        for (var i = 0; i < code.Length; i++)
        {
            result[i] = nonNamespaceDepth == 0;
            if (code[i] == '{')
            {
                var isNamespace = NamespaceHeader().IsMatch(code.AsSpan(0, i));
                stack.Push(isNamespace);
                nonNamespaceDepth += isNamespace ? 0 : 1;
            }
            else if (code[i] == '}' && stack.Count > 0 && !stack.Pop())
            {
                nonNamespaceDepth--;
            }
        }

        result[code.Length] = nonNamespaceDepth == 0;
        return result;
    }

    [GeneratedRegex("""//[^\n]*|/\*[\s\S]*?\*/|\$*\"\"\"[\s\S]*?\"\"\"|\$?@\$?"(?:[^"]|"")*"|\$?"(?:\\.|[^"\\\n])*"|'(?:\\.|[^'\\\n])'""")]
    private static partial Regex CommentOrLiteral();

    [GeneratedRegex(@"\b(?:record\s+(?:struct|class)|class|struct|interface|enum|record)\s+(?<name>[A-Za-z_]\w*)")]
    private static partial Regex TypeDeclaration();

    [GeneratedRegex(@"\bnamespace\s+[\w.]+\s*$")]
    private static partial Regex NamespaceHeader();

    [GeneratedRegex(@"\(\s*this\s+(?<type>[\w.]+)(?:<[^>]*>)?\??\s+\w+")]
    private static partial Regex ThisParameter();
}
