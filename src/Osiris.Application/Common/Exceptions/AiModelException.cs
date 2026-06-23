namespace Osiris.Application.Common.Exceptions;

/// <summary>
/// Raised when the AI provider is unavailable or returns an unusable response. Carries only a safe,
/// user-facing message — never the provider payload — and is mapped to HTTP 503 at the edge.
/// </summary>
public sealed class AiModelException : Exception
{
    public AiModelException(string message)
        : base(message)
    {
    }

    public AiModelException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
