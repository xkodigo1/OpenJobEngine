using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenJobEngine.Infrastructure.Services;

internal static partial class TextCanonicalizer
{
    private static readonly Regex MultiWhitespaceRegex = MultiWhitespace();
    private static readonly Regex NonWordRegex = NonWord();

    public static string Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var collapsed = MultiWhitespaceRegex.Replace(value.Trim(), " ");
        return collapsed;
    }

    public static string CanonicalizeKeyPart(string? value)
    {
        var cleaned = Clean(value).ToLowerInvariant();
        if (cleaned.Length == 0)
        {
            return "unknown";
        }

        var normalized = cleaned.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        var withoutDiacritics = builder.ToString().Normalize(NormalizationForm.FormC);
        var compact = NonWordRegex.Replace(withoutDiacritics, " ");
        var result = MultiWhitespaceRegex.Replace(compact, "-").Trim('-');

        return string.IsNullOrWhiteSpace(result) ? "unknown" : result;
    }

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex MultiWhitespace();

    [GeneratedRegex(@"[^a-z0-9]+", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex NonWord();
}
