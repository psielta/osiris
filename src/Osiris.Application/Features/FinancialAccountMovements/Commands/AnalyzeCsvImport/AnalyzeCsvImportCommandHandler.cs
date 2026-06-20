using MediatR;
using Osiris.Application.Common.Csv;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;
using Osiris.Application.Features.FinancialAccountMovements.DTOs;

namespace Osiris.Application.Features.FinancialAccountMovements.Commands.AnalyzeCsvImport;

public sealed class AnalyzeCsvImportCommandHandler
    : IRequestHandler<AnalyzeCsvImportCommand, Result<CsvAnalysisDto>>
{
    private readonly IFinancialAccountRepository _accounts;
    private readonly ICsvImportPreferenceRepository _preferences;
    private readonly ICsvStatementParser _parser;
    private readonly ICurrentUser _currentUser;

    public AnalyzeCsvImportCommandHandler(
        IFinancialAccountRepository accounts,
        ICsvImportPreferenceRepository preferences,
        ICsvStatementParser parser,
        ICurrentUser currentUser)
    {
        _accounts = accounts;
        _preferences = preferences;
        _parser = parser;
        _currentUser = currentUser;
    }

    public async Task<Result<CsvAnalysisDto>> Handle(
        AnalyzeCsvImportCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        var account = await _accounts.GetByIdAsync(tenantId, request.AccountId, cancellationToken);
        if (account is null)
        {
            return Result<CsvAnalysisDto>.Failure(
                new ResultError("Conta não encontrada.", Code: ResultErrorCodes.NotFound));
        }

        if (!account.IsActive)
        {
            return Result<CsvAnalysisDto>.Failure(new ResultError("A conta está arquivada."));
        }

        var analysis = _parser.Analyze(request.Content, request.Delimiter, request.Encoding);
        if (analysis.SampleRows.Count == 0)
        {
            return Result<CsvAnalysisDto>.Failure(new ResultError(
                "Não foi possível ler o arquivo CSV. Verifique se o arquivo está correto."));
        }

        var preference = await _preferences.GetByAccountAsync(tenantId, account.Id, cancellationToken);
        var savedMapping = preference is null ? null : CsvImportMappingSerializer.Deserialize(preference.Mapping);

        var dto = new CsvAnalysisDto(
            AccountId: account.Id,
            AccountName: account.Name,
            Delimiter: analysis.Delimiter,
            Encoding: analysis.Encoding,
            SuggestedHeaderLineIndex: analysis.SuggestedHeaderLineIndex,
            SampleRows: analysis.SampleRows,
            SavedMapping: savedMapping);

        return Result<CsvAnalysisDto>.Success(dto);
    }
}
