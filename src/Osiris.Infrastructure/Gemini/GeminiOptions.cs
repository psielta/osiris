namespace Osiris.Infrastructure.Gemini;

/// <summary>
/// Configuration for the Gemini AI client used to extract transactions from statement PDFs.
/// The <see cref="ApiKey"/> is a secret supplied via environment/.env (never committed).
/// </summary>
public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";

    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Model used by the existing PDF statement extractor. Kept unchanged for parity.</summary>
    public string Model { get; set; } = "gemini-3.5-flash";

    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/";

    /// <summary>Timeout for the PDF extraction HTTP client.</summary>
    public int TimeoutSeconds { get; set; } = 100;

    /// <summary>Conversational agent model (function calling).</summary>
    public string AgentModel { get; set; } = "gemini-3.5-flash";

    /// <summary>Cheaper model for short auxiliary tasks (titles, summaries).</summary>
    public string FastModel { get; set; } = "gemini-3.1-flash-lite";

    public double Temperature { get; set; } = 0.1;

    public int MaxOutputTokens { get; set; } = 2048;

    /// <summary>Per-request timeout for the agent HTTP client (separate from PDF extraction).</summary>
    public int RequestTimeoutSeconds { get; set; } = 45;
}
