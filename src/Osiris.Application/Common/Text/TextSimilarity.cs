using System.Globalization;
using System.Text;

namespace Osiris.Application.Common.Text;

/// <summary>
/// Lightweight, dependency-free text similarity used to rank reconciliation candidates. Compares two
/// descriptions by token overlap (Jaccard) after lowercasing, dropping accents and tiny/stopword tokens.
/// Token-set overlap is robust to the reordering/extra tokens common in bank descriptions
/// ("PIX ENVIADO JOAO" vs "Joao - pix") where character-level distance would be misleading.
/// </summary>
public static class TextSimilarity
{
    // Minimal Brazilian-Portuguese stopword set: connectors plus generic transfer tokens that carry no
    // discriminating signal between two statement lines.
    private static readonly HashSet<string> Stopwords = new(StringComparer.Ordinal)
    {
        "de", "da", "do", "das", "dos", "e", "a", "o", "as", "os", "em", "no", "na",
        "pix", "ted", "doc", "tef",
    };

    /// <summary>
    /// Returns the Jaccard similarity of the two descriptions, in [0,1]. Both empty/blank returns 1.0;
    /// exactly one empty returns 0.0.
    /// </summary>
    public static double Jaccard(string? a, string? b)
    {
        var tokensA = Tokenize(a);
        var tokensB = Tokenize(b);

        if (tokensA.Count == 0 && tokensB.Count == 0)
        {
            return 1.0;
        }

        if (tokensA.Count == 0 || tokensB.Count == 0)
        {
            return 0.0;
        }

        var intersection = 0;
        foreach (var token in tokensA)
        {
            if (tokensB.Contains(token))
            {
                intersection++;
            }
        }

        var union = tokensA.Count + tokensB.Count - intersection;
        return union == 0 ? 0.0 : (double)intersection / union;
    }

    private static HashSet<string> Tokenize(string? value)
    {
        var tokens = new HashSet<string>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(value))
        {
            return tokens;
        }

        // Drop accents the same way as Slug so "São José" and "sao jose" tokenize identically.
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
            }
            else if (builder.Length > 0 && builder[^1] != ' ')
            {
                builder.Append(' ');
            }
        }

        foreach (var token in builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (token.Length > 1 && !Stopwords.Contains(token))
            {
                tokens.Add(token);
            }
        }

        return tokens;
    }
}
