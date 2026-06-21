using Osiris.Application.Common.Pdf;
using Osiris.Domain.Enums;

namespace Osiris.Web.IntegrationTests.Support;

/// <summary>
/// Deterministic stand-in for the Gemini extractor so integration tests exercise the PDF import flow
/// without calling the real AI API.
/// </summary>
public sealed class FakePdfStatementExtractor : IPdfStatementExtractor
{
    public static readonly IReadOnlyList<ExtractedStatementTransaction> Transactions = new[]
    {
        new ExtractedStatementTransaction("PDF-A1", new DateOnly(2026, 2, 1), 1500m, FinancialAccountMovementType.Income, "Salario pdf"),
        new ExtractedStatementTransaction("PDF-A2", new DateOnly(2026, 2, 2), 90m, FinancialAccountMovementType.Expense, "Mercado pdf"),
    };

    public Task<IReadOnlyList<ExtractedStatementTransaction>> ExtractAsync(byte[] content, CancellationToken cancellationToken) =>
        Task.FromResult(Transactions);
}
