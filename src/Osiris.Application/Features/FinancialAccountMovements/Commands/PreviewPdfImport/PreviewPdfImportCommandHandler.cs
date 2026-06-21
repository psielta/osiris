using System.Globalization;
using MediatR;
using Osiris.Application.Common.Exceptions;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;
using Osiris.Application.Common.Pdf;
using Osiris.Application.Features.FinancialAccountMovements.DTOs;
using Osiris.Application.Features.FinancialAccountMovements.Reconciliation;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.FinancialAccountMovements.Commands.PreviewPdfImport;

public sealed class PreviewPdfImportCommandHandler
    : IRequestHandler<PreviewPdfImportCommand, Result<OfxImportPreviewDto>>
{
    private readonly IFinancialAccountRepository _accounts;
    private readonly IFinancialAccountMovementRepository _movements;
    private readonly IPdfStatementExtractor _extractor;
    private readonly ICurrentUser _currentUser;

    public PreviewPdfImportCommandHandler(
        IFinancialAccountRepository accounts,
        IFinancialAccountMovementRepository movements,
        IPdfStatementExtractor extractor,
        ICurrentUser currentUser)
    {
        _accounts = accounts;
        _movements = movements;
        _extractor = extractor;
        _currentUser = currentUser;
    }

    public async Task<Result<OfxImportPreviewDto>> Handle(
        PreviewPdfImportCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        var account = await _accounts.GetByIdAsync(tenantId, request.AccountId, cancellationToken);
        if (account is null)
        {
            return Result<OfxImportPreviewDto>.Failure(
                new ResultError("Conta não encontrada.", Code: ResultErrorCodes.NotFound));
        }

        if (!account.IsActive)
        {
            return Result<OfxImportPreviewDto>.Failure(new ResultError("A conta está arquivada."));
        }

        IReadOnlyList<ExtractedStatementTransaction> transactions;
        try
        {
            transactions = await _extractor.ExtractAsync(request.Content, cancellationToken);
        }
        catch (PdfStatementExtractionException)
        {
            return Result<OfxImportPreviewDto>.Failure(new ResultError(
                "Não foi possível ler o PDF com a IA. Verifique o arquivo e tente novamente."));
        }

        if (transactions.Count == 0)
        {
            return Result<OfxImportPreviewDto>.Failure(new ResultError(
                "Não foi possível identificar lançamentos no PDF."));
        }

        var externalIds = transactions
            .Select(transaction => transaction.ExternalId)
            .Distinct()
            .ToArray();

        var existing = (await _movements.ListExistingExternalIdsAsync(tenantId, account.Id, externalIds, cancellationToken))
            .ToHashSet(StringComparer.Ordinal);

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var lines = new List<OfxImportLineDto>(transactions.Count);
        for (var index = 0; index < transactions.Count; index++)
        {
            var transaction = transactions[index];

            // A transaction is a duplicate when it already exists, or repeats within this same file.
            var isDuplicate = existing.Contains(transaction.ExternalId) || !seen.Add(transaction.ExternalId);

            lines.Add(new OfxImportLineDto(
                RowKey: index.ToString(CultureInfo.InvariantCulture),
                ExternalId: transaction.ExternalId,
                OccurredOn: transaction.OccurredOn,
                Amount: transaction.Amount,
                Type: transaction.Type,
                IsInflow: transaction.Type.IsInflow(),
                Description: transaction.Description,
                IsDuplicate: isDuplicate,
                SuggestedMovementId: null,
                Candidates: []));
        }

        var duplicateCount = lines.Count(line => line.IsDuplicate);

        var enrichedLines = await ImportReconciliationSuggester.EnrichAsync(
            _movements, tenantId, account.Id, lines, cancellationToken);

        var preview = new OfxImportPreviewDto(
            AccountId: account.Id,
            AccountName: account.Name,
            BankId: null,
            AccountNumber: null,
            CurrencyCode: null,
            PeriodStart: transactions.Min(transaction => transaction.OccurredOn),
            PeriodEnd: transactions.Max(transaction => transaction.OccurredOn),
            TotalCount: enrichedLines.Count,
            NewCount: enrichedLines.Count - duplicateCount,
            DuplicateCount: duplicateCount,
            SuggestedReconciliationCount: enrichedLines.Count(line => line.SuggestedMovementId is not null),
            Lines: enrichedLines);

        return Result<OfxImportPreviewDto>.Success(preview);
    }
}
