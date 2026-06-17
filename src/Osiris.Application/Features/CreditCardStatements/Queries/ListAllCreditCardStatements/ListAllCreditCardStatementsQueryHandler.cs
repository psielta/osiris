using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;
using Osiris.Application.Features.CreditCardStatements.DTOs;
using Osiris.Domain.Entities;

namespace Osiris.Application.Features.CreditCardStatements.Queries.ListAllCreditCardStatements;

public sealed class ListAllCreditCardStatementsQueryHandler
    : IRequestHandler<ListAllCreditCardStatementsQuery, IReadOnlyCollection<CreditCardStatementOverviewDto>>
{
    private readonly ICreditCardStatementRepository _statements;
    private readonly ICreditCardRepository _creditCards;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ListAllCreditCardStatementsQueryHandler(
        ICreditCardStatementRepository statements,
        ICreditCardRepository creditCards,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _statements = statements;
        _creditCards = creditCards;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<IReadOnlyCollection<CreditCardStatementOverviewDto>> Handle(
        ListAllCreditCardStatementsQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;
        var statements = await _statements.ListAsync(tenantId, request.From, request.To, cancellationToken);
        var totalsById = await _statements.GetTotalsAsync(
            tenantId,
            statements.Select(statement => statement.Id).ToArray(),
            cancellationToken);
        var cards = await _creditCards.ListAsync(tenantId, includeArchived: true, cancellationToken);
        var cardNamesById = cards.ToDictionary(card => card.Id, card => card.Name);

        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);

        return statements
            .Select(statement =>
            {
                var totals = totalsById.TryGetValue(statement.Id, out var persisted)
                    ? persisted
                    : new CreditCardStatementTotals(0m, 0m);
                var status = CreditCardStatement.CalculateStatus(
                    totals.InstallmentsTotal,
                    totals.PaymentsTotal,
                    statement.ClosingDate,
                    statement.DueDate,
                    today);

                return new CreditCardStatementOverviewDto(
                    statement.Id,
                    statement.CreditCardId,
                    cardNamesById.GetValueOrDefault(statement.CreditCardId, "Cartão"),
                    statement.ReferenceMonth,
                    statement.ReferenceYear,
                    statement.ClosingDate,
                    statement.DueDate,
                    status,
                    totals.InstallmentsTotal,
                    totals.PaymentsTotal,
                    totals.OpenBalance);
            })
            .OrderBy(statement => statement.DueDate)
            .ThenBy(statement => statement.CreditCardName)
            .ToArray();
    }
}
