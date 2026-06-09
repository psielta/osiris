using Osiris.Application.Common.Interfaces;
using Osiris.Domain.Entities;
using Osiris.Domain.Services;

namespace Osiris.Application.Features.CreditCardStatements.Services;

public sealed record CreditCardStatementResolution(
    IReadOnlyList<CreditCardStatement> StatementPerInstallment,
    IReadOnlyCollection<CreditCardStatement> NewStatements);

/// <summary>
/// Resolves the statement of each installment of a purchase, reusing existing statements and
/// instantiating missing ones. The first installment enters the statement resolved from the
/// purchase date and each subsequent installment enters the following month's statement.
/// </summary>
public sealed class CreditCardStatementResolver
{
    private readonly ICreditCardStatementRepository _statements;

    public CreditCardStatementResolver(ICreditCardStatementRepository statements)
    {
        _statements = statements;
    }

    public async Task<CreditCardStatementResolution> ResolveAsync(
        CreditCard card,
        DateOnly purchaseDate,
        int installmentCount,
        CancellationToken cancellationToken)
    {
        if (installmentCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(installmentCount), "Installment count must be greater than or equal to 1.");
        }

        var firstCycle = CreditCardStatementCycleCalculator.CalculateForPurchase(
            purchaseDate,
            card.ClosingDay,
            card.DueDay);

        var statementPerInstallment = new List<CreditCardStatement>(installmentCount);
        var newStatements = new List<CreditCardStatement>();
        var resolved = new Dictionary<(int Year, int Month), CreditCardStatement>();

        var year = firstCycle.ReferenceYear;
        var month = firstCycle.ReferenceMonth;
        for (var index = 0; index < installmentCount; index++)
        {
            if (index > 0)
            {
                (year, month) = month == 12 ? (year + 1, 1) : (year, month + 1);
            }

            if (!resolved.TryGetValue((year, month), out var statement))
            {
                statement = await _statements.GetByReferenceAsync(card.TenantId, card.Id, year, month, cancellationToken);
                if (statement is null)
                {
                    var cycle = CreditCardStatementCycleCalculator.CalculateForReference(
                        year,
                        month,
                        card.ClosingDay,
                        card.DueDay);

                    statement = new CreditCardStatement(
                        card.TenantId,
                        card.Id,
                        cycle.ReferenceMonth,
                        cycle.ReferenceYear,
                        cycle.ClosingDate,
                        cycle.DueDate);
                    newStatements.Add(statement);
                }

                resolved[(year, month)] = statement;
            }

            statementPerInstallment.Add(statement);
        }

        return new CreditCardStatementResolution(statementPerInstallment, newStatements);
    }
}
