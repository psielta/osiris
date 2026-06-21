namespace Osiris.Infrastructure.Gemini;

/// <summary>
/// Configuration for the Gemini AI client used to extract transactions from statement PDFs.
/// The <see cref="ApiKey"/> is a secret supplied via environment/.env (never committed).
/// </summary>
public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";

    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "gemini-3.5-flash";

    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/";

    public int TimeoutSeconds { get; set; } = 100;
}
