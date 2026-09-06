using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace InterviewPrep.Application.Content;

/// <summary>
/// SHA-256 over a canonical serialization (fixed property order, no whitespace). Entity hashes cover scalar
/// fields only; each translation is hashed separately, so omitting a language never changes an entity hash.
/// </summary>
public static class ContentHasher
{
    private static readonly JsonSerializerOptions Canonical = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string Hash(params object?[] parts)
    {
        var json = JsonSerializer.Serialize(parts, Canonical);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexStringLower(bytes);
    }
}
