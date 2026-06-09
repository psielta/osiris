using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;
using Osiris.Application.Features.CreditCardPurchases.DTOs;
using Osiris.Application.Features.CreditCardPurchases.Services;
using Osiris.Domain.Services;

namespace Osiris.Application.Features.CreditCardPurchases.Queries.GetPurchasePreview;

public sealed class GetPurchasePreviewQueryHandler
    : IRequestHandler<GetPurchasePreviewQuery, PurchasePreviewDto?>
{
    private const decimal HighLimitUsageThreshold = 80m;
    private const int MaxInstallments = 120;

    private readonly ICreditCardRepository _creditCards;
    private readonly ICreditCardStatementRepository _statements;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetPurchasePreviewQueryHandler(
        ICreditCardRepository creditCards,
        ICreditCardStatementRepository statements,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _creditCards = creditCards;
        _statements = statements;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<PurchasePreviewDto?> Handle(
        GetPurchasePreviewQuery request,
        CancellationToken cancellationToken)
    {
        if (request.TotalAmount is null or <= 0m
            || request.PurchaseDate is null
            || request.Installments is null or < 1 or > MaxInstallments)
        {
            return null;
        }

        var tenantId = _currentUser.TenantId;
        var card = await _creditCards.GetByIdAsync(tenantId, request.CreditCardId, cancellationToken);
        if (card is null)
        {
            return null;
        }

        var amounts = CreditCardInstallmentPlanBuilder.SplitAmount(
            decimal.Round(request.TotalAmount.Value, 2),
            request.Installments.Value);
        if (amounts.Any(amount => amount <= 0m))
        {
            return null;
        }

        // Project each installment cycle the same way the resolver will, without persisting.
        var firstCycle = CreditCardStatementCycleCalculator.CalculateForPurchase(
            request.PurchaseDate.Value,
            card.ClosingDay,
            card.DueDay);

        var installments = new List<PurchasePreviewInstallmentDto>(amounts.Count);
        var year = firstCycle.ReferenceYear;
        var month = firstCycle.ReferenceMonth;
        for (var index = 0; index < amounts.Count; index++)
        {
            if (index > 0)
            {
                (year, month) = month == 12 ? (year + 1, 1) : (year, month + 1);
            }

            var cycle = CreditCardStatementCycleCalculator.CalculateForReference(
                year,
                month,
                card.ClosingDay,
                card.DueDay);

            installments.Add(new PurchasePreviewInstallmentDto(
                index + 1,
                amounts[index],
                cycle.ReferenceMonth,
                cycle.ReferenceYear,
                cycle.DueDate));
        }

        var statements = await _statements.ListByCardAsync(tenantId, card.Id, cancellationToken);
        var totalsById = await _statements.GetTotalsAsync(
            tenantId,
            statements.Select(statement => statement.Id).ToArray(),
            cancellationToken);

        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
        var todayCycle = CreditCardStatementCycleCalculator.CalculateForPurchase(
            today,
            card.ClosingDay,
            card.DueDay);

        var usedLimit = 0m;
        var existingFutureTotal = 0m;
        foreach (var statement in statements)
        {
            var totals = totalsById.TryGetValue(statement.Id, out var persisted)
                ? persisted
                : new CreditCardStatementTotals(0m, 0m);
            usedLimit += Math.Max(0m, totals.OpenBalance);

            if (IsAfter(statement.ReferenceYear, statement.ReferenceMonth, todayCycle.ReferenceYear, todayCycle.ReferenceMonth))
            {
                existingFutureTotal += totals.InstallmentsTotal;
            }
        }

        var newFutureTotal = installments
            .Where(installment => IsAfter(
                installment.ReferenceYear,
                installment.ReferenceMonth,
                todayCycle.ReferenceYear,
                todayCycle.ReferenceMonth))
            .Sum(installment => installment.Amount);

        var totalAmount = amounts.Sum();
        var projectedUsedLimit = usedLimit + totalAmount;
        var projectedUsagePercentage = card.Limit > 0m
            ? Math.Round(projectedUsedLimit / card.Limit * 100m, 1)
            : 0m;

        return new PurchasePreviewDto(
            installments,
            totalAmount,
            card.Limit,
            usedLimit,
            card.Limit - usedLimit,
            projectedUsedLimit,
            projectedUsagePercentage,
            card.Limit > 0m && projectedUsagePercentage >= HighLimitUsageThreshold,
            existingFutureTotal + newFutureTotal);
    }

    private static bool IsAfter(int year, int month, int referenceYear, int referenceMonth)
    {
        return year > referenceYear || (year == referenceYear && month > referenceMonth);
    }
}
