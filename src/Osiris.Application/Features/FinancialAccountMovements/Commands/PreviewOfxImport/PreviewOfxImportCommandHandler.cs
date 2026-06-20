using System.Globalization;
using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;
using Osiris.Application.Features.FinancialAccountMovements.DTOs;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.FinancialAccountMovements.Commands.PreviewOfxImport;

public sealed class PreviewOfxImportCommandHandler
    : IRequestHandler<PreviewOfxImportCommand, Result<OfxImportPreviewDto>>
{
    private readonly IFinancialAccountRepository _accounts;
    private readonly IFinancialAccountMovementRepository _movements;
    private readonly IOfxStatementParser _parser;
    private readonly ICurrentUser _currentUser;

    public PreviewOfxImportCommandHandler(
        IFinancialAccountRepository accounts,
        IFinancialAccountMovementRepository movements,
        IOfxStatementParser parser,
        ICurrentUser currentUser)
    {
        _accounts = accounts;
        _movements = movements;
        _parser = parser;
        _currentUser = currentUser;
    }

    public async Task<Result<OfxImportPreviewDto>> Handle(
        PreviewOfxImportCommand request,
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

        var statement = _parser.Parse(request.Content);
        if (statement.Transactions.Count == 0)
        {
            return Result<OfxImportPreviewDto>.Failure(new ResultError(
                "Não foi possível ler lançamentos no arquivo. Verifique se é um extrato OFX válido."));
        }

        var externalIds = statement.Transactions
            .Select(transaction => transaction.ExternalId)
            .Distinct()
            .ToArray();

        var existing = (await _movements.ListExistingExternalIdsAsync(tenantId, account.Id, externalIds, cancellationToken))
            .ToHashSet(StringComparer.Ordinal);

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var lines = new List<OfxImportLineDto>(statement.Transactions.Count);
        for (var index = 0; index < statement.Transactions.Count; index++)
        {
            var transaction = statement.Transactions[index];

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
                IsDuplicate: isDuplicate));
        }

        var duplicateCount = lines.Count(line => line.IsDuplicate);

        var preview = new OfxImportPreviewDto(
            AccountId: account.Id,
            AccountName: account.Name,
            BankId: statement.BankId,
            AccountNumber: statement.AccountId,
            CurrencyCode: statement.CurrencyCode,
            PeriodStart: statement.StartDate,
            PeriodEnd: statement.EndDate,
            TotalCount: lines.Count,
            NewCount: lines.Count - duplicateCount,
            DuplicateCount: duplicateCount,
            Lines: lines);

        return Result<OfxImportPreviewDto>.Success(preview);
    }
}
