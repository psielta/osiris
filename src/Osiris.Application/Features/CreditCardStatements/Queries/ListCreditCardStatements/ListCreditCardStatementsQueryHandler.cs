using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;
using Osiris.Application.Features.CreditCardStatements.DTOs;
using Osiris.Domain.Entities;

namespace Osiris.Application.Features.CreditCardStatements.Queries.ListCreditCardStatements;

public sealed class ListCreditCardStatementsQueryHandler
    : IRequestHandler<ListCreditCardStatementsQuery, IReadOnlyCollection<CreditCardStatementListItemDto>>
{
    private readonly ICreditCardStatementRepository _statements;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ListCreditCardStatementsQueryHandler(
        ICreditCardStatementRepository statements,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _statements = statements;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<IReadOnlyCollection<CreditCardStatementListItemDto>> Handle(
        ListCreditCardStatementsQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;
        var statements = await _statements.ListByCardAsync(tenantId, request.CreditCardId, cancellationToken);
        var totalsById = await _statements.GetTotalsAsync(
            tenantId,
            statements.Select(statement => statement.Id).ToArray(),
            cancellationToken);

        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);

        return statements
            .Select(statement =>
            {
                var totals = totalsById.TryGetValue(statement.Id, out var persisted)
                    ? persisted
                    : new CreditCardStatementTotals(0m, 0m);

                // Status is evaluated at read time so date-driven transitions (Closed, Overdue)
                // show correctly without requiring a write.
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
            })
            .ToArray();
    }
}
