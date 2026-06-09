using System.Globalization;
using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;
using Osiris.Application.Features.Dashboard.DTOs;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.Dashboard.Queries.GetMonthlyDashboardSummary;

public sealed class GetMonthlyDashboardSummaryQueryHandler
    : IRequestHandler<GetMonthlyDashboardSummaryQuery, MonthlyDashboardSummaryDto>
{
    private const decimal HighLimitUsageThreshold = 80m;

    // Next month is "heavy" when its committed installments reach 80% of the selected month's.
    private const decimal HeavyNextMonthRatio = 0.8m;

    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    private readonly IBillRepository _bills;
    private readonly ICategoryRepository _categories;
    private readonly IFinancialAccountRepository _accounts;
    private readonly IFinancialAccountMovementRepository _movements;
    private readonly ICreditCardRepository _creditCards;
    private readonly ICreditCardPurchaseRepository _purchases;
    private readonly ICreditCardStatementRepository _statements;
    private readonly ICreditCardStatementPaymentRepository _statementPayments;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetMonthlyDashboardSummaryQueryHandler(
        IBillRepository bills,
        ICategoryRepository categories,
        IFinancialAccountRepository accounts,
        IFinancialAccountMovementRepository movements,
        ICreditCardRepository creditCards,
        ICreditCardPurchaseRepository purchases,
        ICreditCardStatementRepository statements,
        ICreditCardStatementPaymentRepository statementPayments,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _bills = bills;
        _categories = categories;
        _accounts = accounts;
        _movements = movements;
        _creditCards = creditCards;
        _purchases = purchases;
        _statements = statements;
        _statementPayments = statementPayments;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<MonthlyDashboardSummaryDto> Handle(
        GetMonthlyDashboardSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;
        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
        var monthStart = new DateOnly(request.Year, request.Month, 1);
        var nextMonthStart = monthStart.AddMonths(1);
        var monthAfterNextStart = nextMonthStart.AddMonths(1);

        var categories = await _categories.ListAsync(tenantId, includeArchived: true, cancellationToken);
        var activeAccounts = await _accounts.ListAsync(tenantId, includeArchived: false, cancellationToken);
        var monthMovements = await _movements.ListByMonthAsync(tenantId, request.Year, request.Month, cancellationToken);
        var monthPurchases = await _purchases.ListByMonthAsync(tenantId, request.Year, request.Month, cancellationToken);
        var monthBills = await _bills.ListByMonthAsync(tenantId, request.Year, request.Month, cancellationToken);
        var unpaidBills = await _bills.ListUnpaidAsync(tenantId, cancellationToken);
        var paidBillsInMonth = await _bills.ListPaidInMonthAsync(tenantId, request.Year, request.Month, cancellationToken);
        var monthStatementPayments = await _statementPayments.ListByMonthAsync(
            tenantId,
            request.Year,
            request.Month,
            cancellationToken);
        var allCards = await _creditCards.ListAsync(tenantId, includeArchived: true, cancellationToken);
        var statements = await _statements.ListAsync(tenantId, cancellationToken);
        var totalsById = await _statements.GetTotalsAsync(
            tenantId,
            statements.Select(statement => statement.Id).ToArray(),
            cancellationToken);

        var statementInfos = statements
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

                return new StatementInfo(statement, totals, status, Math.Max(0m, totals.OpenBalance));
            })
            .ToArray();

        var spendingByCategory = BuildSpendingByCategory(categories, monthPurchases, monthBills, monthMovements);
        var spendingTotal = spendingByCategory.Sum(entry => entry.Total);

        var incomeTotal = monthMovements
            .Where(movement => movement.Type == FinancialAccountMovementType.Income)
            .Sum(movement => movement.Amount);
        var directExpensesTotal = monthMovements
            .Where(movement => movement.Type == FinancialAccountMovementType.Expense)
            .Sum(movement => movement.Amount);

        var billsPaidTotal = paidBillsInMonth.Sum(bill => bill.Amount);
        var statementPaymentsTotal = monthStatementPayments.Sum(payment => payment.Amount);

        var billsOpenInMonthTotal = monthBills.Where(bill => !bill.IsPaid).Sum(bill => bill.Amount);
        var billsDueInMonthTotal = monthBills.Sum(bill => bill.Amount);

        var statementsDueInMonth = statementInfos
            .Where(info => info.Statement.DueDate >= monthStart && info.Statement.DueDate < nextMonthStart)
            .ToArray();
        var statementsDueInMonthTotal = statementsDueInMonth.Sum(info => info.Totals.InstallmentsTotal);
        var statementsOpenInMonthTotal = statementsDueInMonth.Sum(info => info.OpenBalance);

        var totalAccountsBalance = activeAccounts.Sum(account => account.CurrentBalance);
        var projectedCashBalance = totalAccountsBalance - billsOpenInMonthTotal - statementsOpenInMonthTotal;

        var totalOpenStatementsBalance = statementInfos.Sum(info => info.OpenBalance);
        var totalOpenBillsBalance = unpaidBills.Sum(bill => bill.Amount);

        var futureStatements = statementInfos
            .Where(info => info.Statement.DueDate >= nextMonthStart)
            .ToArray();
        var futureInstallmentsTotal = futureStatements.Sum(info => info.Totals.InstallmentsTotal);
        var nextMonthInstallmentsTotal = statementInfos
            .Where(info => info.Statement.DueDate >= nextMonthStart && info.Statement.DueDate < monthAfterNextStart)
            .Sum(info => info.Totals.InstallmentsTotal);

        var overdueStatements = statementInfos
            .Where(info => info.Status == CreditCardStatementStatus.Overdue)
            .ToArray();
        var partiallyPaidStatements = statementInfos
            .Where(info => info.Totals.PaymentsTotal > 0m && info.OpenBalance > 0m)
            .ToArray();

        var cardNamesById = allCards.ToDictionary(card => card.Id, card => card.Name);
        var creditCardsSection = BuildCreditCardsSection(
            allCards.Where(card => card.IsActive).ToArray(),
            statementInfos,
            request,
            nextMonthStart);

        var upcomingObligations = BuildUpcomingObligations(
            monthBills,
            statementsDueInMonth,
            cardNamesById,
            today);

        var alerts = BuildAlerts(
            overdueStatements,
            partiallyPaidStatements,
            statementInfos,
            creditCardsSection,
            cardNamesById,
            today,
            statementsDueInMonthTotal,
            nextMonthInstallmentsTotal,
            projectedCashBalance);

        var cashFlow = new CashFlowSummaryDto(
            incomeTotal,
            billsPaidTotal,
            statementPaymentsTotal,
            directExpensesTotal,
            billsOpenInMonthTotal,
            statementsOpenInMonthTotal,
            totalAccountsBalance,
            projectedCashBalance);

        return new MonthlyDashboardSummaryDto(
            request.Year,
            request.Month,
            incomeTotal,
            spendingTotal,
            spendingByCategory,
            cashFlow,
            creditCardsSection,
            upcomingObligations,
            alerts,
            billsDueInMonthTotal,
            monthBills.Count,
            billsOpenInMonthTotal,
            statementsDueInMonthTotal,
            statementsDueInMonth.Length,
            statementsOpenInMonthTotal,
            totalOpenStatementsBalance,
            totalOpenBillsBalance,
            statementPaymentsTotal,
            futureInstallmentsTotal,
            overdueStatements.Length,
            overdueStatements.Sum(info => info.OpenBalance),
            partiallyPaidStatements.Length);
    }

    private static IReadOnlyCollection<SpendingByCategoryDto> BuildSpendingByCategory(
        IReadOnlyCollection<FinancialCategory> categories,
        IReadOnlyCollection<CreditCardPurchase> monthPurchases,
        IReadOnlyCollection<Bill> monthBills,
        IReadOnlyCollection<FinancialAccountMovement> monthMovements)
    {
        // Guid.Empty bucket collects entries without a category (uncategorized manual expenses).
        var buckets = new Dictionary<Guid, SpendingBucket>();

        foreach (var purchase in monthPurchases)
        {
            var bucket = GetBucket(buckets, purchase.CategoryId);
            bucket.CardPurchases += purchase.TotalAmount;
        }

        foreach (var bill in monthBills)
        {
            var bucket = GetBucket(buckets, bill.CategoryId);
            bucket.Bills += bill.Amount;
        }

        // Only plain manual expenses count here. BillPayment and statement payment movements are
        // cash events for spending already counted through bills and card purchases.
        foreach (var movement in monthMovements.Where(m => m.Type == FinancialAccountMovementType.Expense))
        {
            var bucket = GetBucket(buckets, movement.CategoryId ?? Guid.Empty);
            bucket.DirectExpenses += movement.Amount;
        }

        var categoriesById = categories.ToDictionary(category => category.Id);

        return buckets
            .Select(entry =>
            {
                categoriesById.TryGetValue(entry.Key, out var category);
                return new SpendingByCategoryDto(
                    entry.Key == Guid.Empty ? null : entry.Key,
                    category?.Name ?? "Sem categoria",
                    category?.Color,
                    entry.Value.CardPurchases,
                    entry.Value.Bills,
                    entry.Value.DirectExpenses);
            })
            .OrderByDescending(dto => dto.Total)
            .ThenBy(dto => dto.CategoryName)
            .ToArray();
    }

    private static SpendingBucket GetBucket(Dictionary<Guid, SpendingBucket> buckets, Guid categoryId)
    {
        if (!buckets.TryGetValue(categoryId, out var bucket))
        {
            bucket = new SpendingBucket();
            buckets[categoryId] = bucket;
        }

        return bucket;
    }

    private static IReadOnlyCollection<CreditCardDashboardDto> BuildCreditCardsSection(
        IReadOnlyCollection<CreditCard> activeCards,
        IReadOnlyCollection<StatementInfo> statementInfos,
        GetMonthlyDashboardSummaryQuery request,
        DateOnly nextMonthStart)
    {
        var statementsByCard = statementInfos
            .GroupBy(info => info.Statement.CreditCardId)
            .ToDictionary(group => group.Key, group => group.ToArray());

        return activeCards
            .Select(card =>
            {
                var cardStatements = statementsByCard.GetValueOrDefault(card.Id, Array.Empty<StatementInfo>());

                // Open balances across every statement consume the limit until paid.
                var usedLimit = cardStatements.Sum(info => info.OpenBalance);
                var current = cardStatements.SingleOrDefault(info =>
                    info.Statement.ReferenceYear == request.Year
                    && info.Statement.ReferenceMonth == request.Month);
                var futureTotal = cardStatements
                    .Where(info => info.Statement.DueDate >= nextMonthStart)
                    .Sum(info => info.Totals.InstallmentsTotal);

                return new CreditCardDashboardDto(
                    card.Id,
                    card.Name,
                    card.Limit,
                    usedLimit,
                    card.Limit - usedLimit,
                    card.Limit > 0m ? Math.Round(usedLimit / card.Limit * 100m, 1) : 0m,
                    current?.Statement.Id,
                    current?.Totals.InstallmentsTotal ?? 0m,
                    current?.Totals.PaymentsTotal ?? 0m,
                    current?.OpenBalance ?? 0m,
                    current?.Statement.DueDate,
                    current?.Status,
                    futureTotal);
            })
            .ToArray();
    }

    private static IReadOnlyCollection<UpcomingObligationDto> BuildUpcomingObligations(
        IReadOnlyCollection<Bill> monthBills,
        IReadOnlyCollection<StatementInfo> statementsDueInMonth,
        IReadOnlyDictionary<Guid, string> cardNamesById,
        DateOnly today)
    {
        var billObligations = monthBills
            .Where(bill => !bill.IsPaid)
            .Select(bill => new UpcomingObligationDto(
                UpcomingObligationKind.Bill,
                bill.Id,
                CreditCardId: null,
                bill.Description,
                bill.DueDate,
                bill.Amount,
                bill.DueDate < today));

        var statementObligations = statementsDueInMonth
            .Where(info => info.OpenBalance > 0m)
            .Select(info => new UpcomingObligationDto(
                UpcomingObligationKind.CreditCardStatement,
                info.Statement.Id,
                info.Statement.CreditCardId,
                $"Fatura {cardNamesById.GetValueOrDefault(info.Statement.CreditCardId, "cartão")} " +
                    $"{info.Statement.ReferenceMonth:00}/{info.Statement.ReferenceYear}",
                info.Statement.DueDate,
                info.OpenBalance,
                info.Statement.DueDate < today));

        return billObligations
            .Concat(statementObligations)
            .OrderBy(obligation => obligation.DueDate)
            .ThenBy(obligation => obligation.Description)
            .ToArray();
    }

    private static IReadOnlyCollection<DashboardAlertDto> BuildAlerts(
        IReadOnlyCollection<StatementInfo> overdueStatements,
        IReadOnlyCollection<StatementInfo> partiallyPaidStatements,
        IReadOnlyCollection<StatementInfo> statementInfos,
        IReadOnlyCollection<CreditCardDashboardDto> creditCardsSection,
        IReadOnlyDictionary<Guid, string> cardNamesById,
        DateOnly today,
        decimal currentMonthInstallmentsTotal,
        decimal nextMonthInstallmentsTotal,
        decimal projectedCashBalance)
    {
        var alerts = new List<DashboardAlertDto>();

        if (overdueStatements.Count > 0)
        {
            alerts.Add(new DashboardAlertDto(
                DashboardAlertSeverity.Danger,
                overdueStatements.Count == 1
                    ? $"Você tem 1 fatura vencida somando {Brl(overdueStatements.Sum(info => info.OpenBalance))}."
                    : $"Você tem {overdueStatements.Count} faturas vencidas somando " +
                        $"{Brl(overdueStatements.Sum(info => info.OpenBalance))}."));
        }

        if (projectedCashBalance < 0m)
        {
            alerts.Add(new DashboardAlertDto(
                DashboardAlertSeverity.Danger,
                $"O saldo das contas não cobre as contas e faturas em aberto do mês (faltam {Brl(-projectedCashBalance)})."));
        }

        var dueSoonLimit = today.AddDays(7);
        foreach (var info in statementInfos
            .Where(info => info.OpenBalance > 0m
                && info.Statement.DueDate >= today
                && info.Statement.DueDate <= dueSoonLimit)
            .OrderBy(info => info.Statement.DueDate))
        {
            var cardName = cardNamesById.GetValueOrDefault(info.Statement.CreditCardId, "cartão");
            alerts.Add(new DashboardAlertDto(
                DashboardAlertSeverity.Warning,
                $"A fatura do cartão {cardName} vence em {info.Statement.DueDate.ToString("dd/MM", PtBr)} " +
                    $"({Brl(info.OpenBalance)} em aberto)."));
        }

        foreach (var info in partiallyPaidStatements.OrderBy(info => info.Statement.DueDate))
        {
            var cardName = cardNamesById.GetValueOrDefault(info.Statement.CreditCardId, "cartão");
            alerts.Add(new DashboardAlertDto(
                DashboardAlertSeverity.Warning,
                $"A fatura do cartão {cardName} de {info.Statement.ReferenceMonth:00}/{info.Statement.ReferenceYear} " +
                    $"está parcialmente paga ({Brl(info.OpenBalance)} em aberto)."));
        }

        foreach (var card in creditCardsSection.Where(card => card.Limit > 0m
            && card.UsagePercentage >= HighLimitUsageThreshold))
        {
            alerts.Add(new DashboardAlertDto(
                DashboardAlertSeverity.Warning,
                $"O cartão {card.Name} está com {card.UsagePercentage.ToString("0.#", PtBr)}% do limite usado."));
        }

        if (nextMonthInstallmentsTotal > 0m
            && nextMonthInstallmentsTotal >= HeavyNextMonthRatio * currentMonthInstallmentsTotal)
        {
            alerts.Add(new DashboardAlertDto(
                DashboardAlertSeverity.Warning,
                $"O próximo mês já tem {Brl(nextMonthInstallmentsTotal)} comprometidos em parcelas."));
        }

        return alerts
            .OrderByDescending(alert => alert.Severity)
            .ToArray();
    }

    private static string Brl(decimal value) => value.ToString("C", PtBr);

    private sealed record StatementInfo(
        CreditCardStatement Statement,
        CreditCardStatementTotals Totals,
        CreditCardStatementStatus Status,
        decimal OpenBalance);

    private sealed class SpendingBucket
    {
        public decimal CardPurchases { get; set; }
        public decimal Bills { get; set; }
        public decimal DirectExpenses { get; set; }
    }
}
