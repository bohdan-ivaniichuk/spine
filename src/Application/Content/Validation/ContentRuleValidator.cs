using InterviewPrep.Application.Content.Seed;

namespace InterviewPrep.Application.Content.Validation;

/// <summary>
/// Rules the JSON Schema cannot express. Rule ids are stable and documented in
/// specs/001-foundation/contracts/import-cli.md.
/// </summary>
public sealed class ContentRuleValidator
{
    public const string ManifestDuplicates = "manifest-duplicates";
    public const string ManifestPair = "manifest-pair";
    public const string FilePath = "file-path";
    public const string DuplicateTopicFile = "duplicate-topic-file";
    public const string ConceptSlugUnique = "concept-slug-unique";
    public const string IdPrefix = "id-prefix";
    public const string IdType = "id-type";
    public const string McqOneCorrect = "mcq-one-correct";
    public const string ConceptTitle = "concept-title";
    public const string DuplicateId = "duplicate-id";
    public const string ManifestMissing = "manifest-missing";

    public ContentValidationResult Validate(ContentSet set)
    {
        var result = new ContentValidationResult();

        var manifest = set.Manifest?.Parsed;
        if (manifest is null)
        {
            if (set.TopicFiles.Count > 0)
            {
                result.Add("manifest.json", "", ManifestMissing, "content/manifest.json is required when topic files exist");
            }

            return result;
        }

        var pairs = ValidateManifest(set.Manifest!, manifest, result);

        var seenTopics = new Dictionary<(string Domain, string Topic), string>();
        var seenIds = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var file in set.TopicFiles)
        {
            if (file.Parsed is null)
            {
                continue; // schema validation already reported it
            }

            ValidateTopicFile(file, file.Parsed, pairs, seenTopics, seenIds, result);
        }

        return result;
    }

    private static HashSet<(string, string)> ValidateManifest(ManifestSource source, ManifestFile manifest, ContentValidationResult result)
    {
        var pairs = new HashSet<(string, string)>();
        var domainSlugs = new HashSet<string>(StringComparer.Ordinal);
        for (var d = 0; d < manifest.Domains.Count; d++)
        {
            var domain = manifest.Domains[d];
            if (!domainSlugs.Add(domain.Slug))
            {
                result.Add(source.RelativePath, $"/domains/{d}/slug", ManifestDuplicates, $"domain slug '{domain.Slug}' is listed twice");
            }

            var topicSlugs = new HashSet<string>(StringComparer.Ordinal);
            for (var t = 0; t < domain.Topics.Count; t++)
            {
                var topic = domain.Topics[t];
                if (!topicSlugs.Add(topic.Slug))
                {
                    result.Add(source.RelativePath, $"/domains/{d}/topics/{t}/slug", ManifestDuplicates, $"topic slug '{topic.Slug}' is listed twice in domain '{domain.Slug}'");
                }

                pairs.Add((domain.Slug, topic.Slug));
            }
        }

        return pairs;
    }

    private static void ValidateTopicFile(
        TopicFileSource source,
        TopicFile file,
        HashSet<(string, string)> manifestPairs,
        Dictionary<(string, string), string> seenTopics,
        Dictionary<string, string> seenIds,
        ContentValidationResult result)
    {
        var path = source.RelativePath;

        if (!manifestPairs.Contains((file.Domain, file.Topic)))
        {
            result.Add(path, "/topic", ManifestPair, $"(domain '{file.Domain}', topic '{file.Topic}') is not listed in manifest.json");
        }

        var expectedPath = $"{file.Domain}/{file.Topic}.json";
        if (!string.Equals(path.Replace('\\', '/'), expectedPath, StringComparison.Ordinal))
        {
            result.Add(path, "", FilePath, $"file must be at '{expectedPath}'");
        }

        if (seenTopics.TryGetValue((file.Domain, file.Topic), out var otherFile))
        {
            result.Add(path, "/topic", DuplicateTopicFile, $"(domain '{file.Domain}', topic '{file.Topic}') is also declared by '{otherFile}'");
        }
        else
        {
            seenTopics[(file.Domain, file.Topic)] = path;
        }

        var conceptSlugs = new HashSet<string>(StringComparer.Ordinal);
        for (var c = 0; c < file.Concepts.Count; c++)
        {
            var concept = file.Concepts[c];
            var cp = $"/concepts/{c}";

            if (!conceptSlugs.Add(concept.Slug))
            {
                result.Add(path, $"{cp}/slug", ConceptSlugUnique, $"concept slug '{concept.Slug}' appears twice in this file");
            }

            if (concept.Translations.TryGetValue("en", out var en) && !string.Equals(en.Title, concept.Title, StringComparison.Ordinal))
            {
                result.Add(path, $"{cp}/title", ConceptTitle, "concept.title must equal translations.en.title");
            }

            var prefix = $"{file.Domain}.{file.Topic}.{concept.Slug}.";

            for (var q = 0; q < concept.Questions.Count; q++)
            {
                var question = concept.Questions[q];
                var qp = $"{cp}/questions/{q}";

                if (!question.Id.StartsWith(prefix, StringComparison.Ordinal))
                {
                    result.Add(path, $"{qp}/id", IdPrefix, $"id must start with '{prefix}'");
                }

                var idType = IdTypeSegment(question.Id);
                if (idType is not null && !string.Equals(idType, question.Type, StringComparison.Ordinal))
                {
                    result.Add(path, $"{qp}/type", IdType, $"type '{question.Type}' does not match the id segment '{idType}'");
                }

                RegisterId(question.Id, path, $"{qp}/id", seenIds, result);

                if (question.Type == "mcq")
                {
                    foreach (var (language, translation) in question.Translations)
                    {
                        var correct = translation.Options?.Count(o => o.Correct) ?? 0;
                        if (correct != 1)
                        {
                            result.Add(path, $"{qp}/translations/{language}/options", McqOneCorrect, $"exactly one option must be correct (found {correct})");
                        }
                    }
                }
            }

            if (concept.Katas is null)
            {
                continue;
            }

            for (var k = 0; k < concept.Katas.Count; k++)
            {
                var kata = concept.Katas[k];
                var kp = $"{cp}/katas/{k}";
                if (!kata.Id.StartsWith(prefix, StringComparison.Ordinal))
                {
                    result.Add(path, $"{kp}/id", IdPrefix, $"id must start with '{prefix}'");
                }

                RegisterId(kata.Id, path, $"{kp}/id", seenIds, result);
            }
        }
    }

    private static void RegisterId(string id, string path, string pointer, Dictionary<string, string> seenIds, ContentValidationResult result)
    {
        if (seenIds.TryGetValue(id, out var other))
        {
            var where = other == path ? "this file" : $"'{other}'";
            result.Add(path, pointer, DuplicateId, $"id '{id}' is also used in {where}");
        }
        else
        {
            seenIds[id] = path;
        }
    }

    /// <summary>Returns "mcq", "open" or "kata" from an id of the form a.b.c.&lt;type&gt;.n; null if malformed.</summary>
    public static string? IdTypeSegment(string id)
    {
        var parts = id.Split('.');
        return parts.Length == 5 ? parts[3] : null;
    }
}
