using System.Globalization;
using System.Text.Json;

namespace Osiris.Application.Features.AiAssistant.Tools;

/// <summary>Single JSON serializer used by every tool to shape its compact, web-cased output.</summary>
internal static class AiToolJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Serialize(object value) => JsonSerializer.Serialize(value, Options);
}

/// <summary>
/// Lenient readers for tool arguments. The model may send numbers as strings (or vice versa), so these
/// accept both forms and return null/fallback when absent or malformed instead of throwing.
/// </summary>
internal static class AiToolArguments
{
    public static string? GetString(JsonElement arguments, string name)
    {
        return arguments.ValueKind == JsonValueKind.Object
            && arguments.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    public static bool GetBool(JsonElement arguments, string name, bool fallback = false)
    {
        if (arguments.ValueKind == JsonValueKind.Object && arguments.TryGetProperty(name, out var value))
        {
            if (value.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                return value.GetBoolean();
            }

            if (value.ValueKind == JsonValueKind.String && bool.TryParse(value.GetString(), out var parsed))
            {
                return parsed;
            }
        }

        return fallback;
    }

    public static int? GetInt(JsonElement arguments, string name)
    {
        if (arguments.ValueKind == JsonValueKind.Object && arguments.TryGetProperty(name, out var value))
        {
            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
            {
                return number;
            }

            if (value.ValueKind == JsonValueKind.String
                && int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var text))
            {
                return text;
            }
        }

        return null;
    }

    public static decimal? GetDecimal(JsonElement arguments, string name)
    {
        if (arguments.ValueKind == JsonValueKind.Object && arguments.TryGetProperty(name, out var value))
        {
            if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
            {
                return number;
            }

            if (value.ValueKind == JsonValueKind.String
                && decimal.TryParse(value.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var text))
            {
                return text;
            }
        }

        return null;
    }

    public static Guid? GetGuid(JsonElement arguments, string name)
    {
        return Guid.TryParse(GetString(arguments, name), out var value) ? value : null;
    }

    public static DateOnly? GetDate(JsonElement arguments, string name)
    {
        return DateOnly.TryParse(
            GetString(arguments, name),
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var value)
            ? value
            : null;
    }

    /// <summary>Resolves a (year, month) competence from an optional ISO reference date, falling back to today.</summary>
    public static (int Year, int Month) ResolveMonth(JsonElement arguments, DateOnly today, string name = "referenceDate")
    {
        var date = GetDate(arguments, name) ?? today;
        return (date.Year, date.Month);
    }
}
