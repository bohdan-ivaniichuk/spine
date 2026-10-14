using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Spine.Infrastructure.Tests.Content;

/// <summary>
/// A throw-away content directory: <c>schema/</c> copied from the repo, plus whatever manifest and topic files the
/// test writes. Files can be rewritten between imports to simulate content edits.
/// </summary>
internal sealed class ContentDirBuilder : IDisposable
{
    private const string SchemaMarker = "content/schema/content.schema.json";

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public string Dir { get; }

    public ContentDirBuilder()
    {
        Dir = Path.Combine(Path.GetTempPath(), "spine-tests", Guid.NewGuid().ToString("N"));
        var schemaDir = Path.Combine(Dir, "schema");
        Directory.CreateDirectory(schemaDir);
        foreach (var file in Directory.GetFiles(Path.Combine(RepoRoot, "content", "schema"), "*.json"))
        {
            File.Copy(file, Path.Combine(schemaDir, Path.GetFileName(file)));
        }
    }

    public static string RepoRoot
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null)
            {
                if (File.Exists(Path.Combine(dir.FullName, SchemaMarker)))
                {
                    return dir.FullName;
                }

                dir = dir.Parent;
            }

            throw new InvalidOperationException($"Repository root not found above '{AppContext.BaseDirectory}'.");
        }
    }

    /// <summary>
    /// Replaces the copied manifest schema with <c>tests/Fixtures/schema/manifest.schema.json</c>, the schema that allows
    /// <c>titles.uk</c> (a copy of the repo schema; see ManifestTitlesTests for the drift check).
    /// </summary>
    public ContentDirBuilder WithFixtureManifestSchema()
    {
        File.Copy(
            Path.Combine(RepoRoot, "tests", "Fixtures", "schema", "manifest.schema.json"),
            Path.Combine(Dir, "schema", "manifest.schema.json"),
            overwrite: true);
        return this;
    }

    /// <summary>Writes the default manifest (dotnet / clr-memory) and the default topic file.</summary>
    public ContentDirBuilder WithDefaultContent() => WriteManifest(TopicTemplate.Manifest()).WriteTopic(TopicTemplate.ClrMemory());

    public ContentDirBuilder WriteManifest(JsonNode manifest) => WriteManifest(manifest.ToJsonString(WriteOptions));

    public ContentDirBuilder WriteManifest(string json)
    {
        File.WriteAllText(Path.Combine(Dir, "manifest.json"), json);
        return this;
    }

    public ContentDirBuilder WriteTopic(JsonObject topicFile) =>
        WriteTopic(topicFile["domain"]!.GetValue<string>(), topicFile["topic"]!.GetValue<string>(), topicFile.ToJsonString(WriteOptions));

    public ContentDirBuilder WriteTopic(string domain, string topic, string jsonText)
    {
        var dir = Path.Combine(Dir, domain);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, $"{topic}.json"), jsonText);
        return this;
    }

    public void Dispose()
    {
        if (Directory.Exists(Dir))
        {
            Directory.Delete(Dir, recursive: true);
        }
    }
}

/// <summary>
/// Minimal topic file valid against content/schema/content.schema.json. Two concepts; the first has
/// 2 mcq + 1 open + 1 kata with <c>uk</c> translations on the note, mcq.1 and the kata; the second has 2 mcq + 1 open in
/// English only. Every call returns a fresh tree so tests can mutate it freely.
/// </summary>
internal static class TopicTemplate
{
    public const string Domain = "dotnet";
    public const string Topic = "clr-memory";
    public const string File = "dotnet/clr-memory.json";

    public const string StackVsHeap = "stack-vs-heap";
    public const string SpanMemory = "span-memory";

    public const string Mcq1 = "dotnet.clr-memory.stack-vs-heap.mcq.1";
    public const string Mcq2 = "dotnet.clr-memory.stack-vs-heap.mcq.2";
    public const string Open1 = "dotnet.clr-memory.stack-vs-heap.open.1";
    public const string KataId = "dotnet.clr-memory.stack-vs-heap.kata.1";
    public const string SpanMcq1 = "dotnet.clr-memory.span-memory.mcq.1";
    public const string SpanMcq2 = "dotnet.clr-memory.span-memory.mcq.2";
    public const string SpanOpen1 = "dotnet.clr-memory.span-memory.open.1";

    public static readonly string[] QuestionIds = [Mcq1, Mcq2, Open1, SpanMcq1, SpanMcq2, SpanOpen1];

    public const int ConceptCount = 2;
    public const int QuestionCount = 6;
    public const int KataCount = 1;
    /// <summary>3 concept (2 en + 1 uk) + 7 question (6 en + 1 uk) + 2 kata (en + uk).</summary>
    public const int TranslationCount = 12;

    public static JsonObject Manifest() => new()
    {
        ["domains"] = new JsonArray(
            new JsonObject
            {
                ["slug"] = Domain,
                ["title"] = "C# & .NET Runtime",
                ["topics"] = new JsonArray(new JsonObject { ["slug"] = Topic, ["title"] = "Memory & GC" }),
            }),
    };

    /// <summary>The default manifest with <c>titles.uk</c> on the domain and/or the topic (null = omitted).</summary>
    public static JsonObject ManifestWithUkTitles(string? domainUk, string? topicUk)
    {
        var manifest = Manifest();
        var domain = (JsonObject)manifest["domains"]![0]!;
        if (domainUk is not null)
        {
            domain["titles"] = new JsonObject { ["uk"] = domainUk };
        }

        if (topicUk is not null)
        {
            ((JsonObject)domain["topics"]![0]!)["titles"] = new JsonObject { ["uk"] = topicUk };
        }

        return manifest;
    }

    public static JsonObject ClrMemory() => new()
    {
        ["domain"] = Domain,
        ["topic"] = Topic,
        ["concepts"] = new JsonArray(
            Concept(StackVsHeap, "Stack vs Heap", "M", uk: true,
                questions: [Mcq(Mcq1, "Where does a struct field of a class live?", uk: true), Mcq(Mcq2, "Which statement boxes?"), Open(Open1, "Explain value type storage.")],
                katas: [Kata(KataId, uk: true)]),
            Concept(SpanMemory, "Span and Memory", "S", uk: false,
                questions: [Mcq(SpanMcq1, "Why can Span<T> not be a class field?"), Mcq(SpanMcq2, "Which type survives an await?"), Open(SpanOpen1, "Describe zero-allocation parsing.")],
                katas: [])),
    };

    public static string Note(string title) =>
        $"## {title}\n\n" + string.Join(" ", Enumerable.Repeat("This sentence pads the note so that it satisfies the schema's minimum length of two hundred characters.", 3));

    private static JsonObject Concept(string slug, string title, string level, bool uk, JsonObject[] questions, JsonObject[] katas)
    {
        var translations = new JsonObject { ["en"] = new JsonObject { ["title"] = title, ["note"] = Note(title) } };
        if (uk)
        {
            translations["uk"] = new JsonObject { ["title"] = $"{title} (uk)", ["note"] = Note($"{title} (uk)") };
        }

        return new JsonObject
        {
            ["slug"] = slug,
            ["title"] = title,
            ["level"] = level,
            ["translations"] = translations,
            ["questions"] = new JsonArray(questions),
            ["katas"] = new JsonArray(katas),
        };
    }

    private static JsonObject Mcq(string id, string prompt, bool uk = false)
    {
        var translations = new JsonObject { ["en"] = McqTranslation(prompt) };
        if (uk)
        {
            translations["uk"] = McqTranslation($"{prompt} (uk)");
        }

        return new JsonObject
        {
            ["id"] = id,
            ["type"] = "mcq",
            ["level"] = "M",
            ["reviewed"] = true,
            ["translations"] = translations,
            ["tags"] = new JsonArray("memory"),
        };
    }

    public static JsonObject McqTranslation(string prompt, int correctIndex = 1) => new()
    {
        ["prompt"] = prompt,
        ["options"] = new JsonArray(
            Option("Option A", correctIndex == 0),
            Option("Option B", correctIndex == 1),
            Option("Option C", correctIndex == 2),
            Option("Option D", correctIndex == 3)),
        ["explanation"] = "B is right because of the layout rules; A, C and D describe other cases.",
    };

    private static JsonObject Option(string text, bool correct) => new() { ["text"] = text, ["correct"] = correct };

    private static JsonObject Open(string id, string prompt) => new()
    {
        ["id"] = id,
        ["type"] = "open",
        ["level"] = "S",
        ["reviewed"] = true,
        ["translations"] = new JsonObject
        {
            ["en"] = new JsonObject
            {
                ["prompt"] = prompt,
                ["rubric"] = new JsonArray("Mentions the containing variable", "Names a heap case", "Names a stack case"),
                ["explanation"] = "Storage follows the containing variable, not the type.",
            },
        },
        ["tags"] = new JsonArray("memory", "value-types"),
    };

    private static JsonObject Kata(string id, bool uk)
    {
        var translations = new JsonObject { ["en"] = KataTranslation("Implement a struct-based Point with equality.") };
        if (uk)
        {
            translations["uk"] = KataTranslation("Реалізуйте структуру Point з рівністю.");
        }

        return new JsonObject
        {
            ["id"] = id,
            ["title"] = "Point struct",
            ["language"] = "csharp",
            ["reviewed"] = true,
            ["translations"] = translations,
            ["timeLimitMinutes"] = 15,
        };
    }

    private static JsonObject KataTranslation(string task) => new()
    {
        ["task"] = task,
        ["rubric"] = new JsonArray("Is a readonly struct", "Implements IEquatable<Point>", "Overrides GetHashCode"),
        ["referenceSolution"] = "public readonly record struct Point(int X, int Y);",
    };
}
