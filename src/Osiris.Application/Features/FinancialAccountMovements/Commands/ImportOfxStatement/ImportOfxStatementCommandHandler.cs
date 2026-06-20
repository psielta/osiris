using MediatR;
using Osiris.Application.Common.Exceptions;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;
using Osiris.Application.Features.FinancialAccountMovements.DTOs;
using Osiris.Domain.Entities;

namespace Osiris.Application.Features.FinancialAccountMovements.Commands.ImportOfxStatement;

public sealed class ImportOfxStatementCommandHandler
    : IRequestHandler<ImportOfxStatementCommand, Result<OfxImportResultDto>>
{
    private readonly IFinancialAccountRepository _accounts;
    private readonly IFinancialAccountMovementRepository _movements;
    private readonly ICategoryRepository _categories;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ImportOfxStatementCommandHandler(
        IFinancialAccountRepository accounts,
        IFinancialAccountMovementRepository movements,
        ICategoryRepository categories,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _accounts = accounts;
        _movements = movements;
        _categories = categories;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<OfxImportResultDto>> Handle(
        ImportOfxStatementCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        var account = await _accounts.GetByIdAsync(tenantId, request.AccountId, cancellationToken);
        if (account is null)
        {
            return Result<OfxImportResultDto>.Failure(
                new ResultError("Conta não encontrada.", Code: ResultErrorCodes.NotFound));
        }

        if (!account.IsActive)
        {
            return Result<OfxImportResultDto>.Failure(new ResultError("A conta está arquivada."));
        }

        var categoryError = await ValidateCategoriesAsync(tenantId, request.Lines, cancellationToken);
        if (categoryError is not null)
        {
            return Result<OfxImportResultDto>.Failure(categoryError);
        }

        var total = request.Lines.Count;

        var externalIds = request.Lines.Select(line => line.ExternalId).Distinct().ToArray();
        var existing = (await _movements.ListExistingExternalIdsAsync(tenantId, account.Id, externalIds, cancellationToken))
            .ToHashSet(StringComparer.Ordinal);

        var utcNow = _dateTimeProvider.UtcNow;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var movements = new List<FinancialAccountMovement>();

        foreach (var line in request.Lines)
        {
            // Skip what already exists or repeats within the batch (the unique index is the final guard).
            if (existing.Contains(line.ExternalId) || !seen.Add(line.ExternalId))
            {
                continue;
            }

            account.ApplyMovement(line.Type, line.Amount, utcNow);
            movements.Add(new FinancialAccountMovement(
                tenantId,
                account.Id,
                line.Type,
                line.Amount,
                line.OccurredOn,
                line.Description,
                line.CategoryId,
                relatedEntityType: null,
                relatedEntityId: null,
                notes: null,
                externalId: line.ExternalId));
        }

        if (movements.Count == 0)
        {
            return Result<OfxImportResultDto>.Success(new OfxImportResultDto(0, total, total));
        }

        try
        {
            await _movements.AddRangeAsync(movements, account, cancellationToken);
        }
        catch (DuplicateExternalIdException)
        {
            // A concurrent import won the unique-index race; nothing was persisted.
            return Result<OfxImportResultDto>.Success(new OfxImportResultDto(0, total, total));
        }

        return Result<OfxImportResultDto>.Success(
            new OfxImportResultDto(movements.Count, total - movements.Count, total));
    }

    private async Task<ResultError?> ValidateCategoriesAsync(
        Guid tenantId,
        IReadOnlyList<ImportOfxLineInput> lines,
        CancellationToken cancellationToken)
    {
        var categoryIds = lines
            .Where(line => line.CategoryId is not null)
            .Select(line => line.CategoryId!.Value)
            .Distinct();

        foreach (var categoryId in categoryIds)
        {
            var category = await _categories.GetByIdAsync(tenantId, categoryId, cancellationToken);
            if (category is null || !category.IsActive)
            {
                return new ResultError("Categoria não encontrada ou arquivada.");
            }
        }

        return null;
    }
}
