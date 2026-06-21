namespace Osiris.Application.Common.Exceptions;

/// <summary>
/// Thrown when the AI PDF extraction fails (network/API error, non-success status, or an unparseable
/// response). The preview handler turns it into a friendly, user-facing failure.
/// </summary>
public sealed class PdfStatementExtractionException : Exception
{
    public PdfStatementExtractionException(string message)
        : base(message)
    {
    }

    public PdfStatementExtractionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
