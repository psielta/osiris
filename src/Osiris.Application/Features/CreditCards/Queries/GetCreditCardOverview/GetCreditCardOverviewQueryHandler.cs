using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;
using Osiris.Application.Features.CreditCards.DTOs;
using Osiris.Application.Features.CreditCardStatements.DTOs;
using Osiris.Domain.Entities;
using Osiris.Domain.Services;

namespace Osiris.Application.Features.CreditCards.Queries.GetCreditCardOverview;

public sealed class GetCreditCardOverviewQueryHandler
    : IRequestHandler<GetCreditCardOverviewQuery, CreditCardOverviewDto?>
{
    private readonly ICreditCardRepository _creditCards;
    private readonly ICreditCardStatementRepository _statements;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetCreditCardOverviewQueryHandler(
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

    public async Task<CreditCardOverviewDto?> Handle(
        GetCreditCardOverviewQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;
        var card = await _creditCards.GetByIdAsync(tenantId, request.CreditCardId, cancellationToken);
        if (card is null)
        {
            return null;
        }

        var statements = await _statements.ListByCardAsync(tenantId, card.Id, cancellationToken);
        var totalsById = await _statements.GetTotalsAsync(
            tenantId,
            statements.Select(statement => statement.Id).ToArray(),
            cancellationToken);

        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
        var currentCycle = CreditCardStatementCycleCalculator.CalculateForPurchase(
            today,
            card.ClosingDay,
            card.DueDay);

        var usedLimit = 0m;
        var futureTotal = 0m;
        CreditCardStatement? nextStatement = null;
        var (nextYear, nextMonth) = currentCycle.ReferenceMonth == 12
            ? (currentCycle.ReferenceYear + 1, 1)
            : (currentCycle.ReferenceYear, currentCycle.ReferenceMonth + 1);

        foreach (var statement in statements)
        {
            var totals = GetTotals(totalsById, statement.Id);
            usedLimit += Math.Max(0m, totals.OpenBalance);

            if (IsAfter(statement, currentCycle.ReferenceYear, currentCycle.ReferenceMonth))
            {
                futureTotal += totals.InstallmentsTotal;
            }

            if (statement.ReferenceYear == nextYear && statement.ReferenceMonth == nextMonth)
            {
                nextStatement = statement;
            }
        }

        return new CreditCardOverviewDto(
            usedLimit,
            card.Limit - usedLimit,
            card.Limit > 0m ? Math.Round(usedLimit / card.Limit * 100m, 1) : 0m,
            futureTotal,
            nextStatement is null ? null : ToListItem(nextStatement, GetTotals(totalsById, nextStatement.Id), today));
    }

    private static CreditCardStatementTotals GetTotals(
        IReadOnlyDictionary<Guid, CreditCardStatementTotals> totalsById,
        Guid statementId)
    {
        return totalsById.TryGetValue(statementId, out var totals)
            ? totals
            : new CreditCardStatementTotals(0m, 0m);
    }

    private static bool IsAfter(CreditCardStatement statement, int referenceYear, int referenceMonth)
    {
        return statement.ReferenceYear > referenceYear
            || (statement.ReferenceYear == referenceYear && statement.ReferenceMonth > referenceMonth);
    }

    private static CreditCardStatementListItemDto ToListItem(
        CreditCardStatement statement,
        CreditCardStatementTotals totals,
        DateOnly today)
    {
        return new CreditCardStatementListItemDto(
            statement.Id,
            statement.CreditCardId,
            statement.ReferenceMonth,
            statement.ReferenceYear,
            statement.ClosingDate,
            statement.DueDate,
            CreditCardStatement.CalculateStatus(
                totals.InstallmentsTotal,
                totals.PaymentsTotal,
                statement.ClosingDate,
                statement.DueDate,
                today),
            totals.InstallmentsTotal,
            totals.PaymentsTotal,
            totals.OpenBalance);
    }
}
