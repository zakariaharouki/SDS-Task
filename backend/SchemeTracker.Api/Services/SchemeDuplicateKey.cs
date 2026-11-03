using System.Text.RegularExpressions;

namespace SchemeTracker.Api.Services;

/// <summary>
/// Builds the stored key that the unique index uses to block duplicate schemes (design section 3).
/// Punctuation is deliberately left alone, so "Rd" and "Road" stay different.
/// </summary>
public static partial class SchemeDuplicateKey
{
    public static string Create(string name, string siteAddress) =>
        $"{Normalise(name)}|{Normalise(siteAddress)}";

    private static string Normalise(string value) =>
        RepeatedWhitespace().Replace(value.Trim(), " ").ToLowerInvariant();

    [GeneratedRegex(@"\s+")]
    private static partial Regex RepeatedWhitespace();
}
