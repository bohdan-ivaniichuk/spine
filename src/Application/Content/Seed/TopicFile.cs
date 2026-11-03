using System.Text.Json;
using System.Text.Json.Serialization;

namespace Spine.Application.Content.Seed;

public sealed record TopicFile(string Domain, string Topic, IReadOnlyList<ConceptSeed> Concepts);
