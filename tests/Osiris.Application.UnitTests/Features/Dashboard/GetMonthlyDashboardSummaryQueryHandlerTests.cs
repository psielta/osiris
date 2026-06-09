using Osiris.Application.Features.Dashboard.DTOs;
using Osiris.Application.Features.Dashboard.Queries.GetMonthlyDashboardSummary;
using Osiris.Application.UnitTests.Features.Dashboard.Support;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.Dashboard;

public sealed class GetMonthlyDashboardSummaryQueryHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly FakeBillRepository _bills = new();
    private readonly FakeCategoryRepository _categories = new();
    private readonly FakeFinancialAccountRepository _accounts = new();
    private readonly FakeFinancialAccountMovementRepository _movements = new();
    private readonly FakeCreditCardRepository _cards = new();
    private readonly FakeCreditCardPurchaseRepository _purchases = new();
    private readonly FakeCreditCardStatementPaymentRepository _statementPayments = new();
    private readonly FakeCreditCardStatementRepository _statements;

    private readonly FinancialCategory _foodCategory;
    private readonly FinancialCategory _housingCategory;

    // Fixed clock: 2026-06-08. The default query targets June 2026.
    private static readonly DateTime UtcNow = new(2026, 6, 8, 12, 0, 0, DateTimeKind.Utc);

    public GetMonthlyDashboardSummaryQueryHandlerTests()
    {
        _statements = new FakeCreditCardStatementRepository(_statementPayments);
        _foodCategory = new FinancialCategory(_tenantId, "Alimentação", CategoryType.Expense);
        _housingCategory = new FinancialCategory(_tenantId, "Moradia", CategoryType.Expense);
        _categories.Add(_foodCategory);
        _categories.Add(_housingCategory);
    }

    private GetMonthlyDashboardSummaryQueryHandler CreateHandler()
    {
        return new GetMonthlyDashboardSummaryQueryHandler(
            _bills,
            _categories,
            _accounts,
            _movements,
            _cards,
            _purchases,
            _statements,
            _statementPayments,
            new FakeCurrentUser(_tenantId),
            new FakeDateTimeProvider { UtcNow = UtcNow });
    }

    private Task<MonthlyDashboardSummaryDto> HandleAsync(int year = 2026, int month = 6)
    {
        return CreateHandler().Handle(new GetMonthlyDashboardSummaryQuery(year, month), CancellationToken.None);
    }

    private CreditCard SeedCard(decimal limit = 5000m, string name = "Nubank")
    {
        var card = new CreditCard(_tenantId, name, limit, 25, 5, null);
        _cards.Add(card);
        return card;
    }

    private CreditCardStatement SeedStatement(
        CreditCard card,
        int month,
        int year,
        decimal installmentsTotal,
        DateOnly closingDate,
        DateOnly dueDate)
    {
        var statement = new CreditCardStatement(_tenantId, card.Id, month, year, closingDate, dueDate);
        _statements.Add(statement, installmentsTotal);
        return statement;
    }

    private FinancialAccount SeedAccount(decimal balance, string name = "Banco")
    {
        var account = new FinancialAccount(_tenantId, name, FinancialAccountType.CheckingAccount, balance);
        _accounts.Add(account);
        return account;
    }

    [Fact]
    public async Task Handle_SpendingByCategory_ShouldCombineSourcesWithoutStatementPayments()
    {
        var card = SeedCard();
        var account = SeedAccount(1000m);

        // Spending: card purchase 300 + bill 200 (Moradia) + direct expense 100 (Alimentação).
        _purchases.Add(new CreditCardPurchase(
            _tenantId, card.Id, _foodCategory.Id, "Mercado", 300m, new DateOnly(2026, 6, 5), 1));
        _bills.Add(new Bill(_tenantId, _housingCategory.Id, "Aluguel", 200m, new DateOnly(2026, 6, 10)));
        _movements.Add(new FinancialAccountMovement(
            _tenantId, account.Id, FinancialAccountMovementType.Expense, 100m,
            new DateOnly(2026, 6, 7), "Padaria", _foodCategory.Id));

        // Cash-only events that must NOT appear as category spending.
        var statement = SeedStatement(card, 6, 2026, 300m, new DateOnly(2026, 6, 25), new DateOnly(2026, 7, 5));
        _statementPayments.Add(new CreditCardStatementPayment(
            _tenantId, statement.Id, account.Id, 150m, new DateOnly(2026, 6, 6)));
        _movements.Add(new FinancialAccountMovement(
            _tenantId, account.Id, FinancialAccountMovementType.CreditCardStatementPayment, 150m,
            new DateOnly(2026, 6, 6), "Pagamento de fatura",
            relatedEntityType: nameof(CreditCardStatementPayment), relatedEntityId: statement.Id));

        var result = await HandleAsync();

        Assert.Equal(600m, result.SpendingTotal);

        var food = result.SpendingByCategory.Single(entry => entry.CategoryId == _foodCategory.Id);
        Assert.Equal(300m, food.CardPurchasesTotal);
        Assert.Equal(100m, food.DirectExpensesTotal);
        Assert.Equal(400m, food.Total);

        var housing = result.SpendingByCategory.Single(entry => entry.CategoryId == _housingCategory.Id);
        Assert.Equal(200m, housing.BillsTotal);

        // The statement payment shows up in the cash view only.
        Assert.Equal(150m, result.StatementPaymentsInMonthTotal);
        Assert.Equal(150m, result.CashFlow.StatementPaymentsTotal);
    }

    [Fact]
    public async Task Handle_BillPaymentMovement_ShouldNotCountAsDirectExpense()
    {
        var account = SeedAccount(1000m);
        var bill = new Bill(_tenantId, _housingCategory.Id, "Aluguel", 200m, new DateOnly(2026, 6, 10));
        bill.MarkAsPaid(new DateOnly(2026, 6, 7), account.Id, UtcNow);
        _bills.Add(bill);
        _movements.Add(new FinancialAccountMovement(
            _tenantId, account.Id, FinancialAccountMovementType.BillPayment, 200m,
            new DateOnly(2026, 6, 7), "Pagamento de conta: Aluguel",
            relatedEntityType: nameof(Bill), relatedEntityId: bill.Id));

        var result = await HandleAsync();

        // The bill counts once (as a bill), not twice through its payment movement.
        Assert.Equal(200m, result.SpendingTotal);
        var housing = result.SpendingByCategory.Single(entry => entry.CategoryId == _housingCategory.Id);
        Assert.Equal(200m, housing.BillsTotal);
        Assert.Equal(0m, housing.DirectExpensesTotal);
        Assert.Equal(200m, result.CashFlow.BillsPaidTotal);
    }

    [Fact]
    public async Task Handle_ShouldComputeTotalOpenStatementsBalance()
    {
        var card = SeedCard();
        var june = SeedStatement(card, 6, 2026, 500m, new DateOnly(2026, 6, 25), new DateOnly(2026, 7, 5));
        SeedStatement(card, 7, 2026, 300m, new DateOnly(2026, 7, 25), new DateOnly(2026, 8, 5));
        _statementPayments.Add(new CreditCardStatementPayment(
            _tenantId, june.Id, null, 200m, new DateOnly(2026, 6, 6)));

        var result = await HandleAsync();

        // 500 - 200 paid + 300 future = 600 open across all statements.
        Assert.Equal(600m, result.TotalOpenStatementsBalance);
    }

    [Fact]
    public async Task Handle_ShouldCountOverdueStatements()
    {
        var card = SeedCard();

        // Due 2026-06-05, before today (06-08), unpaid -> overdue.
        SeedStatement(card, 5, 2026, 400m, new DateOnly(2026, 5, 25), new DateOnly(2026, 6, 5));

        // Paid statement past due date is not overdue.
        var paid = SeedStatement(card, 4, 2026, 100m, new DateOnly(2026, 4, 25), new DateOnly(2026, 5, 5));
        _statementPayments.Add(new CreditCardStatementPayment(
            _tenantId, paid.Id, null, 100m, new DateOnly(2026, 5, 4)));

        var result = await HandleAsync();

        Assert.Equal(1, result.OverdueStatementsCount);
        Assert.Equal(400m, result.OverdueStatementsBalance);
        Assert.Contains(result.Alerts, alert =>
            alert.Severity == DashboardAlertSeverity.Danger && alert.Message.Contains("vencida"));
    }

    [Fact]
    public async Task Handle_ShouldComputeProjectedCashBalance()
    {
        SeedAccount(1000m);
        var card = SeedCard();

        // Unpaid bill due in June: 200. Statement due in June with 300 open.
        _bills.Add(new Bill(_tenantId, _housingCategory.Id, "Aluguel", 200m, new DateOnly(2026, 6, 20)));
        SeedStatement(card, 5, 2026, 300m, new DateOnly(2026, 5, 25), new DateOnly(2026, 6, 5));

        var result = await HandleAsync();

        Assert.Equal(1000m, result.CashFlow.TotalAccountsBalance);
        Assert.Equal(500m, result.CashFlow.ProjectedCashBalance);
    }

    [Fact]
    public async Task Handle_WhenProjectedBalanceNegative_ShouldAlert()
    {
        SeedAccount(100m);
        _bills.Add(new Bill(_tenantId, _housingCategory.Id, "Aluguel", 200m, new DateOnly(2026, 6, 20)));

        var result = await HandleAsync();

        Assert.Equal(-100m, result.CashFlow.ProjectedCashBalance);
        Assert.Contains(result.Alerts, alert =>
            alert.Severity == DashboardAlertSeverity.Danger && alert.Message.Contains("saldo", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Handle_ShouldComputeUsedLimitAcrossStatements()
    {
        var card = SeedCard(limit: 1000m);
        SeedStatement(card, 6, 2026, 200m, new DateOnly(2026, 6, 25), new DateOnly(2026, 7, 5));
        SeedStatement(card, 7, 2026, 300m, new DateOnly(2026, 7, 25), new DateOnly(2026, 8, 5));

        var result = await HandleAsync();

        var dashboardCard = Assert.Single(result.CreditCards);
        Assert.Equal(500m, dashboardCard.UsedLimit);
        Assert.Equal(500m, dashboardCard.AvailableLimit);
        Assert.Equal(50m, dashboardCard.UsagePercentage);
        Assert.Equal(200m, dashboardCard.CurrentStatementTotal);
        Assert.Equal(new DateOnly(2026, 7, 5), dashboardCard.CurrentStatementDueDate);
    }

    [Fact]
    public async Task Handle_ShouldComputeFutureInstallmentsTotal()
    {
        var card = SeedCard();

        // Due inside June: not future. Due July and August: future relative to June.
        SeedStatement(card, 5, 2026, 100m, new DateOnly(2026, 5, 25), new DateOnly(2026, 6, 5));
        SeedStatement(card, 6, 2026, 200m, new DateOnly(2026, 6, 25), new DateOnly(2026, 7, 5));
        SeedStatement(card, 7, 2026, 300m, new DateOnly(2026, 7, 25), new DateOnly(2026, 8, 5));

        var result = await HandleAsync();

        Assert.Equal(500m, result.FutureInstallmentsTotal);
        var dashboardCard = Assert.Single(result.CreditCards);
        Assert.Equal(500m, dashboardCard.FutureInstallmentsTotal);
    }

    [Fact]
    public async Task Handle_WhenStatementDueWithinSevenDays_ShouldAlert()
    {
        var card = SeedCard();

        // Today is 06-08; due 06-12 is inside the 7-day window.
        SeedStatement(card, 5, 2026, 250m, new DateOnly(2026, 5, 25), new DateOnly(2026, 6, 12));

        var result = await HandleAsync();

        Assert.Contains(result.Alerts, alert =>
            alert.Severity == DashboardAlertSeverity.Warning && alert.Message.Contains("vence em"));
    }

    [Fact]
    public async Task Handle_WhenStatementDueFarAway_ShouldNotRaiseDueSoonAlert()
    {
        var card = SeedCard();
        SeedStatement(card, 6, 2026, 250m, new DateOnly(2026, 6, 25), new DateOnly(2026, 7, 5));

        var result = await HandleAsync();

        Assert.DoesNotContain(result.Alerts, alert => alert.Message.Contains("vence em"));
    }

    [Fact]
    public async Task Handle_WhenLimitUsageAbove80Percent_ShouldAlert()
    {
        var card = SeedCard(limit: 1000m);
        SeedStatement(card, 6, 2026, 850m, new DateOnly(2026, 6, 25), new DateOnly(2026, 7, 5));

        var result = await HandleAsync();

        Assert.Contains(result.Alerts, alert =>
            alert.Severity == DashboardAlertSeverity.Warning && alert.Message.Contains("limite usado"));
    }

    [Fact]
    public async Task Handle_WhenLimitUsageBelow80Percent_ShouldNotAlert()
    {
        var card = SeedCard(limit: 1000m);
        SeedStatement(card, 6, 2026, 500m, new DateOnly(2026, 6, 25), new DateOnly(2026, 7, 5));

        var result = await HandleAsync();

        Assert.DoesNotContain(result.Alerts, alert => alert.Message.Contains("limite usado"));
    }

    [Fact]
    public async Task Handle_PartiallyPaidStatement_ShouldCountAndAlert()
    {
        var card = SeedCard();
        var statement = SeedStatement(card, 6, 2026, 300m, new DateOnly(2026, 6, 25), new DateOnly(2026, 7, 5));
        _statementPayments.Add(new CreditCardStatementPayment(
            _tenantId, statement.Id, null, 100m, new DateOnly(2026, 6, 6)));

        var result = await HandleAsync();

        Assert.Equal(1, result.PartiallyPaidStatementsCount);
        Assert.Contains(result.Alerts, alert => alert.Message.Contains("parcialmente paga"));
    }

    [Fact]
    public async Task Handle_IncomeTotal_ShouldSumOnlyIncomeMovements()
    {
        var account = SeedAccount(1000m);
        _movements.Add(new FinancialAccountMovement(
            _tenantId, account.Id, FinancialAccountMovementType.Income, 3000m,
            new DateOnly(2026, 6, 1), "Salário"));
        _movements.Add(new FinancialAccountMovement(
            _tenantId, account.Id, FinancialAccountMovementType.Expense, 100m,
            new DateOnly(2026, 6, 2), "Padaria"));
        _movements.Add(new FinancialAccountMovement(
            _tenantId, account.Id, FinancialAccountMovementType.Income, 500m,
            new DateOnly(2026, 7, 1), "Freela de julho"));

        var result = await HandleAsync();

        Assert.Equal(3000m, result.IncomeTotal);
    }

    [Fact]
    public async Task Handle_ShouldIgnoreDataFromOtherTenants()
    {
        var foreignTenant = Guid.NewGuid();
        _bills.Add(new Bill(foreignTenant, Guid.NewGuid(), "Conta alheia", 999m, new DateOnly(2026, 6, 10)));
        var foreignAccount = new FinancialAccount(foreignTenant, "Banco Alheio", FinancialAccountType.CheckingAccount, 999m);
        _accounts.Add(foreignAccount);

        var result = await HandleAsync();

        Assert.Equal(0m, result.SpendingTotal);
        Assert.Equal(0m, result.CashFlow.TotalAccountsBalance);
        Assert.Empty(result.UpcomingObligations);
    }

    [Fact]
    public async Task Handle_WhenTenantHasNoData_ShouldShowIncompleteOnboarding()
    {
        var result = await HandleAsync();

        Assert.False(result.Onboarding.HasFinancialAccount);
        Assert.False(result.Onboarding.HasCreditCard);
        Assert.True(result.Onboarding.HasActiveCategories);
        Assert.False(result.Onboarding.HasFirstSpending);
        Assert.False(result.Onboarding.IsComplete);
    }

    [Fact]
    public async Task Handle_WhenTenantHasAccountCardAndSpending_ShouldCompleteOnboarding()
    {
        SeedAccount(100m);
        var card = SeedCard();
        SeedStatement(card, 6, 2026, 100m, new DateOnly(2026, 6, 25), new DateOnly(2026, 7, 5));

        var result = await HandleAsync();

        Assert.True(result.Onboarding.HasFinancialAccount);
        Assert.True(result.Onboarding.HasCreditCard);
        Assert.True(result.Onboarding.HasFirstSpending);
        Assert.True(result.Onboarding.IsComplete);
    }

    [Fact]
    public async Task Handle_WhenOnlyBillExists_ShouldCountAsFirstSpending()
    {
        _bills.Add(new Bill(_tenantId, _housingCategory.Id, "Aluguel", 100m, new DateOnly(2026, 1, 10)));

        var result = await HandleAsync();

        Assert.True(result.Onboarding.HasFirstSpending);
    }

    [Fact]
    public async Task Handle_UpcomingObligations_ShouldIncludeUnpaidBillsAndOpenStatements()
    {
        var card = SeedCard();
        _bills.Add(new Bill(_tenantId, _housingCategory.Id, "Aluguel", 200m, new DateOnly(2026, 6, 10)));
        var paidBill = new Bill(_tenantId, _housingCategory.Id, "Internet", 100m, new DateOnly(2026, 6, 12));
        paidBill.MarkAsPaid(new DateOnly(2026, 6, 5), null, UtcNow);
        _bills.Add(paidBill);
        SeedStatement(card, 5, 2026, 300m, new DateOnly(2026, 5, 25), new DateOnly(2026, 6, 15));

        var result = await HandleAsync();

        Assert.Equal(2, result.UpcomingObligations.Count);
        Assert.Contains(result.UpcomingObligations, obligation =>
            obligation.Kind == UpcomingObligationKind.Bill && obligation.Description == "Aluguel");
        Assert.Contains(result.UpcomingObligations, obligation =>
            obligation.Kind == UpcomingObligationKind.CreditCardStatement && obligation.Amount == 300m);
    }
}
