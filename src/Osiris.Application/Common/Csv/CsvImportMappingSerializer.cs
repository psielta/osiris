using System.Text.Json;

namespace Osiris.Application.Common.Csv;

/// <summary>
/// Serializes a <see cref="CsvImportMapping"/> to/from the opaque payload stored on a
/// <c>CsvImportPreference</c> (and exchanged with clients).
/// </summary>
public static class CsvImportMappingSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Serialize(CsvImportMapping mapping) => JsonSerializer.Serialize(mapping, Options);

    public static CsvImportMapping? Deserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<CsvImportMapping>(json, Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
