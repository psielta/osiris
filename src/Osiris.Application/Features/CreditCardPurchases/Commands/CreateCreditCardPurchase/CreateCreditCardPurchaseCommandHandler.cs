using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;
using Osiris.Application.Features.CreditCardPurchases.Services;
using Osiris.Application.Features.CreditCardStatements.Services;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.CreditCardPurchases.Commands.CreateCreditCardPurchase;

public sealed class CreateCreditCardPurchaseCommandHandler
    : IRequestHandler<CreateCreditCardPurchaseCommand, Result<Guid>>
{
    private readonly ICreditCardRepository _creditCards;
    private readonly ICategoryRepository _categories;
    private readonly ICreditCardPurchaseRepository _purchases;
    private readonly ICreditCardStatementRepository _statements;
    private readonly CreditCardStatementResolver _statementResolver;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateCreditCardPurchaseCommandHandler(
        ICreditCardRepository creditCards,
        ICategoryRepository categories,
        ICreditCardPurchaseRepository purchases,
        ICreditCardStatementRepository statements,
        CreditCardStatementResolver statementResolver,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _creditCards = creditCards;
        _categories = categories;
        _purchases = purchases;
        _statements = statements;
        _statementResolver = statementResolver;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateCreditCardPurchaseCommand request, CancellationToken cancellationToken)
    {
        if (request.CategoryId is null)
        {
            return Result<Guid>.Failure(new ResultError("Selecione a categoria da compra.", nameof(request.CategoryId)));
        }

        if (request.TotalAmount is null)
        {
            return Result<Guid>.Failure(new ResultError("Informe o valor total da compra.", nameof(request.TotalAmount)));
        }

        if (request.PurchaseDate is null)
        {
            return Result<Guid>.Failure(new ResultError("Informe a data da compra.", nameof(request.PurchaseDate)));
        }

        if (request.Installments is null)
        {
            return Result<Guid>.Failure(new ResultError("Informe o número de parcelas.", nameof(request.Installments)));
        }

        var tenantId = _currentUser.TenantId;

        var card = await _creditCards.GetByIdAsync(tenantId, request.CreditCardId, cancellationToken);
        if (card is null)
        {
            return Result<Guid>.Failure(new ResultError("Cartão não encontrado.", Code: ResultErrorCodes.NotFound));
        }

        if (!card.IsActive)
        {
            return Result<Guid>.Failure(new ResultError("O cartão está arquivado."));
        }

        var category = await _categories.GetByIdAsync(tenantId, request.CategoryId.Value, cancellationToken);
        if (category is null || !category.IsActive)
        {
            return Result<Guid>.Failure(new ResultError(
                "Categoria não encontrada ou arquivada.",
                nameof(request.CategoryId)));
        }

        if (category.Type != CategoryType.Expense)
        {
            return Result<Guid>.Failure(new ResultError(
                "A categoria da compra deve ser uma categoria de despesa.",
                nameof(request.CategoryId)));
        }

        var amounts = CreditCardInstallmentPlanBuilder.SplitAmount(request.TotalAmount.Value, request.Installments.Value);
        if (amounts.Any(amount => amount <= 0m))
        {
            return Result<Guid>.Failure(new ResultError(
                "O valor total é muito baixo para esse número de parcelas.",
                nameof(request.Installments)));
        }

        var resolution = await _statementResolver.ResolveAsync(
            card,
            request.PurchaseDate.Value,
            request.Installments.Value,
            cancellationToken);

        var purchase = new CreditCardPurchase(
            tenantId,
            card.Id,
            category.Id,
            request.Description,
            request.TotalAmount.Value,
            request.PurchaseDate.Value,
            request.Installments.Value,
            request.Notes);

        var installments = new List<CreditCardInstallment>(amounts.Count);
        for (var index = 0; index < amounts.Count; index++)
        {
            var statement = resolution.StatementPerInstallment[index];
            installments.Add(new CreditCardInstallment(
                tenantId,
                purchase.Id,
                statement.Id,
                index + 1,
                amounts.Count,
                amounts[index],
                statement.DueDate));
        }

        await RefreshAffectedStatementStatusesAsync(resolution, installments, cancellationToken);
        await _purchases.AddAsync(purchase, installments, resolution.NewStatements, cancellationToken);

        return Result<Guid>.Success(purchase.Id);
    }

    private async Task RefreshAffectedStatementStatusesAsync(
        CreditCardStatementResolution resolution,
        IReadOnlyCollection<CreditCardInstallment> installments,
        CancellationToken cancellationToken)
    {
        var affectedStatements = resolution.StatementPerInstallment.Distinct().ToArray();
        var existingIds = affectedStatements
            .Where(statement => !resolution.NewStatements.Contains(statement))
            .Select(statement => statement.Id)
            .ToArray();
        var persistedTotals = await _statements.GetTotalsAsync(_currentUser.TenantId, existingIds, cancellationToken);

        var utcNow = _dateTimeProvider.UtcNow;
        var today = DateOnly.FromDateTime(utcNow);
        foreach (var statement in affectedStatements)
        {
            var totals = persistedTotals.TryGetValue(statement.Id, out var persisted)
                ? persisted
                : new CreditCardStatementTotals(0m, 0m);
            var addedAmount = installments
                .Where(installment => installment.CreditCardStatementId == statement.Id)
                .Sum(installment => installment.Amount);

            statement.RefreshStatus(totals.InstallmentsTotal + addedAmount, totals.PaymentsTotal, today, utcNow);
        }
    }
}
