using System.Globalization;
using System.Text;

namespace Osiris.Application.Common.Text;

/// <summary>
/// Builds filesystem-friendly slugs (lowercase ASCII, dash-separated) from user-provided names,
/// dropping accents so Brazilian names such as "Cartão Nubank" become "cartao-nubank".
/// </summary>
public static class Slug
{
    public static string From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "documento";
        }

        var normalized = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        var lastWasDash = false;

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
                lastWasDash = false;
            }
            else if (!lastWasDash)
            {
                builder.Append('-');
                lastWasDash = true;
            }
        }

        var slug = builder.ToString().Trim('-');
        return slug.Length == 0 ? "documento" : slug;
    }
}
