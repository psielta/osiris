using Osiris.Application.Common.Exceptions;
using Osiris.Application.Common.Pdf;

namespace Osiris.Application.UnitTests.Features.FinancialAccounts.Support;

internal sealed class FakePdfStatementExtractor : IPdfStatementExtractor
{
    private readonly IReadOnlyList<ExtractedStatementTransaction> _transactions;
    private readonly Exception? _exception;

    public FakePdfStatementExtractor(IReadOnlyList<ExtractedStatementTransaction> transactions)
    {
        _transactions = transactions;
    }

    private FakePdfStatementExtractor(Exception exception)
    {
        _transactions = [];
        _exception = exception;
    }

    public static FakePdfStatementExtractor Throwing() =>
        new(new PdfStatementExtractionException("falha simulada"));

    public Task<IReadOnlyList<ExtractedStatementTransaction>> ExtractAsync(byte[] content, CancellationToken cancellationToken)
    {
        if (_exception is not null)
        {
            throw _exception;
        }

        return Task.FromResult(_transactions);
    }
}
