namespace Osiris.Application.Common.Pdf;

public interface IPdfStatementExtractor
{
    /// <summary>
    /// Extracts the transactions from a bank-statement PDF using AI. Returns an empty list when no
    /// transactions can be identified; throws <see cref="Common.Exceptions.PdfStatementExtractionException"/>
    /// on an API/parse failure.
    /// </summary>
    Task<IReadOnlyList<ExtractedStatementTransaction>> ExtractAsync(byte[] content, CancellationToken cancellationToken);
}
