using System.Globalization;
using MediatR;
using Osiris.Application.Common.Csv;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;
using Osiris.Application.Features.FinancialAccountMovements.DTOs;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.FinancialAccountMovements.Commands.PreviewCsvImport;

public sealed class PreviewCsvImportCommandHandler
    : IRequestHandler<PreviewCsvImportCommand, Result<OfxImportPreviewDto>>
{
    private readonly IFinancialAccountRepository _accounts;
    private readonly IFinancialAccountMovementRepository _movements;
    private readonly ICsvImportPreferenceRepository _preferences;
    private readonly ICsvStatementParser _parser;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PreviewCsvImportCommandHandler(
        IFinancialAccountRepository accounts,
        IFinancialAccountMovementRepository movements,
        ICsvImportPreferenceRepository preferences,
        ICsvStatementParser parser,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _accounts = accounts;
        _movements = movements;
        _preferences = preferences;
        _parser = parser;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<OfxImportPreviewDto>> Handle(
        PreviewCsvImportCommand request,
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

        var transactions = _parser.Parse(request.Content, request.Mapping);
        if (transactions.Count == 0)
        {
            return Result<OfxImportPreviewDto>.Failure(new ResultError(
                "Não foi possível ler lançamentos com este mapeamento. Revise as colunas e os formatos."));
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
                IsDuplicate: isDuplicate));
        }

        await RememberMappingAsync(tenantId, account.Id, request.Mapping, cancellationToken);

        var duplicateCount = lines.Count(line => line.IsDuplicate);

        var preview = new OfxImportPreviewDto(
            AccountId: account.Id,
            AccountName: account.Name,
            BankId: null,
            AccountNumber: null,
            CurrencyCode: null,
            PeriodStart: transactions.Min(transaction => transaction.OccurredOn),
            PeriodEnd: transactions.Max(transaction => transaction.OccurredOn),
            TotalCount: lines.Count,
            NewCount: lines.Count - duplicateCount,
            DuplicateCount: duplicateCount,
            Lines: lines);

        return Result<OfxImportPreviewDto>.Success(preview);
    }

    private async Task RememberMappingAsync(
        Guid tenantId,
        Guid accountId,
        CsvImportMapping mapping,
        CancellationToken cancellationToken)
    {
        var payload = CsvImportMappingSerializer.Serialize(mapping);
        var existing = await _preferences.GetByAccountAsync(tenantId, accountId, cancellationToken);
        if (existing is null)
        {
            await _preferences.AddAsync(new CsvImportPreference(tenantId, accountId, payload), cancellationToken);
        }
        else
        {
            existing.Update(payload, _dateTimeProvider.UtcNow);
            await _preferences.UpdateAsync(existing, cancellationToken);
        }
    }
}
