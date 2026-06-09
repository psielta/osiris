using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;
using Osiris.Application.Features.CreditCardStatements.DTOs;
using Osiris.Domain.Entities;
using Osiris.Domain.Services;

namespace Osiris.Application.Features.CreditCardStatements.Queries.GetCurrentCreditCardStatement;

public sealed class GetCurrentCreditCardStatementQueryHandler
    : IRequestHandler<GetCurrentCreditCardStatementQuery, CreditCardStatementListItemDto?>
{
    private readonly ICreditCardRepository _creditCards;
    private readonly ICreditCardStatementRepository _statements;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetCurrentCreditCardStatementQueryHandler(
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

    public async Task<CreditCardStatementListItemDto?> Handle(
        GetCurrentCreditCardStatementQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;
        var card = await _creditCards.GetByIdAsync(tenantId, request.CreditCardId, cancellationToken);
        if (card is null)
        {
            return null;
        }

        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
        var cycle = CreditCardStatementCycleCalculator.CalculateForPurchase(today, card.ClosingDay, card.DueDay);

        var statement = await _statements.GetByReferenceAsync(
            tenantId,
            card.Id,
            cycle.ReferenceYear,
            cycle.ReferenceMonth,
            cancellationToken);
        if (statement is null)
        {
            return null;
        }

        var totalsById = await _statements.GetTotalsAsync(tenantId, new[] { statement.Id }, cancellationToken);
        var totals = totalsById.TryGetValue(statement.Id, out var persisted)
            ? persisted
            : new CreditCardStatementTotals(0m, 0m);

        var status = CreditCardStatement.CalculateStatus(
            totals.InstallmentsTotal,
            totals.PaymentsTotal,
            statement.ClosingDate,
            statement.DueDate,
            today);

        return new CreditCardStatementListItemDto(
            statement.Id,
            statement.CreditCardId,
            statement.ReferenceMonth,
            statement.ReferenceYear,
            statement.ClosingDate,
            statement.DueDate,
            status,
            totals.InstallmentsTotal,
            totals.PaymentsTotal,
            totals.OpenBalance);
    }
}
